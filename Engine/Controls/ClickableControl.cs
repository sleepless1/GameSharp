using Engine.Input;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public class ClickableControl : DrawableControlBase {
		protected readonly VoidAction PrimaryAction;
		protected readonly VoidAction SecondaryAction;

		public ClickableControl(VoidAction primary, VoidAction secondary = null) {
			Debug.Assert(primary != null, "At least one action must be given to a clickable control");
			
			PrimaryAction = primary;

			if (secondary == null)
				SecondaryAction = () => { };
			else
				SecondaryAction = secondary;
		}

		public override bool ProcessIntent(ControlIntent intent, object data) {
			this.IsPressed = false;
			bool handled = false;

			switch (intent) {
				case ControlIntent.Released:
					if (ScreenSpace.Contains((Vector2)data)) {
						PrimaryAction();
						handled = true;
					}
					break;

				case ControlIntent.AltReleased:
					if (ScreenSpace.Contains((Vector2)data)) {
						SecondaryAction();
						handled = true;
					}
					break;

				case ControlIntent.Held:
					goto case ControlIntent.AltHeld;

				case ControlIntent.AltHeld:
					if(ScreenSpace.Contains((Vector2)data))
						this.IsPressed = true;
					break;

				default:
					handled = base.ProcessIntent(intent, data);
					break;
			}
			return handled;
		}
	}
}
