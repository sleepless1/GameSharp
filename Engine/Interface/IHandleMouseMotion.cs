using Engine.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IHandleMouseMotion : IEngineComponent {
		void OnMotion(object sender, MouseVectorEventArgs args);
	}
}
