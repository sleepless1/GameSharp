using Engine.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IHandleKeyboardPressed : IEngineComponent {
		void OnKeyboardPressed(object sender, KeyboardEventArgs args);
	}
}
