﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CSharp;
using static Roslynator.CSharp.SyntaxTriviaAnalysis;

namespace Roslynator.Formatting.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class FixFormattingOfListAnalyzer : BaseDiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(DiagnosticDescriptors.FixFormattingOfList); }
        }

        public override void Initialize(AnalysisContext context)
        {
            base.Initialize(context);

            context.RegisterSyntaxNodeAction(f => AnalyzeParameterList(f), SyntaxKind.ParameterList);
            context.RegisterSyntaxNodeAction(f => AnalyzeBracketedParameterList(f), SyntaxKind.BracketedParameterList);
            context.RegisterSyntaxNodeAction(f => AnalyzeTypeParameterList(f), SyntaxKind.TypeParameterList);

            context.RegisterSyntaxNodeAction(f => AnalyzeArgumentList(f), SyntaxKind.ArgumentList);
            context.RegisterSyntaxNodeAction(f => AnalyzeBracketedArgumentList(f), SyntaxKind.BracketedArgumentList);
            context.RegisterSyntaxNodeAction(f => AnalyzeAttributeArgumentList(f), SyntaxKind.AttributeArgumentList);
            context.RegisterSyntaxNodeAction(f => AnalyzeTypeArgumentList(f), SyntaxKind.TypeArgumentList);

            context.RegisterSyntaxNodeAction(f => AnalyzeAttributeList(f), SyntaxKind.AttributeList);
            context.RegisterSyntaxNodeAction(f => AnalyzeBaseList(f), SyntaxKind.BaseList);
            context.RegisterSyntaxNodeAction(f => AnalyzeTupleType(f), SyntaxKind.TupleType);
            context.RegisterSyntaxNodeAction(f => AnalyzeTupleExpression(f), SyntaxKind.TupleExpression);

            context.RegisterSyntaxNodeAction(
                f => AnalyzeInitializerExpression(f),
                SyntaxKind.ArrayInitializerExpression,
                SyntaxKind.CollectionInitializerExpression,
                SyntaxKind.ComplexElementInitializerExpression,
                SyntaxKind.ObjectInitializerExpression);
        }

        private static void AnalyzeParameterList(SyntaxNodeAnalysisContext context)
        {
            var parameterList = (ParameterListSyntax)context.Node;

            Analyze(context, parameterList.OpenParenToken, parameterList.Parameters);
        }

        private static void AnalyzeBracketedParameterList(SyntaxNodeAnalysisContext context)
        {
            var parameterList = (BracketedParameterListSyntax)context.Node;

            Analyze(context, parameterList.OpenBracketToken, parameterList.Parameters);
        }

        private static void AnalyzeTypeParameterList(SyntaxNodeAnalysisContext context)
        {
            var parameterList = (TypeParameterListSyntax)context.Node;

            Analyze(context, parameterList.LessThanToken, parameterList.Parameters);
        }

        private static void AnalyzeArgumentList(SyntaxNodeAnalysisContext context)
        {
            var argumentList = (ArgumentListSyntax)context.Node;

            Analyze(context, argumentList.OpenParenToken, argumentList.Arguments);
        }

        private static void AnalyzeBracketedArgumentList(SyntaxNodeAnalysisContext context)
        {
            var argumentList = (BracketedArgumentListSyntax)context.Node;

            Analyze(context, argumentList.OpenBracketToken, argumentList.Arguments);
        }

        private static void AnalyzeAttributeArgumentList(SyntaxNodeAnalysisContext context)
        {
            var argumentList = (AttributeArgumentListSyntax)context.Node;

            Analyze(context, argumentList.OpenParenToken, argumentList.Arguments);
        }

        private static void AnalyzeTypeArgumentList(SyntaxNodeAnalysisContext context)
        {
            var argumentList = (TypeArgumentListSyntax)context.Node;

            Analyze(context, argumentList.LessThanToken, argumentList.Arguments);
        }

        private void AnalyzeAttributeList(SyntaxNodeAnalysisContext context)
        {
            var attributeList = (AttributeListSyntax)context.Node;

            Analyze(context, attributeList.OpenBracketToken, attributeList.Attributes);
        }

        private void AnalyzeBaseList(SyntaxNodeAnalysisContext context)
        {
            var baseList = (BaseListSyntax)context.Node;

            Analyze(context, baseList.ColonToken, baseList.Types);
        }

        private void AnalyzeTupleType(SyntaxNodeAnalysisContext context)
        {
            var tupleType = (TupleTypeSyntax)context.Node;

            Analyze(context, tupleType.OpenParenToken, tupleType.Elements);
        }

        private void AnalyzeTupleExpression(SyntaxNodeAnalysisContext context)
        {
            var tupleExpression = (TupleExpressionSyntax)context.Node;

            Analyze(context, tupleExpression.OpenParenToken, tupleExpression.Arguments);
        }

        private void AnalyzeInitializerExpression(SyntaxNodeAnalysisContext context)
        {
            var initializerExpression = (InitializerExpressionSyntax)context.Node;

            Analyze(context, initializerExpression.OpenBraceToken, initializerExpression.Expressions);
        }

        private static void Analyze<TNode>(
            SyntaxNodeAnalysisContext context,
            SyntaxNodeOrToken openNodeOrToken,
            SeparatedSyntaxList<TNode> nodes) where TNode : SyntaxNode
        {
            TNode first = nodes.FirstOrDefault();

            if (first == null)
                return;

            TextSpan span = nodes.GetSpan(includeExteriorTrivia: false);

            if (span.IsSingleLine(first.SyntaxTree))
            {
                SyntaxTriviaList trailing = openNodeOrToken.GetTrailingTrivia();

                if (!IsOptionalWhitespaceThenOptionalSingleLineCommentThenEndOfLineTrivia(trailing))
                    return;

                int indentationLength = GetIncreasedIndentationLength(openNodeOrToken.Parent);

                if (indentationLength == 0)
                    return;

                SyntaxTriviaList leading = first.GetLeadingTrivia();

                if (leading.Any())
                {
                    SyntaxTrivia last = leading.Last();

                    if (last.IsWhitespaceTrivia()
                        && last.Span.Length == indentationLength)
                    {
                        return;
                    }
                }

                ReportDiagnostic();
            }
            else
            {
                TextLineCollection lines = null;

                IndentationAnalysis indentationAnalysis = IndentationAnalysis.Create(openNodeOrToken.Parent);

                int indentationLength = indentationAnalysis.IncreasedIndentationLength;

                if (indentationLength == 0)
                    return;

                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    SyntaxTriviaList trailing = (i == 0)
                        ? openNodeOrToken.GetTrailingTrivia()
                        : nodes.GetSeparator(i - 1).TrailingTrivia;

                    if (IsOptionalWhitespaceThenOptionalSingleLineCommentThenEndOfLineTrivia(trailing))
                    {
                        SyntaxTriviaList leading = nodes[i].GetLeadingTrivia();

                        if (!leading.Any())
                        {
                            ReportDiagnostic();
                            break;
                        }
                        else
                        {
                            SyntaxTrivia last = leading.Last();

                            if (!last.IsWhitespaceTrivia()
                                || indentationLength != last.Span.Length)
                            {
                                ReportDiagnostic();
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (nodes.Count == 1
                            && first.IsKind(SyntaxKind.Argument))
                        {
                            var argument = (ArgumentSyntax)(SyntaxNode)first;

                            if (ShouldDecreaseIndentation(argument, lines ??= first.SyntaxTree.GetText().Lines))
                                indentationLength = indentationAnalysis.IndentationLength;
                        }

                        if (nodes.Count > 1
                            && (i > 0
                                || !context.Node.IsKind(SyntaxKind.AttributeList)))
                        {
                            ReportDiagnostic();
                            break;
                        }

                        if (lines == null)
                            lines = first.SyntaxTree.GetText().Lines;

                        int lineIndex = lines.IndexOf(span.Start);
                        if (lineIndex < lines.Count - 1)
                        {
                            int lineStartIndex = lines[lineIndex + 1].Start;

                            if (first.Span.Contains(lineStartIndex))
                            {
                                SyntaxToken token = first.FindToken(lineStartIndex);

                                if (!token.IsKind(SyntaxKind.None))
                                {
                                    SyntaxTriviaList leading = token.LeadingTrivia;

                                    if (leading.Any())
                                    {
                                        if (leading.FullSpan.Contains(lineStartIndex))
                                        {
                                            SyntaxTrivia trivia = leading.Last();

                                            if (trivia.IsWhitespaceTrivia()
                                                && trivia.SpanStart == lineStartIndex
                                                && trivia.Span.Length != indentationLength)
                                            {
                                                ReportDiagnostic();
                                                break;
                                            }
                                        }
                                    }
                                    else if (lineStartIndex == token.SpanStart)
                                    {
                                        ReportDiagnostic();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            void ReportDiagnostic()
            {
                DiagnosticHelpers.ReportDiagnostic(
                    context,
                    DiagnosticDescriptors.FixFormattingOfList,
                    Location.Create(first.SyntaxTree, nodes.Span),
                    GetTitle());
            }

            string GetTitle()
            {
                switch (context.Node.Kind())
                {
                    case SyntaxKind.ParameterList:
                    case SyntaxKind.BracketedParameterList:
                    case SyntaxKind.TypeParameterList:
                        return "parameters";
                    case SyntaxKind.ArgumentList:
                    case SyntaxKind.BracketedArgumentList:
                    case SyntaxKind.AttributeArgumentList:
                    case SyntaxKind.TypeArgumentList:
                        return "arguments";
                    case SyntaxKind.AttributeList:
                        return "attributes";
                    case SyntaxKind.BaseList:
                        return "base types";
                    case SyntaxKind.TupleType:
                    case SyntaxKind.TupleExpression:
                        return "a tuple";
                    case SyntaxKind.ArrayInitializerExpression:
                    case SyntaxKind.CollectionInitializerExpression:
                    case SyntaxKind.ComplexElementInitializerExpression:
                    case SyntaxKind.ObjectInitializerExpression:
                        return "an initializer";
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public static bool ShouldDecreaseIndentation(ArgumentSyntax argument, TextLineCollection lines)
        {
            TextLine line = lines.GetLineFromPosition(argument.Span.Start);
            int position = line.EndIncludingLineBreak;
            SyntaxToken token = argument.FindToken(position);

            if (token.IsKind(SyntaxKind.OpenBraceToken)
                && token.IsParentKind(SyntaxKind.Block)
                && CSharpFacts.IsAnonymousFunctionExpression(token.Parent.Parent.Kind()))
            {
                SyntaxTriviaList leading = token.LeadingTrivia;

                if ((leading.Any() && leading.Span.Contains(position))
                    || (!leading.Any() && token.SpanStart == position))
                {
                    SyntaxNode block = token.Parent;

                    position = lines.GetLineFromPosition(argument.Span.End).Start;

                    token = argument.FindToken(position);

                    if (token.IsKind(SyntaxKind.CloseBraceToken)
                        && token.Parent.IsKind(SyntaxKind.Block)
                        && CSharpFacts.IsAnonymousFunctionExpression(token.Parent.Parent.Kind())
                        && object.ReferenceEquals(block, token.Parent))
                    {
                        leading = token.LeadingTrivia;

                        if ((leading.Any() && leading.Span.Contains(position))
                            || (!leading.Any() && token.SpanStart == position))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
