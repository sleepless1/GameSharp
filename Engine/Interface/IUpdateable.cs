using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IUpdateable : IEngineComponent {
		void Update();
	}
}
