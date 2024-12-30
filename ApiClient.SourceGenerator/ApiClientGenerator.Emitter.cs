using Microsoft.CodeAnalysis;
using System.Text;

namespace ApiClient.SourceGenerator;

public sealed partial class ApiClientGenerator
{
	internal sealed class Emitter
	{

		internal static void EmitSource(SourceProductionContext ctx, ApiClientClassInfo apiClientClassInfo, ProjectSettings projectSettings)
		{
			SourceWriter sourceWriter = new();
			foreach (var @using in apiClientClassInfo.Usings!)
			{
				sourceWriter.WriteLine($@"using {@using};");
			}
			sourceWriter.AppendLine();

			sourceWriter.WriteLine($@"namespace {apiClientClassInfo.Namespace}");
			sourceWriter.BeginBlock();
			sourceWriter.WriteLine($"public interface I{apiClientClassInfo.ClassName}");
			sourceWriter.BeginBlock();
			WriterInterfaceMethods(sourceWriter, apiClientClassInfo.Methods);
			sourceWriter.EndBlock();
			sourceWriter.AppendLine();
			sourceWriter.WriteLine($"public partial class {apiClientClassInfo.ClassName} : I{apiClientClassInfo.ClassName}");
			sourceWriter.BeginBlock();
			sourceWriter.WriteLine("private readonly HttpClient _httpClient;");
			sourceWriter.AppendLine();
			WriterMethodsBody(sourceWriter, apiClientClassInfo);
			sourceWriter.AppendLine();
			sourceWriter.WriteLine("private partial void LogError(string path, System.Exception ex);");
			sourceWriter.EndBlock();
			sourceWriter.EndBlock();

			string hintName = $"{apiClientClassInfo.ClassName}_Generated.g.cs";
			ctx.AddSource(hintName, sourceWriter.ToSourceText());
		}

		private static void WriterInterfaceMethods(SourceWriter sourceWriter, MethodInfo[]? methods)
		{
			foreach (var method in methods!)
			{
				sourceWriter.WriteLine($"{FormatReturnType(method.ReturnType!)} {method.Name}({FormatParameters(method.Parameters)});");
			}

			static string FormatParameters(MethodParameter[]? parameters)
			{
				string[] @params = parameters
					.Select(x => $"{x.Type} {x.Name}")
					.ToArray();

				return string.Join(",", @params);
			}
		}

		private static void WriterMethodsBody(SourceWriter sourceWriter, ApiClientClassInfo apiClientClassInfo)
		{
			foreach (var method in apiClientClassInfo.Methods!)
			{
				sourceWriter.WriteLine($"public partial async {FormatReturnType(method.ReturnType!)} {method.Name}({FormatParameters(method.Parameters)})");
				sourceWriter.BeginBlock();
				var url = method.Parameters!.Length > 0 ? BuildUrl(method.Path!, method.Parameters) : $"\"{method.Path}\"";
				sourceWriter.WriteLine($"string url = {url};");
				sourceWriter.AppendLine();
				sourceWriter.WriteLine("try");
				sourceWriter.BeginBlock();
				sourceWriter.WriteLine("using (var request = new System.Net.Http.HttpRequestMessage())");
				sourceWriter.BeginBlock();
				sourceWriter.WriteLine($"request.Method = System.Net.Http.HttpMethod.{method.HttpMethod};");
				sourceWriter.WriteLine("request.RequestUri = new System.Uri(url, System.UriKind.RelativeOrAbsolute);");
				GenerateSourceCodeForHeaders(sourceWriter, method);
				//TODO: content form
				//TODO: content body 
				var cancellationParameter = method.Parameters!.FirstOrDefault(x => x.Type!.EndsWith("CancellationToken"));

				sourceWriter.WriteLine(cancellationParameter is not null ?
					$"using (var response = await _httpClient.SendAsync(request, {cancellationParameter.Name}))" :
					 "using (var response = await _httpClient.SendAsync(request))");

				sourceWriter.BeginBlock();

				GenerateSourceCodeForCheckResponse(sourceWriter, method);
				GenerateSourceCodeForResponse(sourceWriter, method, apiClientClassInfo);

				sourceWriter.EndBlock();
				sourceWriter.EndBlock();
				sourceWriter.EndBlock();
				sourceWriter.WriteLine("catch(System.Exception e)");
				sourceWriter.BeginBlock();
				sourceWriter.WriteLine("LogError(url, e);");
				if (method.ThrowExceptions)
				{
					sourceWriter.WriteLine("throw;");
				}

				sourceWriter.EndBlock();
				if (method.ReturnType!.IsGenericReturnType)
				{
					sourceWriter.AppendLine();
					sourceWriter.WriteLine($"return {DefaultReturnType(method)};");
				}

				sourceWriter.EndBlock();
				sourceWriter.AppendLine();
			}

			static string FormatParameters(MethodParameter[]? parameters)
			{
				string[] @params = parameters
					.Select(x => $"{x.Type} {x.Name}")
					.ToArray();

				return string.Join(",", @params);
			}
		}

