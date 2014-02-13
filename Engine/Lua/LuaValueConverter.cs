using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Engine;
using System.Text;
using Language.Lua;

namespace Engine.Lua {
	public class LuaValueConverter {

		private LuaEnvironment _environment;

		internal LuaValueConverter(LuaEnvironment env) {
			_environment = env;
		}

		#region Basic value types
		public LuaValue NumericToLua(int n) {
			return new LuaNumber(Convert.ToDouble(n));
		}
		public LuaValue NumericToLua(long n) {
			return new LuaNumber(Convert.ToDouble(n));
		}
		public LuaValue NumericToLua(float n) {
			return new LuaNumber(Convert.ToDouble(n));
		}
		public LuaValue NumericToLua(double n) {
			return new LuaNumber(Convert.ToDouble(n));
		}
		public LuaValue StringToLua(string str) {
			return new LuaString(str);
		}
		public LuaValue BooleanToLua(bool b) {
			return b ? LuaBoolean.True : LuaBoolean.False;
		}
		#endregion

		public LuaMultiValue CreateLuaMultiValue(params object[] values) {
			List<LuaValue> list = new List<LuaValue>();
			foreach(var o in values) {
				var type = o.GetType();
				if(type == typeof(string))
					list.Add(StringToLua((string)o));
				else if (type == typeof(int)) {
					list.Add(NumericToLua((int)o));
				}else if (type == typeof(long)) {
					list.Add(NumericToLua((long)o));
				} else if (type == typeof(float)) {
					list.Add(NumericToLua((float)o));
				} else if (type == typeof(double)) {
					list.Add(NumericToLua((double)o));
				} else if (type.IsClass) {
					list.Add(ObjectToLua(o));
				}
			}
			return new LuaMultiValue(list.ToArray());
		}

		public LuaValue ObjectToLua(object o) {
			if(o == null || _environment == null) return LuaNil.Nil;

			LuaTable metatable;
			if (_environment.TryGetMetatableForType(o.GetType(), out metatable)) {
				return new LuaUserdata(o, metatable);
			} else {
				return new LuaUserdata(o);
			}
		}

		#region Accessors

		public LuaFunction GetAccessorLuaFunction(MemberInfo memberInfo) { 
			if(memberInfo.MemberType == MemberTypes.Field || memberInfo.MemberType == MemberTypes.Property) {
				var field = memberInfo as FieldInfo;
				var prop = memberInfo as PropertyInfo;
				Type type = field != null ? field.FieldType : prop.PropertyType;

				if(type == typeof(string)) {
					return GetStringAccessor(memberInfo);
				} else if(type == typeof(int) || type == typeof(long) ||  type == typeof(float) ||  type == typeof(double)) {
					return GetNumericAccessor(memberInfo);
				} else if(type == typeof(bool)) {
					return GetBooleanAccessor(memberInfo);
				} else if(type.IsClass) {
					return GetReferenceTypeAccessor(memberInfo);
				} else if(type.IsEnum) {
					return GetEnumerationAccessor(memberInfo);
				} else if(type.IsValueType) {
					return GetValueTypeAccessor(memberInfo);
				}
			}
			return null;
		}
		
		public LuaFunction GetSetterLuaFunction(MemberInfo memberInfo) {
			// Switch on what type of member we're dealing with.
			if(memberInfo.MemberType == MemberTypes.Field || memberInfo.MemberType == MemberTypes.Property) {
				var field = memberInfo as FieldInfo;
				var prop = memberInfo as PropertyInfo;
				
				if(prop != null && !prop.CanWrite)
					return null; // non-public or read-only property

				if(field != null && !field.IsPublic)
					return null; // non-public field
				
				string name = field != null ? field.Name : prop.Name;
				Type type = field != null ? field.FieldType : prop.PropertyType;
				
				if(type == typeof(String)) {
					return GetStringSetter(memberInfo);
				} else if(type == typeof(Int32) || type == typeof(Int64) || type == typeof(Single) || type == typeof(Double)) {
					return GetNumericSetter(memberInfo);
				} else if (type == typeof(bool)) {
					return GetBooleanSetter(memberInfo);
				} else if (type.IsClass) {
					return GetReferenceTypeSetter(memberInfo);
				} else if (type.IsEnum) {
					return GetEnumerationSetter(memberInfo);
				} else if(type.IsValueType) {
					return GetValueTypeSetter(memberInfo);
				}
			}
			return null;
		}
		
