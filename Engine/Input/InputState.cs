namespace Engine.Input {
	public class InputState {

		public MouseInput Mouse = MouseInput.CreateEmpty();
		public KeyboardInput Keyboard = KeyboardInput.CreateEmpty();

		public void Clear() {
			Mouse.Pressed.Clear();
			Mouse.Held.Clear();
			Mouse.Released.Clear();

			Keyboard.Pressed.Clear();
			Keyboard.Held.Clear();
			Keyboard.Released.Clear();
			Keyboard.Alt = false;
			Keyboard.Ctrl = false;
			Keyboard.Shift = false;
		}
	}
}

