using System;
using Foundation;
using MvvmCross.Binding;
using MvvmCross.Binding.Bindings.Target;
using MvvmCross.Platform.Core;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.Autocomplete;
using UIKit;

namespace Toggl.Daneel.Binding
{
    public sealed class TextViewTextInfoTargetBinding : MvxTargetBinding<UITextView, TextFieldInfo>
    {
        public const string BindingName = "TextFieldInfo";
        private const string selectedTextRangeChangedKey = "selectedTextRange";

        private bool isSelectingText;
        private TextFieldInfo textFieldInfo = TextFieldInfo.Empty;

        private readonly IDisposable selectedTextRangeDisposable;
        private readonly TextViewInfoDelegate infoDelegate = new TextViewInfoDelegate();

        public override MvxBindingMode DefaultMode => MvxBindingMode.TwoWay;

        public TextViewTextInfoTargetBinding(UITextView target)
            : base(target)
        {
            Target.Delegate = infoDelegate;

            infoDelegate.TagDeleted += onTagDeleted;
            infoDelegate.TextChanged += onTextChanged;
            infoDelegate.ProjectDeleted += onProjectDeleted;

            selectedTextRangeDisposable = Target.AddObserver(
                selectedTextRangeChangedKey,
                NSKeyValueObservingOptions.OldNew,
                onSelectedTextRangeChanged
            );
        }

        protected override void SetValue(TextFieldInfo value)
        {
            setTextFieldInfo(value);

            if (isSelectingText || !Target.IsFirstResponder || Target.BeginningOfDocument == null) return;

            Target.AttributedText = value.GetAttributedText();

            var positionToSet = Target.GetPosition(Target.BeginningOfDocument, value.CursorPosition);
            Target.SelectedTextRange = Target.GetTextRange(positionToSet, positionToSet);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (!isDisposing) return;

            Target.Delegate = null;

            selectedTextRangeDisposable?.Dispose();

            infoDelegate.TagDeleted -= onTagDeleted;
            infoDelegate.TextChanged -= onTextChanged;
            infoDelegate.ProjectDeleted -= onProjectDeleted;
        }

        private void onTextChanged(object sender, EventArgs e)
            => queueValueChange();

        private void onSelectedTextRangeChanged(NSObservedChange change)
            => queueValueChange();

        private void onProjectDeleted(object sender, EventArgs e)
        {
            onTextFieldInfoChanged(
                textFieldInfo
                    .RemoveProjectInfo()
                    .WithTextAndCursor(textFieldInfo.Text, textFieldInfo.Text.Length));
        }

        private void onTagDeleted(object sender, TextViewInfoDelegate.TagDeletedEventArgs e)
        {
            onTextFieldInfoChanged(
                textFieldInfo
                    .RemoveTag(textFieldInfo.Tags[e.TagIndex])
                    .WithTextAndCursor(textFieldInfo.Text, e.CursorPosition));
        }

        private void queueValueChange()
        {
            isSelectingText = Target.SelectedRange.Length > 0;
            var reportedCursorPosition = (int)Target.SelectedRange.Location;

            var newDescription = Target.GetDescription();
            var descriptionLength = newDescription.Length;

            var isEditingInTokenRegion = newDescription != textFieldInfo.Text && reportedCursorPosition > newDescription.Length;
            var newCursorPosition = isEditingInTokenRegion ? newDescription.Length : reportedCursorPosition;

            onTextFieldInfoChanged(textFieldInfo.WithTextAndCursor(newDescription, newCursorPosition));
        }

        private void onTextFieldInfoChanged(TextFieldInfo info)
        {
            setTextFieldInfo(info);
            FireValueChanged(info);
        }

        private void setTextFieldInfo(TextFieldInfo info)
        {
            if (textFieldInfo.Text != info.Text 
             || textFieldInfo.ProjectName != info.ProjectName
             || textFieldInfo.Tags.Length != info.Tags.Length)
            Target.SetNeedsDisplay();

            textFieldInfo = info;
        }

        private sealed class TextViewInfoDelegate : NSObject, IUITextViewDelegate
        {
            public class TagDeletedEventArgs : EventArgs
            {
                public int CursorPosition { get; }

                public int TagIndex { get; }

                public TagDeletedEventArgs(nint cursorPosition, nint tagIndex)
                {
                    TagIndex = (int)tagIndex;
                    CursorPosition = (int)cursorPosition;
                }
            }

            public event EventHandler TextChanged;
            public event EventHandler ProjectDeleted;
            public event EventHandler<TagDeletedEventArgs> TagDeleted;

            [Export("textViewDidChange:")]
            public void DidChange(UITextView textView)
                => TextChanged.Raise(this);
           
            [Export("textView:shouldChangeTextInRange:replacementText:")]
            public bool ShouldChangeCharacters(UITextView textView, NSRange range, string text)
            {
                if (!isPressingBackspace(range, text))
                    return true;

                var cursorPosition = range.Location;
                var attrs = textView.AttributedText.GetAttributes(cursorPosition, out var attrRange);

                var isDeletingProject = attrs.ObjectForKey(TokenExtensions.Project) != null;
                if (isDeletingProject)
                {
                    ProjectDeleted.Raise(this);
                    return false;
                }

                var tagIndex = attrs.ObjectForKey(TokenExtensions.TagIndex) as NSNumber;
                var isDeletingTag = tagIndex != null;
                if (isDeletingTag)
                {
                    TagDeleted?.Invoke(this, new TagDeletedEventArgs(cursorPosition, tagIndex.Int32Value));
                    return false;
                }

                return true;
            }

            private static bool isPressingBackspace(NSRange range, string text)
                => range.Length == 1 && text.Length == 0;
        }
    }
}
