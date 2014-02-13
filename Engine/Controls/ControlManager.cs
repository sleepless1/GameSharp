using Engine.Assets;
using Engine.Input;
using Engine.Interface;
using Engine.Lua;
using Language.Lua;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Engine.Controls {
	public class ControlManager : Component, IControlManager {
		private const string CONFIG_PATH = "config/gui.cfg";

		private struct FontDescription {
			public string Name;
			public float Size;
			public FontDescription(string name, float size) {
				Name = name;
				Size = size;
			}
		}

		#region Fields/Properties

		public static GUIConfig Config;
		private IAssetManager _assetManager;
		private bool _isLoaded = false;

		private readonly GameTimer _timer = new GameTimer();
		private static readonly ConcurrentLinkedList<IControl> _controlLayers = new ConcurrentLinkedList<IControl>();
		private IControl First { get { return _controlLayers.First.Value; } }

		#endregion

		#region Initialization/Disposal

		static ControlManager() {
			Config = new GUIConfig(CONFIG_PATH);
		}

		void ILoadable.LoadContent(IAssetManager assetManager) {

			_assetManager = assetManager;
			
			for (var node = _controlLayers.First; node != null; node = node.Next)
				node.Value.LoadContent(this);

			_timer.Start();
			_isLoaded = true;
		}

		void ILoadable.UnloadContent() {
			_assetManager = null;
			
			for (var node = _controlLayers.First; node != null; node = node.Next)
				node.Value.UnloadContent();

			_timer.Reset();
			_isLoaded = false;
		}

		#endregion

		#region Control management

		public bool AddControl(IControl control) {
			if (_controlLayers.Count > 0)
				_controlLayers.First.Value.IsActive = false;

			if (!_controlLayers.TryAddFirst(control)) {
				Console.Error.WriteLine("Failed to acquire control lock. {0} control not added.", control.GetType().Name);
				return false;
			}
			if (_isLoaded)
				control.LoadContent(this);
			control.IsActive = true;
			control.OnClosed += _closedControlHandler;
			_alignControl(control);
			return true;
		}

		private void _alignControl(IControl control) {
			_sanitizeControl(control);
			Vector2 newPosition = control.Position;

			switch (control.VerticalAlignment) {
				case VerticalAlignment.None:
					goto default;

				case VerticalAlignment.Top:
					newPosition.Y = 0;
					break;

				case VerticalAlignment.Center:
					newPosition.Y = (GameApplication.ScreenSize.Height - control.Height) / 2f;
					break;

				case VerticalAlignment.Bottom:
					newPosition.Y = GameApplication.ScreenSize.Height - control.Height;
					break;

				default:
					break;
			}

			switch (control.HorizontalAlignment) {
				case HorizontalAlignment.None:
					goto default;

				case HorizontalAlignment.Left:
					newPosition.X = 0;
					break;

				case HorizontalAlignment.Center:
					newPosition.X = (GameApplication.ScreenSize.Width - control.Width) / 2f;
					break;

				case HorizontalAlignment.Right:
					newPosition.X = GameApplication.ScreenSize.Width - control.Width;
					break;

				default:
					// same as above...
					break;
			}
			control.Position = newPosition;
		}

		/// <summary>
		/// Ensures that a control is not larger than the rendering target
		/// </summary>
		/// <param name="control"></param>
		private void _sanitizeControl(IControl control) {
			Vector2 newSize = control.Size;
			bool resized = false;
			if (control.Width > GameApplication.ScreenSize.Width) {
				resized = true;
				newSize.X = GameApplication.ScreenSize.Width;
			}
			if (control.Height > GameApplication.ScreenSize.Height) {
				resized = true;
				newSize.Y = GameApplication.ScreenSize.Height;
			}
			if (resized)
				control.Size = newSize;
		}

		public bool RemoveControl(IControl control) {
			Debug.Assert(control != null, "Can not remove a null control");
			if (!_controlLayers.TryRemove(control))
				return false;
			
			control.OnClosed -= _closedControlHandler;
			control.Close();

			return true;
		}

		private void _closedControlHandler(object sender, ControlEventArgs args) {
			RemoveControl((IControl)sender);
		}

		public void SetNewFocus(IControl newFocus) {
			Debug.Assert(newFocus != null, "Should never need to set a null focus");
			if (newFocus == First) {
				First.IsActive = true;
				return;
			}

			if (_controlLayers.Contains(newFocus) && !_controlLayers.TryRemove(newFocus)) {
				Console.Error.WriteLine("Could not remove control from layer list, aborting new focus");
				return;
			}
			if (_controlLayers.Count > 0)
				_controlLayers.First.Value.IsActive = false;

			if (!_controlLayers.TryAddFirst(newFocus)) {
				Console.Error.WriteLine("Could not acquire control lock, new focus control leaked.");
				return;
			}
			newFocus.IsActive = true;
		}

		#endregion

		#region Update/Render

		void IUpdateable.Update() {
			long elapsed = _timer.UpdateTicks();
			for (var node = _controlLayers.First; node != null; node = node.Next)
				node.Value.Update(elapsed);
		}

		/// <summary>
		/// Not implemented in interface to more directly manage draw order
		/// plus it makes it's own begin/end draw calls
		/// </summary>
		/// <param name="renderTarget">The Direct2d render target</param>
		public void Render(RenderTarget renderTarget) {
			if (_controlLayers.Count == 0) return;
			renderTarget.BeginDraw();
			// Draws in reverse
			for (var node = _controlLayers.Last; node != null; node = node.Previous)
				node.Value.Render(renderTarget);

			renderTarget.EndDraw();
		}

		#endregion

		#region IAssetManager implementation

		SharpDX.Direct2D1.Factory IAssetManager.Factory2D { get { return _assetManager.Factory2D; } }
		RenderTarget IAssetManager.RenderTarget2D { get { return _assetManager.RenderTarget2D; } }

		TextureAsset IAssetManager.LoadTexture(string path) {
			return _assetManager.LoadTexture(path);
		}

		FontAsset IAssetManager.LoadFont(string name, float size, FontWeight weight, FontStyle style, FontStretch stretch) {
			return _assetManager.LoadFont(name, size, weight, style, stretch);
		}

		BrushAsset IAssetManager.LoadBrush(Color color) {
			return _assetManager.LoadBrush(color);
		}

		BrushAsset IAssetManager.LoadBrush(Bitmap bitmap, BitmapBrushProperties? bitmapProps, BrushProperties? brushProps) {
			return _assetManager.LoadBrush(bitmap, bitmapProps, brushProps);
		}

		TextLayout IAssetManager.MakeTextLayout(TextFormat font, string text, float maxWidth, float maxHeight) {
			return _assetManager.MakeTextLayout(font, text, maxWidth, maxHeight);
		}

		#endregion

		#region Lua commands

		[LuaCommand("Adds a control to the system")]
		private static LuaValue addcontrol(LuaValue[] parameters) {
			try {
				IControl toAdd = parameters[0].Value as IControl;
				GameApplication.ControlManager.AddControl(toAdd);
			} catch (IndexOutOfRangeException) {
				Console.Error.WriteLine("Insufficient parameters.");
			} catch (NullReferenceException) {
				Console.Error.WriteLine("Invalid parameters");
			}

			return LuaNil.Nil;
		}

		[LuaCommand("Removes a control from the system")]
		private static LuaValue removecontrol(LuaValue[] parameters) {
			try {
				IControl toRemove = parameters[0].Value as IControl;
				GameApplication.ControlManager.RemoveControl(toRemove);
			} catch (IndexOutOfRangeException) {
				Console.Error.WriteLine("Insufficient parameters.");
			} catch (NullReferenceException) {
				Console.Error.WriteLine("Invalid parameters");
			}

			return LuaNil.Nil;
		}

		#endregion

		#region MouseHandler implementation

		void IHandleMouseButtonPressed.OnMousePressed(object sender, MouseButtonEventArgs args) {
			if (args.Handled || _controlLayers.Count == 0) return;

			if (args.Button != MouseButton.Left && args.Button != MouseButton.Right)
				return; // Not a button press we're interested in

			// Set first control inactive by default
			First.IsActive = false;

			// Now try to set an active control
			for (var node = _controlLayers.First; node != null; node = node.Next) {
				if (node.Value.ScreenSpace.Contains(args.Position)) {
					SetNewFocus(node.Value);
					args.Handled = true;
					break;
				}
			}
		}

		void IHandleMouseButtonHeld.OnMouseHeld(object sender, MouseButtonEventArgs args) {
			if (args.Handled || _controlLayers.Count == 0) return;

			if (First.IsActive) {
				switch (args.Button) {
					case MouseButton.Left:
						args.Handled = First.ProcessIntent(ControlIntent.Held, args.Position);
						break;

					case MouseButton.Right:
						args.Handled = First.ProcessIntent(ControlIntent.AltHeld, args.Position);
						break;
				}
			} else {
				//(this as IMouseHandler).OnPressed(args);
			}
		}

		void IHandleMouseButtonReleased.OnMouseReleased(object sender, MouseButtonEventArgs args) {
			//Reasons we might not care
			if (args.Handled || _controlLayers.Count == 0 || !First.IsActive) return;
			
			if (First.State != ControlState.Idle) {
				First.State = ControlState.Idle;
			} else if (args.Button == MouseButton.Left) {
				args.Handled = First.ProcessIntent(ControlIntent.Released, args.Position);
			} else if (args.Button == MouseButton.Right) {
				args.Handled = First.ProcessIntent(ControlIntent.AltReleased, args.Position);
			}
		}

		void IHandleMousePosition.OnPosition(object sender, MouseVectorEventArgs args) {
		}

		void IHandleMouseMotion.OnMotion(object sender, MouseVectorEventArgs args) {
			if (args.Handled || _controlLayers.Count == 0) return;

			if (First.IsActive && First.State != ControlState.Idle)
				args.Handled = First.ProcessIntent(ControlIntent.Move, args.Vector);

		}

		#endregion

		#region KeyboardHandler implementation

		void IHandleKeyboardPressed.OnKeyboardPressed(object sender, KeyboardEventArgs args) {
			if (args.Handled || _controlLayers.Count == 0) return;
			//throw new NotImplementedException();
		}

		void IHandleKeyboardHeld.OnKeyboardHeld(object sender, KeyboardEventArgs args) {
			if (args.Handled || _controlLayers.Count == 0) return;
			//throw new NotImplementedException();
		}

		void IHandleKeyboardReleased.OnKeyboardReleased(object sender, KeyboardEventArgs args) {
			if (args.Handled || _controlLayers.Count == 0) return;
			//throw new NotImplementedException();
		}

		#endregion
	}
}
