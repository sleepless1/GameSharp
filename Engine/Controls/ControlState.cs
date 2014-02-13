using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public enum ControlState : byte {
		Idle = 0,
		Moving,
		ResizingUp,
		ResizingDown,
		ResizingLeft,
		ResizingRight,
		ResizingUpRight,
		ResizingUpLeft,
		ResizingDownRight,
		ResizingDownLeft,
	}
}
