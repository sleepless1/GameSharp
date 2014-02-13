using Engine.Assets;
using Engine.Controls;
using Engine.Input;
using Engine.Interface;
using Engine.Lua;
using Language.Lua;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D10;
using SharpDX.DirectInput;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Threading;
using System.Windows.Forms;
using Device1 = SharpDX.Direct3D10.Device1;
using DKeyboard = SharpDX.DirectInput.Keyboard;
using DMouse = SharpDX.DirectInput.Mouse;
using DriverType = SharpDX.Direct3D10.DriverType;
using Factory2D = SharpDX.Direct2D1.Factory;
using FactoryDXGI = SharpDX.DXGI.Factory;
using Keyboard = Engine.Input.Keyboard;
using Mouse = Engine.Input.Mouse;

namespace Engine {
	public enum GameRunType : byte {
		Synchronous,
		Asynchronous 
	}

	public abstract partial class GameApplication : SharpDX.Component {

		#region Fields/Properties

		private static GameApplication _singleton;
		public static int MainThreadId { get { return _singleton._mainThreadId; } }
		public static int UpdateThreadId { get { return _singleton._updateThreadId; } }

		public static float FramesPerSecond { get; private set; }
		public static float UpdatesPerSecond { get; private set; }
		public static DrawingSizeF ScreenSize { get; private set; }
		public static ControlManager ControlManager { get { return _singleton._controlManager; } }

		private long _timeAcc;
		private uint _fpsCount, _upsCount;
		private volatile bool _isRunning = false;
		private bool _vsync = false, _isClosed = false, _doRender = true, _isLoaded = false;
		private readonly int _mainThreadId;
		private int _updateThreadId = -1;
		private GameTimer _timer = new GameTimer();
		private Thread _updateThread;
		private ControlManager _controlManager;
		private CommandConsole _commandConsole;
		private OutputStreams _streams;
		private DirectInput _directInput;
		private DKeyboard _dKeyboard;
		private DMouse _dMouse;
		private AssetManager _assetManager;
		private Device1 _device;
		private SwapChain _swapChain;
		private Texture2D _backBuffer;
		private RenderTargetView _backBufferRenderTargetView;
		private FactoryDXGI _factoryDXGI;

		protected readonly string _formTitle;
		protected ApplicationConfig _appConfiguration;
		protected RenderForm _form;
		protected Viewport Viewport;
		protected Factory2D Factory2D;
		protected RenderTarget RenderTarget2D;

		protected LuaEnvironment Lua { get; private set; }
		protected Keyboard Keyboard { get; private set; }
		protected Mouse Mouse { get; private set; }
		protected IntPtr DisplayHandle { get { return _form.Handle; } }
		protected System.Drawing.Size RenderingSize { get { return _form.ClientSize; } }
		public Device1 Device { get { return _device; } }
		public ApplicationConfig Config { get { return _appConfiguration; } }

		#endregion

		#region Events

		// Mouse events
		public static event MouseButtonEventHandler OnMousePressed;
		public static event MouseButtonEventHandler OnMouseHeld;
		public static event MouseButtonEventHandler OnMouseReleased;
		public static event MouseVectorEventHandler OnMouseMoved;
		public static event MouseVectorEventHandler OnMousePosition;
		public static event MouseScrollEventHandler OnMouseScroll;

		// Keyboard events
		public static event KeyboardEventHandler OnKeyboardPressed;
		public static event KeyboardEventHandler OnKeyboardHeld;
		public static event KeyboardEventHandler OnKeyboardReleased;

		#endregion

		#region Initialization/Disposal

		public GameApplication(string configPath = "config/config.cfg", string formTitle = "Default Form Title")
			: base() {
			if(_singleton != null)
				_singleton.Exit();
			_singleton = this;
			_mainThreadId = Thread.CurrentThread.ManagedThreadId;
			
			_streams = ToDispose<OutputStreams>(new OutputStreams());

			_formTitle = formTitle;
			_appConfiguration = configPath == null ? new ApplicationConfig() : new ApplicationConfig(configPath);
			_initializeForm();
			_initializeGraphics();
			Lua = new LuaEnvironment();
			_commandConsole = ToDispose<CommandConsole>(new CommandConsole(Lua, new DrawingSizeF(Viewport.Width, Viewport.Height), _streams));
			RegisterEngineComponent(_commandConsole);
			_controlManager = ToDispose<ControlManager>(new ControlManager());
			RegisterEngineComponent(_controlManager);
			OnUpdate += Update;
			OnRender += Render;
			OnLoadContent += LoadContent;
			OnUnloadContent += UnloadContent;
		}

