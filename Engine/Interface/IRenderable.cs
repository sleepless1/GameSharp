using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface IRenderable : ILoadable {
		void Render(RenderTarget renderTarget2d);
	}
}
