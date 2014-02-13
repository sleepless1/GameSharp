using Engine.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IHandleMouseButtonReleased : IEngineComponent {
		void OnMouseReleased(object sender, MouseButtonEventArgs args);
	}
}
