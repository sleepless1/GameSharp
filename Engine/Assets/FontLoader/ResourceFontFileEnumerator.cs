using System;

using SharpDX.DirectWrite;
using SharpDX;

namespace Engine.Assets.FontLoader {
	public sealed class ResourceFontFileEnumerator : CallbackBase, FontFileEnumerator {
		private Factory _factory;
		private FontFileLoader _loader;
		private DataStream keyStream;
		private FontFile _currentFontFile;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ResourceFontFileEnumerator"/> class.
		/// </summary>
		/// <param name="factory">The factory.</param>
		/// <param name="loader">The loader.</param>
		/// <param name="keyStream">The key stream.</param>
		public ResourceFontFileEnumerator(Factory factory, FontFileLoader loader, DataStream keyStream)
		{
			
			_factory = factory;
			_loader = loader;
			this.keyStream = keyStream;
		}
		
		/// <summary>
		/// Advances to the next font file in the collection. When it is first created, the enumerator is positioned before the first element of the collection and the first call to MoveNext advances to the first file.
		/// </summary>
		/// <returns>
		/// the value TRUE if the enumerator advances to a file; otherwise, FALSE if the enumerator advances past the last file in the collection.
		/// </returns>
		/// <unmanaged>HRESULT IDWriteFontFileEnumerator::MoveNext([Out] BOOL* hasCurrentFile)</unmanaged>
		bool FontFileEnumerator.MoveNext()
		{
			bool moveNext = keyStream.RemainingLength != 0;
			if (moveNext)
			{
				if (_currentFontFile != null)
					_currentFontFile.Dispose();
				
				_currentFontFile = new FontFile(_factory, keyStream.PositionPointer, 4, _loader);
				keyStream.Position += 4;
			}
			return moveNext;
		}
		
		/// <summary>
		/// Gets a reference to the current font file.
		/// </summary>
		/// <value></value>
		/// <returns>a reference to the newly created <see cref="SharpDX.DirectWrite.FontFile"/> object.</returns>
		/// <unmanaged>HRESULT IDWriteFontFileEnumerator::GetCurrentFontFile([Out] IDWriteFontFile** fontFile)</unmanaged>
		FontFile FontFileEnumerator.CurrentFontFile
		{
			get
			{
				((IUnknown) _currentFontFile).AddReference();
				return _currentFontFile;
			}
		}
	}
}

