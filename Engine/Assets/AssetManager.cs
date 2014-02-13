using System;
using System.Linq;
using System.Collections.Generic;
using SharpDX.Direct2D1;
using SharpDX;
using SharpDX.DXGI;
using System.Runtime.InteropServices;
using SharpDX.DirectWrite;

using Engine;
using DWriteFactory = SharpDX.DirectWrite.Factory;
using System.Diagnostics;
using Engine.Assets.FontLoader;
using System.IO;
using System.Collections.Concurrent;
using Engine.Interface;

namespace Engine.Assets {
	public class AssetManager : IAssetManager {

		private struct FontDescription {
			public string Name;
			public float Size;
			public FontDescription(string name, float size) {
				Name = name;
				Size = size;
			}
		}

		private struct BitmapBrushDescription {
			public Bitmap Bitmap;
			public BitmapBrushProperties? BitmapBrushProps;
			public BrushProperties? BrushProps;
			public BitmapBrushDescription(Bitmap bitmap, BitmapBrushProperties? bitmapProps, BrushProperties? brushProps) {
				Bitmap = bitmap;
				BitmapBrushProps = bitmapProps;
				BrushProps = brushProps;
			}
		}

		private static string _rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
		public static string RootDirectory {
			get { return _rootDirectory; }
			set {
				if(!value.EndsWith("/"))
					value += '/';
				_rootDirectory = AppDomain.CurrentDomain.BaseDirectory + '/' + value;
			}
		}
		
		private static readonly ConcurrentDictionary<Color, SolidColorBrush> _solidColorBrushResources = new ConcurrentDictionary<Color, SolidColorBrush>();
		private static readonly ConcurrentDictionary<string, Bitmap> _bitmapResources = new ConcurrentDictionary<string, Bitmap>();
		private static readonly ConcurrentDictionary<FontDescription, TextFormat> _fontResources = new ConcurrentDictionary<FontDescription, TextFormat>();

		private static AssetManager _singleton;

		private ResourceFontCollectionLoader _fontLoader;
		private RenderTarget _renderTarget2D;
        public RenderTarget RenderTarget2D { get { return _renderTarget2D; } }
		public SharpDX.Direct2D1.Factory Factory2D { get { return _renderTarget2D.Factory; } }

		private DWriteFactory _dwriteFactory;

		private const string FallbackFontFamilyName = "Comic Sans MS";
		private FontCollection _embeddedFonts;
		private FontCollection _systemFonts;
		public bool IsDisposed { get; private set; }

		internal AssetManager(RenderTarget renderTarget2D)
			: base() {
			Debug.Assert(renderTarget2D != null, "RenderTarget should not be null");

			if(_singleton != null)
				_singleton.Dispose();

			_renderTarget2D = renderTarget2D;
			_dwriteFactory = new DWriteFactory();
			_singleton = this;
			_loadFontResources();
			IsDisposed = false;
		}

		~AssetManager() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposeManagedResources) {
			IsDisposed = true;
			if (_fontLoader != null && !_fontLoader.IsDisposed)
				_fontLoader.Dispose();

			if (_embeddedFonts != null && !_embeddedFonts.IsDisposed)
				_embeddedFonts.Dispose();

			if (_systemFonts != null && !_systemFonts.IsDisposed)
				_systemFonts.Dispose();

			if (_dwriteFactory != null && !_dwriteFactory.IsDisposed)
				_dwriteFactory.Dispose();

			foreach (Bitmap bitmap in _bitmapResources.Values) {
				if (!bitmap.IsDisposed)
					bitmap.Dispose();
			}
			_bitmapResources.Clear();

			foreach (TextFormat font in _fontResources.Values) {
				if (!font.IsDisposed)
					font.Dispose();
			}
			_fontResources.Clear();

			foreach (SolidColorBrush brush in _solidColorBrushResources.Values) {
				if (!brush.IsDisposed)
					brush.Dispose();
			}
			_solidColorBrushResources.Clear();
		}

		private void _loadFontResources() {
            _fontLoader = new ResourceFontCollectionLoader(_dwriteFactory);
            _embeddedFonts = new FontCollection(_dwriteFactory, _fontLoader, _fontLoader.Key);
			_systemFonts = _dwriteFactory.GetSystemFontCollection(true); 
		}

		TextureAsset IAssetManager.LoadTexture(string file) {
			Bitmap result = null;
			if (_bitmapResources.TryGetValue(file, out result) && !result.IsDisposed)
				return new TextureAsset(result);

			Stream stream;
			if (File.Exists(RootDirectory + file))
				stream = File.OpenRead(RootDirectory + file);
			else if (File.Exists(file))
				stream = File.OpenRead(file);
			else {
				stream = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
						  from name in assembly.GetManifestResourceNames()
						  where String.Compare(file, name, true) != 0
						  select assembly.GetManifestResourceStream(name)).FirstOrDefault();
			}