		protected override void Dispose(bool disposeManagedResources) {
			if (_isLoaded)
				_unloadContent();

			base.Dispose(disposeManagedResources);
		}

		/// <summary>
		///   In a derived class, implements logic to initialize the application.
		/// </summary>
		protected virtual void Initialize() {
		}

		private void _initializeInputs() {
			Console.Write("Initializing inputs... ");

			_directInput = ToDispose<DirectInput>(new DirectInput());

			_dKeyboard = ToDispose<DKeyboard>(new DKeyboard(_directInput));
			_dKeyboard.Properties.BufferSize = 256;
			_dKeyboard.SetCooperativeLevel(_form, CooperativeLevel.Foreground | CooperativeLevel.Exclusive);
			Keyboard = new Keyboard(_dKeyboard);

			_dMouse = ToDispose<DMouse>(new DMouse(_directInput));
			_dMouse.Properties.AxisMode = DeviceAxisMode.Relative;
			_dMouse.SetCooperativeLevel(_form, CooperativeLevel.Foreground | CooperativeLevel.NonExclusive);
			Mouse = new Mouse(_form, _dMouse);
			Console.WriteLine("done.");
		}

		/// <summary>
		/// Create the form.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		private void _initializeForm() {
			Console.Write("Initializing form... ");

			_form = ToDispose<RenderForm>(new RenderForm(_formTitle) {
				ClientSize = new System.Drawing.Size(_appConfiguration.Width, _appConfiguration.Height)
			});
			_form.FormClosing += _handleFormClosing;
			Console.WriteLine("done.");
		}

		private void _initializeGraphics() {
			Console.Write("Initializing graphic device... ");

			var desc = new SwapChainDescription() {
				BufferCount = 1,
				ModeDescription = new ModeDescription(
					_appConfiguration.Width,
					_appConfiguration.Height,
					new Rational(60, 1),
					Format.R8G8B8A8_UNorm),
				IsWindowed = !_appConfiguration.FullScreen,
				OutputHandle = DisplayHandle,
				SampleDescription = new SampleDescription(1, 0),
				SwapEffect = SwapEffect.Discard,
				Usage = Usage.RenderTargetOutput
			};

			Device1.CreateWithSwapChain(
				DriverType.Hardware,
#if DEBUG
 DeviceCreationFlags.BgraSupport | DeviceCreationFlags.Debug | DeviceCreationFlags.SingleThreaded,
#else
				DeviceCreationFlags.BgraSupport,
#endif
 desc,
				out _device,
				out _swapChain);

			if (_device == null)
				throw new SharpDXException("Failed to initialize graphics device.");

			if (_swapChain == null)
				throw new SharpDXException("Failed to initialize swap chain.");

			ToDispose<Device1>(_device);
			ToDispose<SwapChain>(_swapChain);
			Factory2D = ToDispose<Factory2D>(new Factory2D());

			_factoryDXGI = ToDispose<FactoryDXGI>(_swapChain.GetParent<FactoryDXGI>());
			_factoryDXGI.MakeWindowAssociation(DisplayHandle, WindowAssociationFlags.IgnoreAll);

			_backBuffer = ToDispose<Texture2D>(Texture2D.FromSwapChain<Texture2D>(_swapChain, 0));
			_backBufferRenderTargetView = ToDispose<RenderTargetView>(new RenderTargetView(_device, _backBuffer));

			Viewport = new Viewport(0, 0, _appConfiguration.Width, _appConfiguration.Height);
			using (var surface = _backBuffer.QueryInterface<Surface>()) {
				RenderTarget2D = ToDispose<RenderTarget>(
					new RenderTarget(Factory2D,
						surface,
						new RenderTargetProperties(
							new PixelFormat(
								Format.Unknown,
								AlphaMode.Premultiplied))));
			}
			RenderTarget2D.AntialiasMode = AntialiasMode.PerPrimitive;

			_vsync = Config.VSync;

			ScreenSize = new DrawingSizeF(Viewport.Width, Viewport.Height);

			Console.WriteLine("done.");
		}

