using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data;
using System.Globalization;
using System.Data.Common;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using MyCollections;

namespace DBMapper {

	public class ConnectionMapping {

		public void ChangeConnectionString(string connectionString) {
			DB.throwIfNullOrEmpty<MappingChangeException>(connectionString, "Connection string");
			ConnectionString = connectionString;
		}

		public void ChangeDBVendor(DBVendor dbVendor) {
			DB.throwIfNullOrEmpty<MappingChangeException>(dbVendor, "DB vendor");
			DBVendor = dbVendor;
			setDBVendor(dbVendor);
		}

		public void ChangeUsedDBParameterPrefix(string usedDBParameterPrefix) {
			DB.throwIfNullOrEmpty<MappingChangeException>(usedDBParameterPrefix, "Used DB parameter prefix");
			UsedDBParameterPrefix = usedDBParameterPrefix;
		}

		public string ConnectionName { get; private set; }
		private DbProviderFactory dbProviderFactory;

		public string ProviderInvariantName { get; private set; }
		public string ConnectionString { get; private set; }
		public DBVendor DBVendor { get; private set; }
		public string DBParameterPrefix { get; private set; }
		public string UsedDBParameterPrefix { get; private set; }

		internal CommandFactory CommandFactory;
		internal CommandGeneratorFactory CommandGeneratorFactory;

		//locked
		internal static Dictionary<string, DbCommand> storedProcedureDBFunctionCallDbCommandCache = new Dictionary<string, DbCommand>();

		internal ConnectionMapping(string connectionName, string connectionString, DBVendor dbVendor, string usedDBParameterPrefix) {
			ConnectionName = connectionName;
			ConnectionString = connectionString;
			UsedDBParameterPrefix = usedDBParameterPrefix;
			setDBVendor(dbVendor);
		}

		private void setDBVendor(DBVendor dbVendor) {
			DBVendor = dbVendor;
			if (dbVendor == DBVendor.Oracle) {
				ProviderInvariantName = "Oracle.DataAccess.Client";
				DBParameterPrefix = ":";
				CommandFactory = new OracleCommandFactory();
				CommandGeneratorFactory = new OracleCommandGeneratorFactory();
			}
			else if (dbVendor == DBVendor.Microsoft) {
				ProviderInvariantName = "System.Data.SqlClient";
				DBParameterPrefix = "@";
				CommandFactory = new SqlServerCommandFactory();
				CommandGeneratorFactory = new SqlServerCommandGeneratorFactory();
			}
			dbProviderFactory = DbProviderFactories.GetFactory(ProviderInvariantName);
		}

		internal DbConnection CreateDbConnection() {
			DbConnection dbConnection = dbProviderFactory.CreateConnection();
			dbConnection.ConnectionString = ConnectionString;
			return dbConnection;
		}

		internal DbCommand CreateTextDbCommand(string sql) {
			DbCommand dbCommand = dbProviderFactory.CreateCommand();
			dbCommand.CommandText = sql;
			dbCommand.CommandType = CommandType.Text;
			if (DBVendor == DBVendor.Oracle)
				(dbCommand as Oracle.DataAccess.Client.OracleCommand).BindByName = true;

			return dbCommand;
		}

		//TODO: tek başına çağrılmamalı, ardından mutlaka dbtype, value, size set edilmeli, kullanımını kontrol et
		internal DbParameter CreateDbParameter(string parameterName) {
			DbParameter dbParameter = dbProviderFactory.CreateParameter();
			dbParameter.ParameterName = parameterName;

			return dbParameter;
		}

		//TODO: tek başına çağrılmamalı, ardından mutlaka dbtype, value, size set edilmeli, kullanımını kontrol et
		internal DbParameter CreateDbParameter(string parameterName, object value) {
			DbParameter dbParameter = dbProviderFactory.CreateParameter();
			dbParameter.ParameterName = parameterName;
			if (value == null)
				dbParameter.Value = DBNull.Value;
			else
				dbParameter.Value = value;

			return dbParameter;
		}

		internal DbParameter CreateDbParameterForInputOutputNonTimestamp(string parameterName, object value, Type type) {
			return createDbParameter(ParameterDirection.InputOutput, parameterName, value, type);
		}

		internal DbParameter CreateDbParameterForInputForQueryColumns(string parameterName, object value, Type type) {
			return createDbParameter(ParameterDirection.Input, parameterName, value, type);
		}

		internal DbParameter CreateDbParameterForReturnValueNonTimestamp(Type type) {
			return createDbParameter(ParameterDirection.ReturnValue, "return_value", null, type);
		}

		private DbParameter createDbParameter(ParameterDirection direction, string parameterName, object value, Type type) {
			DbParameter dbParameter = dbProviderFactory.CreateParameter();
			dbParameter.ParameterName = parameterName;
			dbParameter.Direction = direction;
			DB.SetDbParameterValueAndDbTypeForNonTimestamp(dbParameter, value, type);

			return dbParameter;
		}

		internal string ReplaceUsedDBParameterPrefixAndExtractCollectionParameters(string sql, InputParameter[] inputParameters) {
			string replacedSql = replaceUsedDBParameterPrefix(sql, inputParameters);
			string extractedSql = extractCollectionParameters(replacedSql, inputParameters);
			return extractedSql;
		}

		internal string replaceUsedDBParameterPrefix(string sql, InputParameter[] inputParameters) {
			foreach (InputParameter inputParameter in inputParameters)
				sql = sql.Replace(UsedDBParameterPrefix + inputParameter.Name, DBParameterPrefix + inputParameter.Name);

			return sql;
		}

		internal string replaceUsedDBParameterPrefix(string sql, InputParameterNameType[] inputParameterNameTypes) {
			string[] inputParameterNames = new string[inputParameterNameTypes.Length];
			for (int i = 0; i < inputParameterNameTypes.Length; i++)
				inputParameterNames[i] = inputParameterNameTypes[i].Name;

			foreach (string inputParameterName in inputParameterNames)
				sql = sql.Replace(UsedDBParameterPrefix + inputParameterName, DBParameterPrefix + inputParameterName);

			return sql;
		}

