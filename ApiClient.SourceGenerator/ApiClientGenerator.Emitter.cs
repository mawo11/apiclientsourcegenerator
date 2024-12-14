using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace ApiClient.SourceGenerator;

public sealed partial class ApiClientGenerator
{
	internal sealed class Emitter
	{

		internal static void EmitSource(SourceProductionContext ctx, ApiClientClassInfo apiClientClassInfo, ProjectSettings projectSettings)
		{
			SoruceWriter sourceWriter = new();
			foreach (var @using in apiClientClassInfo.Usings)
			{
				sourceWriter.WriteLine($@"using {@using};");
			}
			sourceWriter.AppendLine();

			sourceWriter.WriteLine($@"namespace {apiClientClassInfo.Namespace}");
			sourceWriter.WriteLine("{");
			sourceWriter.Indentation++;
			sourceWriter.WriteLine($"public interface I{apiClientClassInfo.ClassName}");
			sourceWriter.WriteLine("{");
			sourceWriter.Indentation++;
			WriterInterfaceMethods(sourceWriter, apiClientClassInfo.Methods);
			sourceWriter.Indentation--;
			sourceWriter.WriteLine("}");
			sourceWriter.AppendLine();
			sourceWriter.WriteLine($"public partial class {apiClientClassInfo.ClassName} : I{apiClientClassInfo.ClassName}");
			sourceWriter.WriteLine("{");
			sourceWriter.Indentation++;
			sourceWriter.WriteLine("private readonly HttpClient _httpClient;");
			sourceWriter.AppendLine();
			WriterMethodsBody(sourceWriter, apiClientClassInfo.Methods);
			sourceWriter.Indentation--;
			sourceWriter.WriteLine("}");
			sourceWriter.Indentation--;
			sourceWriter.WriteLine("}");

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
				sourceWriter.WriteLine("{");
				sourceWriter.Indentation++;
				sourceWriter.WriteLine("throw new NotImplementedException();");
				sourceWriter.Indentation--;
				sourceWriter.WriteLine("}");
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

		private static (string MethodBody, string InterfaceDeclaration) GenerateGetMethod(MethodDeclarationSyntax method)
		{
			StringBuilder methodBodyStringBuilder = new();
			StringBuilder interfaceDeclarationStringBuilder = new();

			string? baseType = null;
			if (method.ReturnType is GenericNameSyntax genericNameSyntax)
			{
				baseType = string.Join(", ", genericNameSyntax.TypeArgumentList.Arguments);
			}
			else
			{
				baseType = method.ReturnType.ToString();
			}

			methodBodyStringBuilder.Append("");
			methodBodyStringBuilder.Append(method.ReturnType);
			methodBodyStringBuilder.Append(" ");
			methodBodyStringBuilder.Append(method.Identifier);
			methodBodyStringBuilder.Append("(");

			interfaceDeclarationStringBuilder.Append(method.ReturnType);
			interfaceDeclarationStringBuilder.Append(" ");
			interfaceDeclarationStringBuilder.Append(method.Identifier);
			interfaceDeclarationStringBuilder.Append("(");
			List<string> parameterList = [];
			foreach (var parameter in method.ParameterList.Parameters)
			{
				string parameterText = $"{parameter.Type} {parameter.Identifier.Text}";
				parameterList.Add(parameterText);
			}

			methodBodyStringBuilder.Append(string.Join(", ", parameterList));
			methodBodyStringBuilder.AppendLine(")");

			interfaceDeclarationStringBuilder.Append(string.Join(", ", parameterList));
			interfaceDeclarationStringBuilder.AppendLine(");");
			methodBodyStringBuilder.AppendLine("{");

			methodBodyStringBuilder.AppendLine($"return /*{baseType}*/ \"\";");
			methodBodyStringBuilder.AppendLine("}");

			return (methodBodyStringBuilder.ToString(), interfaceDeclarationStringBuilder.ToString());
		}
	}
}


