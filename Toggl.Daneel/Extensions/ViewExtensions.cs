using CoreGraphics;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static class ViewExtensions
    {
        private const int switchHeight = 24;

        public static void Resize(this UISwitch self)
        {
            var scale = switchHeight / self.Frame.Height;
            self.Transform = CGAffineTransform.MakeScale(scale, scale);
        }
    }
}
