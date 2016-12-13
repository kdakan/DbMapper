using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Xml;
using System.Configuration;
using System.Globalization;

namespace DBMapper {

	public static class DB {

		//public static ConnectionStringSettingsCollection ConnectionStringSettingsCollection { internal get; set; }

		//TODO: single thread client side hosting seçeneği için reuseDbCommands = true olarak set edebilmeli, map class üzerinde public olarak set eden bir metod oluştur
		internal static bool reuseDbCommands = false;
		//lock etmeye gerek yok, değişmiyor
		internal static readonly Dictionary<Type, object> valueTypesDefaultValueDictionaryAtConstant = new Dictionary<Type, object> {
		  { typeof(Boolean), default(Boolean) },
		  { typeof(Byte), default(Byte) },
		  { typeof(SByte), default(SByte) },
		  { typeof(Char), default(Char) },
		  { typeof(Single), default(Single) },
		  { typeof(Double), default(Double) },
		  { typeof(Decimal), default(Decimal) },
		  { typeof(Int16), default(Int16) },
		  { typeof(UInt16), default(UInt16) },
		  { typeof(Int32), default(Int32) },
		  { typeof(UInt32), default(UInt32) },
		  { typeof(Int64), default(Int64) },
		  { typeof(UInt64), default(UInt64) },
		  { typeof(DateTime), default(DateTime) },
		};

		internal const string informationSchemaSelectSql =
			@"SELECT UPPER(COLUMN_NAME) AS COLUMN_NAME,
			CASE WHEN DATA_TYPE = 'timestamp' OR DATA_TYPE = 'TIMESTAMP' THEN 
			'byte(8)' ELSE DATA_TYPE + CASE WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL THEN 
			'(' + CAST(CHARACTER_MAXIMUM_LENGTH AS VARCHAR) + ')' ELSE '' END END AS DECLARE_TYPE
			FROM INFORMATION_SCHEMA.COLUMNS
			WHERE UPPER(TABLE_SCHEMA) = '";//'DBO' AND UPPER(TABLE_NAME) = 'TEST'

		//lock etmeye gerek yok, değişmiyor
		internal static readonly Dictionary<Type, KeyValuePair<DbType, int>> dbTypeAndMaxSizePairDictionaryAtConstant = new Dictionary<Type, KeyValuePair<DbType, int>>() {
      { typeof(Boolean), new KeyValuePair<DbType, int>(DbType.Boolean, sizeof(Boolean) * 2) },
      { typeof(Byte), new KeyValuePair<DbType, int>(DbType.Byte, sizeof(Byte) * 2) },
      { typeof(SByte), new KeyValuePair<DbType, int>(DbType.SByte, sizeof(SByte) * 2) },
      { typeof(Char), new KeyValuePair<DbType, int>(DbType.Int32, sizeof(Int32) * 2) },
      { typeof(Single), new KeyValuePair<DbType, int>(DbType.Single, sizeof(Single) * 2) },
      { typeof(Double), new KeyValuePair<DbType, int>(DbType.Double, sizeof(Double) * 2) },
      { typeof(Decimal), new KeyValuePair<DbType, int>(DbType.Decimal, sizeof(Decimal) * 2) },
      { typeof(Int16), new KeyValuePair<DbType, int>(DbType.Int16, sizeof(Int16) * 2) },
      { typeof(UInt16), new KeyValuePair<DbType, int>(DbType.UInt16, sizeof(UInt16) * 2) },
      { typeof(Int32), new KeyValuePair<DbType, int>(DbType.Int32, sizeof(Int32) * 2) },
      { typeof(UInt32), new KeyValuePair<DbType, int>(DbType.UInt32, sizeof(UInt32) * 2) },
      { typeof(Int64), new KeyValuePair<DbType, int>(DbType.Int64, sizeof(Int64) * 2) },
      { typeof(UInt64), new KeyValuePair<DbType, int>(DbType.UInt64, sizeof(UInt64) * 2) },
      { typeof(DateTime), new KeyValuePair<DbType, int>(DbType.DateTime, sizeof(Byte) * 8 * 2) },

			{ typeof(String), new KeyValuePair<DbType, int>(DbType.String, dbParameterMaxSize) },
      { typeof(Byte[]), new KeyValuePair<DbType, int>(DbType.Binary, dbParameterMaxSize) },
      { typeof(XmlNode), new KeyValuePair<DbType, int>(DbType.Xml, dbParameterMaxSize) },
    };