		private string extractCollectionParameters(string sql, InputParameter[] inputParameters) {
			foreach (InputParameter inputParameter in inputParameters) {
				if (inputParameter.Value is ICollection && inputParameter.Value != null) {
					StringBuilder stringBuilder = new StringBuilder();
					// mesela :yilListe isminde collection türünde 3 itemı bulunan bir parametre ile select ediliyorsa,
					// sql içindeki :yilListe -> :yilListe__1, :yilListe__2, :yilListe__3 e dönüştürülür
					// dbCommand'e dbParameter'lar eklenirken de aynı şekilde eklenecek
					int count = (inputParameter.Value as ICollection).Count;
					for (int i = 1; i <= count; i++) {
						stringBuilder.Append(DBParameterPrefix);
						stringBuilder.Append(inputParameter.Name);
						stringBuilder.Append("__");
						stringBuilder.Append(i);
						stringBuilder.Append(",");
					}
					string extractedCollectionParameter = stringBuilder.ToString().TrimEnd(new char[] { ',' });
					if (!string.IsNullOrEmpty(extractedCollectionParameter))
						sql = sql.Replace(DBParameterPrefix + inputParameter.Name, extractedCollectionParameter);
				}
			}
			return sql;
		}

		internal DbParameter CreateInputOutputDbParameterFromValueAndNestedPropertyTypeForTimestamp(string columnName, object value) {
			DbParameter dbParameter = CreateDbParameter(columnName);
			dbParameter.Direction = ParameterDirection.InputOutput;
			SetDbParameterValueAndDbTypeForTimestamp(dbParameter, value);
			return dbParameter;
		}

		internal void SetDbParameterValueAndDbTypeForTimestamp(DbParameter dbParameter, object value) {
			if (value != null) {
				if (DBVendor == DBVendor.Oracle)
					dbParameter.Value = DateTime.FromBinary(BitConverter.ToInt64((byte[])value, 0));
				else
					dbParameter.Value = value;
			}
			else {
				dbParameter.Value = DBNull.Value;
				if (DBVendor == DBVendor.Oracle) {
					dbParameter.DbType = DbType.DateTime;
					dbParameter.Size = DB.dbParameterTimestampSize;
				}
				else if (DBVendor == DBVendor.Microsoft) {
					dbParameter.DbType = DbType.Binary;
					dbParameter.Size = DB.dbParameterTimestampSize;
				}
			}
		}

		private DbCommand createStoredProcedureCallDbCommand(string storedProcedureName) {
			DbCommand dbCommand = null;
			dbCommand = dbProviderFactory.CreateCommand();
			dbCommand.CommandText = storedProcedureName;
			dbCommand.CommandType = CommandType.StoredProcedure;
			if (DBVendor == DBVendor.Oracle)
				((Oracle.DataAccess.Client.OracleCommand)dbCommand).BindByName = true;

			return dbCommand;
		}

		internal DbCommand CreateOrGetStoredProcedureCallDbCommand(string storedProcedureName) {
			DbCommand dbCommand = null;
			if (!DB.reuseDbCommands)
				dbCommand = createStoredProcedureCallDbCommand(storedProcedureName);
			else {
				lock (((IDictionary)storedProcedureDBFunctionCallDbCommandCache).SyncRoot) {
					if (storedProcedureDBFunctionCallDbCommandCache.TryGetValue(storedProcedureName, out dbCommand)) {
						dbCommand.Parameters.Clear();
					}
					else {
						dbCommand = createStoredProcedureCallDbCommand(storedProcedureName);
						storedProcedureDBFunctionCallDbCommandCache.Add(storedProcedureName, dbCommand);
					}
				}
			}

			return dbCommand;
		}

		private DbCommand createDBFunctionCallDbCommand(string dbFunctionName) {
			DbCommand dbCommand = null;
			dbCommand = dbProviderFactory.CreateCommand();
			if (DBVendor == DBVendor.Oracle) {
				dbCommand.CommandText = dbFunctionName;
				dbCommand.CommandType = CommandType.StoredProcedure;
				((Oracle.DataAccess.Client.OracleCommand)dbCommand).BindByName = true;
			}
			if (DBVendor == DBVendor.Microsoft) {
				dbCommand.CommandText = "SELECT " + dbFunctionName + "()";
				dbCommand.CommandType = CommandType.Text;
			}

			return dbCommand;
		}

		internal DbCommand CreateOrGetDBFunctionCallDbCommand(string dbFunctionName) {
			DbCommand dbCommand = null;
			if (!DB.reuseDbCommands)
				dbCommand = createDBFunctionCallDbCommand(dbFunctionName);
			else {
				lock (((IDictionary)storedProcedureDBFunctionCallDbCommandCache).SyncRoot) {
					if (storedProcedureDBFunctionCallDbCommandCache.TryGetValue(dbFunctionName, out dbCommand)) {
						dbCommand.Parameters.Clear();
					}
					else {
						dbCommand = createDBFunctionCallDbCommand(dbFunctionName);
						storedProcedureDBFunctionCallDbCommandCache.Add(dbFunctionName, dbCommand);
					}
				}
			}

			return dbCommand;
		}

		internal void ExecuteNonQuery(DbCommand dbCommand, bool checkRowCount) {
			int rowCount;
			using (dbCommand.Connection = CreateDbConnection()) {
				try {
					dbCommand.Connection.Open();
					rowCount = dbCommand.ExecuteNonQuery();

					//TODO: UNCOMMENT THIS IF BLOCK - COMMENTED OUT ONLY TEMPORARILY FOR MULTITHREAD CONCURRENT TESTS OF UPDATE AND LOGICAL DELETE
					//if (rowCount == 0 && checkRowCount)
					//  throw new RecordNotFoundOrRecordHasBeenChangedException("Record not found or record has been updated since your last query. Please re-query this record and try again.");
				}
				catch (Exception ex) {
					throw;
				}
			}
		}

	}

	public class ValueProviderSet {
		public ValueProvider ValueProvider { get; internal set; }
		public string SequenceName { get; internal set; }
		public string SchemaName { get; internal set; }
		public string DBFunctionName { get; internal set; }
		public Func<object> FunctionDelegate { get; internal set; }
	}

	public class PrimaryKeyMapping {

		public void ChangeSequenceName(string sequenceName) {
			DB.throwIfNullOrEmpty<MappingChangeException>(sequenceName, "Sequence name");
			ValueProviderSet.SequenceName = sequenceName;
		}

