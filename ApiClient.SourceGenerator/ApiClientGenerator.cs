using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace ApiClient.SourceGenerator;

[Generator]
public sealed partial class ApiClientGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		System.Diagnostics.Debugger.Launch();

		IncrementalValueProvider<ProjectSettings> options = context.AnalyzerConfigOptionsProvider
			   .Select((optionsProvider, _) =>
			   {
				   ProjectSettings projectSettings = new();

				   if (optionsProvider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var value))
				   {
					   projectSettings.RootNamespace = value;
				   }

				   return projectSettings;
			   });

		IncrementalValuesProvider<ApiClientClassInfo> apiClientClassDeclarations =
		context.SyntaxProvider
			   .CreateSyntaxProvider(
				   predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax classDeclaration && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) && HasAttribute(classDeclaration.AttributeLists, "ApiClientGenerator"),
				   transform: static (syntaxContext, _) => Parser.Parse(syntaxContext.Node as ClassDeclarationSyntax, syntaxContext.SemanticModel))
			   .Where(static classDeclaration => classDeclaration is not null)!;

		context.RegisterSourceOutput(options, static (SourceProductionContext ctx, ProjectSettings projectSetings) =>
		{
			ctx.AddSource("Common", SourceText.From(Encoding.UTF8.GetString(ApiClientTemplatesResources.CommonAttributes).Replace("{{ns}}", projectSetings.RootNamespace!), Encoding.UTF8));
		});

		context.RegisterSourceOutput(apiClientClassDeclarations.Combine(options), static (SourceProductionContext ctx, (ApiClientClassInfo ApiClientClassInfo, ProjectSettings ProjectSettings) data) =>
		{
			//TODO: emiter per core/framework??
			Emitter.EmitSource(ctx, data.ApiClientClassInfo, data.ProjectSettings);
		});

	}

	private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attrName)
	{
		foreach (var attributeList in attributeLists)
		{
			foreach (var attribute in attributeList.Attributes)
			{
				if (attribute.Name.ToString().EndsWith(attrName))
				{
					return true;
				}
			}
		}

		return false;
	}

	internal sealed class ProjectSettings
	{
		internal string? RootNamespace { get; set; }

		//internal bool IsNetCore { get; set; }
	}

	internal sealed class ApiClientClassInfo
	{
		internal string? Namespace { get; set; }

		internal string? ClassName { get; set; }

		internal MethodInfo[]? Methods { get; set; }

		internal string[]? Usings { get; set; }
	}

	internal sealed class MethodInfo
	{
		public string? HttpMethod { get; set; }

		public string? Path { get; set; }

		public MethodParameter[]? Parameters { get; set; }

		public ReturnType? ReturnType { get; set; }

		public string? Name { get; set; }

		public bool ThrowExceptions { get; set; }
	}

	internal enum ParameterType
	{
		None,
		Query,
		Route,
		Body,
		Form,
	}

	internal sealed class MethodParameter
	{
		public string? Name { get; set; }

		public string? Type { get; set; }

		public ParameterType ParameterType { get; set; }
	}

	internal sealed class ReturnType
	{
		public string? Type { get; set; }

		public bool IsGenericReturnType { get; set; }

		public string? GenrecReturnType { get; set; }
	}
}


