using Engine.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IHandleMouseScrollWheel : IEngineComponent {
		void OnScrolled(object sender, MouseScrollEventArgs args);
	}
}