		//dbParameter max. size'lar garanti olsun diye 2'yle çarpıldı, buna gerek var mı..
		internal const int dbParameterMaxSize = 1024 * 64 * 2;
		internal const int dbParameterTimestampSize = sizeof(Byte) * 8 * 2;

		internal static string schemaDotName(string schemaName, string name) {
			if (!string.IsNullOrEmpty(schemaName))
				return schemaName + "." + name;
			else
				return name;
		}

		internal static void throwIfNullOrEmpty<X>(object value, string name) where X : Exception, new() {
			if (value == null || value.ToString() == string.Empty)
				throw (X)Activator.CreateInstance(typeof(X), new object[] { name + " cannot be null or empty." });

			if (value is ICollection)
				foreach (var item in ((ICollection)value))
					if (value == null || value.ToString() == string.Empty)
						throw (X)Activator.CreateInstance(typeof(X), new object[] { name + " item cannot be null or empty." });
		}

		internal static void throwIfNullOrEmpty<X>(int value, string name) where X : Exception, new() {
			if (value == 0)
				throw (X)Activator.CreateInstance(typeof(X), new object[] { name + " cannot be zero." });
		}

		internal static void throwIfKeyAlreadyExists<K, V, X>(IDictionary<K, V> dictionary, K key) where X : Exception, new() {
			if (dictionary != null && dictionary.ContainsKey(key))
				throw (X)Activator.CreateInstance(typeof(X), new object[] { key + " already exists." });
		}

		internal static void tryInvokeTriggerAction(Action actionDelegate) {
			try {
				actionDelegate.Invoke();
			}
			catch (TargetInvocationException ex) {
				if (ex.InnerException != null) {
					Console.WriteLine(ex.InnerException.Message);
					Console.WriteLine(ex.InnerException.StackTrace);
					throw ex.InnerException;
				}
				else {
					Console.WriteLine(ex.Message);
					Console.WriteLine(ex.StackTrace);
					throw;
				}
			}
		}

		internal static void tryDynamicInvokeTriggerAction(Delegate triggerActionDelegate, object entity) {
			try {
				triggerActionDelegate.DynamicInvoke(entity);
			}
			catch (TargetInvocationException ex) {
				if (ex.InnerException != null) {
					Console.WriteLine(ex.InnerException.Message);
					Console.WriteLine(ex.InnerException.StackTrace);
					throw ex.InnerException;
				}
				else {
					Console.WriteLine(ex.Message);
					Console.WriteLine(ex.StackTrace);
					throw;
				}
			}
		}

		internal static object tryInvokeAutoSetFunction(Func<object> autoSetFunctionDelegate) {
			try {
				return autoSetFunctionDelegate.Invoke();
			}
			catch (TargetInvocationException ex) {
				if (ex.InnerException != null) {
					Console.WriteLine(ex.InnerException.Message);
					Console.WriteLine(ex.InnerException.StackTrace);
					throw ex.InnerException;
				}
				else {
					Console.WriteLine(ex.Message);
					Console.WriteLine(ex.StackTrace);
					throw;
				}
			}

		}

		internal static void tryDynamicInvokeValidatorFunction(Delegate validatorFunctionDelegate, object entity) {
			try {
				object validatorResult = validatorFunctionDelegate.DynamicInvoke(entity);
				if (!(validatorResult is bool) || !((bool)validatorResult))
					throw new Exception("Validation failed.");
			}
			catch (TargetInvocationException ex) {
				if (ex.InnerException != null) {
					Console.WriteLine(ex.InnerException.Message);
					Console.WriteLine(ex.InnerException.StackTrace);
					throw ex.InnerException;
				}
				else {
					Console.WriteLine(ex.Message);
					Console.WriteLine(ex.StackTrace);
					throw;
				}
			}

		}

