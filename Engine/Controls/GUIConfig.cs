using Engine;
using Engine.Assets;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Engine.Controls {
    public class GUIConfig : Config {
        private const string CFG_ASSET_CLOSE_BUTTON = "asset_close_button";
        private const string CFG_ASSET_TITLE_FONT = "asset_titlebar_font";
		private const string CFG_ASSET_STANDARD_FONT = "asset_standard_font";
        private const string CFG_ASSET_STANDARD_FONT_SIZE = "asset_standard_font_size";
		private const string CFG_ASSET_LARGE_FONT_SIZE = "asset_large_font_size";
        private const string CFG_WINDOW_OPACITY = "window_opacity";
		private const string CFG_WINDOW_TITLEBAR_HEIGHT = "titlebar_height";
		private const string CFG_WINDOW_CORNER_RADIUS = "corner_radius";

        private const string CFG_WINDOW_FONT_ACTIVE_COLOR = "font_active_color";
        private const string CFG_WINDOW_FONT_INACTIVE_COLOR = "font_inactive_color";
        private const string CFG_WINDOW_ACTIVE_TITLEBAR_COLOR = "active_titlebar_color";
        private const string CFG_WINDOW_INACTIVE_TITLEBAR_COLOR = "inactive_titlebar_color";
        private const string CFG_WINDOW_ACTIVE_BACKGROUND_COLOR = "active_background_color";
		private const string CFG_WINDOW_INACTIVE_BACKGROUND_COLOR = "inactive_background_color";
		private const string CFG_WINDOW_PRESSED_BACKGROUND_COLOR = "pressed_background_color";
		private const string CFG_WINDOW_PRESSED_BORDER_COLOR = "pressed_border_color";
		private const string CFG_WINDOW_ACTIVE_BORDER_COLOR = "active_border_color";
		private const string CFG_WINDOW_INACTIVE_BORDER_COLOR = "inactive_border_color";

        public string WindowCloseButtonAsset = "textures/GUI/buttons/window_close.png";
        public string WindowTitleFont = "TimeBurner";
        public string WindowStandardFont = "ProFontWindows";
        public float WindowStandardFontSize = 12f;
		public float WindowLargeFontSize = 16f;
        public float WindowOpacity = 1.0f;
        public Int32 WindowTitlebarHeight = 18;
		public float WindowCornerRadius = 4f;
        public Color WindowFontActiveColor = Color.White;
        public Color WindowFontInactiveColor = Color.Gray;
        public Color WindowActiveTitlebarColor = Color.DarkGray;
        public Color WindowInactiveTitlebarColor = Color.LightGray;
        public Color WindowActiveBackgroundColor = Color.AntiqueWhite;
		public Color WindowInactiveBackgroundColor = Color.LightGray;
		public Color WindowPressedBackgroundColor = Color.LightSteelBlue;
		public Color WindowPressedBorderColor = Color.DarkSlateBlue;
		public Color WindowActiveBorderColor = Color.Black;
		public Color WindowInactiveBorderColor = Color.Black;

#if DEBUG
		public Color DebuggingColorPink = Color.HotPink;
		public Color DebuggingColorGreen = Color.Green;
		public Color DebuggingColorBlue = Color.Cyan;
#endif

        public GUIConfig()
            : base() {
        }

        public GUIConfig(string file)
            : base(file) {
        }

        protected override void ProcessKeyValuePair(string key, string value) {
            value = value.Replace("\"", "");
			try {
				switch (key) {
					case CFG_ASSET_CLOSE_BUTTON:
						if (_isValidFile(value))
							WindowCloseButtonAsset = value;
						else
							Console.Error.WriteLine("Could not load close button asset: {0}", value);
						break;

					case CFG_ASSET_TITLE_FONT:
						WindowTitleFont = value;
						break;

					case CFG_ASSET_STANDARD_FONT:
						WindowStandardFont = value;
						break;

					case CFG_ASSET_STANDARD_FONT_SIZE:
						WindowStandardFontSize = Convert.ToSingle(value);
						break;

					case CFG_ASSET_LARGE_FONT_SIZE:
						WindowLargeFontSize = Convert.ToSingle(value);
						break;

					case CFG_WINDOW_OPACITY:
						WindowOpacity = Convert.ToSingle(value);
						break;

					case CFG_WINDOW_TITLEBAR_HEIGHT:
						WindowTitlebarHeight = Convert.ToInt32(value);
						break;

					case CFG_WINDOW_CORNER_RADIUS:
						WindowCornerRadius = Convert.ToSingle(value);
						break;

					case CFG_WINDOW_FONT_ACTIVE_COLOR:
						WindowFontActiveColor = _stringToColor(value);
						break;

					case CFG_WINDOW_FONT_INACTIVE_COLOR:
						WindowFontInactiveColor = _stringToColor(value);
						break;

					case CFG_WINDOW_ACTIVE_TITLEBAR_COLOR:
						WindowActiveTitlebarColor = _stringToColor(value);
						break;

					case CFG_WINDOW_INACTIVE_TITLEBAR_COLOR:
						WindowInactiveTitlebarColor = _stringToColor(value);
						break;

					case CFG_WINDOW_ACTIVE_BACKGROUND_COLOR:
						WindowActiveBackgroundColor = _stringToColor(value);
						break;

					case CFG_WINDOW_INACTIVE_BACKGROUND_COLOR:
						WindowInactiveBackgroundColor = _stringToColor(value);
						break;

					case CFG_WINDOW_PRESSED_BACKGROUND_COLOR:
						WindowPressedBackgroundColor = _stringToColor(value);
						break;

					case CFG_WINDOW_PRESSED_BORDER_COLOR:
						WindowPressedBorderColor = _stringToColor(value);
						break;

					case CFG_WINDOW_ACTIVE_BORDER_COLOR:
						WindowActiveBorderColor = _stringToColor(value);
						break;

					case CFG_WINDOW_INACTIVE_BORDER_COLOR:
						WindowInactiveBorderColor = _stringToColor(value);
						break;

					default:
						Console.Error.WriteLine("Unknown symbol while loading GUI configuration: {0}", key);
						break;
				}
			} catch (Exception e) {
				Console.Error.WriteLine("Error while loading GUI configuration file at: {0} = {1}" + Environment.NewLine + "{2}: {3}", key, value, e.GetType().Name, e.Message);
			}
        }

        private Color _stringToColor(string value) {
            string[] values = value.Split(',');
            try {
                return new Color(
                    Convert.ToByte(values[0]),
                    Convert.ToByte(values[1]),
                    Convert.ToByte(values[2]),
                    Convert.ToByte(values[3]));
            } catch (IndexOutOfRangeException e) {
                Console.Error.WriteLine("Invalid color data in GUIConfig: {0}\n{1}", value, e.Message);
                return Color.Pink; // Punishment?
            }
        }

        private bool _isValidFile(string value) {
            var info = new FileInfo(AssetManager.RootDirectory + value);
            return info.Exists;
        }
    }
}
