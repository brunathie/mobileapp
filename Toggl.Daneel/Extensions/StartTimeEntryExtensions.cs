using System.Linq;
using System.Text;
using Foundation;
using MvvmCross.Platform.UI;
using MvvmCross.Plugins.Color.iOS;
using Toggl.Daneel.Views;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static class StartTimeEntryExtensions
    {
        // Non Breakable space
        private const string nbs = "\u00A0";
        private const byte spacesForTagToken = 6;
        private static readonly NSParagraphStyle paragraphStyle;
        private static readonly UIColor strokeColor = Color.StartTimeEntry.ProjectTokenBorder.ToNativeColor();

        static StartTimeEntryExtensions()
        {
            paragraphStyle = new NSMutableParagraphStyle
            {
                MinimumLineHeight = 30,
                MaximumLineHeight = 30
            };
        }

        public static string GetDescription(this UITextView self, TextFieldInfo info)
        {
            var result = self.Text;

            var projectText = info.PaddedProjectAndTaskName();
            if (!string.IsNullOrEmpty(projectText))
                result = result.Replace(projectText, "");

            foreach (var tag in info.Tags)
                result = result.Replace(PaddedTagName(tag.Name), "");

            return result;
        }

        public static string PaddedProjectAndTaskName(this TextFieldInfo self)
        {
            if (string.IsNullOrEmpty(self.ProjectName)) return "";

            var projectName = self.TaskId != null ? $"{self.ProjectName}: {self.TaskName}" : self.ProjectName;
            var needsTruncation = projectName.Length >= 30;
            var projectString = needsTruncation ? $"{projectName.Substring(0, 27)}..." : projectName;

            var result = $"      {projectString}   ".Replace(" ", nbs);
            return result;
        }

        public static string TagsText(this TextFieldInfo self)
            => self.Tags.Length == 0 ? "" :
                   self.Tags.Aggregate(
                       new StringBuilder(""),
                       (builder, tag) => builder.Append(PaddedTagName(tag.Name)))
                   .ToString();

        public static string PaddedTagName(string tagName)
        {
            var needsTruncation = tagName.Length >= 30;
            var tagString = needsTruncation ? $"{tagName.Substring(0, 27)}..." : tagName;
            var result = $" {nbs}{nbs}{nbs}{tagString.Replace(" ", nbs)}{nbs}{nbs}{nbs}";
            return result;
        }

        public static NSAttributedString GetAttributedText(this TextFieldInfo self)
        {
            var projectName = self.PaddedProjectAndTaskName();
            var tags = self.TagsText();
            var fullText = $"{self.Text}{projectName}{tags}";
            var result = new NSMutableAttributedString(fullText);
            var baselineOffset = string.IsNullOrEmpty(self.Text) ? 5 : 3;

            result.AddAttributes(new UIStringAttributes
            {
                ParagraphStyle = paragraphStyle,
                Font = UIFont.SystemFontOfSize(16, UIFontWeight.Regular),
            }, new NSRange(0, self.Text.Length));

            addProjectAttributesIfNeeded(self, projectName, result, baselineOffset);
            addTagAttributesIfNeeded(self, projectName, tags, result, baselineOffset);

            return result;
        }

        private static void addTagAttributesIfNeeded(TextFieldInfo self, string projectName, string tags, NSMutableAttributedString result, int baselineOffset)
        {
            if (string.IsNullOrEmpty(tags)) return;

            var startingPosition = self.Text.Length + projectName.Length;

            for (int i = 0; i < self.Tags.Length; i++)
            {
                var tagLength = PaddedTagName(self.Tags[i].Name).Length;

                var attributes = new UIStringAttributes
                {
                    BaselineOffset = baselineOffset,
                    ParagraphStyle = paragraphStyle,
                    Font = UIFont.SystemFontOfSize(12, UIFontWeight.Regular),
                };
                attributes.Dictionary[TimeEntryTagsTextView.RoundedBorders] = strokeColor;
                result.AddAttributes(attributes, new NSRange(startingPosition + 1, tagLength - 1));
                result.AddAttribute(
                    TimeEntryTagsTextView.TagIndex, new NSNumber(i),
                    new NSRange(startingPosition, tagLength)
                );

                startingPosition += tagLength;
            }
        }

        private static void addProjectAttributesIfNeeded(TextFieldInfo self, string projectName,
            NSMutableAttributedString resultString, int baselineOffset)
        {
            if (string.IsNullOrEmpty(self.ProjectColor)) return;

            var color = MvxColor.ParseHexString(self.ProjectColor).ToNativeColor();

            var attributes = new UIStringAttributes
            {
                ForegroundColor = color,
                StrokeColor = strokeColor,
                BaselineOffset = baselineOffset,
                ParagraphStyle = paragraphStyle,
                Font = UIFont.SystemFontOfSize(12, UIFontWeight.Regular),
            };
            attributes.Dictionary[TimeEntryTagsTextView.RoundedBackground] = color.ColorWithAlpha(0.12f);

            resultString.AddAttributes(attributes, new NSRange(self.Text.Length, projectName.Length));
        }
    }
}
