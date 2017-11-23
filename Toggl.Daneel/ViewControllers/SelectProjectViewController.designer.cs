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
	[Register ("SelectProjectViewController")]
	partial class SelectProjectViewController
	{
		[Outlet]
		UIKit.UIButton CloseButton { get; set; }

		[Outlet]
		UIKit.UIImageView EmptyStateImage { get; set; }

		[Outlet]
		UIKit.UILabel EmptyStateLabel { get; set; }

		[Outlet]
		UIKit.UITableView ProjectsTableView { get; set; }

		[Outlet]
		UIKit.UITextField TextField { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (CloseButton != null) {
				CloseButton.Dispose ();
				CloseButton = null;
			}

			if (ProjectsTableView != null) {
				ProjectsTableView.Dispose ();
				ProjectsTableView = null;
			}

			if (TextField != null) {
				TextField.Dispose ();
				TextField = null;
			}

			if (EmptyStateLabel != null) {
				EmptyStateLabel.Dispose ();
				EmptyStateLabel = null;
			}

			if (EmptyStateImage != null) {
				EmptyStateImage.Dispose ();
				EmptyStateImage = null;
			}
		}
	}
}