		public LuaFunction GetLuaFunc(MethodInfo methodInfo) {
			if(methodInfo == null ||
				methodInfo.IsAbstract ||
				!methodInfo.IsStatic ||
				methodInfo.ReturnType != typeof(LuaValue))
				return null;
			
			var pInfo = methodInfo.GetParameters();
			if(pInfo.Length != 1 || pInfo[0].ParameterType != typeof(LuaValue[])) 
				return null;
			
			return new LuaFunction((LuaValue[] parameters) => {
				return methodInfo.Invoke(null, new object[] {parameters}) as LuaValue;
			});
		}

		#endregion

		public LuaTable CreateModuleFromTypes(IEnumerable<Type> types, string moduleName = "") {
			LuaTable module = new LuaTable();
			StringBuilder moduleDocs = new StringBuilder();

			foreach(Type t in types) {
				if (t.GetCustomAttributes(true).OfType<LuaExcludeAttribute>().Any())
					continue;

				string typeName = t.Name;
				moduleDocs.AppendLine(typeName);
				StringBuilder typeDoc = new StringBuilder();
				LuaTable subModule = new LuaTable();
				LuaTable metatable = new LuaTable();
				
				metatable.SetNameValue("__index", metatable);
				subModule.MetaTable = metatable;
				
				// Create a constructor where appropriate
				ConstructorInfo constructorInfo = null;
				if (t.IsValueType) {
					subModule.Register("new", (args) => {
						return ObjectToLua(Activator.CreateInstance(t));
					});
					typeDoc.AppendFormat("new() - creates a new instance of the {0} type.\n", typeName);
				} else {
					if ((constructorInfo = t.GetConstructor(new Type[] { })) != null) {
						subModule.Register("new", (args) => {
							try {
								return ObjectToLua(constructorInfo.Invoke(new object[] { }));
							} catch (Exception e) {
								Console.Error.WriteLine("Lua constructor for {0} threw an exception of type {1}: {2}", t.Name, e.GetType().Name, e.Message);
								if (e.InnerException != null)
									Console.Error.WriteLine("Inner exception of type {0}: {1}", e.InnerException.GetType().Name, e.InnerException.Message);
							}
							return LuaNil.Nil;
						});
					} else if ((constructorInfo = t.GetConstructor(new Type[] { typeof(LuaValue[]) })) != null) {
						subModule.Register("new", (args) => {
							try {
								return ObjectToLua(constructorInfo.Invoke(new object[] { args }));
							} catch (Exception e) {
								Console.Error.WriteLine("Lua constructor for {0} threw an exception of type {1}: {2}", t.Name, e.GetType().Name, e.Message);
								if (e.InnerException != null)
									Console.Error.WriteLine("Inner exception of type {0}: {1}", e.InnerException.GetType().Name, e.InnerException.Message);
							}
							return LuaNil.Nil;
						});
					}
					if (constructorInfo != null) {
						if (constructorInfo.GetCustomAttributes(true).OfType<LuaCommandUsageAttribute>().Any()) {
							var attribute = constructorInfo.GetCustomAttributes(true).OfType<LuaCommandUsageAttribute>().Single();
							typeDoc.Append(attribute.Usage);
						} else {
							typeDoc.AppendFormat("new() - creates a new instance of the {0} type.\n", typeName);
						}
					}
				}
				
				// Create member accessors
				var members = from m in t.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
							  where (m.MemberType == MemberTypes.Field ||
								m.MemberType == MemberTypes.Property ||
								m.MemberType == MemberTypes.Method)
							  select m;
				
				foreach(MemberInfo member in members) {
					bool excludeGetter = false, excludeSetter = false;
					if (member.GetCustomAttributes(true).OfType<LuaExcludeAttribute>().Any()) {
						LuaExcludeAttribute exclude = member.GetCustomAttributes(true).OfType<LuaExcludeAttribute>().Single();
						excludeSetter = true;
						if (!exclude.ReadOnly)
							excludeGetter = true;
					}

					var field = member as FieldInfo;
					var prop = member as PropertyInfo;
					var method = member as MethodInfo;

					string memberType = "";
					if (field != null) {
						memberType = field.FieldType.Name;
					} else if (prop != null) {
						memberType = prop.PropertyType.Name;
					} else if (method != null) {
						memberType = method.DeclaringType.Name;
					}

					LuaFunction func = excludeSetter ? null : GetLuaFunc(member as MethodInfo);
					if(func != null) {
						// It's a function, and we'll register it.  No way to create
						// documentation for it, so ideally it also makes use of the
						// LuaCommandUsage attribute for documentation.
						metatable.SetNameValue(member.Name, func);
						if (member.GetCustomAttributes(true).OfType<LuaCommandUsageAttribute>().Any()) {
							LuaCommandUsageAttribute usage = member.GetCustomAttributes(true).OfType<LuaCommandUsageAttribute>().Single();
							typeDoc.AppendLine(String.Format("{0}:{1} - {2}", t.Name, member.Name, usage.Usage));
						} else {
							typeDoc.AppendLine(member.Name);
						}
					}

					LuaFunction getter = excludeGetter ? null : GetAccessorLuaFunction(member);
					if(getter != null) {
						typeDoc.AppendFormat("{0} - Gets a {1} value from the {2} struct\n",
						                     "Get" + member.Name, memberType, typeName);
						metatable.SetNameValue("Get" + member.Name, getter);
					}

					LuaFunction setter = excludeSetter ? null : GetSetterLuaFunction(member);
					if(setter != null) {
						typeDoc.AppendFormat("{0} - Sets a {1} value to the {2} struct\n",
						                     "Set" + member.Name, memberType, typeName);
						metatable.SetNameValue("Set" + member.Name, setter);
					}
				}
				
				// Register the type metatable and help docs
				_environment.SetMetatableForType(t, metatable);
				module.SetNameValue(typeName, subModule);
				LuaEnvironment.RegisterNewUsage(moduleName + "." + typeName, typeDoc.ToString());
			}
			// Register the module's documentation
			LuaEnvironment.RegisterNewUsage(moduleName, moduleDocs.ToString());

			return module;
		}

