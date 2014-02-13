using Engine.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public delegate void ControlAddedEventHandler(object sender, ControlAddedEventArgs eventArgs);
	public class ControlAddedEventArgs : EventArgs {
		public readonly IControl AddedControl;
		public ControlAddedEventArgs(IControl control) {
			AddedControl = control;
		}
	}
}
