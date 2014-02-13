using System;
using System.Linq;
using Language.Lua;

using SharpDX;
using System.Reflection;
using System.Text;

namespace Engine.Lua.Library
{
	/// <summary>
	/// Brings the SharpDX value types within the SharpDX namespace
	 /// into the lua environment.
	/// </summary>
	[LuaLibrary]
	public static class SharpDX
	{		
		public const string ModuleName = "DX"; // Shortened for less hand-strain
		private const string NAMESPACE = "SharpDX";
		
		public static void RegisterModule(LuaEnvironment env) {
			System.Diagnostics.Debug.Assert(env != null);
			var types = from ass in AppDomain.CurrentDomain.GetAssemblies()
						from type in ass.GetTypes()
						where type.Namespace == NAMESPACE
							&& type.IsValueType
							&& !type.IsAbstract
							&& type.IsPublic
						select type;

			LuaTable module = LuaEnvironment.ValueConverter.CreateModuleFromTypes(types, ModuleName);

			module.SetNameValue("_G", env.Environment);
			module.SetNameValue("__index", module);
			env.SetNameValue(ModuleName, module);
		}
	}
}

