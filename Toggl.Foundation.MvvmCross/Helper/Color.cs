using System;
using System.Linq;
using MvvmCross.Platform.UI;

namespace Toggl.Foundation.MvvmCross.Helper
{
    public static class Color
    {
        private static readonly MvxColor lightishGreen = new MvxColor(76, 217, 100);
        private static readonly MvxColor steel = new MvxColor(142, 142, 147);
        private static readonly MvxColor darkMint = new MvxColor(76, 190, 100);
        private static readonly MvxColor pinkishGrey = new MvxColor(206, 206, 206);
        private static readonly MvxColor black = new MvxColor(46, 46, 46);

        public static class Onboarding
        {
            internal static readonly MvxColor TrackPageBorderColor = new MvxColor(14, 150, 213);
            internal static readonly MvxColor TrackPageBackgroundColor = new MvxColor(6, 170, 245);

            internal static readonly MvxColor LogPageBorderColor = new MvxColor(165, 81, 220);
            internal static readonly MvxColor LogPageBackgroundColor = new MvxColor(187, 103, 241);

            internal static readonly MvxColor SummaryPageBorderColor = new MvxColor(230, 179, 31);
            internal static readonly MvxColor SummaryPageBackgroundColor = new MvxColor(241, 195, 63);
            public static readonly MvxColor SummaryPageTimelineSeparators = new MvxColor(164, 173, 176, 89);

            internal static readonly MvxColor LoginPageBackgroundColor = new MvxColor(219, 40, 46);
        }

        public static class Login
        {
            public static readonly MvxColor DisabledButtonColor = new MvxColor(255, 255, 255, 128);
        }

        public static class NavigationBar
        {
            public static readonly MvxColor BackButton = new MvxColor(94, 91, 91);
            public static readonly MvxColor BackgroundColor = new MvxColor(250, 251, 252);
        }

        public static class Main
        {
            public static readonly MvxColor CardBorder = new MvxColor(232, 232, 232);

            public static readonly MvxColor CurrentTimeEntryClientColor = new MvxColor(94, 91, 91);

            public static readonly MvxColor Syncing = new MvxColor(181, 188, 192);
            public static readonly MvxColor SyncCompleted = lightishGreen;
        }

        public static class TimeEntriesLog
        {
            public static readonly MvxColor ClientColor = new MvxColor(163, 163, 163);

            public static readonly MvxColor ButtonBorder = new MvxColor(243, 243, 243);

            public static readonly MvxColor SectionFooter = new MvxColor(250, 251, 252);
        }

        public static class StartTimeEntry
        {
            public static readonly MvxColor Cursor = lightishGreen;

            public static readonly MvxColor Placeholder = pinkishGrey;

            public static readonly MvxColor ActiveButton = lightishGreen;

            public static readonly MvxColor BoldQuerySuggestionColor = new MvxColor(181, 188, 192);

            public static readonly MvxColor InactiveButton = new MvxColor(181, 188, 192);

            public static readonly MvxColor SeparatorColor = new MvxColor(181, 188, 192);

            public static readonly MvxColor TokenText = new MvxColor(94, 91, 91);

            public static readonly MvxColor TokenBorder = new MvxColor(232, 232, 232);

            public static readonly MvxColor AddIconColor = new MvxColor(75, 200, 0);
        }

        internal static MvxColor FromHSV(float hue, float saturation, float value)
        {
            int r = 0, g = 0, b = 0;

            if (saturation == 0)
            {
                r = g = b = (int)(value * 255.0f + 0.5f);
            }
            else
            {
                float h = (hue - (float)Math.Floor(hue)) * 6.0f;
                float f = h - (float)Math.Floor(h);
                float p = value * (1.0f - saturation);
                float q = value * (1.0f - saturation * f);
                float t = value * (1.0f - (saturation * (1.0f - f)));
                switch ((int)h)
                {
                    case 0:
                        r = (int)(value * 255.0f + 0.5f);
                        g = (int)(t * 255.0f + 0.5f);
                        b = (int)(p * 255.0f + 0.5f);
                        break;
                    case 1:
                        r = (int)(q * 255.0f + 0.5f);
                        g = (int)(value * 255.0f + 0.5f);
                        b = (int)(p * 255.0f + 0.5f);
                        break;
                    case 2:
                        r = (int)(p * 255.0f + 0.5f);
                        g = (int)(value * 255.0f + 0.5f);
                        b = (int)(t * 255.0f + 0.5f);
                        break;
                    case 3:
                        r = (int)(p * 255.0f + 0.5f);
                        g = (int)(q * 255.0f + 0.5f);
                        b = (int)(value * 255.0f + 0.5f);
                        break;
                    case 4:
                        r = (int)(t * 255.0f + 0.5f);
                        g = (int)(p * 255.0f + 0.5f);
                        b = (int)(value * 255.0f + 0.5f);
                        break;
                    case 5:
                        r = (int)(value * 255.0f + 0.5f);
                        g = (int)(p * 255.0f + 0.5f);
                        b = (int)(q * 255.0f + 0.5f);
                        break;
                }
            }

            return new MvxColor(r, g, b);
        }

        public static class EditTimeEntry
        {
            public static readonly MvxColor ClientText = new MvxColor(94, 91, 91);
            public static readonly MvxColor DescriptionCharacterCounter = new MvxColor(247, 64, 73);
        }

        public static class ModalDialog
        {
            public static readonly MvxColor BackgroundOverlay = new MvxColor(181, 188, 192);
        }

        public static class Suggestions
        {
            public static readonly MvxColor ClientColor = new MvxColor(163, 163, 163);
        }

        public static class Settings
        {
            public static readonly MvxColor SyncStatusText = new MvxColor(144, 146, 147);
        }

        public static class Common
        {
            public static readonly MvxColor PlaceholderText = pinkishGrey;
        }
      
        public static readonly MvxColor[] DefaultProjectColors =
            Foundation.Helper.Color.DefaultProjectColors.Select(MvxColor.ParseHexString).ToArray();
    }
}
