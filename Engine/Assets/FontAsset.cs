using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Assets {
	public class FontAsset : DisposableAsset<TextFormat> {
		internal FontAsset(TextFormat font)
			: base(font) {
		}
	}
}
