using System;
using Foundation;
using MvvmCross.Plugins.Color.iOS;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;
using static Toggl.Multivac.Extensions.StringExtensions;

namespace Toggl.Daneel.Views
{
    [Register(nameof(TextViewWithCharacterCounter))]
    public sealed class TextViewWithCharacterCounter : TextViewWithPlaceholder
    {
        private readonly UIStringAttributes counterAttributes = new UIStringAttributes
        {
            ForegroundColor = Color.EditTimeEntry.DescriptionCharacterCounter.ToNativeColor()
        };

        private NSRange selectedRange;

        private int remainingLength;
        public int RemainingLength
        {
            get => remainingLength;
            set
            {
                if (remainingLength == value) return;
                remainingLength = value;
                SetText(Text);
            }
        }

        public override string Text
        {
            get => getTextWithoutCounter(base.Text);
            set => base.Text = value;
        }
 
        public TextViewWithCharacterCounter(IntPtr handle) : base(handle)
        {
        }
 
        private string getTextWithoutCounter(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (RemainingLength >= 0)
                return value;
 
            for (int i = value.Length - 1; i > 0; i--)
            {
                if (value[i] == '-')
                {
                    return value.Substring(0, i - 1);
                }
            }
 
            return value;
        }
 
        public override void Changed(UITextView textView)
        {
            base.Changed(textView);
            SelectedRange = selectedRange;
        }

        public override bool ShouldChangeText(UITextView textView, NSRange range, string text)
        {
            if (!base.ShouldChangeText(textView, range, text))
                return false;

            //Special case for deleting whole text. Otherwise the counter would
            //remain as editable text
            if (range.Length > 0 && range.Length == Text.Length)
            {
                Text = "";
                selectedRange = SelectedRange = new NSRange(0, 0);
                return false;
            }

            selectedRange = SelectedRange;
            var textLengthInGraphemes = text.LengthInGraphemes();
            if (text == "")
            {
                selectedRange.Location -= range.Length == 1 ? 1 : 0;
                selectedRange.Length = 0;
            }
            else
            {
                selectedRange.Location += textLengthInGraphemes;
            }

            return true;
        }

        protected override void SetText(string text)
        {
            var attributedString = new NSMutableAttributedString(getTextWithoutCounter(text));

            if (RemainingLength < 0)
            {
                var remainingLengthString = new NSAttributedString($" {RemainingLength}", counterAttributes);
                attributedString.Append(remainingLengthString);
            }

            attributedString.AddAttributes(DefaultTextAttributes, new NSRange(0, attributedString.Length));
            AttributedText = attributedString;
        }
 
        [Export("textViewDidChangeSelection:")]
        public new void SelectionChanged(UITextView textView)
        {
            var counterStart = Text.LengthInGraphemes();
                
            //Don't allow putting the cursor in the red counter
            if (SelectedRange.Length == 0)
            {
                if (SelectedRange.Location > counterStart)
                    SelectedRange = new NSRange(counterStart, 0);
                return;
            }
 
            //Don't allow selecting any text in the red counter
            var difference = counterStart - (SelectedRange.Location + SelectedRange.Length);
            if (difference < 0)
            {
                var range = SelectedRange;
                range.Length += difference;
                SelectedRange = range;
            }
        }
    }
}