		private void _disposeGraphics() {
			if (_assetManager != null && !_assetManager.IsDisposed)
				RemoveAndDispose<AssetManager>(ref _assetManager);

			if(RenderTarget2D != null && !RenderTarget2D.IsDisposed)
				RemoveAndDispose<RenderTarget>(ref RenderTarget2D);

			if (_backBufferRenderTargetView != null && !_backBufferRenderTargetView.IsDisposed)
				RemoveAndDispose<RenderTargetView>(ref _backBufferRenderTargetView);

			if (_backBuffer != null && !_backBuffer.IsDisposed)
				RemoveAndDispose<Texture2D>(ref _backBuffer);

			if (_factoryDXGI != null && !_factoryDXGI.IsDisposed)
				RemoveAndDispose<FactoryDXGI>(ref _factoryDXGI);

			if (Factory2D != null && !Factory2D.IsDisposed)
				RemoveAndDispose<Factory2D>(ref Factory2D);

			if (_swapChain != null && !_swapChain.IsDisposed)
				RemoveAndDispose<SwapChain>(ref _swapChain);

			if (_device != null && !_device.IsDisposed)
				RemoveAndDispose<Device1>(ref _device);

		}
		private void _loadContent() {
			Console.Write("Loading content... ");
			_assetManager = ToDispose<AssetManager>(new AssetManager(RenderTarget2D));
			//_controlManager.LoadContent(_assetManager);
			//_commandConsole.LoadContent(_assetManager);
			OnLoadContent(_assetManager);
			_isLoaded = true;
			Console.WriteLine("finished.");
		}

		private void _unloadContent() {
			Console.WriteLine("Unloading content...");
			//_controlManager.UnloadContent();
			//_commandConsole.UnloadContent();
			OnUnloadContent();
			_isLoaded = false;
			RemoveAndDispose<AssetManager>(ref _assetManager);
		}

		protected virtual void LoadContent(IAssetManager assetManager) {
		}
		protected virtual void UnloadContent() {
		}

		#endregion

		#region Main execution
		/// <summary>
		/// In a derived class, implements logic to update running systems.
		/// </summary>
		protected virtual void Update() {
		}
		/// <summary>
		/// In a derived class, implements logic to render content
		/// </summary>
		protected virtual void Render(RenderTarget renderTarget) {
		}
		/// <summary>
		/// In a derived class, implements logic to perform at the start of execution
		/// </summary>
		protected virtual void BeginRun() {
		}
		/// <summary>
		/// In a derived class, implements logic to perform at the end of execution
		/// </summary>
		protected virtual void EndRun() {
		}
		/// <summary>
		/// In a derived class, implements logic that should occur before all
		/// other rendering.
		/// </summary>
		protected virtual void BeginDraw() {
		}
		/// <summary>
		/// In a derived class, implements logic that should occur after all
		/// other rendering.
		/// </summary>
		protected virtual void EndDraw() {
		}

		private void _beginRun() {
			BeginRun();
		}

		/// <summary>
		/// Runs the application.
		/// </summary>
		public void Run(GameRunType runType = GameRunType.Synchronous) {
			_initializeInputs();
			Initialize();

			_isRunning = true;
			_beginRun();
			_loadContent();
			_timer.Start();

			switch (runType) {
				case GameRunType.Synchronous:
					RenderLoop.Run(_form, () => {
						if (!_isRunning)
							return;

						_update();

						if (_doRender)
							_render();
					});
					break;

				case GameRunType.Asynchronous:
					throw new NotImplementedException("Multiple game loops not working");
					_updateThread = new Thread(() => {
						var offThread = ToDispose<RenderForm>(new RenderForm());
						offThread.Visible = false;
						offThread.SuspendLayout();
						offThread.ClientSize = new System.Drawing.Size();

						RenderLoop.Run(_form, () => {
							if (!_isRunning)
								return;

							_update();
						});

						offThread.Close();
						RemoveAndDispose<RenderForm>(ref offThread);
					});
					_updateThread.Start();
					_updateThreadId = _updateThread.ManagedThreadId;
					RenderLoop.Run(_form, () => {
						if (!_isRunning)
							return;

						if (_doRender)
							_render();
					});
					_updateThread.Join(5000);
					break;
			}
			
			_unloadContent();
			_endRun();
			
			// Dispose explicity
			Dispose();
		}

