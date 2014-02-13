using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public enum ControlIntent : byte {
		/// <summary>
		/// Primary selection (left-click on mouse).
		/// Accompanies a Vector2 value indicating position of selection
		/// </summary>
		Released,
		/// <summary>
		/// Secondary selection (right-click on mouse).
		/// Accompanies a Vector2 value indicating position of selection
		/// </summary>
		AltReleased,
		/// <summary>
		/// Long-press (held) of primary selection.
		/// Accompanies a Vector2 value indicating position of selection
		/// </summary>
		Held,
		/// <summary>
		/// Long-press (held) of secondary selection.
		/// Accompanies a Vector2 value indicating position of selection
		/// </summary>
		AltHeld,
		/// <summary>
		/// A held modifier button (alt, shift, or control typically).
		/// Accompanies an integer between 0 and 2 indicating the modifier
		/// selection. ie: 0 = alt; 1 = shift; 2 = control;
		/// </summary>
		//Modifier,
		/// <summary>
		/// The control should resize at it's top.
		/// Accompanies a float indicating amount of change
		/// </summary>
		//ResizeUp,
		/// <summary>
		/// The control should resize at it's bottom.
		/// Accompanies a float indicating amount of change
		/// </summary>
		//ResizeDown,
		/// <summary>
		/// The control should resize at it's left.
		/// Accompanies a float indicating amount of change
		/// </summary>
		//ResizeLeft,
		/// <summary>
		/// The control should resize on it's right side.
		/// Accompanies a float indicating amount of change
		/// </summary>
		//ResizeRight,
		/// <summary>
		/// The control should resize diagonally up and right.
		/// Accompanies a SharpDX.Vector2 indicating amount of change
		/// </summary>
		//ResizeUpRight,
		/// <summary>
		/// The control should resize diagonally down and right.
		/// Accompanies a SharpDX.Vector2 indicating amount of change
		/// </summary>
		//ResizeDownRight,
		/// <summary>
		/// The control should resize diagonally up and left.
		/// Accompanies a SharpDX.Vector2 indicating amount of change
		/// </summary>
		//ResizeUpLeft,
		/// <summary>
		/// The control should resize diagonally down and left.
		/// Accompanies a SharpDX.Vector2 indicating amount of change
		/// </summary>
		//ResizeDownLeft,
		/// <summary>
		/// The control should move.
		/// Accompanies a SharpDX.Vector2 indicating amount of change
		/// </summary>
		Move,
		/// <summary>
		/// The control is directly below the mouse cursor but there has been no other input
		/// </summary>
		Hovered
	}
}