		internal static string getKeyForInputParameters(InputParameter[] inputParameters) {
			string queryResultsCacheKeyNamesPart = "#";
			string queryResultsCacheKeyValuesPart = "#";
			if (inputParameters != null) {
				var sortedInputParameters = inputParameters.OrderBy(x => x.Name);
				foreach (InputParameter inputParameter in sortedInputParameters) {
					if (inputParameter.Name.Contains(","))
						throw new CommandException("Database parameter name cannot be null or empty.");
					else {
						queryResultsCacheKeyNamesPart += inputParameter.Name + ",";
						if (inputParameter.Value == null || (inputParameter.Value is ICollection && (inputParameter.Value as ICollection).Count == 0)) {
							queryResultsCacheKeyNamesPart += "<-NULL" + ",";
							queryResultsCacheKeyValuesPart += "null,";
						}
						else {
							if (inputParameter.Value is ICollection)
								queryResultsCacheKeyValuesPart += "(" + getKeyForCollectionInputParameterValue(inputParameter.Value as ICollection) + "),";
							else
								queryResultsCacheKeyValuesPart += inputParameter.Value.ToString() + ",";
						}
					}
				}
			}

			return queryResultsCacheKeyNamesPart.TrimEnd(new char[] { ',' }) + queryResultsCacheKeyValuesPart.TrimEnd(new char[] { ',' });
		}

		internal static string getKeyForCollectionInputParameterValue(ICollection collectionValue) {
			string collectionKey = "#";
			if (collectionValue != null) {
				int i = 1;
				foreach (object o in collectionValue) {
					if (o != null)
						collectionKey += i + "=" + o.ToString() + ",";
					else
						collectionKey += i + "=isNull,";
					i++;
				}
			}

			return collectionKey.TrimEnd(new char[] { ',' });
		}

		internal static void SetDbParameterValueAndDbTypeForNonTimestamp(DbParameter dbParameter, object value, Type propertyType) {
			SetDbParameterValueAndDbTypeForNonTimestamp(dbParameter, value, propertyType, 0);
		}

		internal static void SetDbParameterValueAndDbTypeForNonTimestamp(DbParameter dbParameter, object value, Type propertyType, int size) {
			if (value != null && value != DBNull.Value)
				dbParameter.Value = value;
			else {
				dbParameter.Value = DBNull.Value;
				DbType dbType;
				Type underlyingType = Nullable.GetUnderlyingType(propertyType);
				Type type = null;
				if (underlyingType != null)
					type = underlyingType;
				else
					type = propertyType;

				KeyValuePair<DbType, int> dbTypeAndMaxSizePair;
				if (DB.dbTypeAndMaxSizePairDictionaryAtConstant.TryGetValue(type, out dbTypeAndMaxSizePair))
					dbParameter.DbType = dbTypeAndMaxSizePair.Key;

				if (size != 0)
					dbParameter.Size = size;
				else {
					if (dbTypeAndMaxSizePair.Value != 0)
						dbParameter.Size = dbTypeAndMaxSizePair.Value;
					else
						dbParameter.Size = DB.dbParameterMaxSize;
				}
			}
		}

		#region CRUD(Insert/Select/Update/Delete)

