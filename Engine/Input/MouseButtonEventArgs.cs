using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Input {
	public delegate void MouseButtonEventHandler(object sender, MouseButtonEventArgs args);
	public class MouseButtonEventArgs : EventArgs {
		public readonly MouseButton Button;
		public Vector2 Position { get; private set; }
		public bool Handled = false;
		public MouseButtonEventArgs(MouseButton button, Vector2 position) {
			Button = button;
			Position = position;
		}
	}
}