		public void ChangeSchemaName(string schemaName) {
			//DB.throwIfNullOrEmpty<MappingChangeException>(schemaName, "Schema name");
			ValueProviderSet.SchemaName = schemaName;
		}

		public void ChangeDBFunctionName(string dbFunctionName) {
			DB.throwIfNullOrEmpty<MappingChangeException>(dbFunctionName, "DB function name");
			ValueProviderSet.DBFunctionName = dbFunctionName;
		}

		public void ChangeFunctionDelegate(Func<object> functionDelegate) {
			DB.throwIfNullOrEmpty<MappingChangeException>(functionDelegate, "Function delegate");
			ValueProviderSet.FunctionDelegate = functionDelegate;
		}

		public void ChangeValueProvider(PrimaryKeyValueProvider primaryKeyValueProvider) {
			DB.throwIfNullOrEmpty<MappingChangeException>(primaryKeyValueProvider, "Primary key value provider");
			ValueProviderSet.ValueProvider = (ValueProvider)primaryKeyValueProvider;
		}

		public void ChangeNestedPrimaryKeyPropertyNameList(List<string> nestedPrimaryKeyPropertyNameList) {
			DB.throwIfNullOrEmpty<MappingChangeException>(nestedPrimaryKeyPropertyNameList, "Nested primary key property name list");
			NestedPrimaryKeyPropertyNameList = nestedPrimaryKeyPropertyNameList;
		}

		internal List<string> NestedPrimaryKeyPropertyNameList { get; private set; }
		public ReadOnlyCollection<string> NestedPrimaryKeyPropertyNames {
			get {
				return NestedPrimaryKeyPropertyNameList.AsReadOnly();
			}
		}

		public ValueProviderSet ValueProviderSet { get; private set; }

		internal void AddProperty(string nestedPrimaryKeyPropertyName, Enum sequenceName, Enum schemaName, Enum dbFunctionName, Func<object> functionDelegate, PrimaryKeyValueProvider defaultPrimaryKeyValueProvider) {
			NestedPrimaryKeyPropertyNameList.Add(nestedPrimaryKeyPropertyName);
			ValueProviderSet.FunctionDelegate = functionDelegate;
			ValueProviderSet.SequenceName = string.Concat(sequenceName);
			ValueProviderSet.SchemaName = string.Concat(schemaName);
			ValueProviderSet.DBFunctionName = string.Concat(dbFunctionName);
			ValueProviderSet.ValueProvider = (ValueProvider)defaultPrimaryKeyValueProvider;
		}

		internal PrimaryKeyMapping() {
			NestedPrimaryKeyPropertyNameList = new List<string>();
			this.ValueProviderSet = new ValueProviderSet();
		}

	}

	public class ColumnMapping {
		public void ChangeColumnName(string columnName) {
			DB.throwIfNullOrEmpty<MappingChangeException>(columnName, "Column name");
			ColumnName = columnName;
		}

		public void ChangeNestedPropertyName(string nestedPropertyName) {
			DB.throwIfNullOrEmpty<MappingChangeException>(nestedPropertyName, "Nested property name");
			NestedPropertyName = nestedPropertyName;
		}

		public void ChangeIsDBNullableValueType(bool isDBNullableValueType) {
			IsDBNullableValueType = isDBNullableValueType;
		}

		public string ColumnName { get; private set; }
		public string NestedPropertyName { get; private set; }
		public bool IsDBNullableValueType { get; private set; }

		internal ColumnMapping(string columnName, string nestedPropertyName, bool isDBNullableValueType) {
			ColumnName = columnName;
			NestedPropertyName = nestedPropertyName;
			IsDBNullableValueType = isDBNullableValueType;
		}

	}

	public class EntityMapping {

		public void ChangeTableName(string tableName) {
			DB.throwIfNullOrEmpty<MappingChangeException>(tableName, "Table name");
			TableName = tableName;
		}

		public void ChangeSchemaName(string schemaName) {
			//DB.throwIfNullOrEmpty(schemaName, "Schema name");
			SchemaName = schemaName;
		}

		public void ChangeConnectionName(string connectionName) {
			DB.throwIfNullOrEmpty<MappingChangeException>(connectionName, "Connection name");

			ConnectionMapping connectionMapping;
			Map.connectionMappingDictionaryAtMapper.TryGetValue(connectionName, out connectionMapping);
			DB.throwIfNullOrEmpty<MappingChangeException>(connectionMapping, "Connection mapping");

			ConnectionName = connectionName;
			RefreshConnectionMappingFromConnectionName();
		}

		public void GenerateCommands() {
			if (!string.IsNullOrEmpty(TableName) && ConnectionMapping.DBVendor == DBVendor.Microsoft)
				generateSqlServerDBColumnTypeDictionary();

			foreach (QueryMapping queryMapping in QueryMappingDictionary.Values) {
				DB.GenerateSelect(this, queryMapping);
				queryMapping.queryResultCache.Clear();
				if (!string.IsNullOrEmpty(TableName))
					DB.GenerateUpdate(this, queryMapping);
			}

			if (!string.IsNullOrEmpty(TableName)) {
				DB.GenerateInsert(this);
				DB.GenerateDelete(this);
			}
		}

		//public string EntityName { get; private set; }
		public string TableName { get; private set; }
		public string SchemaName { get; private set; }
		public string ConnectionName { get; private set; }

		public Type EntityType { get; private set; }

		internal Dictionary<string, QueryMapping> QueryMappingDictionary = new Dictionary<string, QueryMapping>();
		public TimestampMapping TimestampMapping { get; internal set; }
		internal Dictionary<string, ColumnMapping> ColumnMappingDictionary = new Dictionary<string, ColumnMapping>();
		//RootTypePropertyInfoListCache'in bağımlılığı yok çünkü entity Type ile alakalı, entity Type asla değişmez
		internal List<PropertyInfo> RootTypePropertyInfoListCacheAtMapper;
		//NestedPropertyNameListCache'in  bağımlılığı yok çünkü entity Type ile alakalı, entity Type asla değişmez
		internal List<string> NestedPropertyNameListCacheAtMapper;
		internal Dictionary<string, PropertyInfo> NestedPropertyInfoCacheAtMapper = new Dictionary<string, PropertyInfo>();

