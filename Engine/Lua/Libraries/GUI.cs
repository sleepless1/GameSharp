using System;
using System.Linq;
using System.Text;
using System.Reflection;
using Engine.Lua;
using Language.Lua;
using Engine.Controls;
using Engine.Interface;

namespace Engine.Lua.Library {
	[LuaLibrary]
	public static class GUI {
		public const string ModuleName = "GUI";

		public static void RegisterModule(LuaEnvironment env) {
			System.Diagnostics.Debug.Assert(env != null);

			var types = from ass in AppDomain.CurrentDomain.GetAssemblies()
						from type in ass.GetTypes()
						where !type.IsAbstract && type.IsPublic
						where !type.GetCustomAttributes(true).OfType<LuaExcludeAttribute>().Any()
						where typeof(IControl).IsAssignableFrom(type)
						select type;

			LuaTable moduleTable = LuaEnvironment.ValueConverter.CreateModuleFromTypes(types, ModuleName);
			moduleTable.SetNameValue("_G", env.Environment);
			moduleTable.SetNameValue("__index", moduleTable);
			env.SetNameValue(ModuleName, moduleTable);
		}
	}
}