		public LuaFunction GetStringAccessor(MemberInfo member) {
			var field = member as FieldInfo;
			var prop = member as PropertyInfo;

			if (field == null && prop == null)
				return null;

			if (prop != null && !prop.CanRead)
				return null; // non-public property

			if (field != null && !field.IsPublic)
				return null; // non-public field

			if (field != null) {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = (args[0] as LuaUserdata).Value;
						return StringToLua(Convert.ToString(field.GetValue(data)));
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to getter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to getter function\n" + e.Message);
					} catch (InvalidCastException e) {
						Console.Error.WriteLine("Unable to convert input type for assignment\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			} else {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = (args[0] as LuaUserdata).Value;
						return StringToLua(Convert.ToString(prop.GetValue(data, null)));
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to getter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to getter function\n" + e.Message);
					} catch (InvalidCastException e) {
						Console.Error.WriteLine("Unable to convert input type for assignment\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			}
		}

		public LuaFunction GetStringSetter(MemberInfo member) {
			var field = member as FieldInfo;
			var prop = member as PropertyInfo;

			if (field == null && prop == null)
				return null;

			if (prop != null && !prop.CanRead)
				return null; // non-public property

			if (field != null && !field.IsPublic)
				return null; // non-public field

			if (field != null) {
				return new LuaFunction((LuaValue[] args) => {
					try {
						var data = args[0].Value;
						field.SetValue(data, (args[1] as LuaString).Text);
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			} else {
				return new LuaFunction((LuaValue[] args) => {
					try {
						var data = args[0].Value;
						prop.SetValue(data, (args[1] as LuaString).Text, null);
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			}
		}

		public LuaFunction GetNumericAccessor(MemberInfo member) {
			var field = member as FieldInfo;
			var prop = member as PropertyInfo;

			if (field == null && prop == null)
				return null;

			if (prop != null && !prop.CanRead)
				return null; // non-public property

			if (field != null && !field.IsPublic)
				return null; // non-public field

			return new LuaFunction((LuaValue[] args) => {
				try {
					object data = (args[0] as LuaUserdata).Value;
					return NumericToLua(Convert.ToDouble(field != null ?
						field.GetValue(data) : prop.GetValue(data, null)));
				} catch (IndexOutOfRangeException e) {
					Console.Error.WriteLine("Insufficient parameters supplied to getter function\n" + e.Message);
				} catch (NullReferenceException e) {
					Console.Error.WriteLine("Incompatible parameter supplied to getter function\n" + e.Message);
				} catch (InvalidCastException e) {
					Console.Error.WriteLine("Unable to convert input type for assignment\n" + e.Message);
				}
				return LuaNil.Nil;
			});
		}

		public LuaFunction GetNumericSetter(MemberInfo member) {
			var field = member as FieldInfo;
			var prop = member as PropertyInfo;

			if (field == null && prop == null)
				return null;

			if (prop != null && !prop.CanRead)
				return null; // non-public property

			if (field != null && !field.IsPublic)
				return null; // non-public field

			var type = field != null ? field.FieldType : prop.PropertyType;
			
			if(type == typeof(Int32)) {
					if(field != null) {
						return new LuaFunction((LuaValue[] args) => {
							try {
								var data = args[0].Value;
								field.SetValue(data, Convert.ToInt32((args[1] as LuaNumber).Number));
							} catch (IndexOutOfRangeException e) {
								Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
							} catch (NullReferenceException e) {
								Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
							}
							return LuaNil.Nil;
						});
					} else {
						return new LuaFunction((LuaValue[] args) => {
							try {							
								var data = args[0].Value;
								prop.SetValue(data, Convert.ToInt32((args[1] as LuaNumber).Number), null);
							} catch (IndexOutOfRangeException e) {
								Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
							} catch (NullReferenceException e) {
								Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
							}
							return LuaNil.Nil;
						});
					}
				} else if(type == typeof(Int64)) {
					if(field != null) {
						return new LuaFunction((LuaValue[] args) => {
							try {	
								var data = args[0].Value;
								field.SetValue(data, Convert.ToInt64((args[1] as LuaNumber).Number));
							} catch (IndexOutOfRangeException e) {
								Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
							} catch (NullReferenceException e) {
								Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
							}
							return LuaNil.Nil;
						});
					} else {
						return new LuaFunction((LuaValue[] args) => {	
							try {								
								var data = args[0].Value;
								prop.SetValue(data, Convert.ToInt64((args[1] as LuaNumber).Number), null);
							} catch (IndexOutOfRangeException e) {
								Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
							} catch (NullReferenceException e) {
								Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
							}
							return LuaNil.Nil;
						});
					}
				} else if(type == typeof(Single)) {
					if(field != null) {
						return new LuaFunction((LuaValue[] args) => {
							try {
								var data = args[0].Value;
								field.SetValue(data, Convert.ToSingle((args[1] as LuaNumber).Number));
							} catch (IndexOutOfRangeException e) {
								Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
							} catch (NullReferenceException e) {
								Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
							}
							return LuaNil.Nil;
						});
					} else {
						return new LuaFunction((LuaValue[] args) => {		
							try {						
								var data = args[0].Value;
								prop.SetValue(data, Convert.ToSingle((args[1] as LuaNumber).Number), null);
							} catch (IndexOutOfRangeException e) {
								Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
							} catch (NullReferenceException e) {
								Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
							}
							return LuaNil.Nil;
						});
					}
			} else if (type == typeof(Double)) {
				if (field != null) {
					return new LuaFunction((LuaValue[] args) => {
						try {
							var data = args[0].Value;
							field.SetValue(data, (args[1] as LuaNumber).Number);
						} catch (IndexOutOfRangeException e) {
							Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
						} catch (NullReferenceException e) {
							Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
						}
						return LuaNil.Nil;
					});
				} else {
					return new LuaFunction((LuaValue[] args) => {
						try {
							var data = args[0].Value;
							prop.SetValue(data, (args[1] as LuaNumber).Number, null);
						} catch (IndexOutOfRangeException e) {
							Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
						} catch (NullReferenceException e) {
							Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
						}
						return LuaNil.Nil;
					});
				}
			}
			return null;
		}

		public LuaFunction GetBooleanAccessor(MemberInfo member) {
			var field = member as FieldInfo;
			var prop = member as PropertyInfo;

			if (field == null && prop == null)
				return null;

			if (prop != null && !prop.CanRead)
				return null; // non-public property

			if (field != null && !field.IsPublic)
				return null; // non-public field

			if (field != null) {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = (args[0] as LuaUserdata).Value;
						return BooleanToLua(Convert.ToBoolean(field.GetValue(data)));
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to getter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to getter function\n" + e.Message);
					} catch (InvalidCastException e) {
						Console.Error.WriteLine("Unable to convert input type for assignment\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			} else {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = (args[0] as LuaUserdata).Value;
						return BooleanToLua(Convert.ToBoolean(prop.GetValue(data, null)));
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to getter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to getter function\n" + e.Message);
					} catch (InvalidCastException e) {
						Console.Error.WriteLine("Unable to convert input type for assignment\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			}
		}

		public LuaFunction GetBooleanSetter(MemberInfo member) {
			var field = member as FieldInfo;
			var prop = member as PropertyInfo;

			if (field == null && prop == null)
				return null;

			if (prop != null && !prop.CanRead)
				return null; // non-public property

			if (field != null && !field.IsPublic)
				return null; // non-public field

			if (field != null) {
				return new LuaFunction((LuaValue[] args) => {
					try {
						var data = args[0].Value;
						var boolean = args[1] as LuaBoolean;
						field.SetValue(data, boolean.BoolValue);
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			} else {
				return new LuaFunction((LuaValue[] args) => {
					try {
						var data = args[0].Value;
						var boolean = args[1] as LuaBoolean;
						prop.SetValue(data, boolean.BoolValue, null);
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			}
		}

		public LuaFunction GetReferenceTypeAccessor(MemberInfo member) {
			var field = member as FieldInfo;
			var prop = member as PropertyInfo;

			if (field == null && prop == null)
				return null;

			if (prop != null && !prop.CanRead)
				return null; // non-public property

			if (field != null && !field.IsPublic)
				return null; // non-public field

			if ((field != null ? field.FieldType : prop.PropertyType).IsArray) {
				// TODO: Handle arrays?
				return null;
			}

			if (field != null) {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = args[0].Value;
						// If the reference is a luaComponent, be sure to attach it's metatable.
						return ObjectToLua(Convert.ChangeType(field.GetValue(data), field.FieldType));
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to getter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to getter function\n" + e.Message);
					} catch (InvalidCastException e) {
						Console.Error.WriteLine("Unable to convert input type for assignment\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			} else {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = args[0].Value;
						// If the reference is a luaComponent, be sure to attach it's metatable.
						return ObjectToLua(Convert.ChangeType(prop.GetValue(data, null), prop.PropertyType));
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to getter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to getter function\n" + e.Message);
					} catch (InvalidCastException e) {
						Console.Error.WriteLine("Unable to convert input type for assignment\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			}
		}

		public LuaFunction GetReferenceTypeSetter(MemberInfo member) {
			var field = member as FieldInfo;
			var prop = member as PropertyInfo;

			if (field == null && prop == null)
				return null;

			if (prop != null && !prop.CanRead)
				return null; // non-public property

			if (field != null && !field.IsPublic)
				return null; // non-public field

			if ((field != null ? field.FieldType : prop.PropertyType).IsArray) {
				// TODO: Handle arrays?
				return null;
			}

			if (field != null) {
				return new LuaFunction((LuaValue[] args) => {
					try {
						var data = args[0].Value;
						field.SetValue(data, Convert.ChangeType(args[1].Value, field.FieldType));
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			} else {
				return new LuaFunction((LuaValue[] args) => {
					try {
						var data = args[0].Value;
						prop.SetValue(data, Convert.ChangeType(args[1].Value, prop.PropertyType), null);
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			}
		}

		public LuaFunction GetValueTypeAccessor(MemberInfo member) {
			var field = member as FieldInfo;
			var prop = member as PropertyInfo;

			if (field == null && prop == null)
				return null;

			if (prop != null && !prop.CanRead)
				return null; // non-public property

			if (field != null && !field.IsPublic)
				return null; // non-public field

			if (field != null) {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = args[0].Value;
						return ObjectToLua(
							Convert.ChangeType(field.GetValue(data), field.FieldType));
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Unable to identify object instance\n" + e.Message);
					} catch (InvalidCastException e) {
						Console.Error.WriteLine("Unable to convert value type.\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			} else {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = args[0].Value;
						return ObjectToLua(
							Convert.ChangeType(prop.GetValue(data, null), prop.PropertyType));
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Unable to identify object instance\n" + e.Message);
					} catch (InvalidCastException e) {
						Console.Error.WriteLine("Unable to convert value type.\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			}
		}

		public LuaFunction GetValueTypeSetter(MemberInfo member) {
			var field = member as FieldInfo;
			var prop = member as PropertyInfo;

			if (field == null && prop == null)
				return null;

			if (prop != null && !prop.CanRead)
				return null; // non-public property

			if (field != null && !field.IsPublic)
				return null; // non-public field

			if (field != null) {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = args[0].Value;
						field.SetValue(data, Convert.ChangeType(args[1].Value, field.FieldType));
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to setter function." + e.Message);
					} catch (InvalidCastException e) {
						Console.Error.WriteLine("Unable to convert value type.\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			} else {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = args[0].Value;
						prop.SetValue(data, Convert.ChangeType(args[1].Value, prop.PropertyType), null);
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			}
		}

		public LuaFunction GetEnumerationAccessor(MemberInfo member) {
			var field = member as FieldInfo;
			var prop = member as PropertyInfo;

			if (field == null && prop == null)
				return null;

			if (prop != null && !prop.CanRead)
				return null; // non-public property

			if (field != null && !field.IsPublic)
				return null; // non-public field

			if(field != null) {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = (args[0] as LuaUserdata).Value;
						object value = field.GetValue(data);

						return new LuaMultiValue(new LuaValue[] {
									new LuaString(value.ToString()),
									NumericToLua(Convert.ToInt64(value))
								});
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to getter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to getter function\n" + e.Message);
					} catch (InvalidCastException e) {
						Console.Error.WriteLine("Unable to convert input type for assignment\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			} else {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = (args[0] as LuaUserdata).Value;
						object value = prop.GetValue(data, null);

						return new LuaMultiValue(new LuaValue[] {
									new LuaString(value.ToString()),
									NumericToLua(Convert.ToInt64(value))
								});
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to getter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to getter function\n" + e.Message);
					} catch (InvalidCastException e) {
						Console.Error.WriteLine("Unable to convert input type for assignment\n" + e.Message);
					}
					return LuaNil.Nil;
				});
			}
		}

		public LuaFunction GetEnumerationSetter(MemberInfo member) {
			var field = member as FieldInfo;
			var prop = member as PropertyInfo;

			if (field == null && prop == null)
				return null;

			if (prop != null && !prop.CanRead)
				return null; // non-public property

			if (field != null && !field.IsPublic)
				return null; // non-public field

			if (field != null) {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = args[0].Value;
						var arg = args[1];
						var argName = args[1] as LuaString;
						if (argName != null) {
							foreach (FieldInfo fi in field.FieldType.GetFields(BindingFlags.Public | BindingFlags.Static)) {
								if (fi.Name.ToLower() == arg.Value.ToString().ToLower()) {
									field.SetValue(data, fi.GetRawConstantValue());
								}
							}
							return LuaNil.Nil;
						}
						var argNum = args[1] as LuaNumber;
						if (argNum != null) {
							field.SetValue(data, Convert.ChangeType(argNum.Value, Enum.GetUnderlyingType(field.FieldType)));
							return LuaNil.Nil;
						}
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
					}

					return LuaNil.Nil;
				});
			} else {
				return new LuaFunction((LuaValue[] args) => {
					try {
						object data = args[0].Value;
						var arg = args[1];
						var argName = args[1] as LuaString;
						if (argName != null) {
							foreach (FieldInfo fi in prop.PropertyType.GetFields(BindingFlags.Public | BindingFlags.Static)) {
								if (fi.Name.ToLower() == arg.Value.ToString().ToLower()) {
									prop.SetValue(data, fi.GetRawConstantValue(), null);
								}
							}
							return LuaNil.Nil;
						}
						var argNum = args[1] as LuaNumber;
						if (argNum != null) {
							prop.SetValue(data, Convert.ChangeType(argNum.Value, Enum.GetUnderlyingType(prop.PropertyType)), null);
							return LuaNil.Nil;
						}
					} catch (IndexOutOfRangeException e) {
						Console.Error.WriteLine("Insufficient parameters supplied to setter function\n" + e.Message);
					} catch (NullReferenceException e) {
						Console.Error.WriteLine("Incompatible parameter supplied to setter function\n" + e.Message);
					}

					return LuaNil.Nil;
				});
			}
		}
	}
}

