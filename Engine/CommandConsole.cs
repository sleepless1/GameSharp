using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Language.Lua;
using Engine.Lua;
using System.Diagnostics;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX;
using Engine.Assets;
using System.Windows.Forms;
using Engine.Input;
using Key = SharpDX.DirectInput.Key;
using Engine.Interface;

namespace Engine {
	/// <summary>
	/// Provides run-time access to the lua environment and available functions
	/// </summary>
	internal partial class CommandConsole : Component, IHandleKeyboardPressed, IHandleKeyboardHeld, IHandleMouseScrollWheel, IUpdateable, ILoadable {

		private const string CURSOR = "_";
		private const string COMMAND_PROMPT = "> ";
		private const int COMMAND_PROMPT_LENGTH = 2;
		private const int INPUT_BUFFER_SIZE = 32;
		private const int CONSOLE_MAXLINES = 500;
		private const int COMMAND_HISTORY_COUNT = 50;
		private const long CURSOR_BLINK_DURATION = 400L * 10000L;
		private const int PAGE_SCROLL_AMOUNT = 1;

		private const string FONT_NAME = "Bitstream Vera Sans Mono";
		//private const string FONT_NAME = "Resagnicto";
		//private const string FONT_NAME = "White Rabbit";
		private const float FONT_SIZE = 12f;

		private readonly LuaEnvironment _lua;
		private readonly GameApplication.OutputStreams _streams;
		private readonly GameTimer _timer = new GameTimer();

		private StringBuilder _inputBuilder = new StringBuilder(INPUT_BUFFER_SIZE);

		private OutputView _outputView;
		private InputView _inputView;

		internal bool IsEnabled { get { return _enabled; } }
		private bool _enabled = false;

		internal CommandConsole(LuaEnvironment lua, DrawingSizeF screenSize, GameApplication.OutputStreams streams, int maxLines = 500) {
			Debug.Assert(lua != null, "Lua environment can not be null");
			Debug.Assert(streams != null, "Streams can not be null");
			_streams = streams;
			_lua = lua;
			_outputView = ToDispose<OutputView>(new OutputView(screenSize));
			_inputView = ToDispose<InputView>(new InputView(_outputView));
		}

		void IUpdateable.Update() {
			string text;
			if (_streams.TryReadStandard(out text))
				_outputView.AddLine(text);

			if (_streams.TryReadError(out text))
				_outputView.AddError(text);

			if (_inputBuilder.Length > 0) {
				_inputView.AddText(_inputBuilder.ToString());
				_inputBuilder.Clear();
			}
			_inputView.Update(_timer.UpdateTicks());
		}

		public void Render(RenderTarget renderTarget) {
			if (!_enabled)
				return;

			renderTarget.BeginDraw();

			renderTarget.Transform = Matrix3x2.Identity;
			_outputView.Draw(renderTarget);
			_inputView.Draw(renderTarget);

			renderTarget.EndDraw();
		}

		void ILoadable.LoadContent(IAssetManager assetManager) {
			_outputView.LoadContent(assetManager);
			_inputView.LoadContent(assetManager);
		}

		void ILoadable.UnloadContent() {
			_outputView.UnloadContent();
			_inputView.UnloadContent();
		}

		void IHandleKeyboardPressed.OnKeyboardPressed(object sender, KeyboardEventArgs args) {
			if (args.Handled) return;
			args.Handled = _handleKeypress(args.Key, args.IsShiftPressed);
		}

		void IHandleKeyboardHeld.OnKeyboardHeld(object sender, KeyboardEventArgs args) {
			if (args.Handled) return;
			args.Handled = _handleKeypress(args.Key, args.IsShiftPressed);
		}

		private bool _handleKeypress(Key key, bool isShiftPressed) {
			bool handled = false;
			if (key == Key.Grave) {
				if (_enabled = !_enabled) {
					_timer.Start();
				} else {
					_timer.Stop();
				}
				return true;
			}

			if (!_enabled)
				return handled;

			handled = true;
			char c;
			if (Keyboard.KeypressToChar(key, isShiftPressed, out c)) {
				_inputBuilder.Append(c);
			} else {
				switch (key) {
					case Key.Left:
						_inputView.CursorLeft();
						break;

					case Key.Right:
						_inputView.CursorRight();
						break;

					case Key.UpArrow:
						_inputView.PreviousCommand();
						break;

					case Key.Down:
						_inputView.NextCommand();
						break;

					case Key.PageDown:
						_outputView.Scroll(-PAGE_SCROLL_AMOUNT);
						break;

					case Key.PageUp:
						_outputView.Scroll(PAGE_SCROLL_AMOUNT);
						break;

					case Key.Delete:
						_inputView.Delete();
						break;

					case Key.Back:
						_inputView.Backspace();
						break;

					case Key.Home:
						_inputView.StartOfLine();
						break;

					case Key.End:
						_inputView.EndOfLine();
						break;

					case Key.Return:
						string newline;
						if (isShiftPressed)
							newline = _inputView.ToggleMultilineMode();
						else
							newline = _inputView.Enter();

						if (newline == null) break;

						_outputView.AddLine();
						_outputView.AddLine(COMMAND_PROMPT + newline);
						_outputView.AddLine();
						if (newline != string.Empty)
							_lua.Interpreter(newline);
						break;

					default:
						handled = false;
						break;
				}
			}
			return handled;
		}

		void IHandleMouseScrollWheel.OnScrolled(object sender, MouseScrollEventArgs args) {
			if (!_enabled) return;

			_outputView.Scroll(args.ScrollAmount / 4);
			args.Handled = true;
		}
	}
}
