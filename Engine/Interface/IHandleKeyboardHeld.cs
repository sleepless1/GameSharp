using Engine.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IHandleKeyboardHeld : IEngineComponent {
		void OnKeyboardHeld(object sender, KeyboardEventArgs args);
	}
}