		internal Dictionary<string, string> NestedTableColumnNameDictionaryAtMapper = new Dictionary<string, string>();
		public PrimaryKeyMapping PrimaryKeyMapping { get; internal set; }
		internal Dictionary<string, AutoSetColumnMapping> InsertAutoSetColumnMappingDictionary = new Dictionary<string, AutoSetColumnMapping>();
		internal Dictionary<string, AutoSetColumnMapping> UpdateAutoSetColumnMappingDictionary = new Dictionary<string, AutoSetColumnMapping>();
		internal Dictionary<string, AutoSetColumnMapping> LogicalDeleteAutoSetColumnMappingDictionary = new Dictionary<string, AutoSetColumnMapping>();

		internal List<string> NestedPropertyNameListForLogicalDeleteCacheAtMapper = new List<string>();

		internal Dictionary<string, string> sqlServerDBColumnTypeDictionaryAtMapper;

		internal Dictionary<string, ValueProviderInfo> nestedPropertyValueProviderInfoForInsertCacheAtMapper = new Dictionary<string, ValueProviderInfo>();
		internal Dictionary<string, ValueProviderInfo> nestedPropertyValueProviderInfoForLogicalDeleteCacheAtMapper = new Dictionary<string, ValueProviderInfo>();

		internal void generateSqlServerDBColumnTypeDictionary() {
			sqlServerDBColumnTypeDictionaryAtMapper = new Dictionary<string, string>();
			DbCommand dbCommand = createInformationSchemaDbCommand();
			using (dbCommand.Connection = ConnectionMapping.CreateDbConnection()) {
				dbCommand.Connection.Open();
				using (IDataReader dataReader = dbCommand.ExecuteReader()) {
					while (dataReader.Read()) {
						sqlServerDBColumnTypeDictionaryAtMapper.Add(dataReader.GetString(0), dataReader.GetString(1));
					}
				}
			}
		}

		internal void generateQueryNestedPropertyNameListAndPositionToCaches(EntityMapping entityMapping, QueryMapping queryMapping) {
			DbCommand dbCommand = createQueryColumnsDbCommand(entityMapping, queryMapping);
			using (dbCommand.Connection = ConnectionMapping.CreateDbConnection()) {
				dbCommand.Connection.Open();
				int columnIndex = 0;
				using (IDataReader dataReader = dbCommand.ExecuteReader()) {
					DataTable schemaTable = dataReader.GetSchemaTable();
					queryMapping.PositionToSetterCacheAtMapper = new PropertySetter[schemaTable.Rows.Count];
					queryMapping.PositionToTypeCacheAtMapper = new Type[schemaTable.Rows.Count];
					queryMapping.QueryNestedPropertyNameListCacheAtMapper = new List<string>();

					foreach (DataRow row in schemaTable.Rows) {
						string columnName = row["ColumnName"].ToString();
						ColumnMapping columnMapping = null;
						//dictionaryde yoksa kolon propertyye maplenmemiştir, bir şey yapılmayacak
						if (ColumnMappingDictionary.TryGetValue(columnName, out columnMapping)) {
							queryMapping.QueryNestedPropertyNameListCacheAtMapper.Add(columnMapping.NestedPropertyName);
							PropertyInfo nestedPropertyInfo = entityMapping.NestedPropertyInfoCacheAtMapper[columnMapping.NestedPropertyName];
							PropertySetter setter = entityMapping.NestedPropertySetterCacheAtMapper[columnMapping.NestedPropertyName];

							queryMapping.PositionToSetterCacheAtMapper[columnIndex] = setter;
							queryMapping.PositionToTypeCacheAtMapper[columnIndex] = nestedPropertyInfo.PropertyType;
						}

						columnIndex++;
					}
				}
			}
		}

		internal DbCommand createQueryColumnsDbCommand(EntityMapping entityMapping, QueryMapping queryMapping) {
			string sql = entityMapping.ConnectionMapping.replaceUsedDBParameterPrefix(queryMapping.SelectSql, queryMapping.inputParameterNameTypes);
			DbCommand dbCommand = ConnectionMapping.CreateTextDbCommand(sql);
			foreach (InputParameterNameType inputParameterNameType in queryMapping.inputParameterNameTypes) {
				DbParameter dbParameter = ConnectionMapping.CreateDbParameterForInputForQueryColumns(inputParameterNameType.Name, DBNull.Value, inputParameterNameType.Type);
				dbCommand.Parameters.Add(dbParameter);
			}

			return dbCommand;
		}

		internal DbCommand createInformationSchemaDbCommand() {
			string sql = DB.informationSchemaSelectSql + SchemaName.ToUpperInvariant() + "' AND UPPER(TABLE_NAME) = '" + TableName.ToUpperInvariant() + "'";
			DbCommand dbCommand = ConnectionMapping.CreateTextDbCommand(sql);

			return dbCommand;
		}

		internal bool IsAutoSetColumn(string columnName) {
			if (InsertAutoSetColumnMappingDictionary.Keys.Contains(columnName))
				return true;
			if (UpdateAutoSetColumnMappingDictionary.Keys.Contains(columnName))
				return true;
			if (LogicalDeleteAutoSetColumnMappingDictionary.Keys.Contains(columnName))
				return true;

			return false;
		}

		internal void throwIfAutoSetAndPrimaryKeyColumn(string columnName) {
			if (IsAutoSetColumn(columnName) && PrimaryKeyColumnNameList.Contains(columnName))
				throw new MappingException("Primary key column cannot be auto set");
		}

		internal void throwIfAutoSetAndTimestampColumn(string columnName) {
			if (IsAutoSetColumn(columnName) && TimestampMapping.ColumnName == columnName)
				throw new MappingException("Timestamp column cannot be auto set");
		}

		internal Dictionary<When, Delegate> TriggerActionDelegateDictionary = new Dictionary<When, Delegate>();
		internal Action BeforeSelectCommandTriggerActionDelegate;

		internal Dictionary<Before, Delegate> ValidatorFunctionDelegateDictionary = new Dictionary<Before, Delegate>();

		internal DbCommand InsertDbCommandCacheAtMapper;
		//TODO: bunnları QueryMappinge taşı, dictionaryde tutmaya gerek yok
		internal DbCommand DeleteDbCommandCache;
		internal DbCommand LogicalDeleteDbCommandCache;
		internal Dictionary<string, PropertyGetter> NestedPropertyGetterCacheAtMapper = new Dictionary<string, PropertyGetter>();
		internal Dictionary<string, PropertySetter> NestedPropertySetterCacheAtMapper = new Dictionary<string, PropertySetter>();

