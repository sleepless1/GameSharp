using Engine.Assets;
using Engine.Interface;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IControlManager : IAssetManager, IUpdateable, ILoadable, IMouseHandler, IKeyboardHandler {
		bool AddControl(IControl control);
		bool RemoveControl(IControl control);
		void Render(RenderTarget renderTarget);
	}
}
