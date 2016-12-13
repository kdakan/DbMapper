using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Data.Common;
using System.Configuration;
using System.Reflection;
using System.Collections;
using System.Collections.ObjectModel;

namespace DBMapper {
	internal static class ConnectionMapper {

		#region Connection

		//connectionstring config dosyasında girilmişse kullanılabilir
		internal static void NewConnection(Enum connectionName, DBVendor dbVendor, string usedDBParameterPrefix) {
			DB.throwIfNullOrEmpty<MappingException>(connectionName, "Connection name");
			DB.throwIfKeyAlreadyExists<string, ConnectionMapping, MappingException>(Map.connectionMappingDictionaryAtMapper, connectionName.ToString());

			ConnectionStringSettings connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionName.ToString()];
			//ConnectionStringSettings connectionStringSettings = DB.ConnectionStringSettingsCollection[connectionName.ToString()];

			DB.throwIfNullOrEmpty<MappingException>(connectionStringSettings, "Connection settings");
			DB.throwIfNullOrEmpty<MappingException>(connectionStringSettings.ConnectionString, "Connection string");

			NewConnection(connectionName, connectionStringSettings.ConnectionString, dbVendor, usedDBParameterPrefix);
		}

		internal static void NewConnection(Enum connectionName, string connectionString, DBVendor dbVendor, string usedDBParameterPrefix) {
			DB.throwIfNullOrEmpty<MappingException>(connectionName, "Connection name");
			DB.throwIfNullOrEmpty<MappingException>(connectionString, "Connection string");
			DB.throwIfKeyAlreadyExists<string, ConnectionMapping, MappingException>(Map.connectionMappingDictionaryAtMapper, connectionName.ToString());
			DB.throwIfNullOrEmpty<MappingException>(dbVendor, "DB vendor");

			Map.connectionMappingDictionaryAtMapper.Add(connectionName.ToString(), new ConnectionMapping(connectionName.ToString(), connectionString, dbVendor, usedDBParameterPrefix));
		}

