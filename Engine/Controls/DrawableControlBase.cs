using Engine.Assets;
using Engine.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public class DrawableControlBase : ControlContainer, IDisposable {
		// Graphics objects
		private IAssetManager _assetManager;

		// Draw options
		public bool IsVisible = true;
		public bool IsPressed = false;
		public bool DrawBorder = true;
		public bool DrawBackground = true;
		public bool DrawText = true;
		public float Opacity = ControlManager.Config.WindowOpacity;

		// Colors
		public Color ActiveBackgroundColor = ControlManager.Config.WindowActiveBackgroundColor;
		public Color InactiveBackgroundColor = ControlManager.Config.WindowInactiveBackgroundColor;
		public Color ActiveBorderColor = ControlManager.Config.WindowActiveBorderColor;
		public Color InactiveBorderColor = ControlManager.Config.WindowInactiveBorderColor;
		public Color PressedBackgroundColor = ControlManager.Config.WindowPressedBackgroundColor;
		public Color PressedBorderColor = ControlManager.Config.WindowPressedBorderColor;
		public Color ActiveFontColor = ControlManager.Config.WindowFontActiveColor;
		public Color InactiveFontColor = ControlManager.Config.WindowFontInactiveColor;
		
		// Textures
		private TextureAsset _activeTexture;
		public TextureAsset ActiveTexture { get { return _activeTexture; } }
		public string ActiveTexturePath;

		private TextureAsset _inactiveTexture;
		public TextureAsset InactiveTexture { get { return _inactiveTexture; } }
		public string InactiveTexturePath;

		private TextureAsset _pressedTexture;
		public TextureAsset PressedTexture { get { return _pressedTexture; } }
		public string PressedTexturePath;

		// Brushes
		private BrushAsset _activeBackgroundBrush;
		public BrushAsset ActiveBackgroundBrush { get { return _activeBackgroundBrush; } }
		private BrushAsset _inactiveBackgroundBrush;
		public BrushAsset InactiveBackgroundBrush { get { return _inactiveBackgroundBrush; } }
		private BrushAsset _activeBorderBrush;
		public BrushAsset ActiveBorderBrush { get { return _activeBorderBrush; } }
		private BrushAsset _inactiveBorderBrush;
		public BrushAsset InactiveBorderBrush { get { return _inactiveBorderBrush; } }
		private BrushAsset _pressedBackgroundBrush;
		public BrushAsset PressedBackgroundBrush { get { return _pressedBackgroundBrush; } }
		private BrushAsset _pressedBorderBrush;
		public BrushAsset PressedBorderBrush { get { return _pressedBorderBrush; } }
		private BrushAsset _activeFontBrush;
		public BrushAsset ActiveFontBrush { get { return _activeFontBrush; } }
		private BrushAsset _inactiveFontBrush;
		public BrushAsset InactiveFontBrush { get { return _inactiveFontBrush; } }

		// Selected assets
		protected BrushAsset CurrentBorderBrush;
		protected BrushAsset CurrentBackgroundBrush;
		protected BrushAsset CurrentFontBrush;
		protected TextureAsset CurrentBitmap;

		// Rectangle geometry
		private bool _isRounded = true;
		public bool IsRounded {
			get { return _isRounded; }
			set {
				if (value != _isRounded)
					_recalculateGeometry = true;
				_isRounded = value;
			}
		}
		private bool _recalculateGeometry = true;
		private Geometry _backgroundGeometry;
		public Geometry BackgroundGeometry { get { return _backgroundGeometry; } }

		// Text data
		private bool _hasTextChanged = false;
		private string _text = String.Empty;
		public string Text {
			get { return _text; }
			set {
				if (value != null)
					_text = value;
				_hasTextChanged = true;
			}
		}
		public float TextIndent = 0f;
		public TextSize TextSize = TextSize.Normal;
		public TextAlignment TextAlignment = TextAlignment.Center;
		public ParagraphAlignment ParagraphAlignment = ParagraphAlignment.Center;
		public string FontName = ControlManager.Config.WindowStandardFont;
		public FontAsset Font { get; private set; }
		public TextLayout RenderedText { get; private set; }

		#region Initialization/Disposal

		public DrawableControlBase()
			: base() {
			OnMoved += (sender, args) => { _recalculateGeometry = true; };
			OnResized += (sender, args) => {
				_recalculateGeometry = true;
				_hasTextChanged = true;
			};
		}

		~DrawableControlBase() {
			Dispose(false);
		}

		public bool IsDisposed { get; private set; }
		/// <summary>
		/// Releases resources held by the control
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases resources held by the control
		/// </summary>
		/// <param name="disposing">Whether managed resources should be released as well as native resources</param>
		protected virtual void Dispose(bool disposing) {
			if (IsDisposed) return;
			IsDisposed = true;
			if (RenderedText != null && !RenderedText.IsDisposed)
				RenderedText.Dispose();

			if (_backgroundGeometry != null)
				_backgroundGeometry.Dispose();

			if (_activeBackgroundBrush != null)
				_activeBackgroundBrush.Dispose();

			if (_activeBorderBrush != null)
				_activeBorderBrush.Dispose();

			if (_inactiveBackgroundBrush != null)
				_inactiveBackgroundBrush.Dispose();

			if (_inactiveBorderBrush != null)
				_inactiveBorderBrush.Dispose();

			if (_pressedBackgroundBrush != null)
				_pressedBackgroundBrush.Dispose();
			
			if (_pressedBorderBrush != null) 
				_pressedBorderBrush.Dispose();

			if (_activeFontBrush != null)
				_activeFontBrush.Dispose();

			if (_inactiveFontBrush != null)
				_inactiveFontBrush.Dispose();

			if (_activeTexture != null)
				_activeTexture.Dispose();

			if (_inactiveTexture != null)
				_inactiveTexture.Dispose();

			if (_pressedTexture != null)
				_pressedTexture.Dispose();

			if (Font != null)
				Font.Dispose();
		}

		public override void LoadContent(IAssetManager assetManager) {
			base.LoadContent(assetManager);
			_assetManager = assetManager;

			switch (TextSize) {
				case TextSize.Large:
					Font = assetManager.LoadFont(FontName, ControlManager.Config.WindowLargeFontSize, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal);
					break;

				case TextSize.Normal:
					goto default;

				default:
					Font = assetManager.LoadFont(FontName, ControlManager.Config.WindowStandardFontSize, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal);
					break;
			}

			if (!String.IsNullOrEmpty(ActiveTexturePath)) {
				_activeTexture = assetManager.LoadTexture(ActiveTexturePath);

				if (String.IsNullOrEmpty(InactiveTexturePath))
					_inactiveTexture = _activeTexture;
				else
					_inactiveTexture = assetManager.LoadTexture(InactiveTexturePath);

				if (String.IsNullOrEmpty(PressedTexturePath))
					_pressedTexture = _activeTexture;
				else
					_pressedTexture = assetManager.LoadTexture(PressedTexturePath);
			}

			if (_activeTexture == null) {
				_activeBackgroundBrush = assetManager.LoadBrush(this.ActiveBackgroundColor);
			} else {
				_activeBackgroundBrush = assetManager.LoadBrush(_activeTexture.Resource, new BitmapBrushProperties() {
					ExtendModeX = ExtendMode.Wrap,
					ExtendModeY = ExtendMode.Wrap,
					InterpolationMode = BitmapInterpolationMode.Linear
				}, null);
			}
			_activeBorderBrush = assetManager.LoadBrush(this.ActiveBorderColor);

			if (_inactiveTexture == null) {
				_inactiveBackgroundBrush = assetManager.LoadBrush(this.InactiveBackgroundColor);
			} else {
				_inactiveBackgroundBrush = assetManager.LoadBrush(_inactiveTexture.Resource, new BitmapBrushProperties() {
					ExtendModeX = ExtendMode.Wrap,
					ExtendModeY = ExtendMode.Wrap,
					InterpolationMode = BitmapInterpolationMode.Linear
				}, null);
			}
			_inactiveBorderBrush = assetManager.LoadBrush(this.InactiveBorderColor);
			_pressedBackgroundBrush = assetManager.LoadBrush(this.PressedBackgroundColor);
			_pressedBorderBrush = assetManager.LoadBrush(this.PressedBorderColor);
			_activeFontBrush = assetManager.LoadBrush(this.ActiveFontColor);
			_inactiveFontBrush = assetManager.LoadBrush(this.InactiveFontColor);
		}

		public override void UnloadContent() {
			_activeBackgroundBrush.Dispose();
			_activeBorderBrush.Dispose();
			_inactiveBackgroundBrush.Dispose();
			_inactiveBorderBrush.Dispose();
			_pressedBackgroundBrush.Dispose();
			_pressedBorderBrush.Dispose();
			_activeFontBrush.Dispose();
			_inactiveFontBrush.Dispose();

			if(_activeTexture != null)
				_activeTexture.Dispose();

			if (_inactiveTexture != null)
				_inactiveTexture.Dispose();

			if (_pressedTexture != null)
				_pressedTexture.Dispose();

			Font.Dispose();

			_assetManager = null;

			// Some are created by this object, however.
			// Anything here should also be addressed in Dispose()
			if (RenderedText != null && !RenderedText.IsDisposed)
				RenderedText.Dispose();
			RenderedText = null;

			if (_backgroundGeometry != null && !_backgroundGeometry.IsDisposed)
				_backgroundGeometry.Dispose();
			_backgroundGeometry = null;

			base.UnloadContent();
		}

		#endregion

		public override void Update(long ticks) {
			base.Update(ticks);
			if (_recalculateGeometry) {
				if (_backgroundGeometry != null && !_backgroundGeometry.IsDisposed)
					_backgroundGeometry.Dispose();

				var rect = new DrawingRectangleF(0f, 0f, Width, Height);
				if (IsRounded) {
					_backgroundGeometry = new RoundedRectangleGeometry(_assetManager.Factory2D, new RoundedRectangle() {
						Rect = rect,
						RadiusX = ControlManager.Config.WindowCornerRadius,
						RadiusY = ControlManager.Config.WindowCornerRadius
					});
				} else {
					_backgroundGeometry = new RectangleGeometry(_assetManager.Factory2D, rect);
				}
				_recalculateGeometry = false;
			}
			if (_hasTextChanged && !String.IsNullOrEmpty(_text)) {
				if (RenderedText != null && !RenderedText.IsDisposed)
					RenderedText.Dispose();

				RenderedText = _assetManager.MakeTextLayout(Font.Resource, _text, Width, Height);
				RenderedText.TextAlignment = this.TextAlignment;
				RenderedText.ParagraphAlignment = this.ParagraphAlignment;
				_hasTextChanged = false;
			}
			if (IsActive) {
				CurrentFontBrush = _activeFontBrush;
				if (IsPressed) {
					CurrentBackgroundBrush = _pressedBackgroundBrush;
					CurrentBorderBrush = _pressedBorderBrush;
					CurrentBitmap = _pressedTexture;
				} else {
					CurrentBackgroundBrush = _activeBackgroundBrush;
					CurrentBorderBrush = _activeBorderBrush;
					CurrentBitmap = _activeTexture;
				}
			} else {
				CurrentBackgroundBrush = _inactiveBackgroundBrush;
				CurrentBorderBrush = _inactiveBorderBrush;
				CurrentFontBrush = _inactiveFontBrush;
				CurrentBitmap = _inactiveTexture;
			}
		}

		public override void Render(RenderTarget renderTarget) {
			if (!IsVisible) return;

			CurrentBorderBrush.Resource.Opacity = this.Opacity;
			CurrentBackgroundBrush.Resource.Opacity = this.Opacity;

			renderTarget.Transform = this.Transform;

			//TODO: Check if we need a second rect for this...
			renderTarget.PushAxisAlignedClip(this.ClippingRectangle, AntialiasMode.Aliased);

			try {
				if (DrawBackground) {
					renderTarget.FillGeometry(_backgroundGeometry, CurrentBackgroundBrush.Resource);
					//if (CurrentBitmap != null)
					//	renderTarget.DrawBitmap(CurrentBitmap.Resource, Opacity, BitmapInterpolationMode.Linear);
				}

				if (DrawBorder)
					renderTarget.DrawGeometry(_backgroundGeometry, CurrentBorderBrush.Resource);

				if (DrawText && !String.IsNullOrEmpty(_text))
					renderTarget.DrawTextLayout(new DrawingPointF(TextIndent, 0f), RenderedText, CurrentFontBrush.Resource);

				base.Render(renderTarget);
			} finally {
				renderTarget.PopAxisAlignedClip();
			}
		}

		public override void Close() {
			Dispose();
			base.Close();
		}
	}
}
