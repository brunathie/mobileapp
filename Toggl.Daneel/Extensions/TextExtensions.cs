using CoreGraphics;
using Foundation;
using MvvmCross.Plugins.Color.iOS;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static class TextExtensions
    {
        private const string addIcon = "icAdd";

        public static NSAttributedString PrependWithAddIcon(this string self, double fontHeight)
        {
            var textColor = Color.StartTimeEntry.Placeholder.ToNativeColor();
            var addIconColor = Color.StartTimeEntry.AddIconColor.ToNativeColor();

            var emptyTextString = new NSMutableAttributedString($" {self}");
            emptyTextString.AddAttribute(UIStringAttributeKey.ForegroundColor, textColor, new NSRange(0, emptyTextString.Length));

            var result = addIcon.GetAttachmentString(fontHeight);
            // We need a space before the attachment for colors to work. The Zero-width space character does not work. 
            // The space makes the left margin too big. This is here so the space does not appear. 
            result.AddAttribute(UIStringAttributeKey.Font, UIFont.SystemFontOfSize(0), new NSRange(0, 1));
            result.AddAttribute(UIStringAttributeKey.ForegroundColor, addIconColor, new NSRange(0, result.Length));
            result.Append(emptyTextString);

            return result;
        }

        public static NSAttributedString EndingWithTick(this string self, double fontHeight)
        {
            var tick = GetAttachmentString("icDoneSmall", fontHeight);

            var range = new NSRange(0, 1);
            var attributes = new UIStringAttributes { ForegroundColor = UIColor.White };
            tick.AddAttributes(attributes, range);

            var result = new NSMutableAttributedString(self);
            result.Append(tick);

            return result;
        }

        public static NSMutableAttributedString GetAttachmentString(this string imageName, double fontHeight)
        {
            var attachment = new NSTextAttachment
            {
                Image = UIImage.FromBundle(imageName)
                               .ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate)
            };

            var imageSize = attachment.Image.Size;
            var y = (fontHeight - imageSize.Height) / 2;
            attachment.Bounds = new CGRect(0, y, imageSize.Width, imageSize.Height);

            //There neeeds to be a space before the dot, otherwise the colors don't work
            var result = new NSMutableAttributedString(" ");
            var attachmentString = NSAttributedString.FromAttachment(attachment);
            result.Append(attachmentString);

            return result;
        }
    }
}
