using SharpDX;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DKeyboard = SharpDX.DirectInput.Keyboard;

namespace Engine.Input {
	public class Keyboard {
		private const long HELD_THRESHOLD = 250L * 10000L;
		private const long HELD_REPEAT_DIVISOR = 5L;

		private DKeyboard _dKeyboard;
		public KeyboardInput State { get { return _inputState; } }
		private KeyboardInput _inputState;
		private List<Key> _lastPressedKeys;
		private GameTimer _timer;
		private Dictionary<Key, long> _heldThreshold = new Dictionary<Key, long>();
		private KeyboardState _currentDirectInputState;

		internal Keyboard(DKeyboard keyboard) {
			System.Diagnostics.Debug.Assert(keyboard != null);
			_dKeyboard = keyboard;
			_inputState = KeyboardInput.CreateEmpty();
			_lastPressedKeys = new List<Key>();
			(_timer = new GameTimer()).Start();
			_currentDirectInputState = new KeyboardState();
		}

		internal void Update() {
			KeyboardInput.Clear(ref _inputState);

			try {
				_dKeyboard.GetCurrentState(ref _currentDirectInputState);
			} catch (SharpDXException ex) {
				var result = ex.ResultCode;
				if (result == ResultCode.InputLost || result == ResultCode.NotAcquired) {
					try {
						_dKeyboard.Acquire();
						_dKeyboard.GetCurrentState(ref _currentDirectInputState);
					} catch (SharpDXException exc) {
						result = exc.ResultCode;
						if (result != ResultCode.ReadOnly) {
							Console.Error.WriteLine("Unhandled exception in {0}._processKeyboard(): {1}", this.GetType().Name, exc.Message);
							throw;
						}
					}
				} else {
					Console.Error.WriteLine("Unhandled exception in {0}._processKeyboard(): {1}", this.GetType().Name, ex.Message);
					throw;
				}
			}
			if (_currentDirectInputState == null)
				return;

			long elapsed = _timer.UpdateTicks();
			var currentPressed = _currentDirectInputState.PressedKeys;

			_inputState.Alt = _currentDirectInputState.IsPressed(Key.LeftAlt) || _currentDirectInputState.IsPressed(Key.RightAlt);
			_inputState.Shift = _currentDirectInputState.IsPressed(Key.LeftShift) || _currentDirectInputState.IsPressed(Key.RightShift);
			_inputState.Ctrl = _currentDirectInputState.IsPressed(Key.LeftControl) || _currentDirectInputState.IsPressed(Key.RightControl);

			_inputState.Pressed.Clear();
			_inputState.Pressed.AddRange(currentPressed.Where(key => !_lastPressedKeys.Contains(key)));
			_inputState.Held.Clear();
			_inputState.Held.AddRange(currentPressed.Where((key) => {
				if (!_lastPressedKeys.Contains(key)) {
					_heldThreshold.Remove(key);
					return false;
				}

				long duration;
				if (_heldThreshold.TryGetValue(key, out duration)) {
					duration += elapsed;
					if (duration > HELD_THRESHOLD) {
						_heldThreshold[key] = duration - (HELD_THRESHOLD / HELD_REPEAT_DIVISOR);
						return true;
					}
					_heldThreshold[key] = duration;
				} else {
					duration = elapsed;
					_heldThreshold.Add(key, duration);
				}
				return false;
			}));
			_inputState.Released.Clear();
			_inputState.Released.AddRange(_lastPressedKeys.Where(key => !currentPressed.Contains(key)));

			_lastPressedKeys.Clear();
			_lastPressedKeys.AddRange(currentPressed);
		}

		#region Static helper functions