		public static IList<T> Select<T>(Enum queryName, params InputParameter[] inputParameters) where T : class, new() {
			List<T> result = null;
			Type entityType = typeof(T);

			EntityMapping entityMapping = Map.entityMappingDictionaryAtMapper[entityType];
			QueryMapping queryMapping;
			if (!entityMapping.QueryMappingDictionary.TryGetValue(queryName.ToString(), out queryMapping))
				throw new CommandException("Query mapping cannot be null or empty.");

			if (entityMapping.BeforeSelectCommandTriggerActionDelegate != null)
				DB.tryInvokeTriggerAction(entityMapping.BeforeSelectCommandTriggerActionDelegate);

			if (!queryMapping.IsQueryResultCached)
				result = new SelectCommand(queryName, inputParameters, entityMapping, queryMapping).Execute<T>();
			else {
				string keyForInputParameters = DB.getKeyForInputParameters(inputParameters);
				object cachedResult;
				//TODO: burada aynı query parametre değerleriyle select edildiğinde aynı cache nesnesi lock ediliyor, 
				//uzun süren queryler, cacheden okumak isteyen diğer queryleri bekletecek, çok büyük performans sorunu yaşanır..
				//bunun yerine, dictionary'ye (key, null/dummy) olarak ekle, komutun çalışma süreci locksız olsun,
				//null veya dummy kontrolü locklı olacak, sadece aynı key(parameter değerleri) ile select edenler
				//birbirini bekleyecek, ilk locktan çıktığında tekrar null/dummy kontrolü olacak, 
				//doluysa komut çalıştırılmayacak cachedeki bu değer dönülecek

				//lock (((IDictionary)queryMapping.queryResultCache).SyncRoot) {
				//  if (queryMapping.TryGetCachedQueryResult(keyForDatabadeParameters, out cachedResult))
				//    result = (cachedResult as List<T>);
				//  else {
				//    result = new SelectCommand(queryName, inputParameters, entityMapping, queryMapping).Execute<T>();
				//    queryMapping.AddToQueryResultCache(keyForDatabadeParameters, result);
				//  }
				//}

				//TODO: burası yeniden yazıldı, tekrar test et

				EmptyCacheEntry emptyCacheEntry = null;
				lock (((IDictionary)queryMapping.queryResultCache).SyncRoot) {
					if (queryMapping.TryGetCachedQueryResult(keyForInputParameters, out cachedResult) && cachedResult != null && !(cachedResult is EmptyCacheEntry))
						result = (cachedResult as List<T>);
					else {
						emptyCacheEntry = new EmptyCacheEntry();
						queryMapping.AddToQueryResultCache(keyForInputParameters, emptyCacheEntry);
					}
				}

				if (emptyCacheEntry != null) {
					result = new SelectCommand(queryName, inputParameters, entityMapping, queryMapping).Execute<T>();
					lock (((IDictionary)queryMapping.queryResultCache).SyncRoot) {
						queryMapping.queryResultCache[keyForInputParameters] = result;
					}
				}
			}

			Delegate afterSelectActionDelegate = null;
			if (entityMapping.TriggerActionDelegateDictionary.TryGetValue(When.AfterSelectForEachRow, out afterSelectActionDelegate))
				foreach (T entity in result)
					DB.tryDynamicInvokeTriggerAction(afterSelectActionDelegate, entity);

			return result;
		}

		public static void GenerateSelect(EntityMapping entityMapping, QueryMapping queryMapping) {
			//TODO: inputparameter nameler tanımlanmalı, QueryNestedPropertyNameList de doldurulacak, sonrasında SelectCommandGenerator tekrar yazılacak
			//burada da cache dolduruluyor, inputparameter nameler tanımlanmadığı için şimdilik es geçtim sonra bir bak
			//DB.getKeyForInputParameters(inputParameters);
			new SelectCommandGenerator(entityMapping, queryMapping).Generate();
		}

		public static T SelectFirst<T>(Enum queryName, params InputParameter[] inputParameters) where T : class, new() {
			List<T> selectedList = Select<T>(queryName, inputParameters).ToList();
			if (selectedList.Count > 0)
				return selectedList[0];
			else
				return null;
		}

		public static void Insert(object entity) {
			EntityMapping entityMapping = Map.entityMappingDictionaryAtMapper[entity.GetType()];
			entityMapping.ConnectionMapping.CommandFactory.CreateInsertCommand(entity, entityMapping).Execute();
		}

