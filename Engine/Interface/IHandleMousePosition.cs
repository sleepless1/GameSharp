using Engine.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IHandleMousePosition : IEngineComponent {
		void OnPosition(object sender, MouseVectorEventArgs args);
	}
}
