﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp;
using RefactoringEssentials;

namespace MonoDevelop.CSharp.Diagnostics.RemoveUnnecessaryCast
{
	internal abstract class RemoveUnnecessaryCastDiagnosticAnalyzerBase<TLanguageKindEnum> : DiagnosticAnalyzer where TLanguageKindEnum : struct
	{
		private static string s_localizableTitle = GettextCatalog.GetString ("Remove Unnecessary Cast");
		private static string s_localizableMessage = GettextCatalog.GetString ("Cast is redundant.");

		private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId,
			s_localizableTitle,
			s_localizableMessage,
			DiagnosticAnalyzerCategories.RedundanciesInCode,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			customTags: DiagnosticCustomTags.Unnecessary);

		#region Interface methods

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		{
			get
			{
				return ImmutableArray.Create(s_descriptor);
			}
		}

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) =>
				{
					Diagnostic diagnostic;
					if (TryRemoveCastExpression(nodeContext.SemanticModel, nodeContext.Node, out diagnostic, nodeContext.CancellationToken))
					{
						nodeContext.ReportDiagnostic(diagnostic);
					}
				},
				this.SyntaxKindsOfInterest.ToArray());
		}

		public abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }

		#endregion

		protected abstract bool IsUnnecessaryCast(SemanticModel model, SyntaxNode node, CancellationToken cancellationToken);
		protected abstract TextSpan GetDiagnosticSpan(SyntaxNode node);

		private bool TryRemoveCastExpression(
			SemanticModel model, SyntaxNode node, out Diagnostic diagnostic, CancellationToken cancellationToken)
		{
			diagnostic = default(Diagnostic);
			if (model.IsFromGeneratedCode (cancellationToken))
				return false;
			if (!IsUnnecessaryCast(model, node, cancellationToken))
			{
				return false;
			}

			var tree = model.SyntaxTree;
			var span = GetDiagnosticSpan(node);
			if (tree.OverlapsHiddenPosition(span, cancellationToken))
			{
				return false;
			}

			diagnostic = Diagnostic.Create(s_descriptor, tree.GetLocation(span));
			return true;
		}
	}
}
