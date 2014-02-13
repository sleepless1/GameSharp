using SharpDX;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DMouse = SharpDX.DirectInput.Mouse;

namespace Engine.Input {
	public class Mouse {

		private const long HELD_THRESHOLD = 100L * 10000L;

		private Form _form;
		private DMouse _dMouse;
		private Vector2 _position;
		private Vector2 _motion;
		private MouseState _currentState;

		public MouseInput State { get { return _inputState; } }
		private MouseInput _inputState;
		private List<MouseButton> _currentPressedButtons;
		private List<MouseButton> _lastPressedButtons;
		private Dictionary<MouseButton, long> _heldTiming = new Dictionary<MouseButton, long>();
		private GameTimer _timer;
		internal bool IsPaused = false;

		internal Mouse(Form form, DMouse mouse) {
			System.Diagnostics.Debug.Assert(form != null);
			System.Diagnostics.Debug.Assert(mouse != null);
			_dMouse = mouse;
			_form = form;

			_inputState = MouseInput.CreateEmpty();
			_currentPressedButtons = new List<MouseButton>(5);
			_lastPressedButtons = new List<MouseButton>(5);
			(_timer = new GameTimer()).Start();
			_currentState = new MouseState();
		}

		private void _updatePosition() {
			var oldPos = _position;
			var curPos = _form.PointToClient(Cursor.Position);
			_position = new Vector2(curPos.X, curPos.Y);
			_motion = _position - oldPos;
		}

		internal void Update() {
			try {
				_dMouse.GetCurrentState(ref _currentState);
			} catch (SharpDXException ex) {
				if (ex.ResultCode == ResultCode.InputLost || ex.ResultCode == ResultCode.NotAcquired) {
					try {
						_dMouse.Acquire();
						_dMouse.GetCurrentState(ref _currentState);
					} catch (SharpDXException exc) {
						var result = exc.ResultCode;
						if (result != ResultCode.ReadOnly) {
							Console.Error.WriteLine("Unhandled exception in {0}._processMouse(): {1}", this.GetType().Name, exc.Message);
							throw;
						}
					}
				} else {
					Console.Error.WriteLine("Unhandled exception in {0}._processMouse(): {1}", this.GetType().Name, ex.Message);
					throw;
				}
			} catch (NullReferenceException) {
			}
			_inputState.Clear();
			if (_currentState == null)
				return;

			_updatePosition();
			if (_motion.LengthSquared() > .9f)
				_inputState.Moved = true;
			else
				_inputState.Moved = false;

			_inputState.Position = _position;
			_inputState.Motion = _motion;
			_inputState.ScrollWheel += _currentState.Z;

			_currentPressedButtons.Clear();

			for (int i = 0, j = _currentState.Buttons.Length > (int)MouseButton.X4 ? (int)MouseButton.X4 : _currentState.Buttons.Length;
				i < j; i++) {
				if (_currentState.Buttons[i])
					_currentPressedButtons.Add((MouseButton)i);
			}

			_inputState.Pressed.AddRange(_currentPressedButtons.Where(key => !_lastPressedButtons.Contains(key)));

			long elapsed = _timer.UpdateTicks();
			foreach (var button in _currentPressedButtons.Where(key => _lastPressedButtons.Contains(key))) {
				if (_heldTiming.ContainsKey(button)) {
					if ((_heldTiming[button] += elapsed) > HELD_THRESHOLD)
						_inputState.Held.Add(button);
				} else {
					_heldTiming.Add(button, 0L);
				}
			}
			_inputState.Released.AddRange(_lastPressedButtons.Where(key => !_currentPressedButtons.Contains(key)));

			foreach (var button in _inputState.Released) 
				_heldTiming.Remove(button);

			_lastPressedButtons.Clear();
			_lastPressedButtons.AddRange(_currentPressedButtons);
		}
	}
}
