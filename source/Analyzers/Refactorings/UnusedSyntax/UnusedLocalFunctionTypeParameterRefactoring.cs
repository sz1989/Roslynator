﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Extensions;

namespace Roslynator.CSharp.Refactorings.UnusedSyntax
{
    internal class UnusedLocalFunctionTypeParameterRefactoring : UnusedSyntaxRefactoring<LocalFunctionStatementSyntax, TypeParameterListSyntax, TypeParameterSyntax, ITypeParameterSymbol>
    {
        protected override CSharpSyntaxNode GetBody(LocalFunctionStatementSyntax node)
        {
            return node.BodyOrExpressionBody();
        }

        protected override string GetIdentifier(TypeParameterSyntax syntax)
        {
            return syntax.Identifier.ValueText;
        }

        protected override TypeParameterListSyntax GetList(LocalFunctionStatementSyntax node)
        {
            return node.TypeParameterList;
        }

        protected override SyntaxTokenList GetModifiers(LocalFunctionStatementSyntax node)
        {
            return node.Modifiers;
        }

        protected override SeparatedSyntaxList<TypeParameterSyntax> GetSeparatedList(TypeParameterListSyntax list)
        {
            return list.Parameters;
        }
    }
}