		public static void GenerateInsert(EntityMapping entityMapping) {
			entityMapping.ConnectionMapping.CommandGeneratorFactory.CreateInsertCommandGenerator(entityMapping).Generate();
		}

		public static void Update(object entity, Enum queryName) {
			Type entityType = entity.GetType();
			EntityMapping entityMapping;
			entityMapping = Map.entityMappingDictionaryAtMapper[entityType];

			QueryMapping queryMapping;
			if (!entityMapping.QueryMappingDictionary.TryGetValue(queryName.ToString(), out queryMapping))
				throw new CommandException("Query mapping cannot be null or empty.");

			entityMapping.ConnectionMapping.CommandFactory.CreateUpdateCommand(entity, entityMapping, queryMapping).Execute();

		}

		public static void GenerateUpdate(EntityMapping entityMapping, QueryMapping queryMapping) {
			entityMapping.ConnectionMapping.CommandGeneratorFactory.CreateUpdateCommandGenerator(entityMapping, queryMapping).Generate();
		}

		public static void Delete(object entity) {
			new DeleteCommand(entity).Execute();
		}

		public static void GenerateDelete(EntityMapping entityMapping) {
			new DeleteCommandGenerator(entityMapping).Generate();
		}

		#endregion CRUD(Insert/Select/Update/Delete)

		#region CallProcedure/Function

		public static void CallProcedure(Enum connectionName, Enum schemaName, Enum storedProcedureName, params InputParameter[] inputParameters) {
			CallProcedure(connectionName, schemaName, storedProcedureName, null, inputParameters);
		}

		public static void CallProcedure(Enum connectionName, Enum schemaName, Enum storedProcedureName, OutputParameter[] outputParameters, params InputParameter[] inputParameters) {
			callProcedure(connectionName, schemaName, storedProcedureName, outputParameters, inputParameters);
		}

		private static void callProcedure(Enum connectionName, Enum schemaName, Enum storedProcedureName, OutputParameter[] outputParameters, InputParameter[] inputParameters) {
			DB.throwIfNullOrEmpty<CommandException>(connectionName, "Connection name");
			DB.throwIfNullOrEmpty<CommandException>(storedProcedureName, "Stored procedure name");

			ConnectionMapping connectionMapping;
			if (!Map.connectionMappingDictionaryAtMapper.TryGetValue(connectionName.ToString(), out connectionMapping))
				throw new CommandException("Connection mapping cannot be null or empty.");

			string schemaNameDotStoredProcedureName = storedProcedureName.ToString();
			if (schemaName != null)
				schemaNameDotStoredProcedureName = schemaName.ToString() + "." + storedProcedureName;

			DbCommand dbCommand = connectionMapping.CreateOrGetStoredProcedureCallDbCommand(schemaNameDotStoredProcedureName);

			if (inputParameters != null)
				foreach (InputParameter inputParameter in inputParameters) {
					object value = null;
					if (inputParameter.Value == null) {
						value = DBNull.Value;
					}
					else
						value = inputParameter.Value;

					DbParameter dbParameter = connectionMapping.CreateDbParameter(inputParameter.Name, value);
					dbCommand.Parameters.Add(dbParameter);
				}

			if (outputParameters != null)
				foreach (OutputParameter outputParameter in outputParameters) {
					DbParameter dbParameter = connectionMapping.CreateDbParameterForInputOutputNonTimestamp(outputParameter.Name, outputParameter.Value, outputParameter.Type);
					dbCommand.Parameters.Add(dbParameter);
				}

			using (dbCommand.Connection = connectionMapping.CreateDbConnection()) {
				try {
					dbCommand.Connection.Open();
					dbCommand.ExecuteNonQuery();
				}
				catch (Exception ex) {
					throw;
				}
			}

			if (outputParameters != null)
				foreach (DbParameter dbParameter in dbCommand.Parameters) {
					OutputParameter outputParameter = outputParameters.ToList().Find(x => x.Name == dbParameter.ParameterName);
					if (outputParameter != null)
						outputParameter.Value = dbParameter.Value;
				}

		}

