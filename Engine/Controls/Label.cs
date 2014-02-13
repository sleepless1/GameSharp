using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public class Label : DrawableControlBase {
		public Label(string text = "") {
			this.DrawBackground = false;
			this.DrawBorder = false;
			this.Text = text;
		}
	}
}