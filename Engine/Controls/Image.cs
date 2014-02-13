using Engine.Assets;
using Engine.Interface;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Engine.Controls {
	public sealed class Image : ControlBase, IDisposable {

		private readonly string _imageFilepath;
		private TextureAsset _texture;
		public float Opacity = 1.0f;
		public bool ResizeToImage = true;

		public Image(string imagePath) {
			System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(imagePath), "Image control needs a filepath.");
			System.Diagnostics.Debug.Assert(File.Exists(imagePath), "File does not exist: " + imagePath);
			_imageFilepath = imagePath;
		}

		~Image() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
		}

		private void Dispose(bool disposeManagedResources) {
			if (_texture != null)
				_texture.Dispose();
		}

		public override void Render(SharpDX.Direct2D1.RenderTarget renderTarget) {
			renderTarget.Transform = Transform;
			_texture.Render(renderTarget, Opacity);
		}

		public override bool ProcessIntent(ControlIntent intent, object data) {
			return false;
		}

		public override void LoadContent(IAssetManager assetManager) {
			_texture = assetManager.LoadTexture(_imageFilepath);
			if (ResizeToImage)
				this.Size = new Vector2(_texture.Resource.Size.Width, _texture.Resource.Size.Height);
		}

		public override void UnloadContent() {
			_texture.Dispose();
			_texture = null;
		}
	}
}