			if (stream == null) {
				Console.Error.WriteLine("Failed to load bitmap asset: {0}", file);
				// TODO: Have some sort of placeholder texture to feed back, even a single pixel
				return null;
			}

			try {
				using (var newBitmap = new System.Drawing.Bitmap(stream)) {

					var sourceArea = new System.Drawing.Rectangle(0, 0, newBitmap.Width, newBitmap.Height);
					var bitmapProperties = new BitmapProperties(new PixelFormat(Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied));
					var size = new DrawingSize(newBitmap.Width, newBitmap.Height);

					// Transform pixels from BGRA to RGBA
					int stride = newBitmap.Width * sizeof(int);
					using (var tempStream = new DataStream(newBitmap.Height * stride, true, true)) {
						// Lock System.Drawing.Bitmap
						var bitmapData = newBitmap.LockBits(sourceArea, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

						// Convert all pixels 
						for (int y = 0; y < newBitmap.Height; y++) {
							int offset = bitmapData.Stride * y;
							for (int x = 0; x < newBitmap.Width; x++) {
								// Not optimized 
								byte B = Marshal.ReadByte(bitmapData.Scan0, offset++);
								byte G = Marshal.ReadByte(bitmapData.Scan0, offset++);
								byte R = Marshal.ReadByte(bitmapData.Scan0, offset++);
								byte A = Marshal.ReadByte(bitmapData.Scan0, offset++);
								int rgba = R | (G << 8) | (B << 16) | (A << 24);
								tempStream.Write(rgba);
							}

						}
						newBitmap.UnlockBits(bitmapData);
						tempStream.Position = 0;

						try {
							result = new Bitmap(_renderTarget2D, size, tempStream, stride, bitmapProperties);
						} catch (NullReferenceException) {
							throw new AssetLoadException("Graphics device uninitialized");
						}
					}
				}
			} catch (ArgumentException) {
				Console.Error.WriteLine("Invalid data stream while loading bitmap data");
			} finally {
				stream.Dispose();
			}
			_bitmapResources.AddOrUpdate(file, result, (key, oldValue) => {
				if (!oldValue.IsDisposed)
					oldValue.Dispose();
				return result;
			});
			return new TextureAsset(result);
		}

		FontAsset IAssetManager.LoadFont(string assetName, float fontSize, FontWeight weight, FontStyle style, FontStretch stretch) {
			Debug.Assert(!String.IsNullOrEmpty(assetName), "Must provide a valid font name");
			Debug.Assert(fontSize > .9999, "Font size must be larger than 1");
			var desc = new FontDescription(assetName, fontSize);
			TextFormat font;

			if (_fontResources.TryGetValue(desc, out font) && !font.IsDisposed)
				return new FontAsset(font);
			
			FontCollection collection;
			int i = 0;
			if (_embeddedFonts.FindFamilyName(assetName, out i)) {
				collection = _embeddedFonts;
			} else if (_systemFonts.FindFamilyName(assetName, out i)) {
				collection = _systemFonts;
			} else {
				collection = _systemFonts;
				assetName = FallbackFontFamilyName;
			}
			font = new TextFormat(
				_dwriteFactory,
				assetName,
				collection,
				weight,
				style,
				stretch,
				fontSize);

			_fontResources.AddOrUpdate(desc, font, (key, oldValue) => {
				if (!oldValue.IsDisposed)
					oldValue.Dispose();
				return font;
			});
			return new FontAsset(font);
		}

		TextLayout IAssetManager.MakeTextLayout(TextFormat textFormat, string text, float maxWidth, float maxHeight) {
			System.Diagnostics.Debug.Assert(textFormat != null && !textFormat.IsDisposed, "Can not create layout from null or disposed TextFormat");
			return new TextLayout(_dwriteFactory, text, textFormat, maxWidth, maxHeight);
		}

		BrushAsset IAssetManager.LoadBrush(Color color) {
			SolidColorBrush brush;
			if (_solidColorBrushResources.TryGetValue(color, out brush) && !brush.IsDisposed)
				return new BrushAsset(brush);

			brush = new SolidColorBrush(RenderTarget2D, color);
			_solidColorBrushResources.AddOrUpdate(color, brush, (key, oldValue) => {
				if(!oldValue.IsDisposed)
					oldValue.Dispose();
				return brush;
			});
			return new BrushAsset(brush);
		}

		BrushAsset IAssetManager.LoadBrush(Bitmap bitmap, BitmapBrushProperties? bitmapBrushProps, BrushProperties? brushProps) {
			/// TODFO: THis needs to be finished!!!!
			BitmapBrush brush = new BitmapBrush(RenderTarget2D, bitmap, bitmapBrushProps, brushProps);
			return new BrushAsset(brush);
		}
	}

	[Serializable]
	public class AssetLoadException : Exception {
		public AssetLoadException(string msg) : base(msg) {
		}
	}
}