		internal string InsertSqlCacheAtMapper;
		internal string DeleteSqlCache;
		internal string LogicalDeleteSqlCache;

		internal MultiKeyDictionary<Type, string, PropertyInfo> rootTypePropertyNamePropertyInfoCacheAtMapper = new MultiKeyDictionary<Type, string, PropertyInfo>();

		internal Dictionary<string, string> insertReturningColumnNamePropertyNameCacheAtMapper = new Dictionary<string, string>();
		internal Dictionary<string, string> insertColumnNameValueNameForPrimaryKeyCacheAtMapper = new Dictionary<string, string>();
		internal Dictionary<string, string> insertColumnNameValueNameForNonPrimaryKeyCacheAtMapper = new Dictionary<string, string>();
		internal List<string> insertColumnNameCacheAtMapper = new List<string>();
		internal List<string> insertValueNameCacheAtMapper = new List<string>();

		internal Dictionary<string, string> logicalDeleteReturningColumnNamePropertyNameCacheAtMapper = new Dictionary<string, string>();
		internal Dictionary<string, string> logicalDeleteColumnNameValueNameForPrimaryKeyCacheAtMapper = new Dictionary<string, string>();
		internal Dictionary<string, string> logicalDeleteColumnNameValueNameForNonPrimaryKeyCacheAtMapper = new Dictionary<string, string>();

		internal ConnectionMapping ConnectionMapping;
		internal void RefreshConnectionMappingFromConnectionName() {
			ConnectionMapping connectionMapping;
			Map.connectionMappingDictionaryAtMapper.TryGetValue(ConnectionName, out connectionMapping);
			DB.throwIfNullOrEmpty<MappingException>(connectionMapping, "Connection mapping");

			ConnectionMapping = connectionMapping;
		}

		internal List<string> PrimaryKeyColumnNameList;
		internal void RefreshPrimaryKeyColumnNameList() {
			PrimaryKeyColumnNameList = new List<string>();
			foreach (string nestedPrimaryKeyPropertyName in PrimaryKeyMapping.NestedPrimaryKeyPropertyNameList) {
				string columnName;
				if (NestedTableColumnNameDictionaryAtMapper.TryGetValue(nestedPrimaryKeyPropertyName, out columnName))
					PrimaryKeyColumnNameList.Add(columnName);
			}
		}

		public ReadOnlyCollection<string> PrimaryKeyColumnNames {
			get {
				if (PrimaryKeyColumnNameList != null)
					return PrimaryKeyColumnNameList.AsReadOnly();
				else
					return null;
			}
		}

		public ReadOnlyCollection<ColumnMapping> ColumnMappings {
			get {
				ReadOnlyCollection<ColumnMapping> readOnlyCollection = null;
				if (ColumnMappingDictionary.Values != null)
					readOnlyCollection = ColumnMappingDictionary.Values.ToList().AsReadOnly();

				return readOnlyCollection;
			}
		}

		public ReadOnlyCollection<AutoSetColumnMapping> InsertAutoSetColumnMappings {
			get {
				ReadOnlyCollection<AutoSetColumnMapping> readOnlyCollection = null;
				if (InsertAutoSetColumnMappingDictionary.Values != null)
					readOnlyCollection = InsertAutoSetColumnMappingDictionary.Values.ToList().AsReadOnly();

				return readOnlyCollection;
			}
		}

		public ReadOnlyCollection<AutoSetColumnMapping> UpdateAutoSetColumnMappings {
			get {
				ReadOnlyCollection<AutoSetColumnMapping> readOnlyCollection = null;
				if (UpdateAutoSetColumnMappingDictionary.Values != null)
					readOnlyCollection = UpdateAutoSetColumnMappingDictionary.Values.ToList().AsReadOnly();

				return readOnlyCollection;
			}
		}

		public ReadOnlyCollection<AutoSetColumnMapping> LogicalDeleteAutoSetColumnMappings {
			get {
				ReadOnlyCollection<AutoSetColumnMapping> readOnlyCollection = null;
				if (LogicalDeleteAutoSetColumnMappingDictionary.Values != null)
					readOnlyCollection = LogicalDeleteAutoSetColumnMappingDictionary.Values.ToList().AsReadOnly();

				return readOnlyCollection;
			}
		}

		public ReadOnlyCollection<When> TriggerActions {
			get {
				ReadOnlyCollection<When> readOnlyCollection = null;
				if (TriggerActionDelegateDictionary.Keys != null)
					readOnlyCollection = TriggerActionDelegateDictionary.Keys.ToList().AsReadOnly();

				return readOnlyCollection;
			}
		}

		public ReadOnlyCollection<QueryMapping> QueryMappings {
			get {
				ReadOnlyCollection<QueryMapping> readOnlyCollection = null;
				if (QueryMappingDictionary.Values != null)
					readOnlyCollection = QueryMappingDictionary.Values.ToList().AsReadOnly();

				return readOnlyCollection;
			}
		}

		private List<KeyValuePair<string, string>> primaryKeyColumnNamePropertyNamePairList = new List<KeyValuePair<string, string>>();
		private bool primaryKeyColumnNamePropertyNamePairListLookedUp;
		internal List<KeyValuePair<string, string>> PrimaryKeyColumnNamePropertyNamePairList {
			get {
				if (!primaryKeyColumnNamePropertyNamePairListLookedUp) {
					if (PrimaryKeyMapping.NestedPrimaryKeyPropertyNameList.Count == 0)
						throw new ArgumentException("Primary key property names cannot be null or empty.");

					string primaryKeyColumnName;
					foreach (string nestedPrimaryKeyPropertyName in PrimaryKeyMapping.NestedPrimaryKeyPropertyNameList) {
						primaryKeyColumnName = NestedTableColumnNameDictionaryAtMapper[nestedPrimaryKeyPropertyName];
						primaryKeyColumnNamePropertyNamePairList.Add(new KeyValuePair<string, string>(primaryKeyColumnName, nestedPrimaryKeyPropertyName));
					}
				}
				if (primaryKeyColumnNamePropertyNamePairList.Count == 0)
					throw new ArgumentException("Primary key column names cannot be null or empty.");

				primaryKeyColumnNamePropertyNamePairListLookedUp = true;
				return primaryKeyColumnNamePropertyNamePairList;
			}
		}

