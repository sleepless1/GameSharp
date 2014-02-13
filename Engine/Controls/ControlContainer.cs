using Engine.Assets;
using Engine.Interface;
using Engine.Lua;
using Language.Lua;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Engine.Controls {
	public class ControlContainer : ControlBase, IControlParent {

		#region Fields/Properties

		#region Familial data

		#region Threading

		private SpinLock _controlLock = new SpinLock();

#if DEBUG
		private StackFrame _acquiredLockFrame = null;
#endif


		private bool _lockControlList() {
#if DEBUG
			if (_controlLock.IsHeldByCurrentThread) {
				var frame = new StackFrame(1);
				throw new LockRecursionException(
					String.Format("Lock requested from {0} but already acquired in {1} of the {2} class.", frame.GetMethod().Name, _acquiredLockFrame.GetMethod().Name, this.GetType().Name));
			} else {
				_acquiredLockFrame = new StackFrame(1);
			}
#endif
			bool locked = false;
			_controlLock.Enter(ref locked);
			return locked;
		}

		private void _unlockControlList(bool useMemoryBarrier = false) {
			_controlLock.Exit(useMemoryBarrier);
		}

		#endregion

		private readonly List<IControl> _controls = new List<IControl>(4);
		public bool HasChildren { get { return _controls.Count > 0; } }
		public int ChildCount { get { return _controls.Count; } }
		public IControl FirstChild {
			get {
				if (HasChildren)
					return _controls[0];
				else
					return null;
			}
		}
		public IControl LastChild {
			get {
				if (HasChildren)
					return _controls[_controls.Count - 1];
				else
					return null;
			}
		}

		// Child control data

		/// <summary>
		/// Determines how controls are placed when added.
		/// </summary>
		public LayoutType LayoutOption = LayoutType.None;
		/// <summary>
		/// Represents the amount of screenspace to leave between child
		/// controls and the edge of their parent
		/// </summary>
		public float Margin = DEFAULT_MARGIN;
		/// <summary>
		/// Represents the amount of screenspace to leave between child controls
		/// </summary>
		public float Padding = DEFAULT_MARGIN;

		#endregion

		#endregion

		#region IControlParent implementation

		public virtual bool AddControl(IControl control) {
			System.Diagnostics.Debug.Assert(control != null, "Can not add a null control");

			// Adding a control to itself or to a child of itself
			// will unravel the very fabric of the universe
			for (IControl parent = this; parent != null; parent = parent.Parent) {
				if (parent == control)
					throw new InvalidOperationException("Can not add a Control to itself, or to any children of itself.");
			}

			//_layoutAndAlignChild(control);
			if (!_lockControlList()) {
				control.Close();
				Console.Error.WriteLine("Could not acquire control lock, control discarded.");
				return false;
			}
			try {
				_controls.Add(control);
			} finally {
				_unlockControlList(true);
			}
			control.SetParent(this);
			control.OnResized += this._handleResizeEvent;
			control.OnClosed += this._handleClosedEvent;
			_layoutAndAlignChildren();
			return true;
		}

		public virtual bool RemoveControl(IControl control) {
			if (control.Parent != this) {
				Console.Error.WriteLine("Tried to remove control [{0}] that is not a child of parent [{1}].", control.GetType().Name, this.GetType().Name);
				return false;
			}

			if (!_lockControlList())
				throw new SynchronizationLockException("Could not acquire control lock, control not removed.");
			try {
				_controls.Remove(control);
			} finally {
				_unlockControlList(true);
			}
			control.SetParent(null);
			control.OnResized -= this._handleResizeEvent;
			control.OnClosed -= this._handleClosedEvent;
			return true;
		}

		public override void Update(long ticks) {
			base.Update(ticks);
			if (!HasChildren) return;

			if (!_lockControlList()) {
				Console.Error.Write("Could not acquire lock to control list in {0}.  Skipping child updates this frame.", this.GetType().Name);
				return;
			}
			try {
				for (int i = 0, j = _controls.Count; i < j; i++)
					_controls[i].Update(ticks);
			} finally {
				_unlockControlList();
			}
		}

		public override void Render(RenderTarget renderTarget) {
			if (!HasChildren) return;

			if (!_lockControlList()) {
				Console.Error.Write("Could not acquire lock to control list in {0}.  Skipping child rendering this frame.", this.GetType().Name);
				return;
			}
			try {
				for (int i = 0, j = _controls.Count; i < j; i++) {
					try {
						_controls[i].Render(renderTarget);
					} catch (Exception e) {
						Console.Error.WriteLine("Rendering child control of type {0} resulted in an exception: {1} - {2}", _controls[i].GetType().Name, e.GetType().Name, e.Message);
						if (e.InnerException != null) {
							Console.Error.WriteLine("\tAdditional Information:");
							Console.Error.WriteLine(e.InnerException.Message);
						}
					}
				}
			} finally {
				_unlockControlList();
			}
		}

		public override bool ProcessIntent(ControlIntent intent, object data) {
			bool handled = false;
			try {
				for (int i = 0, j = _controls.Count; !handled && i < j; i++)
					handled = _controls[i].ProcessIntent(intent, data);
			} catch (IndexOutOfRangeException) {
				Console.Error.WriteLine("Control list changed while processing intent, discarding");
			}
			return handled;
		}

		public override void LoadContent(IAssetManager assetManager) {
			if (!_lockControlList())
				throw new SynchronizationLockException("Could not acquire lock to control list.");

			try {
				for (int i = 0, j = _controls.Count; i < j; i++)
					_controls[i].LoadContent(assetManager);

				_controls.TrimExcess();
			} finally {
				_unlockControlList();
			}
		}

		public override void UnloadContent() {
			if (!_lockControlList())
				throw new SynchronizationLockException("Could not acquire lock to control list.");

			try {
				for (int i = 0, j = _controls.Count; i < j; i++)
					_controls[i].UnloadContent();

				_controls.TrimExcess();
			} finally {
				_unlockControlList();
			}
		}

		public override void Close() {
			for (int i = 0; i < _controls.Count; i++)
				this._controls[i].Close();
			if (!_lockControlList())
				throw new SynchronizationLockException("Could not acquire control lock");
			try {
				_controls.Clear();
			} finally {
				_unlockControlList(true);
			}
			base.Close();
		}

		#endregion

		#region Event handlers

		private void _handleClosedEvent(object sender, ControlEventArgs args) {
			this.RemoveControl((IControl)sender);
		}

		private void _handleResizeEvent(object sender, ControlEventArgs args) {
			if(!_isAligning)
				this._layoutAndAlignChildren();
		}

		#endregion

		#region Child layout/alignment/resizing

		private bool _isAligning = false;
		private void _layoutAndAlignChildren() {
			if (!HasChildren || _isAligning) return;
			_isAligning = true;

			// Lock controls so that no new ones are added during inflation
			//if (!_lockControlList())
			//	throw new SynchronizationLockException("Could not acquire control lock");

			try {
				switch (this.LayoutOption) {
					case LayoutType.Vertical:
						_verticalLayoutChildren();
						break;

					case LayoutType.Horizontal:
						_horizontalLayoutChildren();
						break;
				}
				_resizeForChildren();
			} finally {
			//	_unlockControlList();
				_isAligning = false;
			}
		}

		private void _verticalLayoutChildren() {
			IControl previousChild = _controls[0]; // First control
			previousChild.Position = new Vector2(Margin, Margin);
			for (int i = 1, j = _controls.Count; i < j; i++) {
				Vector2 newPosition = new Vector2(Margin, previousChild.Y + previousChild.Height + Padding);
				switch (_controls[i].HorizontalAlignment) {
					case Controls.HorizontalAlignment.Left:
						goto default;

					case Controls.HorizontalAlignment.Center:
						newPosition.X = (this.Width - _controls[i].Width) / 2f;
						break;

					case Controls.HorizontalAlignment.Right:
						newPosition.X = this.Width - Margin - _controls[i].Width;
						break;

					default: break;
				}
				_controls[i].Position = newPosition;
				previousChild = _controls[i];
			}
		}

		private void _horizontalLayoutChildren() {
			IControl previousChild = _controls[0]; // First control
			previousChild.Position = new Vector2(Margin, Margin);
			for (int i = 1, j = _controls.Count; i < j; i++) {
				_controls[i].Position = new Vector2(previousChild.X + previousChild.Width + Padding, previousChild.Y);
				previousChild = _controls[i];
			}
		}

		private void _wrapChildren() {
			Vector2 minSize = new Vector2(MINSIZE);
			for (int i = 0, j = _controls.Count; i < j; i++) {
				if (_controls[i].Width > minSize.X)
					minSize.X = _controls[i].Width;
				if (_controls[i].Height > minSize.Y)
					minSize.Y = _controls[i].Height;
			}
			this.Size = minSize;
		}

		private void _resizeForChildren() {
			Vector2 size;
			switch (this.ResizeOption) {
				case ResizeOptions.WrapChildren:
					size = new Vector2(Margin * 2f, Margin * 2f);
					break;

				case ResizeOptions.ExpandForChildren:
					size = this.Size;
					break;

				case ResizeOptions.None:
					goto default;

				default:
					return;
			}
			for (int i = 0, j = _controls.Count; i < j; i++) {
				if (_controls[i].X + _controls[i].Width > size.X) {
					size.X = _controls[i].X + _controls[i].Width + Margin;
				}
				if (_controls[i].Y + _controls[i].Height > size.Y) {
					size.Y = _controls[i].Y + _controls[i].Height + Margin;
				}
			}
			if (size.X > MaxSize.X)
				size.X = MaxSize.X;
			if (size.Y > MaxSize.Y)
				size.Y = MaxSize.Y;
			this.Size = size;
		}

		#endregion

		#region Lua commands

		[LuaCommand("Adds a child control to a control", Engine.Lua.Library.GUI.ModuleName)]
		private static LuaValue AddChild(LuaValue[] parameters) {
			if (parameters.Length < 2) {
				Console.Error.WriteLine("Insufficient parameters.");
				return LuaNil.Nil;
			}
			try {
				IControlParent parent = parameters[0].Value as IControlParent;
				IControl child = parameters[1].Value as IControl;
				parent.AddControl(child);
			} catch (NullReferenceException) {
				Console.Error.WriteLine("Invalid parameters.");
			}
			return LuaNil.Nil;
		}

		[LuaCommand("Removes a child control from a control", Engine.Lua.Library.GUI.ModuleName)]
		private static LuaValue RemoveChild(LuaValue[] parameters) {
			if (parameters.Length < 2) {
				Console.Error.WriteLine("Insufficient parameters.");
				return LuaNil.Nil;
			}
			try {
				IControlParent parent = parameters[0].Value as IControlParent;
				IControl child = parameters[1].Value as IControl;
				parent.RemoveControl(child);
			} catch (NullReferenceException) {
				Console.Error.WriteLine("Invalid parameters.");
			}
			return LuaNil.Nil;
		}

		#endregion

	}
}