		public static T CallFunction<T>(Enum connectionName, Enum schemaName, Enum dbFunctionName, params InputParameter[] inputParameters) {
			return CallFunction<T>(connectionName, schemaName, dbFunctionName, null, inputParameters);
		}

		public static T CallFunction<T>(Enum connectionName, Enum schemaName, Enum dbFunctionName, OutputParameter[] outputParameters, InputParameter[] inputParameters) {
			object returnValue = callFunction(connectionName, schemaName, dbFunctionName, outputParameters, inputParameters, typeof(T));
			return (T)TypeConverter.ConvertToPropertyType(returnValue, typeof(T));
		}

		private static object callFunction(Enum connectionName, Enum schemaName, Enum dbFunctionName, OutputParameter[] outputParameters, InputParameter[] inputParameters, Type returnType) {
			DB.throwIfNullOrEmpty<CommandException>(connectionName, "Connection name");
			DB.throwIfNullOrEmpty<CommandException>(dbFunctionName, "DB function name");

			ConnectionMapping connectionMapping;
			if (!Map.connectionMappingDictionaryAtMapper.TryGetValue(connectionName.ToString(), out connectionMapping))
				throw new CommandException("Connection mapping cannot be null or empty.");

			string schemaNameDotDBFunctionName = dbFunctionName.ToString();
			if (schemaName != null)
				schemaNameDotDBFunctionName = schemaName.ToString() + "." + dbFunctionName;

			DbCommand dbCommand = connectionMapping.CreateOrGetDBFunctionCallDbCommand(schemaNameDotDBFunctionName);

			if (inputParameters != null)
				foreach (InputParameter inputParameter in inputParameters) {
					object value = null;
					if (inputParameter.Value == null) {
						value = DBNull.Value;
					}
					else
						value = inputParameter.Value;

					DbParameter dbParameter = connectionMapping.CreateDbParameter(inputParameter.Name, value);
					dbCommand.Parameters.Add(dbParameter);
				}

			if (outputParameters != null)
				foreach (OutputParameter outputParameter in outputParameters) {
					DbParameter dbParameter = connectionMapping.CreateDbParameterForInputOutputNonTimestamp(outputParameter.Name, outputParameter.Value, outputParameter.Type);
					dbCommand.Parameters.Add(dbParameter);
				}

			object returnValue = null;

			if (connectionMapping.DBVendor == DBVendor.Oracle) {
				DbParameter returnValueDbParameter = connectionMapping.CreateDbParameterForReturnValueNonTimestamp(returnType);
				dbCommand.Parameters.Add(returnValueDbParameter);

				using (dbCommand.Connection = connectionMapping.CreateDbConnection()) {
					try {
						dbCommand.Connection.Open();
						dbCommand.ExecuteNonQuery();
					}
					catch (Exception ex) {
						throw;
					}
				}

				foreach (DbParameter dbParameter in dbCommand.Parameters) {
					if (dbParameter.ParameterName == "return_value")
						returnValue = dbParameter.Value;
				}
			}
			else if (connectionMapping.DBVendor == DBVendor.Microsoft) {
				using (dbCommand.Connection = connectionMapping.CreateDbConnection()) {
					dbCommand.Connection.Open();
					using (IDataReader dataReader = dbCommand.ExecuteReader()) {
						dataReader.Read();
						returnValue = dataReader.GetValue(0);
					}
				}
			}

			if (outputParameters != null)
				foreach (DbParameter dbParameter in dbCommand.Parameters) {
					OutputParameter outputParameter = outputParameters.ToList().Find(x => x.Name == dbParameter.ParameterName);
					if (outputParameter != null)
						outputParameter.Value = dbParameter.Value;
				}


			return returnValue;
		}

		#endregion CallProcedure/Function

	}


}

