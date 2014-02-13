using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	[Flags]
	public enum FillOptions : byte {
		None = 0x00,
		FillVertical = 0x01,
		FillHorizontal = 0x02
	}
}
