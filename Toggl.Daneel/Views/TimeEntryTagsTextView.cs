using System;
using CoreGraphics;
using CoreText;
using Foundation;
using UIKit;

namespace Toggl.Daneel.Views
{
    [Register(nameof(TimeEntryTagsTextView))]
    public class TimeEntryTagsTextView : UITextView
    {
        public static readonly NSString TagIndex = new NSString("TagIndex");
        public static readonly NSString RoundedBorders = new NSString("RoundedBorders");
        public static readonly NSString RoundedBackground = new NSString("RoundedBackground");

        private const int circleRadius = 2;
        private const int tokenHeight = 22;
        private const int circlePadding = 6;
        private const int circleYOffset = 9;
        private const int circleDiameter = 4;
        private const int whiteSpaceOffset = 10;
        private const float tokenCornerRadius = 6.0f;

        public TimeEntryTagsTextView(IntPtr handle)
            : base(handle)
        {
        }

        public override void Draw(CGRect rect)
        {
            var framesetter = new CTFramesetter(AttributedText);

            var xOffset = TextContainer.LineFragmentPadding + TextContainerInset.Right;
            var yOffset = TextContainerInset.Top;

            var path = new CGPath();
            path.AddRect(rect.Inset(xOffset, yOffset));

            var totalFrame = framesetter.GetFrame(new NSRange(0, AttributedText.Length), path, null);

            var context = UIGraphics.GetCurrentContext();
            context.TextMatrix = CGAffineTransform.MakeIdentity();
            context.TranslateCTM(0, Bounds.Size.Height);
            context.ScaleCTM(1.0f, -1.0f);

            var lines = totalFrame.GetLines();
            var lineCount = lines.Length;

            var origins = new CGPoint[lineCount];
            totalFrame.GetLineOrigins(new NSRange(0, 0), origins);

            for (var index = 0; index < lineCount; index++)
            {
                var line = lines[index];

                foreach (var run in line.GetGlyphRuns())
                {
                    var attributes = run.GetAttributes();

                    var borderColor = attributes.Dictionary[RoundedBorders];
                    var backgroundColor = attributes.Dictionary[RoundedBackground];
                    if (backgroundColor == null && borderColor == null) continue;

                    var x = line.GetOffsetForStringIndex(run.StringRange.Location, out var _) + whiteSpaceOffset;
                    var y = origins[index].Y;

                    var tokenPath = UIBezierPath.FromRoundedRect(new CGRect(
                        x: x, y: y,
                        width: run.GetTypographicBounds(new NSRange(0, 0), out var _, out var _, out var _) - whiteSpaceOffset,
                        height: tokenHeight
                    ), tokenCornerRadius);

                    context.AddPath(tokenPath.CGPath);

                    if (backgroundColor != null)
                    {
                        var color = (UIColor)backgroundColor;
                        context.SetFillColor(color.CGColor);
                        context.DrawPath(CGPathDrawingMode.Fill);

                        var circle = UIBezierPath.FromRoundedRect(new CGRect(
                            x: x + circlePadding,
                            y: y + circleYOffset,
                            width: circleDiameter,
                            height: circleDiameter
                        ), circleRadius);

                        context.AddPath(circle.CGPath);
                        context.SetFillColor(color.ColorWithAlpha(1.0f).CGColor);
                        context.DrawPath(CGPathDrawingMode.Fill);
                    }
                    else
                    {
                        var color = ((UIColor)borderColor).CGColor;
                        context.SetStrokeColor(color);
                        context.DrawPath(CGPathDrawingMode.Stroke);
                    }
                }
            }

            path.Dispose();
            totalFrame.Dispose();
            framesetter.Dispose();
        }
    }
}
