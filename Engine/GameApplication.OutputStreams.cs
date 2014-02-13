using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Engine {
	public abstract partial class GameApplication {
		internal class OutputStreams : SharpDX.Component {

			private const int BUFSIZ = 1024;

			private StreamReader _standardReader;
			private StreamReader _errorReader;
#if DEBUG
			private const string ERRORLOG_FILENAME = "error.log";
			private StreamWriter _errorFile;
#endif

			internal OutputStreams() {
				_standardReader = ToDispose<StreamReader>(new StreamReader(new BufferedStream(new MemoryStream(BUFSIZ))));
				_errorReader = ToDispose<StreamReader>(new StreamReader(new BufferedStream(new MemoryStream(BUFSIZ))));
#if DEBUG
				try {
					_errorFile = ToDispose<StreamWriter>(new StreamWriter(new FileStream(ERRORLOG_FILENAME, FileMode.Create, FileAccess.Write)));
				} catch (FileLoadException e) {
					Console.Error.WriteLine("Could not access error log: {0}", e.Message);
				}
#endif
				Console.SetOut(new StreamWriter(_standardReader.BaseStream) { AutoFlush = true });
				Console.SetError(new StreamWriter(_errorReader.BaseStream) { AutoFlush = true });
			}

			protected override void Dispose(bool disposeManagedResources) {
				if (disposeManagedResources) {
					_errorReader.Close();
					_standardReader.Close();
					RemoveAndDispose<StreamReader>(ref _errorReader);
					RemoveAndDispose<StreamReader>(ref _standardReader);
					Console.SetError(new StreamWriter(Console.OpenStandardError()));
					Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));
#if DEBUG
					if (_errorFile != null) {
						_errorFile.Close();
						RemoveAndDispose<StreamWriter>(ref _errorFile);
					}
#endif
				}
				base.Dispose(disposeManagedResources);
			}

			/// <summary>
			/// If data is available, reads it to 'output' and returns true, otherwise
			/// returns false.
			/// </summary>
			/// <returns>
			/// <c>true</c>, if data was read, <c>false</c> otherwise.
			/// </returns>
			/// <param name='output'>
			/// The output string
			/// </param>
			public bool TryReadStandard(out string output) {
				if (_standardReader.BaseStream.Length > 0L) {
					_standardReader.BaseStream.Position = 0;
					output = _standardReader.ReadToEnd();

					// Clear the buffer
					_standardReader.BaseStream.SetLength(0L);
					_standardReader.BaseStream.Position = 0;
					return true;
				} else {
					output = null;
					return false;
				}
			}

			/// <summary>
			/// If data is available, reads it to 'output' and returns true, otherwise
			/// returns false.
			/// </summary>
			/// <returns>
			/// <c>true</c>, if data was read, <c>false</c> otherwise.
			/// </returns>
			/// <param name='output'>
			/// The output string
			/// </param>
			public bool TryReadError(out string output) {
				if (_errorReader.BaseStream.Length > 0L) {
					_errorReader.BaseStream.Position = 0;
					output = _errorReader.ReadToEnd();
#if DEBUG
					if (_errorFile != null)
						_errorFile.Write(output);
#endif

					// Clear the buffer
					_errorReader.BaseStream.SetLength(0L);
					_errorReader.BaseStream.Position = 0;
					return true;
				} else {
					output = null;
					return false;
				}
			}
		}
	}
}
