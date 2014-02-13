using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Engine.Assets {
	public class TextureAsset : DisposableAsset<Bitmap> {
		private readonly RectangleF? _sourceRectangle;
		internal TextureAsset(Bitmap bitmap, RectangleF? sourceRectangle = null)
			: base(bitmap) {
				_sourceRectangle = sourceRectangle;
		}

		public void Render(RenderTarget renderTarget, float opacity = 1.0f, RectangleF? destinationRectangle = null) {
			renderTarget.DrawBitmap(this.Resource, destinationRectangle, opacity, BitmapInterpolationMode.Linear, _sourceRectangle);
		}
	}
}
