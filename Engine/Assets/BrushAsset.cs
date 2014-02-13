using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Assets {
	public class BrushAsset : DisposableAsset<Brush> {
		internal BrushAsset(Brush brush)
			: base(brush) {
		}
	}
}
