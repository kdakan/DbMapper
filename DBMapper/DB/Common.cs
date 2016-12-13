using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;
using System.Collections;

namespace DBMapper {

	internal delegate object PropertyGetter(object target);

	internal delegate void PropertySetter(object target, object value);

	internal static class PropertyGetterSetterFactory {

		internal static PropertyGetter CreateGetter(PropertyInfo propertyInfo) {
			/*
			* If there's no getter return null
			*/
			MethodInfo getMethod = propertyInfo.GetGetMethod();
			if (getMethod == null)
				return null;
			/*
			* Create the dynamic method
			*/
			Type[] arguments = new Type[1];
			arguments[0] = typeof(object);
			DynamicMethod getter = new DynamicMethod(
			String.Concat("_Get", propertyInfo.Name, "_"),
			typeof(object), arguments, propertyInfo.DeclaringType);
			ILGenerator generator = getter.GetILGenerator();
			generator.DeclareLocal(typeof(object));
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
			generator.EmitCall(OpCodes.Callvirt, getMethod, null);
			if (!propertyInfo.PropertyType.IsClass)
				generator.Emit(OpCodes.Box, propertyInfo.PropertyType);

			generator.Emit(OpCodes.Ret);
			/*
			* Create the delegate and return it
			*/
			return (PropertyGetter)getter.CreateDelegate(typeof(PropertyGetter));
		}

		internal static PropertySetter CreateSetter(PropertyInfo propertyInfo) {
			/*
			* If there's no setter return null
			*/
			MethodInfo setMethod = propertyInfo.GetSetMethod();
			if (setMethod == null)
				return null;
			/*
			* Create the dynamic method
			*/
			Type[] arguments = new Type[2];
			arguments[0] = arguments[1] = typeof(object);
			DynamicMethod setter = new DynamicMethod(
			String.Concat("_Set", propertyInfo.Name, "_"),
			typeof(void), arguments, propertyInfo.DeclaringType);
			ILGenerator generator = setter.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
			generator.Emit(OpCodes.Ldarg_1);
			if (propertyInfo.PropertyType.IsClass)
				generator.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
			else
				generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);