		private void _endRun() {
			EndRun();
			_dMouse.Unacquire();
			_dKeyboard.Unacquire();
			_singleton = null;
		}
		
		/// <summary>
		/// Quits the application.
		/// </summary>
		public void Exit() {
			_isRunning = false;
			_doRender = false;
			if(!_isClosed)
				_form.Close();
		}

		private void _update() {
			if (_form.Focused) {
				Mouse.Update();
				Keyboard.Update();
			}

			//_commandConsole.ProcessKeyboard(Keyboard);
			//_commandConsole.ProcessMouse(Mouse);
			//_commandConsole.Update();

			// Fire input events
			if (OnMousePressed != null) {
				foreach (var button in Mouse.State.Pressed)
					OnMousePressed(this, new MouseButtonEventArgs(button, Mouse.State.Position));
			}
			if (OnMouseHeld != null) {
				foreach (var button in Mouse.State.Held)
					OnMouseHeld(this, new MouseButtonEventArgs(button, Mouse.State.Position));
			}
			if (OnMouseReleased != null) {
				foreach (var button in Mouse.State.Released)
					OnMouseReleased(this, new MouseButtonEventArgs(button, Mouse.State.Position));
			}

			if (OnMouseMoved != null && Mouse.State.Moved)
				OnMouseMoved(this, new MouseVectorEventArgs(Mouse.State.Motion));

			if (OnMousePosition != null)
				OnMousePosition(this, new MouseVectorEventArgs(Mouse.State.Position));

			if (OnMouseScroll != null && Mouse.State.ScrollWheel != 0)
				OnMouseScroll(this, new MouseScrollEventArgs(Mouse.State.ScrollWheel));

			// Keyboard events
			if (OnKeyboardPressed != null) {
				foreach (var key in Keyboard.State.Pressed)
					OnKeyboardPressed(this, new KeyboardEventArgs(key, Keyboard.State.Shift, Keyboard.State.Ctrl, Keyboard.State.Alt));
			}

			if (OnKeyboardHeld != null) {
				foreach (var key in Keyboard.State.Held)
					OnKeyboardHeld(this, new KeyboardEventArgs(key, Keyboard.State.Shift, Keyboard.State.Ctrl, Keyboard.State.Alt));
			}

			if (OnKeyboardReleased != null) {
				foreach (var key in Keyboard.State.Released)
					OnKeyboardReleased(this, new KeyboardEventArgs(key, Keyboard.State.Shift, Keyboard.State.Ctrl, Keyboard.State.Alt));
			}

			//_controlManager.ProcessMouse(Mouse);
			//_controlManager.ProcessKeyboard(Keyboard);
			//_controlManager.Update();

			OnUpdate();
			_upsCount++;
			if ((_timeAcc += _timer.UpdateTicks()) > 1000L * 10000L) {
				UpdatesPerSecond = _upsCount / (_timeAcc / (1000L * 10000L)); ;
				_upsCount = 0;
				FramesPerSecond = _fpsCount / (_timeAcc / (1000L * 10000L));
				_fpsCount = 0;
				_timeAcc -= 1000L * 10000L;
			}
		}

		private void _render() {
			_device.Rasterizer.SetViewports(Viewport);
			_device.OutputMerger.SetTargets(_backBufferRenderTargetView);
			_device.ClearRenderTargetView(_backBufferRenderTargetView, Color.CornflowerBlue);
			
			BeginDraw();
			OnRender(RenderTarget2D);
			EndDraw();

			_controlManager.Render(RenderTarget2D);
			_commandConsole.Render(RenderTarget2D);

			_swapChain.Present(_vsync ? 1 : 0, PresentFlags.None);
			_fpsCount++;
		}

		#endregion

		#region System event handlers
		
