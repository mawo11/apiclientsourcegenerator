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
		//System.Diagnostics.Debugger.Launch();

		IncrementalValueProvider<string?> options = context.AnalyzerConfigOptionsProvider
			   .Select((optionsProvider, _) =>
			   {
				   if (optionsProvider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var value))
				   {
					   return value;
				   }

				   return null;
			   });

		IncrementalValueProvider<string?> compilationProvider = context.CompilationProvider
		   .Select((compilationProvider, _) =>
		   {
			   return compilationProvider.GlobalNamespace.GetNamespaceMembers().FirstOrDefault()?.ToString();
		   });
		IncrementalValuesProvider<ApiClientClassInfo> apiClientClassDeclarations =
		context.SyntaxProvider
			   .CreateSyntaxProvider(
				   predicate: static (syntaxNode, _) => syntaxNode is ClassDeclarationSyntax classDeclaration && classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) && HasAttribute(classDeclaration.AttributeLists, "ApiClientGenerator"),
				   transform: static (syntaxContext, _) => Parser.Parse(syntaxContext.Node as ClassDeclarationSyntax, syntaxContext.SemanticModel))
			   .Where(static classDeclaration => classDeclaration is not null)!;

		context.RegisterSourceOutput(options.Combine(compilationProvider), static (SourceProductionContext ctx, (string? RootNamespace, string? GlobalNamespace) data) =>
		{
			ctx.AddSource("Common", SourceText.From(Encoding.UTF8.GetString(ApiClientTemplatesResources.CommonAttributes).Replace("{{ns}}", data.RootNamespace ?? data.GlobalNamespace ?? "ApiClient.Generated"), Encoding.UTF8));
		});

		context.RegisterSourceOutput(apiClientClassDeclarations
			.Combine(options)
			.Combine(compilationProvider), static (SourceProductionContext ctx, ((ApiClientClassInfo ClassInfo, string? RootNamespace) ApiClientClassInfo, string? GlobalNamespace) Data) =>
		{
			Data.ApiClientClassInfo.ClassInfo.CommonNamespace = Data.ApiClientClassInfo.RootNamespace ?? Data.GlobalNamespace ?? "ApiClient.Generated";
			Emitter.EmitSource(ctx, Data.ApiClientClassInfo.ClassInfo);
		});
	}

	private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attrName)
	{
		foreach (var attributeList in attributeLists)
		{
			foreach (var attribute in attributeList.Attributes)
			{
				if (attribute.Name.ToString().StartsWith(attrName))
				{
					return true;
				}
			}
		}

		return false;
	}

	internal enum SerializationMode
	{
		Inherit,
		Newtonsoft,
		SystemTextJson,
		Custom
	}

	internal sealed class ApiClientClassInfo
	{
		internal string? CommonNamespace { get; set; }

		internal string? Namespace { get; set; }

		internal string? ClassName { get; set; }

		internal MethodInfo[]? Methods { get; set; }

		internal string[]? Usings { get; set; }

		internal bool NetCore { get; set; } = false;

		internal SerializationMode Serialization { get; set; } = SerializationMode.Newtonsoft;

		internal int ConnectionTooLongWarnInMs { get; set; } = 0;
	}

	internal sealed class MethodInfo
	{
		internal string? HttpMethod { get; set; }

		internal string? Path { get; set; }

		internal MethodParameter[]? Parameters { get; set; }

		internal ReturnType? ReturnType { get; set; }

		internal string? Name { get; set; }

		internal bool ThrowExceptions { get; set; }

		internal SerializationMode Serialization { get; set; } = SerializationMode.Inherit;

		internal string? CustomSerializationMethodDeclaration { get; set; }

		internal string? CustomDeserializationMethodDeclaration { get; set; }
	}

	internal enum ParameterType
	{
		None,
		Query,
		Route,
		Body,
		Form,
		Header,
	}

	internal sealed class MethodParameter
	{
		public string? Name { get; set; }

		public string? Type { get; set; }

		public ParameterType ParameterType { get; set; }

		public string? AliasAs { get; set; }

		public string? Fmt { get; set; }

		public string? Header { get; set; }
	}

	internal sealed class ReturnType
	{
		public string? Type { get; set; }

		public bool IsGenericReturnType { get; set; }

		public string? GenericReturnType { get; set; }

		public bool IsArray { get; set; }

		public string? ArrayItemType { get; set; }
	}

}


