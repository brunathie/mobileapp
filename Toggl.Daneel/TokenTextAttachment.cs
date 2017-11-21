using System;
using CoreGraphics;
using UIKit;

namespace Toggl.Daneel
{
    public sealed class TokenTextAttachment : NSTextAttachment
    {
        public nfloat FontDescender { get; set; }

        public override CGRect GetAttachmentBounds(NSTextContainer textContainer, CGRect proposedLineFragment, CGPoint glyphPosition, nuint characterIndex)
        {
            var superRect = base.GetAttachmentBounds(textContainer, proposedLineFragment, glyphPosition, characterIndex);
            superRect.Y = FontDescender;
            return superRect;
        }
    }
}
