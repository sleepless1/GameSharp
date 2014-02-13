using Engine.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public class ScrollableContainer : DrawableControlBase {
		private const float SCROLLTAB_SIZE = 16f;
		private const float MAXIMUM_INTERNAL_SIZE = 32e3f;

		private RectangleF _horizontalScrollTab;
		private RectangleF _verticalScrollTab;
		private Vector2 _scrollPosition;

		private Matrix3x2 _scrollTransform = Matrix3x2.Identity;
		private DrawingRectangleF _contentArea = new DrawingRectangleF();

		private ControlContainer _content;
		private Vector2 _size; // The size of this, not the container

		public ScrollableContainer() {
			_content = new ControlContainer() {
				Size = new Vector2(MAXIMUM_INTERNAL_SIZE)
			};

			_horizontalScrollTab = new RectangleF();
		}

		#region IControlParent implementation

		public bool AddControl(IControl control) {
			return _content.AddControl(control);
		}

		public bool RemoveControl(IControl control) {
			return _content.RemoveControl(control);
		}

		#endregion

		#region IControl implementation

		public bool IsActive {
			get {
				return _content.IsActive;
			}
			set {
				_content.IsActive = value;
			}
		}

		public ControlState State {
			get {
				return _content.State;
			}
			set {
				_content.State = value;
			}
		}

		public IControlParent Parent {
			get { return _content.Parent; }
		}

		public event ControlEventHandler OnClosed;
		public event ControlEventHandler OnResized;
		public event ControlEventHandler OnMoved;

		public Vector2 Size {
			get {
				return _size;
			}
			set {
				_size = value;
			}
		}

		public float Width {
			get { return _size.X; }
		}

		public float Height {
			get { return _size.Y; }
		}

		public Vector2 Position {
			get {
				return _content.Position;
			}
			set {
				_content.Position = value;
			}
		}

		public float X {
			get { return _content.X; }
		}

		public float Y {
			get { return _content.Y; }
		}

		public RectangleF ScreenSpace {
			get { return _content.ScreenSpace; }
		}

		public Matrix3x2 Transform {
			get { return _content.Transform; }
		}

		public VerticalAlignment VerticalAlignment {
			get {
				return _content.VerticalAlignment;
			}
			set {
				_content.VerticalAlignment = value;
			}
		}

		public HorizontalAlignment HorizontalAlignment {
			get {
				return _content.HorizontalAlignment;
			}
			set {
				_content.HorizontalAlignment = value;
			}
		}

		public FillOptions FillOptions {
			get {
				return _content.FillOptions;
			}
			set {
				_content.FillOptions = value;
			}
		}

		public bool ProcessIntent(ControlIntent intent, object data) {
			return _content.ProcessIntent(intent, data);
		}

		public void Update(long ticks) {
			_content.Update(ticks);
		}

		public void Close() {
			_content.Close();
		}

		public void SetParent(IControlParent control) {
			_content.SetParent(control);
		}

		public void Render(RenderTarget renderTarget2d) {
			_content.Render(renderTarget2d);
		}

		public void LoadContent(IAssetManager assetManager) {
			_content.LoadContent(assetManager);
		}

		public void UnloadContent() {
			_content.UnloadContent();
		}

		#endregion
	}
}