		private void _handleResize(object sender, EventArgs e) {
			if (_form.WindowState == FormWindowState.Minimized) {
				return;
			}
			if(_isLoaded)
				_unloadContent();

			_disposeGraphics();
			_initializeGraphics();

			if (!_isLoaded)
				_loadContent();
		}

		private void _handleFormClosing(object sender, EventArgs e) {
			_isClosed = true;
			Exit();
		}

		#endregion

		#region IEngineComponent management

		private Action OnUpdate;
		private Action<RenderTarget> OnRender;
		private Action<IAssetManager> OnLoadContent;
		private Action OnUnloadContent;

		public void RegisterEngineComponent(IEngineComponent component) {
			if (component is IUpdateable)
				OnUpdate += (component as IUpdateable).Update;

			if (component is IRenderable)
				OnRender += (component as IRenderable).Render;

			if (component is ILoadable) {
				OnLoadContent += (component as ILoadable).LoadContent;
				OnUnloadContent += (component as ILoadable).UnloadContent;
			}

			if (component is IHandleMouseButtonPressed)
				OnMousePressed += (component as IHandleMouseButtonPressed).OnMousePressed;

			if (component is IHandleMouseButtonHeld)
				OnMouseHeld += (component as IHandleMouseButtonHeld).OnMouseHeld;

			if (component is IHandleMouseButtonReleased)
				OnMouseReleased += (component as IHandleMouseButtonReleased).OnMouseReleased;

			if (component is IHandleMouseMotion)
				OnMouseMoved += (component as IHandleMouseMotion).OnMotion;

			if (component is IHandleMousePosition)
				OnMousePosition += (component as IHandleMousePosition).OnPosition;

			if (component is IHandleMouseScrollWheel)
				OnMouseScroll += (component as IHandleMouseScrollWheel).OnScrolled;

			if (component is IHandleKeyboardPressed)
				OnKeyboardPressed += (component as IHandleKeyboardPressed).OnKeyboardPressed;

			if (component is IHandleKeyboardHeld)
				OnKeyboardHeld += (component as IHandleKeyboardHeld).OnKeyboardHeld;

			if (component is IHandleKeyboardReleased)
				OnKeyboardReleased += (component as IHandleKeyboardReleased).OnKeyboardReleased;
		}

		public void UnregisterEngineComponent(IEngineComponent component) {
			if (component is IUpdateable)
				OnUpdate -= (component as IUpdateable).Update;

			if (component is IRenderable)
				OnRender -= (component as IRenderable).Render;
			
			if (component is ILoadable) {
				OnLoadContent -= (component as ILoadable).LoadContent;
				OnUnloadContent -= (component as ILoadable).UnloadContent;
			}

			if(component is IHandleMouseButtonPressed)
				OnMousePressed -= (component as IHandleMouseButtonPressed).OnMousePressed;
			
			if(component is IHandleMouseButtonHeld)
				OnMouseHeld -= (component as IHandleMouseButtonHeld).OnMouseHeld;

			if(component is IHandleMouseButtonReleased)
				OnMouseReleased -= (component as IHandleMouseButtonReleased).OnMouseReleased;

			if (component is IHandleMouseMotion)
				OnMouseMoved -= (component as IHandleMouseMotion).OnMotion;

			if (component is IHandleMousePosition)
				OnMousePosition -= (component as IHandleMousePosition).OnPosition;

			if (component is IHandleMouseScrollWheel)
				OnMouseScroll -= (component as IHandleMouseScrollWheel).OnScrolled;

			if (component is IHandleKeyboardPressed)
				OnKeyboardPressed -= (component as IHandleKeyboardPressed).OnKeyboardPressed;

			if (component is IHandleKeyboardHeld)
				OnKeyboardHeld -= (component as IHandleKeyboardHeld).OnKeyboardHeld;

			if (component is IHandleKeyboardReleased)
				OnKeyboardReleased -= (component as IHandleKeyboardReleased).OnKeyboardReleased;
		}

		#endregion

		#region Lua commands

		[LuaCommand("Exits the game")]
		public static LuaValue exit(LuaValue[] arg) {
			_singleton.Exit();
			return LuaNil.Nil;
		}

		#endregion
	}
}

