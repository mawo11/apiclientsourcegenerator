using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ApiClient.SourceGenerator;

public sealed partial class ApiClientGenerator
{
	internal sealed class Parser
	{
		internal static ApiClientClassInfo? Parse(ClassDeclarationSyntax? classDeclaration, SemanticModel semanticModel)
		{
			if (classDeclaration == null)
			{
				return null;
			}

			return new ApiClientClassInfo()
			{
				Usings = GatheringUsing(classDeclaration),
				Namespace = GetNamespace(classDeclaration),
				ClassName = classDeclaration.Identifier.Text,
				Methods = GatheringMethods(classDeclaration.Members.OfType<MethodDeclarationSyntax>()),
			};
		}

		private static string[] GatheringUsing(ClassDeclarationSyntax classDeclaration)
		{
			List<string> usings = [];

			var parent = classDeclaration.Parent;
			while (parent is not null && parent.Parent is not null)
			{
				parent = parent.Parent;
			}

			if (parent is CompilationUnitSyntax compilationUnit)
			{
				foreach (var usingDirective in compilationUnit.Usings)
				{
					usings.Add(usingDirective.Name!.ToString());
				}
			}

			return [.. usings];
		}

		private static MethodInfo[] GatheringMethods(IEnumerable<MethodDeclarationSyntax> sourceMethodDeclarations)
		{
			List<MethodInfo> methods = new(sourceMethodDeclarations.Count());
			foreach (var method in sourceMethodDeclarations)
			{
				var methodInfo = CheckForApiMethod(method);
				if (methodInfo == null)
				{
					continue;
				}

				methodInfo.Name = method.Identifier.Text;
				methodInfo.Parameters = GatheringParameters(method.ParameterList.Parameters);
				methodInfo.ReturnType = GatheringReturnTypeInfo(method.ReturnType);
				//TOOD: exception handling 
				//TODO: logging 
				//TOOD: fallback return type
				methods.Add(methodInfo);
			}

			return [.. methods];
		}

		private static MethodParameter[]? GatheringParameters(SeparatedSyntaxList<ParameterSyntax> parameters)
		{
			List<MethodParameter> parameterList = [];
			foreach (var parameter in parameters)
			{
				MethodParameter method = new()
				{
					Name = parameter.Identifier.Text,
					Type = parameter.Type!.ToString()
				};

				parameterList.Add(method);
			}

			return [.. parameterList];
		}

		private static ReturnType? GatheringReturnTypeInfo(TypeSyntax returnType)
		{
			return returnType switch
			{
				GenericNameSyntax => new ReturnType
				{
					IsGenericReturnType = true,
					Type = ((GenericNameSyntax)returnType).Identifier.ValueText,
					GenrecReturnType = ((GenericNameSyntax)returnType).TypeArgumentList.Arguments[0].ToString()
				},
				IdentifierNameSyntax => new ReturnType
				{
					IsGenericReturnType = false,
					Type = ((IdentifierNameSyntax)returnType).Identifier.ValueText
				},
				_ => null
			};
		}

		private static string GetNamespace(SyntaxNode classDeclaration)
		{
			SyntaxNode? current = classDeclaration;
			while (current is not null)
			{
				if (current is NamespaceDeclarationSyntax namespaceDeclaration)
				{
					return namespaceDeclaration.Name.ToString();
				}
				else if (current is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclaration)
				{
					return fileScopedNamespaceDeclaration.Name.ToString();
				}
				current = current.Parent;
			}
			return "";
		}

		private static MethodInfo? CheckForApiMethod(MethodDeclarationSyntax methodDeclaration)
		{
			(string Name, HttpMethod HttpMethod)[] methods = [
				("Get", HttpMethod.Get),
				("Post", HttpMethod.Post),
				("Put", HttpMethod.Put),
				("Delete", HttpMethod.Delete)
				];

			foreach (var method in methods)
			{
				if (GetAttribute(methodDeclaration.AttributeLists.FirstOrDefault()?.Attributes ?? [], method.Name, method.HttpMethod, out MethodInfo? methodInfo))
				{
					return methodInfo;
				}
			}

			return null;
		}

		private static bool GetAttribute(SeparatedSyntaxList<AttributeSyntax> attributes, string attributeName, HttpMethod httpMethod, out MethodInfo? methodInfo)
		{
			foreach (var attribute in attributes)
			{
				if (attribute.Name.ToString().StartsWith(attributeName))
				{
					methodInfo = new MethodInfo
					{
						HttpMethod = httpMethod,
						Path = ExtractPath(attribute.ArgumentList?.Arguments ?? [])
					};

					return true;
				}
			}

			methodInfo = null;
			return false;

			static string? ExtractPath(SeparatedSyntaxList<AttributeArgumentSyntax> arguments)
			{
				if (arguments.Count == 0)
				{
					return null;
				}

				return (arguments[0].Expression as LiteralExpressionSyntax)?.Token.ValueText;
			}
		}
	}
}


