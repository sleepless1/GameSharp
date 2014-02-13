using Engine.Lua;
using Language.Lua;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public enum NotificationType : byte {
		Message = 0,
		Temporary = 1,
	}

	public class Notification : DrawableControlBase {

		private const float DEFAULT_WIDTH = 384f;
		private const float DEFAULT_HEIGHT = 84f;
		private const float DEFAULT_BUTTON_HEIGHT = 32f;

		public static long TemporaryNotificationDuration = 5000L * 10000L;

		public long Duration = 0L;
		public bool Expires = false;

		private VoidAction _buttonAction;
		public VoidAction ButtonAction { get { return _buttonAction; } set { if (value != null) _buttonAction = value; } }

		public Notification(string message)
			: this(message, new DrawingRectangleF(0f, 0f, DEFAULT_WIDTH, DEFAULT_HEIGHT)) {
		}

		public Notification(string message, float x, float y, float width, float height, NotificationType type = NotificationType.Message)
			: this(message, new DrawingRectangleF(x, y, width, height), type) {
		}

		public Notification(string message, Vector2 position, Vector2 size, NotificationType type = NotificationType.Message)
			: this(message, new DrawingRectangleF(position.X, position.Y, size.X, size.Y), type) {
		}

		[LuaCommandUsage("Creates a new notification window.  new(Message, X, Y, Width, Height)", Lua.Library.GUI.ModuleName)]
		public Notification(LuaValue[] parameters)
			: this(((LuaString)parameters[0]).Text, (float)((LuaNumber)parameters[1]).Number, (float)((LuaNumber)parameters[2]).Number, (float)((LuaNumber)parameters[3]).Number, (float)((LuaNumber)parameters[4]).Number) {
		}


		public Notification(string message, DrawingRectangleF screenArea, NotificationType type = NotificationType.Message)
			: base() {
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(message), "Notification message should not be null or empty.");
			this.IsVisible = true;
			this.IsActive = true;
			this.IsRounded = false;
			this.LayoutOption = LayoutType.Vertical;
			this.ResizeOption = ResizeOptions.WrapChildren;
			this.HorizontalAlignment = Controls.HorizontalAlignment.Center;
			this.VerticalAlignment = Controls.VerticalAlignment.Center;
			this.DrawBackground = true;
			this.DrawBorder = true;
			_buttonAction = () => { this.Close(); };

			Position = screenArea.Position;
			Size = screenArea.Size;

			var label = new Label() {
				Text = message,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			label.Size = Size;
			AddControl(label);

			switch (type) {
				case NotificationType.Message:
					var button = new Button(_buttonAction) {
						Text = "Ok",
						Size = new Vector2(this.Width / 4f, DEFAULT_BUTTON_HEIGHT),
						VerticalAlignment = VerticalAlignment.Top,
						HorizontalAlignment = HorizontalAlignment.Center
					};
					AddControl(button);
					break;

				case NotificationType.Temporary:
					this.Expires = true;
					this.Duration = Notification.TemporaryNotificationDuration;
					break;
			}
		}

		public override void LoadContent(Interface.IAssetManager assetManager) {
			base.LoadContent(assetManager);
			//this.Size = new Vector2(DEFAULT_WIDTH * 1.5f, DEFAULT_HEIGHT * 1.5f);
		}
	}
}
