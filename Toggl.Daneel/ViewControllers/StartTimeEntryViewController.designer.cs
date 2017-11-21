// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.ViewControllers
{
	[Register ("StartTimeEntryViewController")]
	partial class StartTimeEntryViewController
	{
		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton BillableButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.NSLayoutConstraint BillableButtonWidthConstraint { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.NSLayoutConstraint BottomDistanceConstraint { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIView BottomOptionsSheet { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton CloseButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton DateTimeButton { get; set; }

		[Outlet]
		UIKit.UILabel DescriptionRemainingLengthLabel { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UITextView DescriptionTextView { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton DoneButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton DurationButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton ProjectsButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UITableView SuggestionsTableView { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UIButton TagsButton { get; set; }

		[Outlet]
		[GeneratedCode ("iOS Designer", "1.0")]
		UIKit.UILabel TimeLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (BillableButton != null) {
				BillableButton.Dispose ();
				BillableButton = null;
			}

			if (BillableButtonWidthConstraint != null) {
				BillableButtonWidthConstraint.Dispose ();
				BillableButtonWidthConstraint = null;
			}

			if (BottomDistanceConstraint != null) {
				BottomDistanceConstraint.Dispose ();
				BottomDistanceConstraint = null;
			}

			if (BottomOptionsSheet != null) {
				BottomOptionsSheet.Dispose ();
				BottomOptionsSheet = null;
			}

			if (CloseButton != null) {
				CloseButton.Dispose ();
				CloseButton = null;
			}

			if (DateTimeButton != null) {
				DateTimeButton.Dispose ();
				DateTimeButton = null;
			}

			if (DescriptionRemainingLengthLabel != null) {
				DescriptionRemainingLengthLabel.Dispose ();
				DescriptionRemainingLengthLabel = null;
			}

			if (DescriptionTextView != null) {
				DescriptionTextView.Dispose ();
				DescriptionTextView = null;
			}

			if (DoneButton != null) {
				DoneButton.Dispose ();
				DoneButton = null;
			}

			if (DurationButton != null) {
				DurationButton.Dispose ();
				DurationButton = null;
			}

			if (ProjectsButton != null) {
				ProjectsButton.Dispose ();
				ProjectsButton = null;
			}

			if (SuggestionsTableView != null) {
				SuggestionsTableView.Dispose ();
				SuggestionsTableView = null;
			}

			if (TagsButton != null) {
				TagsButton.Dispose ();
				TagsButton = null;
			}

			if (TimeLabel != null) {
				TimeLabel.Dispose ();
				TimeLabel = null;
			}
		}
	}
}
