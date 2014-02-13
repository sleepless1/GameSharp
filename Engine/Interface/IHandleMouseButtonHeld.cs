using Engine.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IHandleMouseButtonHeld : IEngineComponent {
		void OnMouseHeld(object sender, MouseButtonEventArgs args);
	}
}
