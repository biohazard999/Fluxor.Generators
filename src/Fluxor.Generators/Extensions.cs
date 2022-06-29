using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fluxor.Generators;

internal static class Extensions
{
    public static bool HasModifier(this TypeDeclarationSyntax @class, SyntaxKind kind)
        => @class == null
            ? false
            : @class.Modifiers.Any(mod => mod.Text == SyntaxFactory.Token(kind).Text);

    public static bool IsAttributeDeclared(this ISymbol symbol, INamedTypeSymbol attributeSymbol)
        => symbol == null ? false : symbol
            .GetAttributes()
            .Any(m =>
                m.AttributeClass is not null
                //We can safely compare with ToString because it represents just the NamedTypeSymbol, not the attributes or overloads
                && m.AttributeClass.ToString() == attributeSymbol.ToString()
             );
}