		/// <summary>
		/// Converts a DirectInput.Key value to a char
		/// </summary>
		/// <param name="key">DirectInput.Key</param>
		/// <param name="character">out char</param>
		/// <returns>True if an appropriate character exists for the keypress</returns>
		public static bool KeypressToChar(Key key, bool isShiftPressed, out char character) {
			bool keyFound = true;
			switch (key) {
				case Key.A:
					character = isShiftPressed ? 'A' : 'a';
					break;

				case Key.B:
					character = isShiftPressed ? 'B' : 'b';
					break;

				case Key.C:
					character = isShiftPressed ? 'C' : 'c';
					break;

				case Key.D:
					character = isShiftPressed ? 'D' : 'd';
					break;

				case Key.E:
					character = isShiftPressed ? 'E' : 'e';
					break;

				case Key.F:
					character = isShiftPressed ? 'F' : 'f';
					break;

				case Key.G:
					character = isShiftPressed ? 'G' : 'g';
					break;

				case Key.H:
					character = isShiftPressed ? 'H' : 'h';
					break;

				case Key.I:
					character = isShiftPressed ? 'I' : 'i';
					break;

				case Key.J:
					character = isShiftPressed ? 'J' : 'j';
					break;

				case Key.K:
					character = isShiftPressed ? 'K' : 'k';
					break;

				case Key.L:
					character = isShiftPressed ? 'L' : 'l';
					break;

				case Key.M:
					character = isShiftPressed ? 'M' : 'm';
					break;

				case Key.N:
					character = isShiftPressed ? 'N' : 'n';
					break;

				case Key.O:
					character = isShiftPressed ? 'O' : 'o';
					break;

				case Key.P:
					character = isShiftPressed ? 'P' : 'p';
					break;

				case Key.Q:
					character = isShiftPressed ? 'Q' : 'q';
					break;

				case Key.R:
					character = isShiftPressed ? 'R' : 'r';
					break;

				case Key.S:
					character = isShiftPressed ? 'S' : 's';
					break;

				case Key.T:
					character = isShiftPressed ? 'T' : 't';
					break;

				case Key.U:
					character = isShiftPressed ? 'U' : 'u';
					break;

				case Key.V:
					character = isShiftPressed ? 'V' : 'v';
					break;

				case Key.W:
					character = isShiftPressed ? 'W' : 'w';
					break;

				case Key.X:
					character = isShiftPressed ? 'X' : 'x';
					break;

				case Key.Y:
					character = isShiftPressed ? 'Y' : 'y';
					break;

				case Key.Z:
					character = isShiftPressed ? 'Z' : 'z';
					break;

				case Key.D1:
					character = isShiftPressed ? '!' : '1';
					break;

				case Key.D2:
					character = isShiftPressed ? '@' : '2';
					break;

				case Key.D3:
					character = isShiftPressed ? '#' : '3';
					break;

				case Key.D4:
					character = isShiftPressed ? '$' : '4';
					break;

				case Key.D5:
					character = isShiftPressed ? '%' : '5';
					break;

				case Key.D6:
					character = isShiftPressed ? '^' : '6';
					break;

				case Key.D7:
					character = isShiftPressed ? '&' : '7';
					break;

				case Key.D8:
					character = isShiftPressed ? '*' : '8';
					break;

				case Key.D9:
					character = isShiftPressed ? '(' : '9';
					break;

				case Key.D0:
					character = isShiftPressed ? ')' : '0';
					break;

				case Key.Minus:
					character = isShiftPressed ? '_' : '-';
					break;

				case Key.Equals:
					character = isShiftPressed ? '+' : '=';
					break;

				case Key.Tab:
					character = '\t';
					break;

				case Key.LeftBracket:
					character = isShiftPressed ? '{' : '[';
					break;

				case Key.RightBracket:
					character = isShiftPressed ? '}' : ']';
					break;

				case Key.Semicolon:
					character = isShiftPressed ? ':' : ';';
					break;

				case Key.Apostrophe:
					character = isShiftPressed ? '"' : '\'';
					break;

				case Key.Backslash:
					character = isShiftPressed ? '|' : '\\';
					break;

				case Key.Comma:
					character = isShiftPressed ? '<' : ',';
					break;

				case Key.Period:
					character = isShiftPressed ? '>' : '.';
					break;

				case Key.Slash:
					character = isShiftPressed ? '?' : '/';
					break;

				case Key.Space:
					character = ' ';
					break;

				default:
					character = ' ';
					keyFound = false;
					break;
			}
			return keyFound;
		}

		#endregion
	}
}
