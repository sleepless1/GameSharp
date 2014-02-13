using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Input {
	public delegate void KeyboardEventHandler(object sender, KeyboardEventArgs args);
	public class KeyboardEventArgs : EventArgs {
		public readonly Key Key;
		public readonly bool IsShiftPressed;
		public readonly bool IsCtrlPressed;
		public readonly bool IsAltPressed;
		
		public bool Handled = false;

		public KeyboardEventArgs(Key key, bool shift = false, bool ctrl = false, bool alt = false) {
			Key = key;
			IsShiftPressed = shift;
			IsCtrlPressed = ctrl;
			IsAltPressed = alt;
		}		
	}
}
