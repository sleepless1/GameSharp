using System;
using System.Linq;
using Language.Lua;
using System.Reflection;
using System.Collections.Generic;

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using Engine;
using Engine.Lua.Library;
//using Artemis.Utils;
using Language.Lua.Library;
using System.Collections.Concurrent;

namespace Engine.Lua {

	public class LuaEnvironment {

		#region Fields/Properties

		private static LuaEnvironment _singleton;
		private static Exception _lastError;
		private static string _lastErrorCmd = "";

		private static readonly TaskFactory _taskFactory = new TaskFactory();
		private static readonly ConcurrentDictionary<string, string> _usageDocs = new ConcurrentDictionary<string, string>();
		private static readonly ConcurrentDictionary<Type, LuaTable> _metatableCatalog = new ConcurrentDictionary<Type, LuaTable>();
		public static LuaValueConverter ValueConverter { get; private set; }

		private SpinLock _envLock = new SpinLock();
		private LuaTable _env;
		public LuaTable Environment { get { return _env; } }


		#endregion

		#region Code handling
		
		public LuaValue Interpreter(string luaCode) {
			luaCode = luaCode.Trim();
			if (!luaCode.Contains(' ') && !luaCode.Contains('(') && !luaCode.Contains(')'))
				luaCode += "()";

			bool lockTaken = false;
			_envLock.Enter(ref lockTaken);
			if (!lockTaken) {
				Console.Error.WriteLine("Could not acquire environment lock, aborting execution.");
				return LuaNil.Nil;
			}

			LuaValue toReturn = LuaNil.Nil;
			try {
				toReturn = LuaInterpreter.Interpreter(luaCode, Environment);
			} catch (Exception e) {
				_lastError = e;
				_lastErrorCmd = luaCode;
				Console.Error.WriteLine("Error processing input");
			} finally {
				_envLock.Exit(true);
			}
			return toReturn;
		}
		
		public Chunk Parse(string luaCode) {
			var chunk = LuaInterpreter.Parse(luaCode);
			chunk.Enviroment = Environment;
			return chunk;
		}
		
		public LuaValue RunFile(string fileName) {
			try {
				Console.WriteLine("Executing {0}", fileName);
				return LuaInterpreter.RunFile(fileName, Environment);
			} catch (FileNotFoundException e) {
				Console.Error.WriteLine(e.Message);
				return LuaNil.Nil;
			}
		}

		#endregion

		#region Environment management

		public void SetMetatableForType(Type type, LuaTable metatable) {
			_metatableCatalog.AddOrUpdate(type, metatable, (key, existingValue) => { return metatable; });
		}

		public bool TryGetMetatableForType(Type type, out LuaTable metatable) {
			return _metatableCatalog.TryGetValue(type, out metatable);
		}

		public LuaTable GetMetatableForType(Type type) {
			LuaTable metatable;
			if (_metatableCatalog.TryGetValue(type, out metatable))
				return metatable;
			else
				return null;
		}
		/// <summary>
		/// Registers a function with the global environment
		/// </summary>
		/// <param name="name">Name of the command to add</param>
		/// <param name="func">Logic of the command to add</param>
		public void Register(string name, LuaFunc func, string module = null) {
			if (String.IsNullOrEmpty(module)) {
				lock (_env)
					Environment.Register(name, func);
			} else {
				LuaTable table;
				lock (_env)
					table = Environment.GetValue(module) as LuaTable;
				try {
					lock (table)
						table.Register(name, func);
				} catch (NullReferenceException) {
					Console.Error.WriteLine("{0} module not found.", module);
				}
			}
		}

		/// <summary>
		/// Sets a value within the global environment
		/// </summary>
		/// <param name="name">Key to store the item under</param>
		/// <param name="value">The value to store</param>
		public void SetNameValue(string name, LuaValue value) {
			lock (_env)
				Environment.SetNameValue(name, value);
		}

		public static void RegisterNewUsage(string key, string usage) {
			if (_usageDocs.ContainsKey(key)) return;

			_usageDocs.TryAdd(key, usage);
		}

		#endregion

		#region Initialization and disposal

		internal LuaEnvironment() {
			_singleton = this;
			ResetEnvironment();
		}

		public void ResetEnvironment() {
			Console.WriteLine("Initializing lua...");
			ValueConverter = new LuaValueConverter(this);
			_metatableCatalog.Clear();
			_env = LuaInterpreter.CreateGlobalEnviroment();

			var taskFactory = new TaskFactory();
			var taskList = new List<Task>();

			var libraries = from assembly in AppDomain.CurrentDomain.GetAssemblies()
							from type in assembly.GetTypes()
							where type.IsPublic && type.GetCustomAttributes(true).OfType<LuaLibraryAttribute>().Any()
							from method in type.GetMethods()
							where method.IsStatic
								&& method.IsPublic
								&& method.ReturnType == typeof(void)
								&& method.GetParameters().Length == 1
								&& method.GetParameters()[0].ParameterType == typeof(LuaEnvironment)
							select new Tuple<string, MethodInfo>(type.Name, method);

			foreach (var lib in libraries) {
				taskList.Add(taskFactory.StartNew(() => {
					lib.Item2.Invoke(null, new object[] { this });
					Console.WriteLine("\t» {0} library loaded", lib.Item1);
				}));
			}

			Task.WaitAll(taskList.ToArray());

			taskList.Clear();
			taskList.Add(taskFactory.StartNew(_processCommands));
			taskList.Add(taskFactory.StartNew(_processHelpDocs));
			Task.WaitAll(taskList.ToArray());
			Console.WriteLine("Lua environment initialized.");
		}

