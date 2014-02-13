using System;

namespace Engine {
	public class ApplicationConfig : Config {
		private const string CFG_WIDTH = "window_width";
		private const string CFG_HEIGHT = "window_height";
		private const string CFG_VSYNC = "vsync";
		private const string CFG_FULLSCREEN = "fullscreen";

		public int Width = 1024;
		public int Height = 768;
		public bool VSync = false;
		public bool FullScreen = false;

		public ApplicationConfig() : base() {
		}

		public ApplicationConfig(string path) : base(path) {
		}

        protected override void ProcessKeyValuePair(string key, string value) {
			if(key == CFG_WIDTH)
				Width = Convert.ToInt32(value);
			else if(key == CFG_HEIGHT)
				Height = Convert.ToInt32(value);
			else if(key == CFG_VSYNC)
				VSync = Convert.ToBoolean(value);
			else if(key == CFG_FULLSCREEN)
				FullScreen = Convert.ToBoolean(value);
		}
	}
}