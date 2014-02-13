using System;
using System.Diagnostics;

namespace Engine.Lua {	
	
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
	public class LuaCommandUsageAttribute : Attribute {
		private string m_usage;
		public string Usage {
			get { return m_usage; }
		}
		
		private string m_module;
		public string Module {
			get { return m_module; }
		}
		
		public LuaCommandUsageAttribute(string usage = "", string module = "") {
			m_usage = usage;
			m_module = module;
		}
	}
}

