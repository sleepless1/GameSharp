using System;

namespace Engine.Lua {

	/// <summary>
	/// Applicable to classes, properties, fields, and enumerations.
	/// Excludes them from the lua system.
	/// If given a true value, it does not exclude the attached member,
	/// but instead makes it readonly to the lua system.  Note this has
	/// no effect when set on classes, methods, or constructors.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property |
	                AttributeTargets.Field | AttributeTargets.Enum |
	                AttributeTargets.Constructor)]
	public class LuaExcludeAttribute : Attribute {
		private bool _readOnly = false;
		public bool ReadOnly { get { return _readOnly; } }
		public LuaExcludeAttribute(bool readOnly = false) {
			_readOnly = readOnly;
		}
	}
}

