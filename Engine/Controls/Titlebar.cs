using SharpDX;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Engine.Controls {
	internal class Titlebar : DrawableControlBase {
		Button _closeButton;
		internal Titlebar(string title = "")
			: base() {
			this.Margin = 0f;
			this.Padding = 0f;
			this.LayoutOption = LayoutType.Horizontal;
			this.VerticalAlignment = VerticalAlignment.Top;
			this.TextAlignment = TextAlignment.Leading;
			this.DrawBackground = false;
			this.DrawBorder = false;
			this.Text = title;
			//Set impossibly wide to force it's parent to resize to it's width
			this.Size = new SharpDX.Vector2(float.MaxValue, ControlManager.Config.WindowTitlebarHeight);

			// Setup close button
			_closeButton = new Button(() => {
				Debug.Assert(HasParent, "A titlebar should always have a parent control!");
				this.Parent.Close();
			});
			_closeButton.Size = new SharpDX.Vector2(Size.Y, Size.Y);
			_closeButton.ActiveTexturePath = ControlManager.Config.WindowCloseButtonAsset;
			_closeButton.VerticalAlignment = VerticalAlignment.Center;
			_closeButton.HorizontalAlignment = HorizontalAlignment.Center;
			//_closeButton.DrawBackground = false;
			_closeButton.DrawBorder = false;
			_closeButton.IsRounded = false;
			_closeButton.ActiveBackgroundColor = _closeButton.InactiveBackgroundColor = Color.Transparent;
			this.AddControl(_closeButton);
			this.OnResized += (sender, eventargs) => {
				_closeButton.Position = new SharpDX.Vector2(this.Width - _closeButton.Width, 0f);
			};
		}

		public override bool ProcessIntent(ControlIntent intent, object data) {
			Debug.Assert(HasParent, "A titlebar should always have a parent control!");
			bool handled = false;
			switch (intent) {
				case ControlIntent.Released:
					goto default; // See if the close button is interested

				case ControlIntent.Held:
					if (_closeButton.ScreenSpace.Contains((Vector2)data))
						goto default;
					else if (ScreenSpace.Contains((Vector2)data))
						State = ControlState.Moving;
					handled = true;
					break;

				case ControlIntent.Move:
					if (State == ControlState.Moving) {
						Vector2 newPosition = Parent.Position + (Vector2)data;
						DrawingPoint mouseCorrection = new DrawingPoint();
						if (newPosition.X > GameApplication.ScreenSize.Width - Parent.Width) {
							mouseCorrection.X = (int)((GameApplication.ScreenSize.Width - Parent.Width) - newPosition.X);
							newPosition.X = GameApplication.ScreenSize.Width - Parent.Width;
						} else if (newPosition.X < 0) {
							mouseCorrection.X = (int)-newPosition.X;
							newPosition.X = 0;
						}
						if (newPosition.Y > GameApplication.ScreenSize.Height - Parent.Height) {
							mouseCorrection.Y = (int)((GameApplication.ScreenSize.Height - Parent.Height) - newPosition.Y);
							newPosition.Y = GameApplication.ScreenSize.Height - Parent.Height;
						} else if (newPosition.Y < 0) {
							mouseCorrection.Y = (int)-newPosition.Y;
							newPosition.Y = 0;
						}
						Cursor.Position = new System.Drawing.Point(
							Cursor.Position.X + mouseCorrection.X,
							Cursor.Position.Y + mouseCorrection.Y);
						Parent.Position = Parent.Position + (Vector2)data;
						handled = true;
					}
					break;

				default:
					handled = base.ProcessIntent(intent, data);
					break;
			}
			return handled;
		}

		public override void LoadContent(Interface.IAssetManager assetManager) {
			base.LoadContent(assetManager);
		}

		public override void Render(SharpDX.Direct2D1.RenderTarget renderTarget) {
			base.Render(renderTarget);
		}
	}
}
