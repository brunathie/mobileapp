using System;
using MvvmCross.Binding;
using MvvmCross.Binding.Bindings.Target;
using UIKit;

public sealed class TextFieldPlaceholderTargetBinding : MvxTargetBinding<UITextField, string>
{
    public const string BindingName = "Placeholder";

    public override MvxBindingMode DefaultMode => MvxBindingMode.OneWay;

    public TextFieldPlaceholderTargetBinding(UITextField target)
        : base(target)
    {
    }

    protected override void SetValue(string value)
    {
        Target.Placeholder = value;
    }
}
