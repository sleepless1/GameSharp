using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Engine.Assets;
using Engine.Interface;

namespace Engine {
	internal partial class CommandConsole {
		private class OutputView : Component {
			private struct Line {
				public Brush TextColor;
				public TextLayout Text;
				public Line(Brush color, TextLayout text) {
					this.TextColor = color;
					this.Text = text;
				}
			}

			private readonly Color BACKGROUND = new Color(24, 24, 24, 198);
			private readonly Color NORMALTEXT = Color.LimeGreen;
			private readonly Color DEBUGTEXT = Color.Yellow;
			private readonly Color ERRORTEXT = Color.Red;
			private readonly string[] _lineSplit = new string[] { Environment.NewLine };

			public RectangleF OutputRectangle;
			private int _currentLine = 0;

			private readonly LinkedList<Line> _buffer = new LinkedList<Line>();
			private FontAsset _font;
			private BrushAsset _backgroundColor;
			private BrushAsset _standardColor;
			private BrushAsset _errorColor;

			private IAssetManager _assetManager;

			public readonly DrawingSizeF Bounds;

			#region Initialization

			public OutputView(DrawingSizeF bounds) {
				Bounds = bounds;
				OutputRectangle.Right = Bounds.Width;
			}

			public void LoadContent(IAssetManager assets) {
				_buffer.Clear();
				_assetManager = assets;
				_font = ToDispose<FontAsset>(assets.LoadFont(FONT_NAME, FONT_SIZE, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal));
				_backgroundColor = ToDispose<BrushAsset>(assets.LoadBrush(BACKGROUND));
				_standardColor = ToDispose<BrushAsset>(assets.LoadBrush(NORMALTEXT));
				_errorColor = ToDispose<BrushAsset>(assets.LoadBrush(ERRORTEXT));
			}

			public void UnloadContent() {
				Clear();
				RemoveAndDispose<FontAsset>(ref _font);
				RemoveAndDispose<BrushAsset>(ref _backgroundColor);
				RemoveAndDispose<BrushAsset>(ref _standardColor);
				RemoveAndDispose<BrushAsset>(ref _errorColor);
				_assetManager = null;
			}

			#endregion

			#region Controls

			public void AddLine(string text = "") {
				string[] lines = text.TrimEnd().Split(_lineSplit, StringSplitOptions.None);

				for(int i = 0, j = lines.Length; i < j; i++)
					_buffer.AddFirst(new Line(_standardColor.Resource, ToDispose<TextLayout>(_assetManager.MakeTextLayout(_font.Resource, lines[i], OutputRectangle.Width, OutputRectangle.Height))));

				while (_buffer.Count > CONSOLE_MAXLINES) {
					TextLayout last = _buffer.Last.Value.Text;
					_buffer.RemoveLast();
					RemoveAndDispose<TextLayout>(ref last);
				}
			}

			public void AddError(string text = "") {
				var line = new Line(_errorColor.Resource, ToDispose<TextLayout>(_assetManager.MakeTextLayout(_font.Resource, text.TrimEnd(), OutputRectangle.Width, OutputRectangle.Height)));

				_buffer.AddFirst(line);
				while (_buffer.Count > CONSOLE_MAXLINES) {
					TextLayout last = _buffer.Last.Value.Text;
					_buffer.RemoveLast();
					RemoveAndDispose<TextLayout>(ref last);
				}
			}

			public void Clear() {
				var node = _buffer.First;
				while (node != null) {
					TextLayout line = node.Value.Text;
					RemoveAndDispose<TextLayout>(ref line);
					node = node.Next;
				}
				_buffer.Clear();
			}

			public void Scroll(int amount) {
				_currentLine += amount;
				if (_currentLine < 0)
					_currentLine = 0;
				else if (_currentLine >= _buffer.Count)
					_currentLine = _buffer.Count - 1;
			}

			#endregion

			public void Draw(RenderTarget renderTarget) {
				renderTarget.FillRectangle(OutputRectangle, _backgroundColor.Resource);

				var bufferedLine = _buffer.First;
				// Move to current line
				for (int i = 0; i < _currentLine; i++)
					bufferedLine = bufferedLine.Next;

				DrawingPointF origin = new DrawingPointF(0f, OutputRectangle.Bottom);
				while (origin.Y > 0f && bufferedLine != null) {
					origin.Y -= bufferedLine.Value.Text.Metrics.Height;
					renderTarget.DrawTextLayout(origin, bufferedLine.Value.Text, bufferedLine.Value.TextColor);
					bufferedLine = bufferedLine.Next;
				}
			}
		}
	}
}
