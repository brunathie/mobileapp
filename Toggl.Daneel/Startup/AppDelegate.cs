using MvvmCross.Core.ViewModels;
using MvvmCross.iOS.Platform;
using MvvmCross.Platform;
using Foundation;
using UIKit;

namespace Toggl.Daneel
{
    [Register("AppDelegate")]
    public sealed class AppDelegate : MvxApplicationDelegate
    {
        public override UIWindow Window { get; set; }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            Window = new UIWindow(UIScreen.MainScreen.Bounds);

            var setup = new Setup(this, Window);
            setup.Initialize();

            var startup = Mvx.Resolve<IMvxAppStart>();
            startup.Start();

            Window.MakeKeyAndVisible();

            #if ENABLE_TEST_CLOUD
            Xamarin.Calabash.Start();
            #endif

            #if USE_ANALYTICS
            Firebase.Core.App.Configure();
            Firebase.CrashReporting.Loader.ForceLoad();
            #endif

            return true;
        }
    }
}
