using System;
using CoreGraphics;
using Foundation;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Views
{
    [Register(nameof(ActivityIndicatorView))]
    public sealed class ActivityIndicatorView : UIImageView
    {
        private const float animationDuration = 0.5F;

        private string imageResource = "icLoader";
        public string ImageResource
        {
            get => imageResource;
            set => Image = UIImage.FromBundle(imageResource = value);
        }

        public ActivityIndicatorView(CGRect frame)
            : base (frame)
        {
            init();
        }

        public ActivityIndicatorView(IntPtr handle)
            : base(handle)
        {
        }

        private void init()
        {
            Image = UIImage.FromBundle(ImageResource);
            ContentMode = UIViewContentMode.Center;

            rotateView();
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            init();
        }

        private void rotateView()
        {
            UIView.Animate(Animation.Timings.SpiderBro, 0, UIViewAnimationOptions.CurveLinear,
                () => Transform = CGAffineTransform.Rotate(Transform, (nfloat)Math.PI), rotateView);
        }
    }
}
