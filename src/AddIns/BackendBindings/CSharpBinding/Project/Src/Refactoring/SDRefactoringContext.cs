﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Threading;
using CSharpBinding.Parser;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Parser;
using ICSharpCode.SharpDevelop.Refactoring;

namespace CSharpBinding.Refactoring
{
	public class SDRefactoringContext : RefactoringContext
	{
		readonly CSharpAstResolver resolver;
		readonly ITextEditor editor;
		readonly ITextSource textSource;
		readonly TextLocation location;
		volatile IDocument document;
		int selectionStart, selectionLength;
		
		public SDRefactoringContext(ITextSource textSource, CSharpAstResolver resolver, TextLocation location, int selectionStart, int selectionLength, CancellationToken cancellationToken)
			: base(resolver, cancellationToken)
		{
			this.resolver = resolver;
			this.textSource = textSource;
			this.document = textSource as IDocument;
			this.selectionStart = selectionStart;
			this.selectionLength = selectionLength;
			this.location = location;
		}
		
		public SDRefactoringContext(ITextEditor editor, CSharpAstResolver resolver, TextLocation location)
			: base(resolver, CancellationToken.None)
		{
			this.resolver = resolver;
			this.editor = editor;
			this.textSource = editor.Document;
			this.document = editor.Document;
			this.selectionStart = editor.SelectionStart;
			this.selectionLength = editor.SelectionLength;
			this.location = location;
		}
		
		public override bool Supports(Version version)
		{
			CSharpProject project = resolver.TypeResolveContext.Compilation.GetProject() as CSharpProject;
			if (project == null)
				return false;
			return project.LanguageVersion >= version;
		}
		
		public Script StartScript()
		{
			var formattingOptions = FormattingOptionsFactory.CreateSharpDevelop();
			if (editor != null)
				return new EditorScript(editor, this, formattingOptions);
			else if (document == null || document is ReadOnlyDocument)
				throw new InvalidOperationException("Cannot start a script in a read-only context");
			else
				return new DocumentScript(document, formattingOptions, this.TextEditorOptions);
		}
		
		public override TextLocation Location {
			get { return location; }
		}
		
		public override TextLocation SelectionStart {
			get { return GetLocation(selectionStart); }
		}
		
		public override TextLocation SelectionEnd {
			get {
				return GetLocation(selectionStart + selectionLength);
			}
		}
		
		public override string SelectedText {
			get {
				return textSource.GetText(selectionStart, selectionLength);
			}
		}
		
		public override bool IsSomethingSelected {
			get {
				return selectionLength > 0;
			}
		}
		
		public override string GetText(int offset, int length)
		{
			return textSource.GetText(offset, length);
		}
		
		public override string GetText(ISegment segment)
		{
			return textSource.GetText(segment);
		}
		
		public ITextSourceVersion Version {
			get { return textSource.Version; }
		}
		
		public override IDocumentLine GetLineByOffset(int offset)
		{
			if (document == null)
				document = new ReadOnlyDocument(textSource, resolver.UnresolvedFile.FileName);
			return document.GetLineByOffset(offset);
		}
		
		public override int GetOffset(TextLocation location)
		{
			if (document == null)
				document = new ReadOnlyDocument(textSource, resolver.UnresolvedFile.FileName);
			return document.GetOffset(location);
		}
		
		public override TextLocation GetLocation(int offset)
		{
			if (document == null)
				document = new ReadOnlyDocument(textSource, resolver.UnresolvedFile.FileName);
			return document.GetLocation(offset);
		}
		
		public override AstType CreateShortType(IType fullType)
		{
			CSharpResolver csResolver;
			lock (resolver) {
				csResolver = resolver.GetResolverStateBefore(GetNode());
			}
			var builder = new TypeSystemAstBuilder(csResolver);
			return builder.ConvertType(fullType);
		}
	}
}