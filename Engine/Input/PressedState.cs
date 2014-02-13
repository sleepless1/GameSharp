using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Input {
	public enum PressedState : byte {
		Pressed,
		Released,
		Held,
		Idle
	}
}
