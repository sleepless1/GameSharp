using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Interface {
	public interface ILoadable : IEngineComponent {
		void LoadContent(IAssetManager assetManager);
		void UnloadContent();
	}
}
