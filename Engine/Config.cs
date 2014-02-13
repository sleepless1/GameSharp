using System;
using System.IO;

namespace Engine {
	public abstract class Config {

        private static string _configRootDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static string ConfigRootDirectory {
            get { return _configRootDirectory; }
            set {
                if (!value.EndsWith("/"))
                    value += '/';
                _configRootDirectory = AppDomain.CurrentDomain.BaseDirectory + '/' + value;
            }
        }
		
		public Config() {
		}

		public Config(string fileName) {
			_initialize(new FileInfo(_configRootDirectory + fileName));
		}

        public Config(FileInfo fileInfo) {
            _initialize(fileInfo);
        }

		private void _initialize(FileInfo fileInfo) {
			StreamReader reader = null;
			try {
				reader = fileInfo.OpenText();
				_onFileRead(reader);
            } catch (FileNotFoundException e) {
                Console.Error.WriteLine("{0}: {1}", e, e.Message);
            } catch (FileLoadException e) {
                Console.Error.WriteLine("{0}: {1}", e, e.Message);
			} finally {
				if(reader != null)
					reader.Close();
			}
		}

        private void _onFileRead(System.IO.StreamReader reader) {
            while (!reader.EndOfStream) {
                string line = reader.ReadLine();
				if(String.IsNullOrWhiteSpace(line))
					continue;

				// Clean out any whitespace chars
				line = line.Replace(" ", "")
					.Replace("\t", "")
					.Replace("\n", "")
					.Replace("\r", "");

                if (line.StartsWith("#")) //Skip comments
                    continue;

                line = line.Split('#')[0]; // Throw out any potential trailing comments
                string[] split = line.Split('=');
				string key, value;
				try {
					key = split[0];
					value = split[1];
				} catch (IndexOutOfRangeException) {
					Console.Error.WriteLine(
						"{0}._onFileRead(StreamReader) syntax error.  Discarding line:\n{1}\t", this.GetType().Name, line);
					continue;
				}
                try {
                    ProcessKeyValuePair(key, value);
                } catch (FormatException e) {
                    Console.Error.WriteLine("{0} while reading config file.\n{1}", e, e.Message);
                } catch (OverflowException e) {
                    Console.Error.WriteLine("{0} while reading config file.\n{1}", e, e.Message);
                }
            }
        }

        protected abstract void ProcessKeyValuePair(string key, string value);
	}
}

