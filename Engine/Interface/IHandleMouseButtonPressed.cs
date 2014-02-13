using Engine.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IHandleMouseButtonPressed : IEngineComponent {
		void OnMousePressed(object sender, MouseButtonEventArgs args);
	}
}
