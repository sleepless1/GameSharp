using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Input {
	public delegate void MouseScrollEventHandler(object sender, MouseScrollEventArgs args);
	public class MouseScrollEventArgs {
		public bool Handled = false;
		public readonly int ScrollAmount;
		public MouseScrollEventArgs(int amount) {
			ScrollAmount = amount;
		}
	}
}
