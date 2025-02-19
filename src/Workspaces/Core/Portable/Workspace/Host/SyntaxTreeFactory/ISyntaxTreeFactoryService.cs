﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Factory service for creating syntax trees.
    /// </summary>
    internal interface ISyntaxTreeFactoryService : ILanguageService
    {
        ParseOptions GetDefaultParseOptions();

        ParseOptions GetDefaultParseOptionsWithLatestLanguageVersion();

        ParseOptions TryParsePdbParseOptions(IReadOnlyDictionary<string, string> compilationOptionsMetadata);

        // new tree from root node
        SyntaxTree CreateSyntaxTree(string? filePath, ParseOptions options, Encoding? encoding, SyntaxNode root);

        // new tree from text
        SyntaxTree ParseSyntaxTree(string? filePath, ParseOptions options, SourceText text, CancellationToken cancellationToken);

        bool CanCreateRecoverableTree(SyntaxNode root);

        // new recoverable tree from root node
        SyntaxTree CreateRecoverableTree(ProjectId cacheKey, string? filePath, ParseOptions options, ValueSource<TextAndVersion> text, Encoding? encoding, SyntaxNode root);

        SyntaxNode DeserializeNodeFrom(Stream stream, CancellationToken cancellationToken);
    }
}