		private void _processCommands() {

			// Replacing the default 'dofile' command:
			this.Register("dofile", (args) => {
				try {
					LuaString file = (LuaString)args[0];
					return RunFile(file.Text);
				} catch (InvalidCastException e) {
					Console.Error.WriteLine("Could not identify filename.\n" + e.Message);
				}
				return LuaNil.Nil;
			});

			var commands = from assembly in AppDomain.CurrentDomain.GetAssemblies()
						   from type in assembly.GetTypes()
						   from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
						   from attribute in method.GetCustomAttributes(false)
						   where attribute is LuaCommandAttribute
						   where method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(LuaValue[]) && method.ReturnType == typeof(LuaValue)
						   select new Tuple<MethodInfo, LuaCommandAttribute>(method, attribute as LuaCommandAttribute);

			foreach (var tuple in commands) {
				this.Register(tuple.Item1.Name, (args) => {
					return (LuaValue)tuple.Item1.Invoke(null, new object[] { args });
				}, tuple.Item2.DestinationModule);
			}
		}

		private void _processHelpDocs() {

			var attributes = from assembly in AppDomain.CurrentDomain.GetAssemblies()
							 from type in assembly.GetTypes()
							 from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
							 from attribute in method.GetCustomAttributes(false)
							 where attribute is LuaCommandUsageAttribute
							 select new Tuple<string, LuaCommandUsageAttribute>(method.Name, attribute as LuaCommandUsageAttribute);
			
			foreach (var attribute in attributes) {
				string name;
				if (attribute.Item2.Module != "")
					name = attribute.Item2.Module + "." + attribute.Item1;
				else
					name = attribute.Item1;

				RegisterNewUsage(name, attribute.Item2.Usage);
			}
		}

		#endregion
		
		[LuaCommand("Displays a listing of commands within the given module or information about the given command.")]
		private static LuaValue help(LuaValue[] parameters) {
			try {
				if (parameters.Length == 1) {
					var text = parameters[0] as LuaString;
					if (_usageDocs.ContainsKey(text.Text)) {
						Console.WriteLine(text.Text + " - " + _usageDocs[text.Text] + "\n");
						return LuaBoolean.True;
					} else {
						Console.WriteLine("Command not found: {0}", text.Text);
						return LuaBoolean.False;
					}
				} else {
					foreach (string s in _usageDocs.Keys.OrderBy<string, string>(doc => doc)) {
						if (!s.Contains(".")) {
							Console.WriteLine(s);
						}
					}
					return LuaBoolean.True;
				}
			} catch (NullReferenceException e) {
				string errMsg = "Lua command error: Parameter error in help(): Expected LuaString";
				Console.Error.WriteLine(errMsg);
				throw new LuaError(errMsg, e);
			} catch (IndexOutOfRangeException e) {
				string errMsg = "Lua command error: Parameter error in help(): No parameters given";
				Console.Error.WriteLine(errMsg);
				throw new LuaError(errMsg, e);
			}
		}

		[LuaCommand("Displays information contained in the last LuaError exception caught by the environment")]
		private static LuaValue lasterror(LuaValue[] parameters) {
			if (_lastError == null) {
				Console.WriteLine("No error logged.");
			} else {
				Console.Error.WriteLine(_lastError.GetType().Name);
				Console.Error.WriteLine(_lastError.Message);
				if (_lastError.InnerException != null) {
					Console.Error.WriteLine(System.Environment.NewLine + "Inner exception: {0}", _lastError.InnerException.GetType().Name);
					Console.Error.WriteLine(_lastError.InnerException.Message);
				}
			}
			return LuaNil.Nil;
		}

		[LuaCommand("Runs the lua code given as a string parameter off-thread.")]
		private static LuaValue fork(LuaValue[] parameters) {
			if (parameters.Length < 1) {
				Console.Error.WriteLine("No code to fork and run");
				return LuaNil.Nil;
			}
			string luaCode;
			try {
				luaCode = ((LuaString)parameters[0]).Text;
			} catch (InvalidCastException) {
				throw new LuaError("Lua code is not a valid string");
			}
			_taskFactory.StartNew(() => {
				try {
					_singleton.Interpreter(luaCode);
				} catch (LockRecursionException) {
					Console.Error.WriteLine("Can not fork within a fork command.");
				}
			});
			return LuaNil.Nil;
		}

		[LuaCommand("Causes the current thread to sleep for the given duration in milliseconds.  Has no effect when run on the main thread.")]
		private static LuaValue sleep(LuaValue[] parameters) {
			int thisThread = Thread.CurrentThread.ManagedThreadId;
			if (thisThread == GameApplication.MainThreadId || thisThread == GameApplication.UpdateThreadId)
				return LuaNil.Nil;

			if (parameters.Length < 1) {
				Console.Error.WriteLine("No sleep duration given");
				return LuaNil.Nil;
			}

			int time;
			try {
				time = (int)((LuaNumber)parameters[0]).Number;
			} catch (InvalidCastException) {
				Console.Error.WriteLine("Invalid sleep parameter given");
				return LuaNil.Nil;
			}
			if (time > 0)
				Thread.Sleep(time);
			return LuaNil.Nil;
		}
	}	

	[Serializable]
	public class LuaRegistrationException : Exception {
		public LuaRegistrationException(string msg) : base(msg) {
			if (Thread.CurrentThread.ManagedThreadId == GameApplication.MainThreadId) {
			}
		}
	}
}

