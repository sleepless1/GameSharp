using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Input {

	public delegate void MouseVectorEventHandler(object sender, MouseVectorEventArgs args);

	public class MouseVectorEventArgs : EventArgs {
		public Vector2 Vector { get; private set; }

		public bool Handled = false;

		public MouseVectorEventArgs(Vector2 motion)
			: base() {
			Vector = motion;
		}
	}
}
