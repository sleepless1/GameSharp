using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public enum LayoutType : byte {
		/// <summary>
		/// Controls are positioned side-by-side, with subsequent controls positioned to the right
		/// of previously added controls.  Resizing may be performed depending on other options.
		/// </summary>
		Horizontal,
		/// <summary>
		/// Controls are positioned up and down, with subsequent controls positioned below any previously
		/// added controls.  Resizing may be performed depending on other options.
		/// </summary>
		Vertical,
		/// <summary>
		/// The size and position of the control or it's children will not be automatically adjusted
		/// </summary>
		None
	}
}
