using Engine.Controls;
using Engine.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IControl : IRenderable {

		bool IsActive { get; set; }
		ControlState State { get; set; }
		IControlParent Parent { get; }

		#region Events

		event ControlEventHandler OnClosed;
		event ControlEventHandler OnResized;
		event ControlEventHandler OnMoved;

		#endregion

		#region Positional data

		Vector2 Size { get; set; }
		float Width { get; }
		float Height { get; }
		
		Vector2 Position { get; set; }
		float X { get; }
		float Y { get; }

		/// <summary>
		/// Returns a RectangleF that represents the control area in screen space
		/// </summary>
		RectangleF ScreenSpace { get; }
		Matrix3x2 Transform { get; }

		VerticalAlignment VerticalAlignment { get; set; }
		HorizontalAlignment HorizontalAlignment { get; set; }
		FillOptions FillOptions { get; set; }

		#endregion

		#region Implementation logic

		bool ProcessIntent(ControlIntent intent, object data);
		void Update(long ticks);
		void Close();
		void SetParent(IControlParent control);
		
		#endregion
	}
}
