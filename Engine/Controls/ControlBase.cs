using Engine.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public abstract class ControlBase : IControl {
		protected const float MINSIZE = 8f;
		protected const float MINIMUM_VISIBLE = 32f;
		protected const float DEFAULT_MARGIN = 8f;
		protected const float ALLOWED_OVERDRAW = 1f;

		#region Fields/Properties

		private bool _isResizing = false;
		private bool _isClosed = false;
		private bool _isActive;
		public bool HasParent { get { return Parent != null; } }
		public IControlParent Parent { get; private set; }
		public bool IsActive {
			get {
				if (HasParent)
					return Parent.IsActive;
				else
					return _isActive;
			}
			set {
				if (HasParent)
					Parent.IsActive = value;
				else
					_isActive = value;
			}
		}

		private ControlState _state = ControlState.Idle;
		public ControlState State {
			get {
				if (HasParent)
					return Parent.State;
				else
					return _state;
			}
			set {
				if (HasParent)
					Parent.State = value;
				else
					_state = value;
			}
		}

		#region Positional data

		public VerticalAlignment VerticalAlignment { get; set; }
		public HorizontalAlignment HorizontalAlignment { get; set; }
		public FillOptions FillOptions { get; set; }
		public ResizeOptions ResizeOption { get; set; }

		private Matrix3x2 _transform = Matrix3x2.Identity;
		public Matrix3x2 Transform { get { return _transform; } }

		private RectangleF _screenSpace;
		public RectangleF ScreenSpace { get { return _screenSpace; } }
		public RectangleF ClippingRectangle {
			get { return new RectangleF(-ALLOWED_OVERDRAW, -ALLOWED_OVERDRAW, Width + ALLOWED_OVERDRAW, Height + ALLOWED_OVERDRAW); }
		}

		private Vector2 _maxSize = new Vector2(GameApplication.ScreenSize.Width, GameApplication.ScreenSize.Height);
		public Vector2 MaxSize {
			get { return _maxSize; }
			set {
				if (value.X < MINSIZE)
					value.X = MINSIZE;
				else if (value.X > GameApplication.ScreenSize.Width)
					value.X = GameApplication.ScreenSize.Width;

				if (value.Y < MINSIZE)
					value.Y = MINSIZE;
				else if (value.Y > GameApplication.ScreenSize.Height)
					value.Y = GameApplication.ScreenSize.Height;

				_maxSize = value;
			}
		}
		private Vector2 _size = new Vector2(MINSIZE);
		public Vector2 Size {
			get { return _size; }
			set {
				_size = value;
				if (OnResized != null && !_isResizing) {
					_isResizing = true;
					OnResized(this, new ControlEventArgs(_size));
					_isResizing = false;
				}
			}
		}
		public float Width {
			get { return _size.X; }
		}
		public float Height {
			get { return _size.Y; }
		}

		private Vector2 _position;
		public Vector2 Position {
			get { return _position; }
			set {
				_position = value;
				if (OnMoved != null)
					OnMoved(this, new ControlEventArgs(_position));
			}
		}
		public float X {
			get { return _position.X; }
		}
		public float Y {
			get { return _position.Y; }
		}

		#endregion

		#endregion
		#region Initialization/Disposal

		public ControlBase() {
			VerticalAlignment = VerticalAlignment.Center;
			HorizontalAlignment = HorizontalAlignment.Center;
			FillOptions = FillOptions.None;
			ResizeOption = ResizeOptions.WrapChildren;
		}

		#endregion

		#region IControl Implementation

		public event ControlEventHandler OnClosed;
		public event ControlEventHandler OnResized;
		public event ControlEventHandler OnMoved;


		public void SetParent(IControlParent control) {
			Parent = control;
		}

		public virtual void Update(long ticks) {
			if (HasParent)
				_transform = Parent.Transform * Matrix3x2.Translation(Position);
			else
				_transform = Matrix3x2.Translation(Position);

			var screenPos = _transform.TranslationVector;
			_screenSpace = new RectangleF(screenPos.X, screenPos.Y, screenPos.X + this.Width, screenPos.Y + this.Height);
		}

		public virtual void Close() {
			if (_isClosed) return;
			_isClosed = true;
			if (OnClosed != null)
				OnClosed(this, null);
		}

		public abstract void Render(RenderTarget renderTarget);
		public abstract bool ProcessIntent(ControlIntent intent, object data);
		public abstract void LoadContent(IAssetManager assetManager);
		public abstract void UnloadContent();

		#endregion
	}
}
