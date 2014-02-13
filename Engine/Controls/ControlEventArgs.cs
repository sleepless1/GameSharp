using Engine.Interface;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public delegate void ControlEventHandler(object sender, ControlEventArgs args);
	public class ControlEventArgs : EventArgs {
		public readonly Vector2 Vector;
		public ControlEventArgs(Vector2 vector) {
			Vector = vector;
		}
	}
}
