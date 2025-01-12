using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

			var apiClientClassInfo = new ApiClientClassInfo()
			{
				Usings = GatheringUsing(classDeclaration),
				Namespace = GetNamespace(classDeclaration),
				ClassName = classDeclaration.Identifier.Text,
				Methods = GatheringMethods(classDeclaration.Members
					.OfType<MethodDeclarationSyntax>()
					.Where(method => method.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword)))
					.ToArray()
				)
			};

			var arguments = classDeclaration.AttributeLists[0].Attributes[0].ArgumentList!.Arguments;
			foreach (var argument in arguments)
			{
				if (argument.NameEquals is null)
				{
					continue;
				}

				switch (argument.NameEquals.Name.ToString())
				{
					case "NetCore":
						if (argument.Expression is LiteralExpressionSyntax netCore && netCore.Token.Value is not null)
						{
							apiClientClassInfo.NetCore = (bool)netCore.Token.Value;
						}

						break;
					case "Serialization":
						apiClientClassInfo.Serialization = (argument.Expression as MemberAccessExpressionSyntax)!.Name.ToString() switch
						{
							"Newtonsoft" => SerializationMode.Newtonsoft,
							"SystemTextJson" => SerializationMode.SystemTextJson,
							"Custom" => SerializationMode.Custom,
							_ => SerializationMode.Newtonsoft
						};

						break;
					case "ConnectionTooLongWarn":
						if (argument.Expression is LiteralExpressionSyntax connectionTooLongWarnInMs && connectionTooLongWarnInMs.Token.Value is not null)
						{
							apiClientClassInfo.ConnectionTooLongWarn = (int)connectionTooLongWarnInMs.Token.Value;
						}
						break;
				}
			}

			return apiClientClassInfo;
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
					methodInfo = new()
					{
						MethodForGenerating = false
					};
				}

				methodInfo.Name = method.Identifier.Text;
				methodInfo.Parameters = GatheringParameters(method.ParameterList.Parameters, methodInfo.Path!, methodInfo.MethodForGenerating);
				methodInfo.ReturnType = GatheringReturnTypeInfo(method.ReturnType);
				if (methodInfo.MethodForGenerating)
				{
					methodInfo.ThrowExceptions = HasAttribute(method.AttributeLists, "ThrowsExceptions");
					var connectionTooLongWarnAttribute = GetAttribute("ConnectionTooLongWarn", method.AttributeLists);
					if (connectionTooLongWarnAttribute != null && connectionTooLongWarnAttribute.ArgumentList!.Arguments[0].Expression is LiteralExpressionSyntax connectionTooLongWarn)
					{
						methodInfo.ConnectionTooLongWarn = (int)connectionTooLongWarn.Token.Value!;
					}

					var attribute = GetAttribute("Serialization", method.AttributeLists);
					if (attribute is not null)
					{
						methodInfo.Serialization = (attribute.ArgumentList!.Arguments[0].Expression as MemberAccessExpressionSyntax)!.Name.ToString() switch
						{
							"Newtonsoft" => SerializationMode.Newtonsoft,
							"SystemTextJson" => SerializationMode.SystemTextJson,
							"Custom" => SerializationMode.Custom,
							_ => SerializationMode.Inherit
						};
					}
				}

				methods.Add(methodInfo);
			}

			return [.. methods];
		}

		private static MethodParameter[]? GatheringParameters(SeparatedSyntaxList<ParameterSyntax> parameters, string path, bool methodForGenerating)
		{
			List<MethodParameter> parameterList = [];

			foreach (var parameter in parameters)
			{
				MethodParameter methodParameter = new()
				{
					Name = parameter.Identifier.Text,
					Type = parameter.Type!.ToString(),
				};

				parameterList.Add(methodParameter);
				if (!methodForGenerating)
				{
					continue;
				}

				var asliasAsAttribute = GetAttribute("AliasAs", parameter.AttributeLists);
				if (asliasAsAttribute is not null && asliasAsAttribute.ArgumentList!.Arguments[0].Expression is LiteralExpressionSyntax literalExpression)
				{
					methodParameter.AliasAs = literalExpression.Token.Value as string;
				}

				var fmtAttribute = GetAttribute("Fmt", parameter.AttributeLists);
				if (fmtAttribute is not null && fmtAttribute.ArgumentList!.Arguments[0].Expression is LiteralExpressionSyntax fmtLiteralExpression)
				{
					methodParameter.Fmt = fmtLiteralExpression.Token.Value as string;
				}

				if (path.IndexOf($"{{{methodParameter.Name}}}") > -1)
				{
					methodParameter.ParameterType = ParameterType.Route;
					continue;
				}

				var bodyAttribute = GetAttribute("Body", parameter.AttributeLists);
				if (bodyAttribute is not null)
				{
					bool formElemenet = bodyAttribute.ArgumentList is not null && bodyAttribute.ArgumentList!.Arguments[0].Expression is LiteralExpressionSyntax netCore && netCore.Token.Value is not null && (bool)netCore.Token.Value;

					methodParameter.ParameterType = formElemenet ? ParameterType.Form : ParameterType.Body;
					continue;
				}

				var headerAttribute = GetAttribute("Header", parameter.AttributeLists);
				if (headerAttribute is not null && headerAttribute.ArgumentList!.Arguments[0].Expression is LiteralExpressionSyntax headerLiteralExpression)
				{
					methodParameter.Header = headerLiteralExpression.Token.Value as string;
					methodParameter.ParameterType = ParameterType.Header;
					continue;
				}

				if (!methodParameter.Type.EndsWith("CancellationToken"))
				{
					methodParameter.ParameterType = ParameterType.Query;
					continue;
				}
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
					IsArray = ((GenericNameSyntax)returnType).TypeArgumentList.Arguments[0] is ArrayTypeSyntax,
					ArrayItemType = ((GenericNameSyntax)returnType).TypeArgumentList.Arguments[0] is ArrayTypeSyntax arrayTypeSyntax ? arrayTypeSyntax.ElementType.ToString() : null,
					GenericReturnType = ((GenericNameSyntax)returnType).TypeArgumentList.Arguments[0].ToString()
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
			string[] httpMethods = ["Get", "Post", "Put", "Delete"];

			foreach (var method in httpMethods)
			{
				var attribute = GetAttribute(method, methodDeclaration.AttributeLists);

				if (attribute == null)
				{
					continue;
				}

				return new MethodInfo
				{
					HttpMethod = method,
					Path = ExtractPath(attribute.ArgumentList?.Arguments ?? []),
					MethodForGenerating = true
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

		private static AttributeSyntax? GetAttribute(string name, SyntaxList<AttributeListSyntax> attributeLists)
		{
			foreach (var attributeList in attributeLists)
			{
				foreach (var attribute in attributeList.Attributes)
				{
					if (attribute.Name.ToString().StartsWith(name, StringComparison.InvariantCultureIgnoreCase))
					{
						return attribute;
					}
				}
			}

			return null;
		}

	}
}


