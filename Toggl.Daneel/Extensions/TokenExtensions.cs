﻿using System;
using CoreGraphics;
using Foundation;
using MvvmCross.Platform.UI;
using MvvmCross.Plugins.Color.iOS;
using Toggl.Multivac.Extensions;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static class TokenExtensions
    {
        private const int lineHeight = 24;
        private const int tokenHeight = 22;
        private const int tokenPadding = 6;
        private const int maxTextLength = 50;
        private const int interTokenSpacing = 6;
        private const float tokenCornerRadius = 6.0f;
        private const int tokenVerticallOffset = (lineHeight - tokenHeight) / 2;

        private const int dotPadding = 6;
        private const int dotDiameter = 4;
        private const int dotRadius = dotDiameter / 2;
        private const int dotYOffset = (lineHeight / 2) - dotRadius;

        private static readonly nfloat textVerticalOffset;
        private static readonly NSParagraphStyle paragraphStyle;
        private static readonly UIStringAttributes tagAttributes;
        private static readonly UIFont tokenFont = UIFont.SystemFontOfSize(12, UIFontWeight.Regular);
        private static readonly UIFont regularFont = UIFont.SystemFontOfSize(16, UIFontWeight.Regular);
        private static readonly UIColor borderColor = Color.StartTimeEntry.TokenBorder.ToNativeColor();

        public static readonly NSString Project = new NSString(nameof(Project));
        public static readonly NSString TagIndex = new NSString(nameof(TagIndex));

        static TokenExtensions()
        {
            paragraphStyle = new NSMutableParagraphStyle
            {
                MinimumLineHeight = lineHeight,
                MaximumLineHeight = lineHeight
            };

            tagAttributes = new UIStringAttributes
            {
                Font = tokenFont,
                ForegroundColor = Color.StartTimeEntry.TokenText.ToNativeColor()
            };

            textVerticalOffset = (lineHeight / 2) - (tokenFont.CapHeight / 2) - 3;
        }

        public static string GetDescription(this UITextView self) => self.Text.Replace("￼", "");

        public static NSAttributedString GetAttributedText(this TextFieldInfo self)
        {
            var result = new NSMutableAttributedString(self.Text);
            result.AddAttributes(new UIStringAttributes
            {
                Font = regularFont,
                ParagraphStyle = paragraphStyle
            }, new NSRange(0, result.Length));

            addProjectAttachmentsIfNeeded(self, result);
            addTagAttachmentsIfNeeded(self, result);

            return result;
        }

        public static string TruncatedAt(this string self, int location)
            => self.Length <= location ? self : $"{self.UnicodeSafeSubstring(0, location - 3)}...";

        private static void addProjectAttachmentsIfNeeded(TextFieldInfo info, NSMutableAttributedString finalString)
        {
            if (string.IsNullOrEmpty(info.ProjectColor)) return;

            var color = MvxColor.ParseHexString(info.ProjectColor).ToNativeColor();

            var projectName = new NSAttributedString(info.ProjectName.TruncatedAt(maxTextLength), new UIStringAttributes
            {
                Font = tokenFont,
                ForegroundColor = color
            });

            var image = getProjectToken(projectName, color);
            var textAttachment = new TokenTextAttachment { Image = image, FontDescender = regularFont.Descender };
            var tokenString = new NSMutableAttributedString(NSAttributedString.FromAttachment(textAttachment));
            var attributes = new UIStringAttributes { ParagraphStyle = paragraphStyle };
            attributes.Dictionary[Project] = new NSObject();
            tokenString.AddAttributes(attributes, new NSRange(0, tokenString.Length));

            finalString.Append(tokenString);
        }

        private static void addTagAttachmentsIfNeeded(TextFieldInfo info, NSMutableAttributedString finalString)
        {
            if (info.Tags.Length == 0) return;

            for (var i = 0; i < info.Tags.Length; i++)
            {
                var tag = info.Tags[i];
                var tagName = new NSMutableAttributedString(tag.Name.TruncatedAt(maxTextLength), tagAttributes);

                var image = getTagToken(tagName);
                var textAttachment = new TokenTextAttachment { Image = image, FontDescender = regularFont.Descender };

                var tokenString = new NSMutableAttributedString(NSAttributedString.FromAttachment(textAttachment));
                var attributes = new UIStringAttributes { ParagraphStyle = paragraphStyle };
                attributes.Dictionary[TagIndex] = new NSNumber(i);
                tokenString.AddAttributes(attributes, new NSRange(0, tokenString.Length));

                finalString.Append(tokenString);
            }
        }

        private static UIImage getProjectToken(NSAttributedString stringToDraw, UIColor projectColor)
        {
            const int circleWidth = dotDiameter + dotPadding;
            var totalWidth = stringToDraw.Size.Width + circleWidth + interTokenSpacing + (tokenPadding * 2);
            var size = new CGSize(totalWidth, lineHeight);
            UIGraphics.BeginImageContextWithOptions(size, false, 0.0f);
            using (var context = UIGraphics.GetCurrentContext())
            {
                var tokenPath = UIBezierPath.FromRoundedRect(new CGRect(
                    x: interTokenSpacing, 
                    y: tokenVerticallOffset,
                    width: totalWidth - interTokenSpacing,
                    height: tokenHeight
                ), tokenCornerRadius);
                context.AddPath(tokenPath.CGPath);
                context.SetFillColor(projectColor.ColorWithAlpha(0.12f).CGColor);
                context.FillPath();

                var dot = UIBezierPath.FromRoundedRect(new CGRect(
                    x: dotPadding + interTokenSpacing, 
                    y: dotYOffset,
                    width: dotDiameter,
                    height: dotDiameter
                ), dotRadius);
                context.AddPath(dot.CGPath);
                context.SetFillColor(projectColor.CGColor);
                context.FillPath();

                stringToDraw.DrawString(new CGPoint(circleWidth + interTokenSpacing + tokenPadding, textVerticalOffset));

                var image = UIGraphics.GetImageFromCurrentImageContext();
                UIGraphics.EndImageContext();
                return image;
            }
        }

        private static UIImage getTagToken(NSAttributedString stringToDraw)
        {
            var size = new CGSize(stringToDraw.Size.Width + interTokenSpacing + (tokenPadding * 2), lineHeight);
            UIGraphics.BeginImageContextWithOptions(size, false, 0.0f);
            using (var context = UIGraphics.GetCurrentContext())
            {
                var tokenPath = UIBezierPath.FromRoundedRect(new CGRect(
                    x: interTokenSpacing, 
                    y: tokenVerticallOffset,
                    width: size.Width - interTokenSpacing,
                    height: tokenHeight
                ), tokenCornerRadius);
                context.AddPath(tokenPath.CGPath);
                context.SetStrokeColor(borderColor.CGColor);
                context.StrokePath();

                stringToDraw.DrawString(new CGPoint(interTokenSpacing + tokenPadding, textVerticalOffset));

                var image = UIGraphics.GetImageFromCurrentImageContext();
                UIGraphics.EndImageContext();
                return image;
            }
        }
    }
}