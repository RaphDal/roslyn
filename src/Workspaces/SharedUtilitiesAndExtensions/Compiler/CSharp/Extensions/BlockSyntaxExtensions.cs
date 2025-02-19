﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class BlockSyntaxExtensions
    {
        public static bool TryConvertToExpressionBody(
            this BlockSyntax? block,
            LanguageVersion languageVersion,
            ExpressionBodyPreference preference,
            [NotNullWhen(true)] out ExpressionSyntax? expression,
            out SyntaxToken semicolonToken)
        {
            if (preference != ExpressionBodyPreference.Never &&
                block != null && block.Statements.Count == 1)
            {
                var firstStatement = block.Statements[0];

                if (TryGetExpression(firstStatement, languageVersion, out expression, out semicolonToken) &&
                    MatchesPreference(expression, preference))
                {
                    // The close brace of the block may have important trivia on it (like 
                    // comments or directives).  Preserve them on the semicolon when we
                    // convert to an expression body.
                    semicolonToken = semicolonToken.WithAppendedTrailingTrivia(
                        block.CloseBraceToken.LeadingTrivia.Where(t => !t.IsWhitespaceOrEndOfLine()));
                    return true;
                }
            }

            expression = null;
            semicolonToken = default;
            return false;
        }

        public static bool TryConvertToArrowExpressionBody(
            this BlockSyntax block,
            SyntaxKind declarationKind,
            LanguageVersion languageVersion,
            ExpressionBodyPreference preference,
            [NotNullWhen(true)] out ArrowExpressionClauseSyntax? arrowExpression,
            out SyntaxToken semicolonToken)
        {
            // We can always use arrow-expression bodies in C# 7 or above.
            // We can also use them in C# 6, but only a select set of member kinds.
            var acceptableVersion =
                languageVersion >= LanguageVersion.CSharp7 ||
                (languageVersion >= LanguageVersion.CSharp6 && IsSupportedInCSharp6(declarationKind));

            if (acceptableVersion &&
                block.TryConvertToExpressionBody(languageVersion, preference, out var expression, out semicolonToken))
            {
                arrowExpression = SyntaxFactory.ArrowExpressionClause(expression);
                return true;
            }

            arrowExpression = null;
            semicolonToken = default;
            return false;
        }

        private static bool IsSupportedInCSharp6(SyntaxKind declarationKind)
        {
            switch (declarationKind)
            {
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                    return false;
            }

            return true;
        }

        public static bool MatchesPreference(
            ExpressionSyntax expression, ExpressionBodyPreference preference)
        {
            if (preference == ExpressionBodyPreference.WhenPossible)
            {
                return true;
            }

            Contract.ThrowIfFalse(preference == ExpressionBodyPreference.WhenOnSingleLine);
            return CSharpSyntaxFacts.Instance.IsOnSingleLine(expression, fullSpan: false);
        }

        private static bool TryGetExpression(StatementSyntax firstStatement, LanguageVersion languageVersion, [NotNullWhen(true)] out ExpressionSyntax? expression, out SyntaxToken semicolonToken)
        {
            if (firstStatement is ExpressionStatementSyntax exprStatement)
            {
                expression = exprStatement.Expression;
                semicolonToken = exprStatement.SemicolonToken;
                return true;
            }
            else if (firstStatement is ReturnStatementSyntax returnStatement)
            {
                if (returnStatement.Expression != null)
                {
                    // If there are any comments or directives on the return keyword, move them to
                    // the expression.
                    expression = firstStatement.GetLeadingTrivia().Any(t => t.IsDirective || t.IsSingleOrMultiLineComment())
                        ? returnStatement.Expression.WithLeadingTrivia(returnStatement.GetLeadingTrivia())
                        : returnStatement.Expression;
                    semicolonToken = returnStatement.SemicolonToken;
                    return true;
                }
            }
            else if (firstStatement is ThrowStatementSyntax throwStatement)
            {
                if (languageVersion >= LanguageVersion.CSharp7 && throwStatement.Expression != null)
                {
                    expression = SyntaxFactory.ThrowExpression(throwStatement.ThrowKeyword, throwStatement.Expression);
                    semicolonToken = throwStatement.SemicolonToken;
                    return true;
                }
            }

            expression = null;
            semicolonToken = default;
            return false;
        }
    }
}
