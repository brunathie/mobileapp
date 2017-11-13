using System;
using System.Globalization;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS.Views;
using MvvmCross.Platform.Converters;
using MvvmCross.Plugins.Color.iOS;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Views
{
    public partial class CreateEntityViewCell : MvxTableViewCell
    {
        public static readonly NSString Key = new NSString(nameof(CreateEntityViewCell));
        public static readonly UINib Nib;

        static CreateEntityViewCell()
        {
            Nib = UINib.FromName(nameof(CreateEntityViewCell), NSBundle.MainBundle);
        }

        protected CreateEntityViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            this.DelayBind(() =>
            {
                var bindingSet = this.CreateBindingSet<CreateEntityViewCell, string>();

                bindingSet.Bind(TextLabel)
                          .For(v => v.AttributedText)
                          .To(vm => vm)
                          .WithConversion(new CreateEntityValueConverter(TextLabel.Font.CapHeight));

                bindingSet.Apply();
            });
        }

        private class CreateEntityValueConverter : MvxValueConverter<string, NSAttributedString>
        {
            private readonly nfloat fontHeight;
            private readonly NSAttributedString cachedAddIcon;
            private readonly UIColor textColor;
            public CreateEntityValueConverter(nfloat fontHeight)
            {
                this.fontHeight = fontHeight;
                textColor = Color.StartTimeEntry.Placeholder.ToNativeColor();

                // We need to cache this icon because iOS can't create images on background threads
                // and MvvmCross uses background threads in lists for performance. Caching is a much
                // better solution than marshalling to the MainThread for every cell.
                cachedAddIcon = "".PrependWithAddIcon(fontHeight);
            }

            protected override NSAttributedString Convert(string value, Type targetType, object parameter, CultureInfo culture)
            {
                var result = new NSMutableAttributedString(cachedAddIcon);
                var text = new NSMutableAttributedString(value);

                text.AddAttribute(UIStringAttributeKey.ForegroundColor, textColor, new NSRange(0, text.Length));
                result.Append(text);

                return result;
            }
        }
    }
}
