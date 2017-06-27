//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.TypeSystem;

namespace Microsoft.VisualStudio.Platform
{
	[Export (typeof (IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession>))]
	//[Export (typeof (ICompletionSourceProvider))]
	[Name (PredefinedCompletionPresenterNames.RoslynCompletionPresenter)]
	[ContentType (ContentTypeNames.RoslynContentType)]
	internal sealed class CompletionPresenter : IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession>//, ICompletionSourceProvider
	{
		public ICompletionPresenterSession CreateSession (ITextView textView, ITextBuffer subjectBuffer, ICompletionSession sessionOpt)
		{
			return new CompletionPresenterSession (textView, subjectBuffer);
		}
	}

	internal class CompletionPresenterSession : ICompletionPresenterSession
	{
		private ITextView _textView;
		private ITextBuffer _subjectBuffer;

		public CompletionPresenterSession (ITextView textView, ITextBuffer subjectBuffer)
		{
			_textView = textView;
			_subjectBuffer = subjectBuffer;
		}

		public event EventHandler<CompletionItemEventArgs> ItemSelected;
		public event EventHandler<CompletionItemEventArgs> ItemCommitted;
		public event EventHandler<CompletionItemFilterStateChangedEventArgs> FilterStateChanged;
		public event EventHandler<EventArgs> Dismissed;

		public void Dismiss ()
		{
		}

		public void PresentItems (ITrackingSpan triggerSpan, IList<CompletionItem> items, CompletionItem selectedItem, CompletionItem suggestionModeItem, bool suggestionMode, bool isSoftSelected, ImmutableArray<CompletionItemFilter> completionItemFilters, string filterText)
		{
		}

		public void SelectNextItem ()
		{
		}

		public void SelectNextPageItem ()
		{
		}

		public void SelectPreviousItem ()
		{
		}

		public void SelectPreviousPageItem ()
		{
		}
	}

	[Export(typeof(IWaitIndicator))]
    internal class WaitIndicator : IWaitIndicator
    {
        public IWaitContext StartWait (string title, string message, bool allowCancel, bool showProgress)
        {
            return new WaitContext ();
        }

        public WaitIndicatorResult Wait (string title, string message, bool allowCancel, bool showProgress, Action<IWaitContext> action)
        {
            using (var waitContext = StartWait (title, message, allowCancel, showProgress))
            {
                try
                {
                    action (waitContext);

                    return WaitIndicatorResult.Completed;
                }
                catch (OperationCanceledException)
                {
                    return WaitIndicatorResult.Canceled;
                }
                catch (AggregateException e)
                {
                    var operationCanceledException = e.InnerExceptions[0] as OperationCanceledException;
                    if (operationCanceledException != null)
                    {
                        return WaitIndicatorResult.Canceled;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        internal class WaitContext : IWaitContext
        {
            public CancellationToken CancellationToken => throw new NotImplementedException ();

            public bool AllowCancel { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }
            public string Message { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }

            public IProgressTracker ProgressTracker => throw new NotImplementedException ();

            public void Dispose ()
            {
                throw new NotImplementedException ();
            }
        }
    }

    public class RoslynCommandTarget
    {
        internal ICommandHandlerService CurrentHandlers { get; set; }

        internal ITextBuffer _languageBuffer;
        internal ITextView _textView;

        private RoslynCommandTarget(ITextView textView, ITextBuffer languageBuffer)
        {
            ICommandHandlerServiceFactory commandHandlerServiceFactory = CompositionManager.GetExportedValue<ICommandHandlerServiceFactory>();

            if (commandHandlerServiceFactory != null)
            {
                commandHandlerServiceFactory.Initialize (languageBuffer.ContentType.TypeName);

                CurrentHandlers = commandHandlerServiceFactory.GetService (languageBuffer);
            }

            _languageBuffer = languageBuffer;
            _textView = textView;
        }

        public static RoslynCommandTarget FromViewAndBuffer(ITextView textView, ITextBuffer languageBuffer)
        {
            return languageBuffer.Properties.GetOrCreateSingletonProperty<RoslynCommandTarget>(() => new RoslynCommandTarget(textView, languageBuffer));
        }

        public void ExecuteTypeCharacter(char typedChar, Action lastHandler)
        {
            CurrentHandlers?.Execute (_languageBuffer.ContentType,
                args: new TypeCharCommandArgs (_textView, _languageBuffer, typedChar),
                lastHandler: lastHandler);
        }
    }

    public static class PlatformExtensions
    {
        public static ITextBuffer GetPlatformTextBuffer(this MonoDevelop.Ide.Editor.TextEditor textEditor)
        {
            return textEditor.TextView.TextBuffer;
        }

        public static ITextView GetPlatformTextView(this MonoDevelop.Ide.Editor.TextEditor textEditor)
        {
            return textEditor.TextView;
        }

        public static MonoDevelop.Ide.Editor.ITextDocument GetTextEditor(this ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetProperty<MonoDevelop.Ide.Editor.ITextDocument>(typeof(MonoDevelop.Ide.Editor.ITextDocument));
        }

        public static MonoDevelop.Ide.Editor.ITextDocument GetTextEditor (this ITextView textView)
        {
            return textView.Properties.GetProperty<MonoDevelop.Ide.Editor.TextEditor>(typeof(MonoDevelop.Ide.Editor.TextEditor));
        }
    }
}