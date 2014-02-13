using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public class Button : ClickableControl {

		public Button(VoidAction leftClick, VoidAction rightClick = null)
			: base(leftClick, rightClick) {
		}

	}
}
