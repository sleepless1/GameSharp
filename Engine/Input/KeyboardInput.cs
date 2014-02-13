using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Input {
	public struct KeyboardInput {
		public List<Key> Held;
		public List<Key> Pressed;
		public List<Key> Released;
		public bool Shift;
		public bool Alt;
		public bool Ctrl;

		public static KeyboardInput CreateEmpty() {
			var keys = new KeyboardInput();
			keys.Held = new List<Key>();
			keys.Pressed = new List<Key>();
			keys.Released = new List<Key>();
			keys.Shift = false;
			keys.Alt = false;
			keys.Ctrl = false;
			return keys;
		}

		public static void Clear(ref KeyboardInput input) {
			input.Held.Clear();
			input.Pressed.Clear();
			input.Released.Clear();
			input.Shift = false;
			input.Alt = false;
			input.Ctrl = false;
		}
	}
}
