using Engine.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public class Window : DrawableControlBase {

		private const float DEFAULT_WIDTH = 412f;
		private const float DEFAULT_HEIGHT = 412f;

		public ScrollableContainer Container { get; private set; }

		public Window(string title = "") : base() {
			this.DrawBackground = true;
			this.DrawBorder = true;
			this.Padding = 0f;
			this.Margin = 0f;
			this.LayoutOption = LayoutType.Vertical;
			this.ResizeOption = ResizeOptions.WrapChildren;
			var titlebar = new Titlebar(title);
			titlebar.TextIndent = 4f;
			this.OnResized += (sender, eventargs) => {
				titlebar.Size = new SharpDX.Vector2(eventargs.Vector.X, titlebar.Height);
			};

			base.AddControl(titlebar);
			base.AddControl((Container = new ScrollableContainer() {
				HorizontalAlignment = Controls.HorizontalAlignment.Center,
				VerticalAlignment = Controls.VerticalAlignment.Center,
				LayoutOption = LayoutType.Vertical,
				ResizeOption = ResizeOptions.WrapChildren
			}));
			this.Size = new SharpDX.Vector2(DEFAULT_WIDTH, DEFAULT_HEIGHT);
		}

		public override bool AddControl(IControl control) {
			return Container.AddControl(control);
		}

		public override bool RemoveControl(IControl control) {
			return Container.RemoveControl(control);
		}
	}
}
