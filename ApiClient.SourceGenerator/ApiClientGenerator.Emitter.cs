using Microsoft.CodeAnalysis;

namespace ApiClient.SourceGenerator;

public sealed partial class ApiClientGenerator
{
	internal sealed class Emitter
	{

		internal static void EmitSource(SourceProductionContext ctx, ApiClientClassInfo apiClientClassInfo, ProjectSettings projectSettings)
		{
			SoruceWriter sourceWriter = new();
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
			WriterMethodsBody(sourceWriter, apiClientClassInfo.Methods);
			sourceWriter.AppendLine();
			sourceWriter.WriteLine("private partial void LogError(string path, System.Exception ex);");
			sourceWriter.EndBlock();
			sourceWriter.EndBlock();

			string hintName = $"{apiClientClassInfo.ClassName}_Generated.g.cs";
			ctx.AddSource(hintName, sourceWriter.ToSourceText());
		}

		private static void WriterInterfaceMethods(SoruceWriter sourceWriter, MethodInfo[]? methods)
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

		private static void WriterMethodsBody(SoruceWriter sourceWriter, MethodInfo[]? methods)
		{
			foreach (var method in methods!)
			{
				sourceWriter.WriteLine($"public partial async {FormatReturnType(method.ReturnType!)} {method.Name}({FormatParameters(method.Parameters)})");
				sourceWriter.BeginBlock();
				sourceWriter.WriteLine("try");
				sourceWriter.BeginBlock();
				sourceWriter.WriteLine("using (var request = new System.Net.Http.HttpRequestMessage())");
				sourceWriter.BeginBlock();
				sourceWriter.WriteLine($"request.Method = System.Net.Http.HttpMethod.{method.HttpMethod};");
				sourceWriter.WriteLine($"request.RequestUri = new System.Uri(\"{method.Path}\", System.UriKind.RelativeOrAbsolute);");
				//TOOD: headers
				//TODO: content form
				//TODO: content body 
				var cancellationParameter = method.Parameters!.FirstOrDefault(x => x.Type!.EndsWith("CancellationToken"));
				sourceWriter.WriteLine(cancellationParameter is not null ?
					$"using (var response = await _httpClient.SendAsync(request, {cancellationParameter.Name}))" :
					 "using (var response = await _httpClient.SendAsync(request))");

				sourceWriter.BeginBlock();
				if (method.ThrowExceptions || !method.ReturnType!.IsGenericReturnType)
				{
					sourceWriter.WriteLine("response.EnsureSuccessStatusCode();");
				}
				else
				{
					sourceWriter.WriteLine("if (!response.IsSuccessStatusCode)");
					sourceWriter.BeginBlock();
					sourceWriter.WriteLine(method.ReturnType!.IsGenericReturnType ? "return default;" : "return;");
					sourceWriter.EndBlock();
				}


				if (method.ReturnType!.IsGenericReturnType)
				{
					switch (method.ReturnType!.GenrecReturnType!.ToLowerInvariant())
					{
						case "bool":
							sourceWriter.WriteLine("return true;");
							break;
						case "string":
							sourceWriter.WriteLine("return await response.Content.ReadAsStringAsync();");
							break;
						default:
							sourceWriter.WriteLine("if (response is not null)");
							sourceWriter.BeginBlock();
							//if (cancellationParameter is not null)
							//{
							//	sourceWriter.WriteLine($"var content = await response.Content.ReadAsStringAsync({cancellationParameter.Name});");
							//}
							//else
							//{
							sourceWriter.WriteLine("var content = await response.Content.ReadAsStringAsync();");

							//}
							//TODO: seralizacja
							sourceWriter.EndBlock();
							break;
					}
				}

				sourceWriter.EndBlock();
				sourceWriter.EndBlock();
				sourceWriter.EndBlock();
				sourceWriter.WriteLine("catch(System.Exception e)");
				sourceWriter.BeginBlock();
				sourceWriter.WriteLine($"LogError(\"{method.Path}\", e);");
				if (method.ThrowExceptions)
				{
					sourceWriter.WriteLine("throw;");
				}
				sourceWriter.EndBlock();
				if (method.ReturnType!.IsGenericReturnType)
				{
					sourceWriter.AppendLine();
					//TODO: fallback
					sourceWriter.WriteLine("return default;");
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

		private static string FormatReturnType(ReturnType returnType)
		{
			if (returnType.IsGenericReturnType)
			{
				return $"{returnType.Type}<{returnType.GenrecReturnType}>";
			}

			return returnType.Type!;
		}
	}
}


