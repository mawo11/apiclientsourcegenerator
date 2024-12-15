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
				methodInfo.ThrowExceptions = HasAttribute(method.AttributeLists, "ThrowsExceptions");
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
			SeparatedSyntaxList<AttributeSyntax>? attributes = methodDeclaration.AttributeLists.FirstOrDefault()?.Attributes;

			if (attributes == null)
			{
				return null;
			}

			string[] httpMethods = ["Get", "Post", "Put", "Delete"];

			foreach (var method in httpMethods)
			{
				var attribute = attributes.Value.FirstOrDefault(x => x.Name.ToString().StartsWith(method, StringComparison.InvariantCultureIgnoreCase));

				if (attribute == null)
				{
					continue;
				}

				return new MethodInfo
				{
					HttpMethod = method,
					Path = ExtractPath(attribute.ArgumentList?.Arguments ?? [])
				};
			}

			return null;

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


