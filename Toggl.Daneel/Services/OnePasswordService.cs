using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AgileBits;
using Foundation;
using MvvmCross.iOS.Views.Presenters;
using MvvmCross.Platform;
using Toggl.Foundation.MvvmCross.Services;
using LoginHandler = AgileBits.OnePasswordLoginDictionaryCompletionBlock;

namespace Toggl.Daneel.Services
{
    public sealed class OnePasswordService : NSObject, IPasswordManagerService
    {
        public bool IsAvailable => OnePasswordExtension.SharedExtension.IsAppExtensionAvailable;

        public IObservable<PasswordManagerResult> GetLoginInformation()
        {
            return Observable.Create<PasswordManagerResult>(observer =>
            {
                var presenter = Mvx.Resolve<IMvxIosViewPresenter>() as MvxIosViewPresenter;

                OnePasswordExtension.SharedExtension.FindLoginForURLString(
                    "https://www.toggl.com", 
                    presenter.MasterNavigationController, 
                    this, 
                    getOnePasswordHandler(observer)
                );

                return Disposable.Empty;
            });
        }

        private LoginHandler getOnePasswordHandler(IObserver<PasswordManagerResult> observer) => (dict, ex) =>
        {
            if (dict != null && dict.Any())
            {
                var email = dict[AppExtensionLoginDictionarykeys.UsernameKey] as NSString;
                var password = dict[AppExtensionLoginDictionarykeys.PasswordKey] as NSString;

                var info = new PasswordManagerResult(email, password);
                observer.OnNext(info);
            }
            else
            {
                observer.OnNext(PasswordManagerResult.None);
            }

            observer.OnCompleted();
        };
    }
}