		private static void GenerateSourceCodeForHeaders(SourceWriter sourceWriter, MethodInfo method)
		{
			var headerParameters = method.Parameters
				.Where(x => x.ParameterType == ParameterType.Header)
				.ToArray();

			foreach (var headerParameter in headerParameters)
			{
				sourceWriter.WriteLine(string.IsNullOrEmpty(headerParameter.Fmt) ?
					$"string[] {headerParameter.Name}_header = $\"{headerParameter.Header} {{{headerParameter.Name}}}\".Split(':');" :
					$"string[] {headerParameter.Name}_header = $\"{headerParameter.Header} {{{headerParameter.Name}.ToString(\"{headerParameter.Fmt}\")}}\".Split(':');");

				sourceWriter.WriteLine($"request.Headers.Add({headerParameter.Name}_header[0], {headerParameter.Name}_header[1]);");
			}
		}

		private static string BuildUrl(string path, MethodParameter[] parameters)
		{
			List<string> paramNames = new(parameters.Length);
			StringBuilder urlBuilder = new(path.Length);
			urlBuilder.Append("$\"");
			urlBuilder.Append(path);

			var queryParameters = parameters.Where(x => x.ParameterType == ParameterType.Query).ToArray();
			if (queryParameters.Length > 0)
			{
				urlBuilder.Append('?');

				for (int i = 0; i < queryParameters.Length; i++)
				{
					var parameter = queryParameters[i];
					urlBuilder.Append(string.IsNullOrEmpty(parameter.AliasAs) ? parameter.Name : parameter.AliasAs);
					urlBuilder.Append("={");
					urlBuilder.Append(parameter.Name);
					if (!string.IsNullOrEmpty(parameter.Fmt))
					{
						urlBuilder.Append(".ToString(\"");
						urlBuilder.Append(parameter.Fmt);
						urlBuilder.Append("\")");
					}

					urlBuilder.Append("}");
					if (i < queryParameters.Length - 1)
					{
						urlBuilder.Append('&');
					}
				}
			}

			urlBuilder.Append("\"");
			return urlBuilder.ToString();
		}

		private static void GenerateSourceCodeForCheckResponse(SourceWriter sourceWriter, MethodInfo method)
		{
			if (method.ThrowExceptions || !method.ReturnType!.IsGenericReturnType)
			{
				sourceWriter.WriteLine("response.EnsureSuccessStatusCode();");
			}
			else
			{
				sourceWriter.WriteLine("if (!response.IsSuccessStatusCode)");
				sourceWriter.BeginBlock();
				sourceWriter.WriteLine(method.ReturnType!.IsGenericReturnType ? $"return {DefaultReturnType(method)};" : "return;");
				sourceWriter.EndBlock();
			}
		}

		private static void GenerateSourceCodeForResponse(SourceWriter sourceWriter, MethodInfo method, ApiClientClassInfo apiClientClassInfo)
		{
			if (!method.ReturnType!.IsGenericReturnType)
			{
				return;
			}

			switch (method.ReturnType!.GenericReturnType!.ToLowerInvariant())
			{
				case "bool":
					sourceWriter.WriteLine("return true;");
					break;
				case "string":
					sourceWriter.WriteLine($"return await response.Content.ReadAsStringAsync({GetCancellationParameter(method, apiClientClassInfo.NetCore)});");
					break;
				case "byte[]":
					sourceWriter.WriteLine($"return await response.Content.ReadAsByteArrayAsync({GetCancellationParameter(method, apiClientClassInfo.NetCore)});");
					break;
				default:
					sourceWriter.WriteLine("if(response.StatusCode == System.Net.HttpStatusCode.OK)");
					sourceWriter.BeginBlock();
					sourceWriter.WriteLine("if (response.Content is not null)");
					sourceWriter.BeginBlock();
					sourceWriter.WriteLine($"var content = await response.Content.ReadAsStringAsync({GetCancellationParameter(method, apiClientClassInfo.NetCore)});");

					sourceWriter.WriteLine("if(!string.IsNullOrEmpty(content))");
					sourceWriter.BeginBlock();
					//sourceWriter.WriteLine($"return System.Text.Json.JsonSerializer.Deserialize<{method.ReturnType.GenericReturnType}>(content);");
					sourceWriter.WriteLine($"return Newtonsoft.Json.JsonConvert.DeserializeObject<{method.ReturnType.GenericReturnType}>(content);");
					sourceWriter.EndBlock();

					sourceWriter.EndBlock();
					sourceWriter.EndBlock();
					break;
			}

			static string GetCancellationParameter(MethodInfo method, bool isNetCore)
			{
				if (isNetCore)
				{
					var cancellationParameter = method.Parameters!.FirstOrDefault(x => x.Type!.EndsWith("CancellationToken"));
					if (cancellationParameter is not null)
					{
						return cancellationParameter.Name!;
					}
				}

				return string.Empty;
			}
		}

		private static string FormatReturnType(ReturnType returnType)
		{
			if (returnType.IsGenericReturnType)
			{
				return $"{returnType.Type}<{returnType.GenericReturnType}>";
			}

			return returnType.Type!;
		}

		private static string DefaultReturnType(MethodInfo method)
		{
			if (!method.ReturnType!.IsGenericReturnType)
			{
				return "default";
			}

			return method.ReturnType!.GenericReturnType!.ToLowerInvariant() switch
			{
				"bool" => "false",
				"string" => "null",
				"byte[]" => "Array.Empty<byte>()",
				_ => method.ReturnType.IsArray ? $"Array.Empty<{method.ReturnType.ArrayItemType}>()" : "default",
			};
		}
	}
}


