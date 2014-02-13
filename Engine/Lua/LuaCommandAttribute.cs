using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine.Lua {
	[AttributeUsage(AttributeTargets.Method)]
	public class LuaCommandAttribute : LuaCommandUsageAttribute {
		public readonly string DestinationModule;

		public LuaCommandAttribute(string usage = "", string destinationModule = "") : base(usage) {
			DestinationModule = destinationModule;
		}
	}
}