			generator.EmitCall(OpCodes.Callvirt, setMethod, null);
			generator.Emit(OpCodes.Ret);
			/*
			* Create the delegate and return it
			*/
			return (PropertySetter)setter.CreateDelegate(typeof(PropertySetter));
		}

	}

	public class InputParameter {
		public string Name { get; set; }
		public object Value { get; set; }

		public InputParameter(string name, object value) {
			Name = name;
			Value = value;
		}

		public InputParameter() {
		}

	}

	public class InputParameterNameType {
		public string Name { get; set; }
		public Type Type { get; set; }

		public InputParameterNameType(string name, Type type) {
			Name = name;
			Type = type;
		}

		public InputParameterNameType() {
		}

	}


	public class OutputParameter {
		public string Name { get; set; }
		public object Value { get; set; }
		public Type Type { get; set; }

		public OutputParameter(string name, object value, Type type) {
			Name = name;
			Value = value;
			Type = type;
		}

		public OutputParameter() {
		}

	}

	[Serializable]
	public class RecordNotFoundOrRecordHasBeenChangedException : Exception {

		public RecordNotFoundOrRecordHasBeenChangedException()
			: base() {
		}

		public RecordNotFoundOrRecordHasBeenChangedException(string message)
			: base(message) {
		}

		public RecordNotFoundOrRecordHasBeenChangedException(string message, Exception innerException)
			: base(message, innerException) {
		}

		protected RecordNotFoundOrRecordHasBeenChangedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			: base(info, context) {
		}

	}

	//ThreadAbortException gibi bir exception olmalı ki devam edemesin, 
	//unrecoverable türde bir exception olmalı..
	[Serializable]
	public class MappingException : Exception {

		public MappingException()
			: base() {
		}

		public MappingException(string message)
			: base(message) {
		}

		public MappingException(string message, Exception innerException)
			: base(message, innerException) {
		}

		protected MappingException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			: base(info, context) {
		}

	}

	[Serializable]
	public class MappingChangeException : Exception {

		public MappingChangeException()
			: base() {
		}

		public MappingChangeException(string message)
			: base(message) {
		}

		public MappingChangeException(string message, Exception innerException)
			: base(message, innerException) {
		}

		protected MappingChangeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			: base(info, context) {
		}

	}

	[Serializable]
	public class CommandException : Exception {

		public CommandException()
			: base() {
		}

		public CommandException(string message)
			: base(message) {
		}

		public CommandException(string message, Exception innerException)
			: base(message, innerException) {
		}

		protected CommandException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
			: base(info, context) {
		}

	}

	internal static class TypeConverter {

		internal static object ConvertToPropertyType(object columnValue, Type propertyType) {
			object returnObject = null;

			Type underlyingTypeForNullable = Nullable.GetUnderlyingType(propertyType);
			if (underlyingTypeForNullable == null)
				returnObject = Convert.ChangeType(columnValue, propertyType, CultureInfo.InvariantCulture);
			else {
				if (underlyingTypeForNullable.IsEnum) {
					if (columnValue.GetType() != typeof(int))
						columnValue = Convert.ChangeType(columnValue, typeof(int), CultureInfo.InvariantCulture);
					returnObject = Enum.ToObject(underlyingTypeForNullable, (int)columnValue);
				}
				else {
					if (columnValue.GetType() != underlyingTypeForNullable)
						columnValue = Convert.ChangeType(columnValue, underlyingTypeForNullable, CultureInfo.InvariantCulture);
					returnObject = nullableConverterDelegateDictionaryAtConstant[underlyingTypeForNullable](columnValue);
				}
			}

			return returnObject;
		}

		//valuetype'lar generic tip olarak geçirilemediği için her birine ayrı metod gerekiyor
		//lock etmeye gerek yok, değişmiyor
		private static readonly Dictionary<Type, Func<object, object>> nullableConverterDelegateDictionaryAtConstant = new Dictionary<Type, Func<object, object>>() {
		  { typeof(Boolean), delegate(object value) { return (Boolean?)value; } },
		  { typeof(Byte), delegate(object value) { return (Byte?)value; } },
		  { typeof(SByte), delegate(object value) { return (SByte?)value; } },
		  { typeof(Char), delegate(object value) { return (Char?)value; } },
		  { typeof(Single), delegate(object value) { return (Single?)value; } },
		  { typeof(Double), delegate(object value) { return (Double?)value; } },
		  { typeof(Decimal), delegate(object value) { return (Decimal?)value; } },
		  { typeof(Int16), delegate(object value) { return (Int16?)value; } },
		  { typeof(UInt16), delegate(object value) { return (UInt16?)value; } },
		  { typeof(Int32), delegate(object value) { return (Int32?)value; } },
		  { typeof(UInt32), delegate(object value) { return (UInt32?)value; } },
		  { typeof(Int64), delegate(object value) { return (Int64?)value; } },
		  { typeof(UInt64), delegate(object value) { return (UInt64?)value; } },
		  { typeof(DateTime), delegate(object value) { return (DateTime?)value; } },
    };

		internal static object GetDefaultValue(Type type) {
			if (!type.IsValueType)
				return null;

			Type valueType;
			if (type.IsEnum)
				valueType = Enum.GetUnderlyingType(type);
			else
				valueType = type;

			object defaultValue;
			if (DB.valueTypesDefaultValueDictionaryAtConstant.TryGetValue(valueType, out defaultValue))
				return defaultValue;
			else {
				//nullable tipler de buraya giriyor
				return null;
			}
		}

	}

	internal static class NestedPropertyHelper {

		internal static List<string> CreateNestedPropertyNameList(List<PropertyInfo> propertyInfoList) {
			//system classlar hariç, içerilen tüm nested classları ağacın dalları şeklinde tarayıp içerdikleri tüm propertyleri listeler
			List<string> nestedPropertyNameList = new List<string>();
			foreach (PropertyInfo propertyInfo in propertyInfoList) {
				if (propertyInfo.PropertyType.IsClass && propertyInfo.PropertyType.Namespace != "System" && !propertyInfo.PropertyType.Namespace.StartsWith("System.", StringComparison.Ordinal)) {
					List<string> innerNestedPropertyNameList = CreateNestedPropertyNameList(propertyInfo.PropertyType.GetProperties().ToList());
					foreach (string innerNestedPropertyName in innerNestedPropertyNameList)
						nestedPropertyNameList.Add(propertyInfo.Name + "." + innerNestedPropertyName);
				}
				else
					nestedPropertyNameList.Add(propertyInfo.Name);
			}

			return nestedPropertyNameList;
		}

		internal static PropertyInfo CreatePropertyInfoForNestedPropertyName(Type rootType, string nestedPropertyName) {
			if (!nestedPropertyName.Contains("."))
				return rootType.GetProperty(nestedPropertyName);
			else {
				int firstDotPosition = nestedPropertyName.IndexOf(".", StringComparison.Ordinal);
				string nestedPropertyNameAfterFirstDot = nestedPropertyName.Substring(firstDotPosition + 1);
				string propertyNameBeforeFirstDot = nestedPropertyName.Substring(0, firstDotPosition);
				PropertyInfo propertyInfoBeforeFirstDot = rootType.GetProperty(propertyNameBeforeFirstDot);
				return CreatePropertyInfoForNestedPropertyName(propertyInfoBeforeFirstDot.PropertyType, nestedPropertyNameAfterFirstDot);
			}
		}

	}

	internal class EmptyCacheEntry {
	}

	public interface IMapEntity {
		void MapEntity();
	}

	public interface IMapConnection {
		void MapConnection();
	}

}
