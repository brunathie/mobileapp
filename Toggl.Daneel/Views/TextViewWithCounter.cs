using System;
using Foundation;
using MvvmCross.Platform.Core;
using MvvmCross.Plugins.Color.iOS;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;
using static Toggl.Multivac.Extensions.StringExtensions;

namespace Toggl.Daneel.Views
{
    [Register(nameof(TextViewWithCounter))]
    public sealed class TextViewWithCounter : UITextView, IUITextViewDelegate
    {
        public event EventHandler CountedTextChanged;

        private NSRange selectedRange;

        private string countedText;
        public string CountedText
        {
            get => countedText;
            set
            {
                if (countedText == value) return;
                countedText = getTextWithoutCounter(value);
                setAttributedText(countedText);
                CountedTextChanged.Raise(this);
            }
        }

        public int MaxLengthInBytes { get; set; }
       
        public TextViewWithCounter(IntPtr handle) : base(handle)
        {
            Delegate = this;
        }

        private void setAttributedText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                AttributedText = new NSAttributedString("");
                return;
            }

            var charactersRemaining = MaxLengthInBytes - CountedText.LengthInUtf8Bytes();
            var resultString = new NSMutableAttributedString(countedText);

            if (charactersRemaining < 0)
            {
                var attributes = new UIStringAttributes
                {
                    ForegroundColor = Color.EditTimeEntry.DescriptionCharacterCounter.ToNativeColor()
                };
                resultString.Append(new NSAttributedString($" {charactersRemaining}", attributes));
            }

            var centeredAttributes = new UIStringAttributes
            {
                ParagraphStyle = new NSMutableParagraphStyle { Alignment = UITextAlignment.Center }
            };

            resultString.AddAttributes(centeredAttributes, new NSRange(0, resultString.Length));

            AttributedText = resultString;
        }

        private string getTextWithoutCounter(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.LengthInUtf8Bytes() <= MaxLengthInBytes)
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

        [Export("textView:shouldChangeTextInRange:replacementText:")]
        public new bool ShouldChangeText(UITextView textView, NSRange range, string text)
        {
            if (text == "\n")
            {
                ResignFirstResponder();
                return false;
            }
            selectedRange = textView.SelectedRange;
            if (text == "")
            {
                selectedRange.Location -= text.LengthInGraphemes();
                selectedRange.Length = 0;
            }
            else
            {
                selectedRange.Location += text.LengthInGraphemes();
            }
            return true;
        }

        [Export("textViewDidChange:")]
        public new void Changed(UITextView textView)
        {
            CountedText = Text.Replace('\n', ' ');
            SelectedRange = selectedRange;
        }

        [Export("textViewDidChangeSelection:")]
        public new void SelectionChanged(UITextView textView)
        {
            var counterStart = CountedText.LengthInGraphemes();

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
                var r = SelectedRange;
                r.Length += difference;
                SelectedRange = r;
            }
        }
    }
}