		internal EntityMapping(string tableName, string schemaName, string connectionName, Type entityType) {
			TableName = tableName;
			SchemaName = schemaName;
			ConnectionName = connectionName;
			EntityType = entityType;
			RootTypePropertyInfoListCacheAtMapper = entityType.GetProperties().ToList();
			NestedPropertyNameListCacheAtMapper = NestedPropertyHelper.CreateNestedPropertyNameList(RootTypePropertyInfoListCacheAtMapper);

			PrimaryKeyMapping = new PrimaryKeyMapping();
			TimestampMapping = new TimestampMapping();

			RefreshConnectionMappingFromConnectionName();
		}

		internal PropertyGetter GetGetterForNestedProperty(string nestedPropertyName, PropertyInfo propertyInfo) {
			PropertyGetter getter;
			NestedPropertyGetterCacheAtMapper.TryGetValue(nestedPropertyName, out getter);

			return getter;
		}

		internal void GenerateSetterCacheEntryAtMapper(string nestedPropertyName, PropertyInfo propertyInfo) {
			PropertySetter setter;
			if (!NestedPropertySetterCacheAtMapper.TryGetValue(nestedPropertyName, out setter)) {
				setter = PropertyGetterSetterFactory.CreateSetter(propertyInfo);
				NestedPropertySetterCacheAtMapper.Add(nestedPropertyName, setter);
			}
		}

		internal void GenerateGetterCacheEntryAtMapper(string nestedPropertyName, PropertyInfo propertyInfo) {
			PropertyGetter getter;
			if (!NestedPropertyGetterCacheAtMapper.TryGetValue(nestedPropertyName, out getter)) {
				getter = PropertyGetterSetterFactory.CreateGetter(propertyInfo);
				NestedPropertyGetterCacheAtMapper.Add(nestedPropertyName, getter);
			}
		}

		internal void SetNestedPropertyValue(Type rootType, object rootObject, string nestedPropertyName, object valueToSet, PropertySetter setterForLeafProperty) {
			if (!nestedPropertyName.Contains(".")) {
				setterForLeafProperty(rootObject, valueToSet);
			}
			else {
				int firstDotPosition = nestedPropertyName.IndexOf(".", StringComparison.Ordinal);
				string nestedPropertyNameAfterFirstDot = nestedPropertyName.Substring(firstDotPosition + 1);
				string propertyNameBeforeFirstDot = nestedPropertyName.Substring(0, firstDotPosition);
				PropertyInfo propertyInfoBeforeFirstDot;// = rootType.GetProperty(propertyNameBeforeFirstDot);
				propertyInfoBeforeFirstDot = rootTypePropertyNamePropertyInfoCacheAtMapper[rootType, propertyNameBeforeFirstDot];
				PropertyGetter getter = GetGetterForNestedProperty(propertyNameBeforeFirstDot, propertyInfoBeforeFirstDot);
				object objectBeforeFirstDot = getter(rootObject);
				Type propertyTypeBeforeFirstDot = propertyInfoBeforeFirstDot.PropertyType;
				if (objectBeforeFirstDot == null) {
					objectBeforeFirstDot = Activator.CreateInstance(propertyTypeBeforeFirstDot);
					PropertySetter setter = NestedPropertySetterCacheAtMapper[propertyNameBeforeFirstDot];
					setter(rootObject, objectBeforeFirstDot);
				}
				SetNestedPropertyValue(propertyTypeBeforeFirstDot, objectBeforeFirstDot, nestedPropertyNameAfterFirstDot, valueToSet, setterForLeafProperty);
			}
		}

		internal DbParameter CreateAndSetInputDbParameterFromNonTimestampNestedProperty(string columnName, object entity, string nestedPropertyName, bool isDBNullableValueType) {
			PropertyInfo nestedPropertyInfo = NestedPropertyInfoCacheAtMapper[nestedPropertyName];
			PropertyGetter getter = GetGetterForNestedProperty(nestedPropertyName, nestedPropertyInfo);
			object value = GetNestedPropertyValue(entity, nestedPropertyName, getter);

			DbParameter dbParameter = ConnectionMapping.CreateDbParameter(columnName);

			Type propertyType = nestedPropertyInfo.PropertyType;
			if (propertyType.IsValueType && isDBNullableValueType && (value == null || value.Equals(TypeConverter.GetDefaultValue(propertyType))) && !PrimaryKeyMapping.NestedPrimaryKeyPropertyNameList.Contains(nestedPropertyName))
				value = DBNull.Value;
			else if (propertyType.IsEnum || (Nullable.GetUnderlyingType(propertyType) != null && Nullable.GetUnderlyingType(propertyType).IsEnum))
				value = (int)value;

			DB.SetDbParameterValueAndDbTypeForNonTimestamp(dbParameter, value, nestedPropertyInfo.PropertyType);

			return dbParameter;
		}

		internal DbParameter CreateAndSetInputDbParameterForOldTimestampFromTimestampNestedProperty(string columnName, object entity, string nestedPropertyName) {
			PropertyInfo nestedPropertyInfo = NestedPropertyInfoCacheAtMapper[nestedPropertyName];
			PropertyGetter getter = GetGetterForNestedProperty(nestedPropertyName, nestedPropertyInfo);
			object value = GetNestedPropertyValue(entity, nestedPropertyName, getter);

			if (value == null)
				return null;
			else {
				DbParameter dbParameter = ConnectionMapping.CreateDbParameter(columnName);
				ConnectionMapping.SetDbParameterValueAndDbTypeForTimestamp(dbParameter, value);

				return dbParameter;
			}
		}

		internal DbParameter CreateInputOutputDbParameterFromValueAndNestedPropertyTypeForNonTimestamp(string columnName, object value, string nestedPropertyName, bool isDBNullableValueType) {
			return createDbParameterFromValueAndNestedPropertyTypeForNonTimestamp(ParameterDirection.InputOutput, columnName, value, nestedPropertyName, isDBNullableValueType);
		}

