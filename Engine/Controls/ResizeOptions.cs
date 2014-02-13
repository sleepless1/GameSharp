using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public enum ResizeOptions : byte {
		/// <summary>
		/// The control will expand if not large enough to accomodate children + margin & padding values,
		/// but it will not shrink if too large for it's contents (as with WrapChildren).
		/// </summary>
		ExpandForChildren,
		/// <summary>
		/// The parent control will expand as with the ExpandForChildren option, however,
		/// the control will also shrink to eliminate extra space remaining.
		/// </summary>
		WrapChildren,
		/// <summary>
		/// The control will not resize, children will be clipped at the parent's
		/// boundaries, depending upon the childs layout and alignment options.
		/// </summary>
		None
	}
}
