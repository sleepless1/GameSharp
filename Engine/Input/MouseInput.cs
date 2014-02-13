using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Input {
	public struct MouseInput {
		public List<MouseButton> Held;
		public List<MouseButton> Pressed;
		public List<MouseButton> Released;
		public bool DoubleClicked;
		public bool Moved;
		public Vector2 Motion; 
		public Vector2 Position;
		public int ScrollWheel;

		public void Clear() {
			Held.Clear();
			Pressed.Clear();
			Released.Clear();
			DoubleClicked = false;
			Motion = new Vector2();
			Position = new Vector2();
			ScrollWheel = 0;
		}

		public static MouseInput CreateEmpty() {
			var mouse = new MouseInput();
			mouse.Held = new List<MouseButton>();
			mouse.Pressed = new List<MouseButton>();
			mouse.Released = new List<MouseButton>();

			mouse.DoubleClicked = false;
			mouse.Motion = new Vector2();
			mouse.Position = new Vector2();
			mouse.ScrollWheel = 0;
			return mouse;
		}
	}
}