		private DbParameter createDbParameterFromValueAndNestedPropertyTypeForNonTimestamp(ParameterDirection direction, string columnName, object value, string nestedPropertyName, bool isDBNullableValueType) {
			DbParameter dbParameter = ConnectionMapping.CreateDbParameter(columnName);
			dbParameter.Direction = direction;

			PropertyInfo nestedPropertyInfo = NestedPropertyInfoCacheAtMapper[nestedPropertyName];

			Type propertyType = nestedPropertyInfo.PropertyType;
			if (propertyType.IsValueType && isDBNullableValueType && (value == null || value.Equals(TypeConverter.GetDefaultValue(propertyType))) && !PrimaryKeyMapping.NestedPrimaryKeyPropertyNameList.Contains(nestedPropertyName))
				value = DBNull.Value;
			else if (propertyType.IsEnum || (Nullable.GetUnderlyingType(propertyType) != null && Nullable.GetUnderlyingType(propertyType).IsEnum))
				value = (int)value;

			DB.SetDbParameterValueAndDbTypeForNonTimestamp(dbParameter, value, nestedPropertyInfo.PropertyType);

			return dbParameter;
		}

		internal object GetNestedPropertyValue(object entity, string nestedPropertyName) {
			PropertyInfo nestedPropertyInfo = NestedPropertyInfoCacheAtMapper[nestedPropertyName];
			PropertyGetter getter = GetGetterForNestedProperty(nestedPropertyName, nestedPropertyInfo);
			return GetNestedPropertyValue(entity, nestedPropertyName, getter);
		}

		internal object GetNestedPropertyValue(object entity, string nestedPropertyName, PropertyGetter getterForLeafProperty) {
			return GetNestedPropertyValue(EntityType, entity, nestedPropertyName, getterForLeafProperty);
		}

		internal object GetNestedPropertyValue(Type rootType, object rootObject, string nestedPropertyName, PropertyGetter getterForLeafProperty) {
			if (!nestedPropertyName.Contains(".")) {
				return getterForLeafProperty(rootObject);
			}
			else {
				int firstDotPosition = nestedPropertyName.IndexOf(".", StringComparison.Ordinal);
				string nestedPropertyNameAfterFirstDot = nestedPropertyName.Substring(firstDotPosition + 1);
				string propertyNameBeforeFirstDot = nestedPropertyName.Substring(0, firstDotPosition);
				PropertyInfo propertyInfoBeforeFirstDot;// = rootType.GetProperty(propertyNameBeforeFirstDot);
				propertyInfoBeforeFirstDot = rootTypePropertyNamePropertyInfoCacheAtMapper[rootType, propertyNameBeforeFirstDot];
				PropertyGetter getter = GetGetterForNestedProperty(propertyNameBeforeFirstDot, propertyInfoBeforeFirstDot);
				object objectBeforeFirstDot = getter(rootObject);
				//nested obje null ise property de null dön
				if (objectBeforeFirstDot == null) {
					return null;
				}
				return GetNestedPropertyValue(propertyInfoBeforeFirstDot.PropertyType, objectBeforeFirstDot, nestedPropertyNameAfterFirstDot, getterForLeafProperty);
			}
		}

		//inner (nested) classlara erişim için gereken cacheler hazırlanıyor
		internal void GenerateRootTypePropertyNamePropertyInfoAndGetterSetterCacheEntryAtMapper(Type rootType, string nestedPropertyName) {
			if (!nestedPropertyName.Contains(".")) {
				return;
			}
			else {
				int firstDotPosition = nestedPropertyName.IndexOf(".", StringComparison.Ordinal);
				string nestedPropertyNameAfterFirstDot = nestedPropertyName.Substring(firstDotPosition + 1);
				string propertyNameBeforeFirstDot = nestedPropertyName.Substring(0, firstDotPosition);
				PropertyInfo propertyInfoBeforeFirstDot;// = rootType.GetProperty(propertyNameBeforeFirstDot);
				if (!rootTypePropertyNamePropertyInfoCacheAtMapper.TryGetValue(rootType, propertyNameBeforeFirstDot, out propertyInfoBeforeFirstDot)) {
					propertyInfoBeforeFirstDot = rootType.GetProperty(propertyNameBeforeFirstDot);
					rootTypePropertyNamePropertyInfoCacheAtMapper.Add(rootType, propertyNameBeforeFirstDot, propertyInfoBeforeFirstDot);
				}
				GenerateGetterCacheEntryAtMapper(propertyNameBeforeFirstDot, propertyInfoBeforeFirstDot);
				GenerateSetterCacheEntryAtMapper(propertyNameBeforeFirstDot, propertyInfoBeforeFirstDot);

				GenerateRootTypePropertyNamePropertyInfoAndGetterSetterCacheEntryAtMapper(propertyInfoBeforeFirstDot.PropertyType, nestedPropertyNameAfterFirstDot);
			}
		}


	}

	public class TimestampMapping {

		public void ChangeColumnName(string columnName) {
			DB.throwIfNullOrEmpty<MappingChangeException>(columnName, "Column name");
			ColumnName = columnName;
		}

		public void ChangeNestedPropertyName(string nestedPropertyName) {
			DB.throwIfNullOrEmpty<MappingChangeException>(nestedPropertyName, "Nested property name");
			NestedPropertyName = nestedPropertyName;
		}

		public void ChangeSchemaName(string schemaName) {
			//DB.throwIfNullOrEmpty(schemaName, "Schema name");
			ValueProviderSet.SchemaName = schemaName;
		}

		public void ChangeDBFunctionName(string dbFunctionName) {
			DB.throwIfNullOrEmpty<MappingChangeException>(dbFunctionName, "DB function name");
			ValueProviderSet.DBFunctionName = dbFunctionName;
		}

		public void ChangeFunctionDelegate(Func<object> functionDelegate) {
			DB.throwIfNullOrEmpty<MappingChangeException>(functionDelegate, "Function delegate");
			ValueProviderSet.FunctionDelegate = functionDelegate;
		}

		public void ChangeValueProvider(TimestampValueProvider timestampValueProvider) {
			DB.throwIfNullOrEmpty<MappingChangeException>(timestampValueProvider, "Timestamp value provider");
			ValueProviderSet.ValueProvider = (ValueProvider)timestampValueProvider;
		}
		
		public string ColumnName { get; private set; }
		public string NestedPropertyName { get; private set; }

		public ValueProviderSet ValueProviderSet { get; private set; }

