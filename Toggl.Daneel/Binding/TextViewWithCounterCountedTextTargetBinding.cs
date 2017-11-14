using System;
using MvvmCross.Binding;
using MvvmCross.Binding.Bindings.Target;
using Toggl.Daneel.Views;

namespace Toggl.Daneel.Binding
{
    public sealed class TextViewWithCounterCountedTextTargetBinding : MvxTargetBinding<TextViewWithCounter, string>
    {
        public const string BindingName = "CountedText";

        public override MvxBindingMode DefaultMode => MvxBindingMode.TwoWay;

        public TextViewWithCounterCountedTextTargetBinding(TextViewWithCounter target)
            : base(target)
        {
            target.CountedTextChanged += onCountedTextChanged;
        }

        protected override void SetValue(string value)
        {
            Target.CountedText = value;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (!isDisposing) return;
            Target.CountedTextChanged -= onCountedTextChanged;
        }

        private void onCountedTextChanged(object sender, EventArgs e)
        {
            FireValueChanged(Target.CountedText);
        }
    }
}
