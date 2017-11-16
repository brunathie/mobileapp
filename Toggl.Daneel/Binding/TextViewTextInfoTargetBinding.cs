using System;
using Foundation;
using MvvmCross.Binding;
using MvvmCross.Binding.Bindings.Target;
using MvvmCross.Platform.Core;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Views;
using Toggl.Foundation.Autocomplete;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.Binding
{
    public sealed class TextViewTextInfoTargetBinding : MvxTargetBinding<UITextView, TextFieldInfo>
    {
        public const string BindingName = "TextFieldInfo";
        private const string selectedTextRangeChangedKey = "selectedTextRange";

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

            Target.AttributedText = value.GetAttributedText();

            if (!Target.IsFirstResponder || Target.BeginningOfDocument == null) return;

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
            var attributes = Target.AttributedText.GetAttributes(e.Index, out var effectiveRange);
            var index = attributes[TimeEntryTagsTextView.TagIndex] as NSNumber;
            var tag = textFieldInfo.Tags[index.Int32Value];

            onTextFieldInfoChanged(
                textFieldInfo
                    .RemoveTag(tag)
                    .WithTextAndCursor(textFieldInfo.Text, (int)effectiveRange.Location));
        }

        private void queueValueChange()
        {
            var selectedRangeStart = Target.SelectedTextRange?.Start;
            if (selectedRangeStart == null) return;

            var newDescription = Target.GetDescription(textFieldInfo);
            var descriptionLength = newDescription.Length;

            var newCursorPosition = (int)Target.GetOffsetFromPosition(Target.BeginningOfDocument, selectedRangeStart);

            var isInsideDescriptionBounds = newCursorPosition <= descriptionLength;
            if (isInsideDescriptionBounds)
            {
                onTextFieldInfoChanged(textFieldInfo.WithTextAndCursor(newDescription, newCursorPosition));
                return;
            }

            var maxLength = (int)(Target.AttributedText.Length == 0 ? 0 : Target.AttributedText.Length - 1);
            var attributes = Target.AttributedText.GetAttributes(newCursorPosition.Clamp(0, maxLength), out var effectiveRange);

            var oldCursorPosition = textFieldInfo.CursorPosition;
            var isMovingForward = newCursorPosition >= oldCursorPosition;

            var actualCursorPosition = (int)effectiveRange.Location + (int)(isMovingForward ? effectiveRange.Length : -1);
            onTextFieldInfoChanged(textFieldInfo.WithTextAndCursor(newDescription, actualCursorPosition));
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
                public TagDeletedEventArgs(nint index)
                {
                    Index = index;
                }

                public nint Index { get; }
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

                var isDeletingProject = attrs.ObjectForKey(TimeEntryTagsTextView.RoundedBackground) != null;
                if (isDeletingProject)
                {
                    ProjectDeleted.Raise(this);
                    return false;
                }

                var isDeletingTag = attrs.ObjectForKey(TimeEntryTagsTextView.TagIndex) != null;
                if (isDeletingTag)
                {
                    TagDeleted?.Invoke(this, new TagDeletedEventArgs(cursorPosition));
                    return false;
                }

                return true;
            }

            private static bool isPressingBackspace(NSRange range, string text)
                => range.Length == 1 && text.Length == 0;
        }
    }
}