		internal TimestampMapping(string columnName, string nestedPropertyName, Enum schemaName, Enum dbFunctionName, Func<object> functionDelegate, TimestampValueProvider timestampValueProvider) {
			ColumnName = columnName;
			NestedPropertyName = nestedPropertyName;
			ValueProviderSet = new ValueProviderSet();
			ValueProviderSet.FunctionDelegate = functionDelegate;
			ValueProviderSet.SchemaName = string.Concat(schemaName);
			ValueProviderSet.DBFunctionName = string.Concat(dbFunctionName);
			ValueProviderSet.ValueProvider = (ValueProvider)timestampValueProvider;
		}

		internal TimestampMapping() {
			ValueProviderSet = new ValueProviderSet();
		}

		internal bool ExistsForNestedPropertyName(string nestedPropertyName) {
			return NestedPropertyName == nestedPropertyName;
		}

		internal bool Exists() {
			return (!string.IsNullOrEmpty(ColumnName));
		}

	}

	public class AutoSetColumnMapping {

		public void ChangeColumnName(string columnName) {
			DB.throwIfNullOrEmpty<MappingChangeException>(columnName, "Column name");
			ColumnName = columnName;
		}

		public void ChangeSchemaName(string schemaName) {
			ValueProviderSet.SchemaName = schemaName;
		}

		public void ChangeDBFunctionName(string dbFunctionName) {
			DB.throwIfNullOrEmpty<MappingChangeException>(dbFunctionName, "DB Function name");
			ValueProviderSet.DBFunctionName = dbFunctionName;
		}

		public void ChangeFunctionDelegate(Func<object> functionDelegate) {
			DB.throwIfNullOrEmpty<MappingChangeException>(functionDelegate, "Function delegate");
			ValueProviderSet.FunctionDelegate = functionDelegate;
		}

		public void ChangeValueProvider(AutoSetValueProvider autoSetValueProvider) {
			DB.throwIfNullOrEmpty<MappingChangeException>(autoSetValueProvider, "Autoset value provider");
			ValueProviderSet.ValueProvider = (ValueProvider)autoSetValueProvider;
		}
		
		public string ColumnName { get; private set; }
		public ValueProviderSet ValueProviderSet { get; private set; }

		internal AutoSetColumnMapping(string columnName, Enum schemaName, Enum dbFunctionName, Func<object> functionDelegate, AutoSetValueProvider autoSetValueProvider) {
			ColumnName = columnName;
			ValueProviderSet = new ValueProviderSet();
			ValueProviderSet.SchemaName = string.Concat(schemaName);
			ValueProviderSet.DBFunctionName = string.Concat(dbFunctionName);
			ValueProviderSet.FunctionDelegate = functionDelegate;
			ValueProviderSet.ValueProvider = (ValueProvider)autoSetValueProvider;
		}

	}

	public class QueryMapping {

		public void ChangeSelectSql(string selectSql) {
			DB.throwIfNullOrEmpty<MappingChangeException>(selectSql, "Select sql");
			SelectSql = selectSql;
		}

		public string QueryName { get; private set; }
		public string SelectSql { get; private set; }

		internal PropertySetter[] PositionToSetterCacheAtMapper = new PropertySetter[0];
		internal Type[] PositionToTypeCacheAtMapper = new Type[0];

		//queryname parametresiyle update edildiğinde önceden bu query ile select edilmiş olmalı ki bu dictionaryden data bulunabilsin
		internal List<string> QueryNestedPropertyNameListCacheAtMapper = new List<string>();
		internal string UpdateSqlCache;
		internal DbCommand UpdateDbCommandCache;

		internal DbCommand SelectDbCommandCacheAtMapper;

		internal List<string> NestedPropertyNameListForUpdateCacheAtMapper = new List<string>();

		internal Dictionary<string, ValueProviderInfo> nestedPropertyValueProviderInfoForUpdateCacheAtMapper = new Dictionary<string, ValueProviderInfo>();

		public bool IsQueryResultCached { get; private set; }
		public int MaxNumberOfQueriesToHoldInCache { get; private set; }
		//sycroot'a ulaşarak lock etmek için private yerine internala çekildi
		//locked
		internal Dictionary<string, object> queryResultCache = new Dictionary<string, object>();

		internal InputParameterNameType[] inputParameterNameTypes = new InputParameterNameType[0];
		//dictionary içinde add remove yapıldığında sıra bozuluyor, dic.First() veya dic.Keys.First() yanlış değer dönüyor
		//ilk eklenmişi bulmak için ayrı bir yerde eklenme saati mi tutulmalı?
		private List<string> queryResultKeyList = new List<string>();

		internal Dictionary<string, string> updateReturningColumnNamePropertyNameCacheAtMapper = new Dictionary<string, string>();
		internal Dictionary<string, string> updateColumnNameValueNameForPrimaryKeyCacheAtMapper = new Dictionary<string, string>();
		internal Dictionary<string, string> updateColumnNameValueNameForNonPrimaryKeyCacheAtMapper = new Dictionary<string, string>();

		internal QueryMapping(string queryName, string sql, bool isQueryResultCached, int maxNumberOfQueriesToHoldInCache, InputParameterNameType[] inputParameterNameTypes) {
			QueryName = queryName;
			SelectSql = sql;
			IsQueryResultCached = isQueryResultCached;
			this.MaxNumberOfQueriesToHoldInCache = maxNumberOfQueriesToHoldInCache;
			this.inputParameterNameTypes = inputParameterNameTypes;
		}

		internal void AddToQueryResultCache(string keyForInputParameters, object result) {
			int excessCount = queryResultCache.Count - MaxNumberOfQueriesToHoldInCache + 1;
			//cachelenecek query adedi aşılıyorsa ilk eklenmiş cache entryleri yoket
			if (MaxNumberOfQueriesToHoldInCache > 0 && excessCount > 0)
				for (int i = 0; i < excessCount; i++) {
					queryResultCache.Remove(queryResultKeyList.First());
					queryResultKeyList.RemoveAt(0);
				}

			queryResultCache.Add(keyForInputParameters, result);
			queryResultKeyList.Add(keyForInputParameters);
		}

		internal bool TryGetCachedQueryResult(string keyForInputParameters, out object cachedResult) {
			return queryResultCache.TryGetValue(keyForInputParameters, out cachedResult);
		}

	}

}
