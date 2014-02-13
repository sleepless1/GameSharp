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
		private class InputView : Component {

			#region Fields

			private readonly Color BACKGROUND = Color.Black;
			private readonly Color TEXT = Color.Green;

			public RectangleF OutputRectangle;
			public float LineSpacing { get { return _cursor == null ? 0f : _cursor.Metrics.Height; } }

			private int _cursorPosition = 0;
			private float _lineSpacing = 0f;
			private bool _hasTextChanged;
			private IAssetManager _assetManager;
			private BrushAsset _backgroundColor;
			private BrushAsset _textColor;
			private FontAsset _font;
			private TextLayout _cursor;
			private TextLayout _currentLayout;
			private DrawingPointF _cursorDrawingPoint;
			private readonly StringBuilder _text = new StringBuilder(1024);
			private readonly LinkedList<string> _history = new LinkedList<string>();
			private readonly OutputView _outputView;
			private LinkedListNode<string> _historyItem = null;
			private long _cursorAnimCounter = 0L;
			private bool _drawCursor = true;
			private bool _multilineMode = false;

			#endregion

			#region Initialization/Disposal

			public InputView(OutputView outputView) {
				System.Diagnostics.Debug.Assert(outputView != null, "OutputView can not be null");
				_outputView = outputView;
				OutputRectangle.Bottom = OutputRectangle.Top = _outputView.OutputRectangle.Bottom;
				OutputRectangle.Right = _outputView.OutputRectangle.Right;
			}

			public void LoadContent(IAssetManager assets) {
				_assetManager = assets;
				_font = ToDispose<FontAsset>(assets.LoadFont(FONT_NAME, FONT_SIZE, FontWeight.Normal, FontStyle.Normal, FontStretch.Normal));
				_currentLayout = ToDispose<TextLayout>(assets.MakeTextLayout(_font.Resource, "", float.MaxValue, float.MaxValue));
				_cursor = ToDispose<TextLayout>(assets.MakeTextLayout(_font.Resource, CURSOR, float.MaxValue, float.MaxValue));
				_lineSpacing = _cursor.Metrics.Height;
				_backgroundColor = ToDispose<BrushAsset>(assets.LoadBrush(BACKGROUND));
				_textColor = ToDispose<BrushAsset>(assets.LoadBrush(TEXT));
				Reset();
			}

			public void UnloadContent() {
				RemoveAndDispose<FontAsset>(ref _font);
				RemoveAndDispose<TextLayout>(ref _currentLayout);
				RemoveAndDispose<TextLayout>(ref _cursor);
				RemoveAndDispose<BrushAsset>(ref _backgroundColor);
				RemoveAndDispose<BrushAsset>(ref _textColor);
				_assetManager = null;
			}

			#endregion

			#region Update/Draw

			public void Update(long ticks) {
				_cursorAnimCounter += ticks;
				if (_cursorAnimCounter >= CURSOR_BLINK_DURATION) {
					_cursorAnimCounter -= CURSOR_BLINK_DURATION;
					_drawCursor = !_drawCursor;
				}

				// Calculate draw position for input cursor
				float hitX, hitY;
				_currentLayout.HitTestTextPosition(_text.Length - _cursorPosition, false, out hitX, out hitY);
				_cursorDrawingPoint = new DrawingPointF(OutputRectangle.Left + hitX, OutputRectangle.Top + hitY);

				if (!_hasTextChanged) return;  // If the text hasn't changed, nothing below needs to be done
				RemoveAndDispose<TextLayout>(ref _currentLayout);

				_currentLayout = ToDispose<TextLayout>(_assetManager.MakeTextLayout(_font.Resource, _text.ToString(), OutputRectangle.Width, float.MaxValue));
				OutputRectangle.Top = _outputView.OutputRectangle.Bottom;
				OutputRectangle.Bottom = OutputRectangle.Top + _currentLayout.Metrics.Height;

				// Resize the output view as necessary (input field can grow)
				if (OutputRectangle.Bottom > _outputView.Bounds.Height) {
					_outputView.OutputRectangle.Bottom = _outputView.Bounds.Height - _currentLayout.Metrics.Height;
					OutputRectangle.Top = _outputView.OutputRectangle.Bottom;
					OutputRectangle.Bottom = OutputRectangle.Top + _currentLayout.Metrics.Height;
				}
				_hasTextChanged = false;
			}

			public void Draw(RenderTarget renderTarget) {
				renderTarget.FillRectangle(OutputRectangle, _backgroundColor.Resource);
				renderTarget.DrawTextLayout(new DrawingPointF(OutputRectangle.X, OutputRectangle.Y), _currentLayout, _textColor.Resource);
				if (_drawCursor)
					renderTarget.DrawTextLayout(_cursorDrawingPoint, _cursor, _textColor.Resource);
			}

			#endregion

			#region Controls

			public void AddText(char text) {
				AddText(text);
			}

			public void AddText(string text) {
				_text.Insert(_text.Length - _cursorPosition, text);
				_hasTextChanged = true;
			}

			public void CursorLeft() {
				if (++_cursorPosition > _text.Length - COMMAND_PROMPT_LENGTH)
					_cursorPosition = _text.Length - COMMAND_PROMPT_LENGTH;
			}

			public void CursorRight() {
				if (--_cursorPosition <= 0)
					_cursorPosition = 0;
			}

			public void Reset() {
				ClearText();
				ClearHistory();
			}

			public void ClearText() {
				_text.Clear();
				_text.Append(COMMAND_PROMPT);
				_outputView.OutputRectangle.Bottom = _outputView.Bounds.Height * .66f;
				_cursorPosition = 0;
				_hasTextChanged = true;
			}

			public void ClearHistory() {
				_history.Clear();
				_historyItem = null;
			}

			public void PreviousCommand() {
				if (_history.Count == 0 || _historyItem == _history.Last) 
					return;
				
				if (_historyItem == null)
					_historyItem = _history.First;
				else
					_historyItem = _historyItem.Next;

				ClearText();
				AddText(_historyItem.Value);
			}

			public void NextCommand() {
				if (_historyItem == null) return;

				ClearText();
				if (_historyItem == _history.First) {
					_historyItem = null;
					return;
				}

				_historyItem = _historyItem.Previous;
				AddText(_historyItem.Value);
			}

			public string Enter() {
				_hasTextChanged = true;
				if (_multilineMode) {
					_text.Append(Environment.NewLine);
					return null;
				} else {
					_text.Remove(0, COMMAND_PROMPT_LENGTH);
					_historyItem = null;
					_history.AddFirst(_text.ToString());

					while (_history.Count > COMMAND_HISTORY_COUNT)
						_history.RemoveLast();
					ClearText();
					return _history.First.Value;
				}
			}

			public void StartOfLine() {
				_cursorPosition = _text.Length - COMMAND_PROMPT_LENGTH;
			}

			public void EndOfLine() {
				_cursorPosition = 0;
			}

			public void Backspace() {
				if (_cursorPosition < _text.Length - COMMAND_PROMPT_LENGTH) {
					_text.Remove(_text.Length - 1 - _cursorPosition, 1);
					if (_cursorPosition > _text.Length - COMMAND_PROMPT_LENGTH)
						_cursorPosition = 0;
					_hasTextChanged = true;
				}
			}

			public void Delete() {
				if (_cursorPosition > 0) {
					_text.Remove(_text.Length - _cursorPosition, 1);
					_cursorPosition--;
					_hasTextChanged = true;
				}
			}

			public string ToggleMultilineMode() {
				_multilineMode = !_multilineMode;
				return Enter();
			}

			#endregion
		}
	}
}
