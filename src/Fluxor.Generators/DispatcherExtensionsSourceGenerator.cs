﻿using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fluxor.Generators;

[Generator]
public class DispatcherExtensionsSourceGenerator : ISourceGenerator
{

    public void Initialize(GeneratorInitializationContext context)
        => context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

    internal class SyntaxReceiver : ISyntaxContextReceiver
    {
        public List<TypeDeclarationSyntax> Types { get; } = new();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is TypeDeclarationSyntax typeDeclarationSyntax)
            {
                if (typeDeclarationSyntax.AttributeLists.Count > 0)
                {
                    var classSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax);

                    if (classSymbol is not null)
                    {
                        Types.Add(typeDeclarationSyntax);
                    }
                }
            }
        }
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var addedFiles = new List<string>();
        var compilation = context.Compilation;

        if (context.SyntaxContextReceiver is not SyntaxReceiver syntaxReceiver)
        {
            return;
        }

        var dispatchableAttribute = compilation.GetTypeByMetadataName("Fluxor.DispatchableAttribute");

        if (dispatchableAttribute is null)
        {
            compilation = AddFile(
                context,
                compilation,
                addedFiles,
                CreateAttribute(CurlyIndenter.Create()).ToString(),
                "DispatchableAttribute"
            );
        }

        dispatchableAttribute = compilation.GetTypeByMetadataName("Fluxor.DispatchableAttribute");
        if (dispatchableAttribute is null)
        {
            return;
        }

        var enumType = compilation.GetTypeByMetadataName("System.Enum");

        var dispatchables = new List<(TypeDeclarationSyntax type, INamedTypeSymbol symbol)>();

        foreach (var type in syntaxReceiver.Types)
        {
            var semanticModel = compilation.GetSemanticModel(type.SyntaxTree);

            var symbol = semanticModel.GetDeclaredSymbol(type);

            if (symbol is null)
            {
                continue;
            }

            if (symbol is INamedTypeSymbol typeSymbol)
            {
                if (!typeSymbol.IsRecord)
                {
                    continue;
                }
                if (typeSymbol.IsAttributeDeclared(dispatchableAttribute))
                {
                    dispatchables.Add((type, symbol));
                }
            }
        }

        var builder = CurlyIndenter.Create();

        builder.WriteLine("using System;");
        builder.WriteLine();
        builder.WriteLine("using Fluxor;");
        builder.WriteLine();
        builder.WriteLine("#nullable enable");
        builder.WriteLine();

        var assemblyName = context.Compilation.Assembly.Name.Replace(".", "_");

        using (builder.OpenBrace("namespace Fluxor"))
        using (builder.OpenBrace($"public static class {assemblyName}_DispatcherExtensions"))
        {
            foreach (var (type, symbol) in dispatchables)
            {
                var ctors = symbol.Constructors
                    .Where(ctor => ctor.DeclaredAccessibility == Accessibility.Public)
                    .ToArray();

                foreach (var ctor in ctors)
                {
                    var sb = new StringBuilder();
                    foreach (var parameters in ctor.Parameters)
                    {
                        sb.Append(", ");
                        sb.Append(parameters.Type);
                        sb.Append(" ");
                        sb.Append(parameters.Name);

                        if (parameters.HasExplicitDefaultValue)
                        {
                            sb.Append(" = ");
                            if (parameters.ExplicitDefaultValue is string)
                            {
                                sb.Append($"\"{parameters.ExplicitDefaultValue}\"");
                            }
                            else if (parameters.Type is INamedTypeSymbol nullAble && nullAble.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
                            {
                                var underlyingType = nullAble.TypeArguments[0];

                                if (underlyingType.SpecialType == SpecialType.None)
                                {
                                    var enumVal = underlyingType.GetMembers().OfType<IFieldSymbol>().First(f => f.ConstantValue?.Equals(parameters.ExplicitDefaultValue) ?? false).ToString();
                                    if (parameters.ExplicitDefaultValue is null)
                                    {
                                        sb.Append("null");
                                    }
                                    else
                                    {
                                        sb.Append(enumVal);
                                    }
                                }
                                else
                                {
                                    sb.Append(parameters.ExplicitDefaultValue?.ToString() ?? "null");
                                }
                            }
                            else if (parameters.Type.BaseType is not null
                                && parameters.Type.BaseType.Equals(enumType, SymbolEqualityComparer.IncludeNullability))
                            {
                                if (parameters.Type is INamedTypeSymbol enumParameter)
                                {
                                    var enumVal = enumParameter.GetMembers().OfType<IFieldSymbol>().First(f => f.ConstantValue?.Equals(parameters.ExplicitDefaultValue) ?? false).ToString();

                                    sb.Append(enumVal);
                                }
                            }
                            else
                            {
                                if (parameters.ExplicitDefaultValue is null)
                                {
                                    sb.Append("null");
                                }
                                else
                                {
                                    sb.Append(parameters.ExplicitDefaultValue.ToString());
                                }
                            }
                        }
                    }

                    using (builder.OpenBrace($"public static void Dispatch{symbol.Name}(this IDispatcher dispatcher{sb})"))
                    {
                        var arguments = string.Join(", ", ctor.Parameters.Select(param => param.Name));
                        builder.WriteLine($"dispatcher.Dispatch(new {symbol.ContainingNamespace}.{symbol.Name}({arguments}));");
                    }

                    builder.WriteLine();
                }
            }
        }

        compilation = AddFile(
            context,
            compilation,
            addedFiles,
            builder.ToString(),
            "DispatcherExtensions"
        );
    }

    private static Compilation AddFile(GeneratorExecutionContext context, Compilation compilation, IList<string> addedSourceFiles, string syntax, string fileName)
    {
        fileName = $"{fileName}.g.cs";

        if (!addedSourceFiles.Contains(fileName))
        {
            addedSourceFiles.Add(fileName);

            var source = SourceText.From(syntax, Encoding.UTF8);

            context.AddSource(fileName, source);
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(syntax, (CSharpParseOptions)context.ParseOptions, cancellationToken: context.CancellationToken);

        return compilation.AddSyntaxTrees(syntaxTree);
    }

    internal CurlyIndenter CreateAttribute(CurlyIndenter syntaxWriter, string visibility = "internal")
    {
        _ = syntaxWriter ?? throw new ArgumentNullException(nameof(syntaxWriter));

        syntaxWriter.WriteLine($"using System;");
        syntaxWriter.WriteLine($"using System.Runtime.CompilerServices;");
        syntaxWriter.WriteLine();

        using (syntaxWriter.OpenBrace($"namespace Fluxor"))
        {
            syntaxWriter.WriteLine("[CompilerGenerated]");
            syntaxWriter.WriteLine("[AttributeUsage(AttributeTargets.Class, Inherited = false)]");
            using (syntaxWriter.OpenBrace($"{visibility} sealed class DispatchableAttribute : Attribute"))
            {
                syntaxWriter.WriteLine($"public DispatchableAttribute() {{ }}");
            }
        }

        return syntaxWriter;
    }
}
