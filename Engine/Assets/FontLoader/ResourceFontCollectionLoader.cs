using System;
using System.Linq;

using SharpDX.DirectWrite;
using SharpDX;
using System.Collections.Generic;
using System.IO;

namespace Engine.Assets.FontLoader {
	public class ResourceFontCollectionLoader : CallbackBase, FontCollectionLoader, FontFileLoader {
		private readonly List<ResourceFontFileStream> _fontStreams = new List<ResourceFontFileStream>();
		private readonly List<ResourceFontFileEnumerator> _enumerators = new List<ResourceFontFileEnumerator>();
		private readonly DataStream _keyStream;
		private readonly Factory _factory;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ResourceFontLoader"/> class.
		/// </summary>
		/// <param name="factory">The factory.</param>
		public ResourceFontCollectionLoader(Factory factory)
		{
			_factory = factory;

			var resources = from assembly in AppDomain.CurrentDomain.GetAssemblies()
							from name in assembly.GetManifestResourceNames()
							where name.EndsWith(".ttf")
							select assembly.GetManifestResourceStream(name);

			foreach (Stream stream in resources) {
				DataStream dxstream = new DataStream((int)stream.Length, true, true);
				dxstream.Write(Utilities.ReadStream(stream), 0, (int)stream.Length);
				dxstream.Position = 0;
				_fontStreams.Add(new ResourceFontFileStream(dxstream));
			}

			/*
			foreach (var name in System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceNames())
			{
				if (name.EndsWith(".ttf"))
				{
					var fontBytes = Utilities.ReadStream(System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream(name));
					var stream = new DataStream(fontBytes.Length, true, true);
					stream.Write(fontBytes, 0, fontBytes.Length);
					stream.Position = 0;
					_fontStreams.Add(new ResourceFontFileStream(stream));
				}
			}
			 */
			
			// Build a Key storage that stores the index of the font
			_keyStream = new DataStream(sizeof(int) * _fontStreams.Count, true, true);
			for (int i = 0; i < _fontStreams.Count; i++ )
				_keyStream.Write((int)i);
			_keyStream.Position = 0;
			
			// Register the loader
			_factory.RegisterFontFileLoader(this);
			_factory.RegisterFontCollectionLoader(this);
		}

		protected override void Dispose(bool disposing) {
			// Make sure the loader is unregistered or we'll leak
			if(_factory != null && !_factory.IsDisposed) {
				try {
					_factory.UnregisterFontFileLoader(this);
					_factory.UnregisterFontCollectionLoader(this);
				} catch (SharpDXException) {
					// If exiting due to an error condition, exceptions of this type or
					// likely and not relevant.
				}
			}
			base.Dispose(disposing);
		}		
		
		/// <summary>
		/// Gets the key used to identify the FontCollection as well as storing index for fonts.
		/// </summary>
		/// <value>The key.</value>
		public DataStream Key
		{
			get
			{
				return _keyStream;
			}
		}
		
		/// <summary>
		/// Creates a font file enumerator object that encapsulates a collection of font files. The font system calls back to this interface to create a font collection.
		/// </summary>
		/// <param name="factory">Pointer to the <see cref="SharpDX.DirectWrite.Factory"/> object that was used to create the current font collection.</param>
		/// <param name="collectionKey">A font collection key that uniquely identifies the collection of font files within the scope of the font collection loader being used. The buffer allocated for this key must be at least  the size, in bytes, specified by collectionKeySize.</param>
		/// <returns>
		/// a reference to the newly created font file enumerator.
		/// </returns>
		/// <unmanaged>HRESULT IDWriteFontCollectionLoader::CreateEnumeratorFromKey([None] IDWriteFactory* factory,[In, Buffer] const void* collectionKey,[None] int collectionKeySize,[Out] IDWriteFontFileEnumerator** fontFileEnumerator)</unmanaged>
		public FontFileEnumerator CreateEnumeratorFromKey(Factory factory, DataStream collectionKey)
		{
			var enumerator = new ResourceFontFileEnumerator(factory, this, new DataStream(_keyStream.DataPointer, _keyStream.Length, true,true));
			_enumerators.Add(enumerator);
			
			return enumerator;
		}
		
		/// <summary>
		/// Creates a font file stream object that encapsulates an open file resource.
		/// </summary>
		/// <param name="fontFileReferenceKey">A reference to a font file reference key that uniquely identifies the font file resource within the scope of the font loader being used. The buffer allocated for this key must at least be the size, in bytes, specified by  fontFileReferenceKeySize.</param>
		/// <returns>
		/// a reference to the newly created <see cref="SharpDX.DirectWrite.FontFileStream"/> object.
		/// </returns>
		/// <remarks>
		/// The resource is closed when the last reference to fontFileStream is released.
		/// </remarks>
		/// <unmanaged>HRESULT IDWriteFontFileLoader::CreateStreamFromKey([In, Buffer] const void* fontFileReferenceKey,[None] int fontFileReferenceKeySize,[Out] IDWriteFontFileStream** fontFileStream)</unmanaged>
		public FontFileStream CreateStreamFromKey(DataStream fontFileReferenceKey)
		{
			var index = fontFileReferenceKey.Read<int>();
			return _fontStreams[index];
		}
	}
}