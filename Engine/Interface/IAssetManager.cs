using Engine.Assets;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IAssetManager : IDisposable {
		RenderTarget RenderTarget2D { get; }
		SharpDX.Direct2D1.Factory Factory2D { get; }

		FontAsset LoadFont(string assetName, float fontSize, FontWeight weight, FontStyle style, FontStretch stretch);
		TextLayout MakeTextLayout(TextFormat font, string text, float maxWidth, float maxHeight);
		TextureAsset LoadTexture(string asset);
		BrushAsset LoadBrush(Color color);
		BrushAsset LoadBrush(Bitmap bitmap, BitmapBrushProperties? bitmapBrushProps, BrushProperties? brushProps);
	}
}