		#endregion Connection
	}

	public class EntityMapper {
		EntityMapping entityMapping;
		public PrimaryKeyValueProvider DefaultPrimaryKeyValueProvider;
		public TimestampValueProvider DefaultTimestampValueProvider;

		internal EntityMapper(EntityMapping entityMapping, PrimaryKeyValueProvider defaultPrimaryKeyValueProvider, TimestampValueProvider defaultTimestampValueProvider) {
			this.entityMapping = entityMapping;
			DefaultPrimaryKeyValueProvider = defaultPrimaryKeyValueProvider;
			DefaultTimestampValueProvider = defaultTimestampValueProvider;
		}

		#region TableEntity

		// defaultPrimaryKeyValueProvider ve defaultTimestampValueProvider 0 da geçilebilir, bu değerler sadece mapping sırasında kullanılacak
		internal static EntityMapper NewTableEntity<T>(object tableName, Enum schemaName, Enum connectionName, PrimaryKeyValueProvider defaultPrimaryKeyValueProvider, TimestampValueProvider defaultTimestampValueProvider) {
			DB.throwIfKeyAlreadyExists<Type, EntityMapping, MappingException>(Map.entityMappingDictionaryAtMapper, typeof(T));
			DB.throwIfNullOrEmpty<MappingException>(tableName, "Table name");
			DB.throwIfNullOrEmpty<MappingException>(schemaName, "Schema name");
			DB.throwIfNullOrEmpty<MappingException>(connectionName, "Connection name");

			EntityMapping entityMapping = new EntityMapping(tableName.ToString(), schemaName.ToString(), connectionName.ToString(), typeof(T));
			Map.entityMappingDictionaryAtMapper.Add(typeof(T), entityMapping);

			return new EntityMapper(entityMapping, defaultPrimaryKeyValueProvider, defaultTimestampValueProvider);
		}

		#endregion TableEntity

		#region ViewEntity

		internal static EntityMapper NewViewEntity<T>(Enum connectionName) {
			DB.throwIfKeyAlreadyExists<Type, EntityMapping, MappingException>(Map.entityMappingDictionaryAtMapper, typeof(T));
			DB.throwIfNullOrEmpty<MappingException>(connectionName, "Connection name");

			EntityMapping entityMapping = new EntityMapping(null, null, connectionName.ToString(), typeof(T));
			Map.entityMappingDictionaryAtMapper.Add(typeof(T), entityMapping);
			return new EntityMapper(entityMapping, 0, 0);
		}

		#endregion ViewEntity

		#region PrimaryKey

		internal void NewPrimaryKey<T>(Enum columnName, Expression<Func<T, object>> memberExpression, Enum sequenceName, Enum schemaName, Enum dbFunctionName, Func<object> functionDelegate, PrimaryKeyValueProvider primaryKeyValueProvider) {
			DB.throwIfNullOrEmpty<MappingException>(columnName, "Column name");

			if (primaryKeyValueProvider == PrimaryKeyValueProvider.AppFunctionDelegate)
				DB.throwIfNullOrEmpty<MappingException>(functionDelegate, "Function delegate");
			else if (primaryKeyValueProvider == PrimaryKeyValueProvider.Sequence)
				DB.throwIfNullOrEmpty<MappingException>(sequenceName, "Sequence name");
			else if (primaryKeyValueProvider == PrimaryKeyValueProvider.DBFunction) {
				//DB.throwIfNullOrEmpty<MappingException>(schemaName, "Schema name");
				DB.throwIfNullOrEmpty<MappingException>(dbFunctionName, "DB function name");
			}

			string nestedPrimaryKeyPropertyName = parseNestedPropertyNameFromMemberExpression<T>(memberExpression);
			DB.throwIfNullOrEmpty<MappingException>(nestedPrimaryKeyPropertyName, "Primary key property name");

			entityMapping.PrimaryKeyMapping.AddProperty(nestedPrimaryKeyPropertyName, sequenceName, schemaName, dbFunctionName, functionDelegate, primaryKeyValueProvider);
			newColumn(columnName, nestedPrimaryKeyPropertyName, true, false);
			entityMapping.RefreshPrimaryKeyColumnNameList();

			entityMapping.throwIfAutoSetAndPrimaryKeyColumn(columnName.ToString());
		}

		#endregion PrimaryKey

		#region TimestampProperty

		internal void NewTimestamp<T>(Enum columnName, Expression<Func<T, object>> memberExpression, Enum schemaName, Enum dbFunctionName, Func<object> functionDelegate, TimestampValueProvider timestampValueProvider) {
			string nestedTimestampPropertyName = parseNestedPropertyNameFromMemberExpression<T>(memberExpression);
			DB.throwIfNullOrEmpty<MappingException>(nestedTimestampPropertyName, "Timestamp property name");
			DB.throwIfNullOrEmpty<MappingException>(columnName, "Column name");

			if (timestampValueProvider == TimestampValueProvider.AppFunctionDelegate)
				DB.throwIfNullOrEmpty<MappingException>(functionDelegate, "Function delegate");
			else if (timestampValueProvider == TimestampValueProvider.DBFunction) {
				//DB.throwIfNullOrEmpty<MappingException>(schemaName, "Schema name");
				DB.throwIfNullOrEmpty<MappingException>(dbFunctionName, "DB function name");
			}

			entityMapping.TimestampMapping = new TimestampMapping(columnName.ToString(), nestedTimestampPropertyName, schemaName, dbFunctionName, functionDelegate, timestampValueProvider);

			newColumn(columnName, nestedTimestampPropertyName, true, false);

			entityMapping.throwIfAutoSetAndTimestampColumn(columnName.ToString());
		}

		#endregion TimestampProperty

		#region AutoSetColumn

		internal void NewAutoSetColumn<T>(Before before, Enum columnName, Enum schemaName, Enum dbFunctionName, Func<object> functionDelegate, AutoSetValueProvider autoSetValueProvider) {
			DB.throwIfNullOrEmpty<MappingException>(before, "Before");
			DB.throwIfNullOrEmpty<MappingException>(columnName, "Column name");

			if (autoSetValueProvider == AutoSetValueProvider.AppFunctionDelegate)
				DB.throwIfNullOrEmpty<MappingException>(functionDelegate, "Function delegate");
			else if (autoSetValueProvider == AutoSetValueProvider.DBFunction) {
				//DB.throwIfNullOrEmpty<MappingException>(schemaName, "Schema name");
				DB.throwIfNullOrEmpty<MappingException>(dbFunctionName, "DB function name");
			}

			if (before == Before.Insert)
				entityMapping.InsertAutoSetColumnMappingDictionary.Add(columnName.ToString(), new AutoSetColumnMapping(columnName.ToString(), schemaName, dbFunctionName, functionDelegate, autoSetValueProvider));
			else if (before == Before.Update)
				entityMapping.UpdateAutoSetColumnMappingDictionary.Add(columnName.ToString(), new AutoSetColumnMapping(columnName.ToString(), schemaName, dbFunctionName, functionDelegate, autoSetValueProvider));
			else if (before == Before.LogicalDelete)
				entityMapping.LogicalDeleteAutoSetColumnMappingDictionary.Add(columnName.ToString(), new AutoSetColumnMapping(columnName.ToString(), schemaName, dbFunctionName, functionDelegate, autoSetValueProvider));

			entityMapping.throwIfAutoSetAndPrimaryKeyColumn(columnName.ToString());
			entityMapping.throwIfAutoSetAndTimestampColumn(columnName.ToString());
		}

		#endregion AutoSetColumn

		#region TriggerAction

		internal void NewTriggerAction<T>(When when, Action<T> triggerActionDelegate) {
			DB.throwIfNullOrEmpty<MappingException>(when, "When");
			DB.throwIfNullOrEmpty<MappingException>(triggerActionDelegate, "Trigger action delegate");

			entityMapping.TriggerActionDelegateDictionary.Add(when, triggerActionDelegate);
		}

		internal void NewTriggerActionBeforeSelectCommand<T>(Action triggerActionDelegate) {
			DB.throwIfNullOrEmpty<MappingException>(triggerActionDelegate, "Trigger action delegate");

			entityMapping.BeforeSelectCommandTriggerActionDelegate = triggerActionDelegate;
		}

		#endregion TriggerAction

		#region ValidatorFunction

		internal void NewValidatorFunction<T>(Before before, Func<T, bool> validatorFunctionDelegate) {
			DB.throwIfNullOrEmpty<MappingException>(before, "Before");
			DB.throwIfNullOrEmpty<MappingException>(validatorFunctionDelegate, "Validator function delegate");

			entityMapping.ValidatorFunctionDelegateDictionary.Add(before, validatorFunctionDelegate);
		}

		#endregion ValidatorFunction

		#region TableColumn

		internal void NewTableColumn<T>(Expression<Func<T, object>> memberExpression, Enum columnName) {
			NewTableColumn<T>(memberExpression, columnName, true);
		}

		internal void NewTableColumn<T>(Expression<Func<T, object>> memberExpression, Enum columnName, bool isDBNullableValueType) {
			string nestedPropertyName = parseNestedPropertyNameFromMemberExpression<T>(memberExpression);
			newColumn(columnName, nestedPropertyName, true, isDBNullableValueType);
		}

		#endregion TableColumn

		#region ViewColumn

		internal void NewViewColumn<T>(Expression<Func<T, object>> memberExpression, Enum columnName) {
			string nestedPropertyName = parseNestedPropertyNameFromMemberExpression<T>(memberExpression);
			newColumn(columnName, nestedPropertyName, false, true);
		}

		#endregion ViewColumn

		#region Query

		internal void NewQuery<T>(Enum queryName, string sql, InputParameterNameType[] inputParameterNameTypes) {
			newQuery(queryName, sql, false, 0, inputParameterNameTypes);
		}

		internal void NewQuery<T>(Enum queryName, string sql, bool isQueryResultCached, InputParameterNameType[] inputParameterNameTypes) {
			newQuery(queryName, sql, isQueryResultCached, 0, inputParameterNameTypes);
		}

		internal void NewQuery<T>(Enum queryName, string sql, int maxNumberOfQueriesToHoldInCache, InputParameterNameType[] inputParameterNameTypes) {
			newQuery(queryName, sql, true, maxNumberOfQueriesToHoldInCache, inputParameterNameTypes);
		}

		private void newQuery(Enum queryName, string sql, bool isQueryResultCached, int maxNumberOfQueriesToHoldInCache, InputParameterNameType[] inputParameterNameTypes) {
			DB.throwIfNullOrEmpty<MappingException>(queryName, "Query name");
			DB.throwIfNullOrEmpty<MappingException>(sql, "Sql");

			if (maxNumberOfQueriesToHoldInCache < 0)
				throw new MappingException("Max. no. of queries to hold in cache cannot be negative.");

			QueryMapping queryMapping = new QueryMapping(queryName.ToString(), sql, isQueryResultCached, maxNumberOfQueriesToHoldInCache, inputParameterNameTypes);
			entityMapping.QueryMappingDictionary.Add(queryName.ToString(), queryMapping);
		}

		#endregion Query

		private string parseNestedPropertyNameFromMemberExpression<T>(Expression<Func<T, object>> memberExpression) {
			DB.throwIfNullOrEmpty<MappingException>(memberExpression, "Member expression");

			MemberExpression propertyMemberExpression = null;
			string nestedPropertyName;
			if (memberExpression.Body is MemberExpression)
				propertyMemberExpression = (memberExpression.Body as MemberExpression);
			else if (memberExpression.Body is UnaryExpression) // value typelar objecte cast edildiği için member expression, unary (cast) expression içerisinde yeralıyor
      {
				UnaryExpression unaryExpression = (memberExpression.Body as UnaryExpression);
				if (unaryExpression.Operand is MemberExpression) {
					propertyMemberExpression = (unaryExpression.Operand as MemberExpression);
				}
			}

			DB.throwIfNullOrEmpty<MappingException>(propertyMemberExpression, "Property member expression");

			int firstDotPosition = propertyMemberExpression.ToString().IndexOf(".", StringComparison.Ordinal);
			nestedPropertyName = propertyMemberExpression.ToString().Substring(firstDotPosition + 1);
			PropertyInfo propertyInfo = (propertyMemberExpression.Member as PropertyInfo);
			DB.throwIfNullOrEmpty<MappingException>(propertyInfo, "Property info");

			//mesela DateTime.Now gibi birşey girilmişse entity tipinin bir nested property'si olmadığı için hata verecek
			if (typeof(T) != getOutermostTypeFromNestedMemberExpression(propertyMemberExpression))
				throw new MappingException("Property is in not a direct or nested property of the entity type.");

			entityMapping.NestedPropertyInfoCacheAtMapper.Add(nestedPropertyName, propertyInfo);
			PropertyGetter getter = PropertyGetterSetterFactory.CreateGetter(propertyInfo);
			entityMapping.NestedPropertyGetterCacheAtMapper.Add(nestedPropertyName, getter);
			PropertySetter setter = PropertyGetterSetterFactory.CreateSetter(propertyInfo);
			entityMapping.NestedPropertySetterCacheAtMapper.Add(nestedPropertyName, setter);

			entityMapping.GenerateRootTypePropertyNamePropertyInfoAndGetterSetterCacheEntryAtMapper(typeof(T), nestedPropertyName);

			return nestedPropertyName;
		}

		private static Type getOutermostTypeFromNestedMemberExpression(MemberExpression propertyMemberExpression) {
			if (propertyMemberExpression.Expression == null)
				return null;
			else if (propertyMemberExpression.Expression is MemberExpression)
				return getOutermostTypeFromNestedMemberExpression(propertyMemberExpression.Expression as MemberExpression);
			else
				return propertyMemberExpression.Expression.Type;
		}

		private void newColumn(Enum columnName, string nestedPropertyName, bool isTableColumn, bool isDBNullableValueType) {
			DB.throwIfNullOrEmpty<MappingException>(columnName, "Column name");
			DB.throwIfNullOrEmpty<MappingException>(nestedPropertyName, "Property name");

			ColumnMapping columnMapping = new ColumnMapping(columnName.ToString(), nestedPropertyName, isDBNullableValueType);
			entityMapping.ColumnMappingDictionary.Add(columnName.ToString(), columnMapping);

			if (isTableColumn) {
				entityMapping.NestedTableColumnNameDictionaryAtMapper.Add(nestedPropertyName, columnName.ToString());
				entityMapping.RefreshPrimaryKeyColumnNameList();
			}

			entityMapping.throwIfAutoSetAndPrimaryKeyColumn(columnName.ToString());
			entityMapping.throwIfAutoSetAndTimestampColumn(columnName.ToString());
		}

	}

	public static class Map {

		internal static object lockerForMap = new object();

		internal static Dictionary<string, ConnectionMapping> connectionMappingDictionaryAtMapper = new Dictionary<string, ConnectionMapping>();

		internal static Dictionary<Type, EntityMapping> entityMappingDictionaryAtMapper = new Dictionary<Type, EntityMapping>();

		public static ReadOnlyCollection<ConnectionMapping> ConnectionMappings {
			get {
				ReadOnlyCollection<ConnectionMapping> readOnlyCollection = null;
				if (Map.connectionMappingDictionaryAtMapper.Values != null)
					readOnlyCollection = Map.connectionMappingDictionaryAtMapper.Values.ToList().AsReadOnly();

				return readOnlyCollection;
			}
		}

		public static ReadOnlyCollection<EntityMapping> EntityMappings {
			get {
				ReadOnlyCollection<EntityMapping> readOnlyCollection = null;
				if (Map.entityMappingDictionaryAtMapper.Values != null)
					readOnlyCollection = Map.entityMappingDictionaryAtMapper.Values.ToList().AsReadOnly();

				return readOnlyCollection;
			}
		}

		public static void GenerateCommands() {
			lock (lockerForMap) {
				foreach (EntityMapping entityMapping in Map.entityMappingDictionaryAtMapper.Values) {
					entityMapping.GenerateCommands();
				}
			}
		}

	}
}
