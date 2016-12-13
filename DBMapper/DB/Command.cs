using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Reflection;
using System.Collections;
using System.Globalization;

namespace DBMapper {
	internal class BaseCommand {

		protected EntityMapping entityMapping;

		protected virtual void initialize() {
			throw new NotImplementedException();
		}

		protected virtual DbCommand createDbCommand() {
			throw new NotImplementedException();
		}

		protected virtual string createCommandText() {
			throw new NotImplementedException();
		}

		protected virtual DbCommand createOrGetDbCommand() {
			throw new NotImplementedException();
		}

		protected virtual DbParameter[] createDbParameterList() {
			throw new NotImplementedException();
		}

		internal virtual List<T> Execute<T>() where T : class, new() {
			throw new NotImplementedException();
		}

		internal virtual void Execute() {
			throw new NotImplementedException();
		}

	}

	internal class SelectCommand : BaseCommand {

		internal string QueryName;
		protected InputParameter[] inputParameters;

		internal QueryMapping QueryMapping;

		internal SelectCommand(Enum queryName, InputParameter[] inputParameters, EntityMapping entityMapping, QueryMapping queryMapping) {
			this.QueryName = queryName.ToString();
			this.inputParameters = inputParameters;
			this.entityMapping = entityMapping;
			QueryMapping = queryMapping;
		}

		internal override List<T> Execute<T>() {
			List<T> entityList = new List<T>();

			DbCommand dbCommand = createOrGetDbCommand();
			dbCommand.CommandText = entityMapping.ConnectionMapping.ReplaceUsedDBParameterPrefixAndExtractCollectionParameters(dbCommand.CommandText, inputParameters);

			DbParameter[] dbParamterList = createDbParameterList();

			dbCommand.Parameters.AddRange(dbParamterList);

			using (dbCommand.Connection = entityMapping.ConnectionMapping.CreateDbConnection()) {
				dbCommand.Connection.Open();
				using (IDataReader dataReader = dbCommand.ExecuteReader()) {

					while (dataReader.Read()) {
						T entity = new T();
						int fieldCount = dataReader.FieldCount;

						for (int i = 0; i < fieldCount; i++) {
							fillPropertyAtSelect(entity, dataReader, i);
						}
						entityList.Add(entity);
					}
				}
			}

			return entityList;
		}

		protected override DbParameter[] createDbParameterList() {
			List<DbParameter> dbParameterList = new List<DbParameter>();
			foreach (InputParameter inputParameter in inputParameters) {
				if (inputParameter.Value is ICollection && inputParameter.Value != null) {
					int i = 1;
					foreach (object item in (inputParameter.Value as ICollection)) {
						DbParameter dbParameter = entityMapping.ConnectionMapping.CreateDbParameter(inputParameter.Name + "__" + i, item);
						dbParameterList.Add(dbParameter);
						i++;
					}
				}
				else {
					DbParameter dbParameter = entityMapping.ConnectionMapping.CreateDbParameter(inputParameter.Name, inputParameter.Value);
					dbParameterList.Add(dbParameter);
				}
			}

			return dbParameterList.ToArray();
		}

		protected override DbCommand createDbCommand() {
			DbCommand dbCommand = null;
			string sql = QueryMapping.SelectSql;
			dbCommand = entityMapping.ConnectionMapping.CreateTextDbCommand(sql);

			return dbCommand;
		}

		protected override DbCommand createOrGetDbCommand() {
			DbCommand dbCommand = null;
			if (!DB.reuseDbCommands)
				dbCommand = createDbCommand();
			else {
				dbCommand = QueryMapping.SelectDbCommandCacheAtMapper;
				if (dbCommand != null) {
					dbCommand.Parameters.Clear();
					dbCommand.CommandText = QueryMapping.SelectSql;
				}
				else {
					dbCommand = createDbCommand();
					QueryMapping.SelectDbCommandCacheAtMapper = dbCommand;
				}
			}
			return dbCommand;
		}

		private void fillPropertyAtSelect<T>(T entity, IDataReader dataReader, int columnIndex) {
			// selectten gelen kolonla eşleşen property yoksa o kolon için birşey yapılmaz
			// selectte olmayan propertylar için ise zaten bir şey yapılmıyor
			PropertySetter setter = QueryMapping.PositionToSetterCacheAtMapper[columnIndex];
			string columnName = dataReader.GetName(columnIndex);
			if (setter != null) {
				object columnValue = dataReader.GetValue(columnIndex);
				string nestedPropertyName = entityMapping.ColumnMappingDictionary[columnName].NestedPropertyName;
				if (columnValue == DBNull.Value) {
					columnValue = TypeConverter.GetDefaultValue(QueryMapping.PositionToTypeCacheAtMapper[columnIndex]);
				}
				else {
					// tablo dışından gelen değerlerde tipler uyuşmayabiliyor 
					// (mesela 99 as a şeklinde select edilen değer, datareaderdan decimal olarak geliyor
					// bunu propertyde int tanımlandıysa inte cast etmek gerekiyor
					Type type = QueryMapping.PositionToTypeCacheAtMapper[columnIndex];
					if (columnValue.GetType() != type)
						//oracle da timestamp kolon datetime olarak geliyor
						if (columnName == entityMapping.TimestampMapping.ColumnName)
							columnValue = BitConverter.GetBytes(((DateTime)columnValue).Ticks);
						else if (columnValue.GetType() == typeof(DateTime) && type == typeof(byte[]))
							columnValue = BitConverter.GetBytes(((DateTime)columnValue).Ticks);
						else {
							if (type.IsEnum) {
								if (columnValue.GetType() != typeof(int))
									columnValue = Convert.ChangeType(columnValue, typeof(int), CultureInfo.InvariantCulture);
								columnValue = Enum.ToObject(type, (int)columnValue);
							}
							else
								columnValue = TypeConverter.ConvertToPropertyType(columnValue, type);
						}
				}
				entityMapping.SetNestedPropertyValue(entityMapping.EntityType, entity, nestedPropertyName, columnValue, setter);
			}
		}

	}

	internal class DeleteCommand : BaseCommand {

		private object entity;

		internal DeleteCommand(object entity) {
			this.entity = entity;
		}

		internal override void Execute() {
			if (entity == null)
				throw new CommandException("Entity instance cannot be null.");

			Type entityType = entity.GetType();
			entityMapping = Map.entityMappingDictionaryAtMapper[entityType];

			if (entityMapping.LogicalDeleteAutoSetColumnMappingDictionary.Count != 0) {
				entityMapping.ConnectionMapping.CommandFactory.CreateLogicalDeleteCommand(entity, entityMapping).Execute();
			}
			else {
				DB.throwIfNullOrEmpty<CommandException>(entityMapping.TableName, "Table name");
				DB.throwIfNullOrEmpty<CommandException>(entityMapping.SchemaName, "Schema name");

				Delegate beforeDeleteActionDelegate = null;
				if (entityMapping.TriggerActionDelegateDictionary.TryGetValue(When.BeforeDeleteForEachRow, out beforeDeleteActionDelegate))
					DB.tryDynamicInvokeTriggerAction(beforeDeleteActionDelegate, entity);

				DbParameter[] dbParameterList = createDbParameterList();

				DbCommand dbCommand = createOrGetDbCommand();

				dbCommand.Parameters.AddRange(dbParameterList);

				entityMapping.ConnectionMapping.ExecuteNonQuery(dbCommand, true);

				Delegate afterDeleteActionDelegate = null;
				if (entityMapping.TriggerActionDelegateDictionary.TryGetValue(When.AfterDeleteForEachRow, out afterDeleteActionDelegate))
					DB.tryDynamicInvokeTriggerAction(afterDeleteActionDelegate, entity);
			}

		}

		protected override DbParameter[] createDbParameterList() {
			List<DbParameter> dbParameterList = new List<DbParameter>();

			foreach (string nestedPrimaryKeyPropertyName in entityMapping.PrimaryKeyMapping.NestedPrimaryKeyPropertyNameList) {
				string columnName = entityMapping.NestedTableColumnNameDictionaryAtMapper[nestedPrimaryKeyPropertyName];
				DbParameter dbParameter = entityMapping.CreateAndSetInputDbParameterFromNonTimestampNestedProperty(columnName, entity, nestedPrimaryKeyPropertyName, false);
				dbParameterList.Add(dbParameter);
			}

			//delete için diğer dbparameterlara ek olarak, OLD_ ile başlayan 
			//mevcut timestamp property değeri için input dbparameter yaratalım, App = 1 mantığıyla oluşrurulacak
			if (entityMapping.TimestampMapping.Exists()) {
				DbParameter dbParameterForCurrentTimestamp = null;
				//App = 1, //property değeriyle input dbparameter yarat, sqle kolonu koy
				//timestamp property değeri null ise dbparameter yaratma (selectten çekilmediği varsayılır, logicaldelete'de query ismi parametre olarak geçilmediği için buna bakılıyor)
				dbParameterForCurrentTimestamp = entityMapping.CreateAndSetInputDbParameterForOldTimestampFromTimestampNestedProperty("OLD_" + entityMapping.TimestampMapping.ColumnName, entity, entityMapping.TimestampMapping.NestedPropertyName);
				if (dbParameterForCurrentTimestamp != null)
					dbParameterList.Add(dbParameterForCurrentTimestamp);
				else
					throw new MappingException("Old value for timestamp property can not be null.");
			}

			return dbParameterList.ToArray();
		}

		protected override DbCommand createOrGetDbCommand() {
			DbCommand dbCommand = null;
			if (DB.reuseDbCommands && entityMapping.DeleteDbCommandCache != null) {
				dbCommand = entityMapping.DeleteDbCommandCache;
				dbCommand.Parameters.Clear();
			}
			else {
				if (string.IsNullOrEmpty(entityMapping.DeleteSqlCache))
					entityMapping.DeleteSqlCache = createCommandText();
				dbCommand = entityMapping.ConnectionMapping.CreateTextDbCommand(entityMapping.DeleteSqlCache);
				if (DB.reuseDbCommands)
					entityMapping.DeleteDbCommandCache = dbCommand;
			}
			return dbCommand;
		}

		protected override string createCommandText() {
			StringBuilder sqlStringBuilder = new StringBuilder();
			sqlStringBuilder.Append("DELETE ");
			sqlStringBuilder.Append(entityMapping.SchemaName);
			sqlStringBuilder.Append(".");
			sqlStringBuilder.Append(entityMapping.TableName);
			sqlStringBuilder.Append(" WHERE ");

			bool isFirstKey = true;
			foreach (KeyValuePair<string, string> primaryKeyColumnNamePropertyNamePair in entityMapping.PrimaryKeyColumnNamePropertyNamePairList) {
				if (!isFirstKey)
					sqlStringBuilder.Append(" AND ");

				sqlStringBuilder.Append(primaryKeyColumnNamePropertyNamePair.Key);
				sqlStringBuilder.Append(" = ");
				sqlStringBuilder.Append(entityMapping.ConnectionMapping.DBParameterPrefix);
				sqlStringBuilder.Append(primaryKeyColumnNamePropertyNamePair.Key);
				isFirstKey = false;
			}

			if (entityMapping.TimestampMapping.Exists()) {
				sqlStringBuilder.Append(" AND ");
				sqlStringBuilder.Append(entityMapping.TimestampMapping.ColumnName);
				sqlStringBuilder.Append(" = ");
				sqlStringBuilder.Append(entityMapping.ConnectionMapping.DBParameterPrefix);
				sqlStringBuilder.Append("OLD_" + entityMapping.TimestampMapping.ColumnName);
			}

			return sqlStringBuilder.ToString();
		}

	}

	internal class FillBackCommand : BaseCommand {

		protected object entity;
		protected DbCommand dbCommand;
		protected FillBackCommandMode fillBackCommandMode;

		internal QueryMapping QueryMapping;

		protected bool checkRowCount;
		protected When whenBefore;
		protected When whenAfter;
		protected Before before;

		internal override void Execute() {
			initialize();

			DbParameter[] dbParameterList = createDbParameterList();

			dbCommand = createOrGetDbCommand();

			dbCommand.Parameters.AddRange(dbParameterList.ToArray());

			entityMapping.ConnectionMapping.ExecuteNonQuery(dbCommand, checkRowCount);

			fillReturnValuePropertiesAfterDbCommand();

			Delegate afterActionDelegate = null;
			if (entityMapping.TriggerActionDelegateDictionary.TryGetValue(whenAfter, out afterActionDelegate))
				DB.tryDynamicInvokeTriggerAction(afterActionDelegate, entity);
		}

		protected virtual IList<string> getNestedPropertyNameListToInsertUpdate() {
			throw new NotImplementedException();
		}

		protected virtual string getSequenceNextValueSqlString(string sequenceName) {
			//return null;
			throw new NotImplementedException();
		}

		protected override DbParameter[] createDbParameterList() {
			//Primary key, timestamp, property mapli autoset column ve diğer table columnlar için
			//ValueProvider deperlerine göre yapılacaklar:
			//App = 1, //property değeriyle input dbparameter yarat, sqle kolonu koy
			//AppFunctionDelegate = 2, //delegate invoke et, bu değerle inputoutput dbparameter yarat, sqle kolonu koy, sqlde return et/property set et
			//Sequence = 3, //boş değerle inputoutput dbparameter yarat, sqle sequence koy, sqlde return et/property set et
			//DBFunction = 4, //boş değerle inputoutput dbparameter yarat, sqle dbfunction koy, sqlde return et/property set et
			//DBTriggerredAutoValue = 5 //boş değerle inputoutput dbparameter yarat, sqle kolonu koyma, sqlde return et/property set et

			List<DbParameter> dbParameterList = new List<DbParameter>();
			//mappingde olmayan propertylar için birşey yapılmaz

			IList<string> nestedPropertyNameListToInsertUpdate = getNestedPropertyNameListToInsertUpdate();

			//update ve logicalDelete için diğer dbparameterlara ek olarak, OLD_ ile başlayan 
			//mevcut timestamp property değeri için input dbparameter, App = 1 mantığıyla oluşturulacak
			if (fillBackCommandMode != FillBackCommandMode.Insert && nestedPropertyNameListToInsertUpdate.Contains(entityMapping.TimestampMapping.NestedPropertyName)) {
				DbParameter dbParameterForCurrentTimestamp = null;
				//App = 1, //property değeriyle input dbparameter yarat, sqle kolonu koy
				//timestamp property değeri null ise dbparameter yaratma (selectten çekilmediği varsayılır, logicaldelete'de query ismi parametre olarak geçilmediği için buna bakılıyor)
				dbParameterForCurrentTimestamp = entityMapping.CreateAndSetInputDbParameterForOldTimestampFromTimestampNestedProperty("OLD_" + entityMapping.TimestampMapping.ColumnName, entity, entityMapping.TimestampMapping.NestedPropertyName);
				if (dbParameterForCurrentTimestamp != null)
					dbParameterList.Add(dbParameterForCurrentTimestamp);
				else
					throw new MappingException("Old value for timestamp property can not be null.");
			}

			Dictionary<string, AutoSetColumnMapping> autoSetColumnMappingDictionary = null;
			Dictionary<string, ValueProviderInfo> nestedPropertyValueProviderInfoCacheAtMapper = null;

			if (fillBackCommandMode == FillBackCommandMode.Insert) {
				autoSetColumnMappingDictionary = entityMapping.InsertAutoSetColumnMappingDictionary;
				nestedPropertyValueProviderInfoCacheAtMapper = entityMapping.nestedPropertyValueProviderInfoForInsertCacheAtMapper;
			}
			else if (fillBackCommandMode == FillBackCommandMode.Update) {
				autoSetColumnMappingDictionary = entityMapping.UpdateAutoSetColumnMappingDictionary;
				nestedPropertyValueProviderInfoCacheAtMapper = QueryMapping.nestedPropertyValueProviderInfoForUpdateCacheAtMapper;
			}
			else if (fillBackCommandMode == FillBackCommandMode.LogicalDelete) {
				autoSetColumnMappingDictionary = entityMapping.LogicalDeleteAutoSetColumnMappingDictionary;
				nestedPropertyValueProviderInfoCacheAtMapper = entityMapping.nestedPropertyValueProviderInfoForLogicalDeleteCacheAtMapper;
			}

			foreach (string nestedPropertyName in nestedPropertyNameListToInsertUpdate) {

				ValueProviderInfo valueProviderInfo = null;
				if (!nestedPropertyValueProviderInfoCacheAtMapper.TryGetValue(nestedPropertyName, out valueProviderInfo))
					continue;

				DbParameter dbParameter = null;

				if (valueProviderInfo.valueProvider == ValueProvider.App) {
					//App = 1, //property değeriyle input dbparameter yarat, sqle kolonu koy
					//timestamp için app tanımı yok..
					dbParameter = entityMapping.CreateAndSetInputDbParameterFromNonTimestampNestedProperty(valueProviderInfo.columnName, entity, nestedPropertyName, valueProviderInfo.isDBNullableValueType);
				}
				else if (valueProviderInfo.valueProvider == ValueProvider.AppFunctionDelegate) {
					//AppFunctionDelegate = 2, //delegate invoke et, bu değerle inputoutput dbparameter yarat, sqle kolonu koy, sqlde return et/property set et
					object value = DB.tryInvokeAutoSetFunction(valueProviderInfo.valueProviderSet.FunctionDelegate);
					//değer delegate'den geleceği için byte[] dönüştürmeden direkt set edilecek
					dbParameter = entityMapping.CreateInputOutputDbParameterFromValueAndNestedPropertyTypeForNonTimestamp(valueProviderInfo.columnName, value, nestedPropertyName, valueProviderInfo.isDBNullableValueType);
				}
				else if (valueProviderInfo.valueProvider == ValueProvider.Sequence) {
					//Sequence = 3, //boş değerle inputoutput dbparameter yarat, sqle sequence koy, sqlde return et/property set et
					dbParameter = entityMapping.CreateInputOutputDbParameterFromValueAndNestedPropertyTypeForNonTimestamp(valueProviderInfo.columnName, null, nestedPropertyName, valueProviderInfo.isDBNullableValueType);
				}
				else if (valueProviderInfo.valueProvider == ValueProvider.DBFunction) {
					//DBFunction = 4, //boş değerle inputoutput dbparameter yarat, sqle dbfunction koy, sqlde return et/property set et
					if (valueProviderInfo.isForTimestamp)
						dbParameter = entityMapping.ConnectionMapping.CreateInputOutputDbParameterFromValueAndNestedPropertyTypeForTimestamp(valueProviderInfo.columnName, null);
					else
						dbParameter = entityMapping.CreateInputOutputDbParameterFromValueAndNestedPropertyTypeForNonTimestamp(valueProviderInfo.columnName, null, nestedPropertyName, valueProviderInfo.isDBNullableValueType);
				}
				else if (valueProviderInfo.valueProvider == ValueProvider.DBTriggerredAutoValue) {
					//DBTriggerredAutoValue = 5 //boş değerle inputoutput dbparameter yarat, sqle kolonu koyma, sqlde return et/property set et
					if (valueProviderInfo.isForTimestamp)
						dbParameter = entityMapping.ConnectionMapping.CreateInputOutputDbParameterFromValueAndNestedPropertyTypeForTimestamp(valueProviderInfo.columnName, null);
					else
						dbParameter = entityMapping.CreateInputOutputDbParameterFromValueAndNestedPropertyTypeForNonTimestamp(valueProviderInfo.columnName, null, nestedPropertyName, valueProviderInfo.isDBNullableValueType);
				}

				if (dbParameter != null)
					dbParameterList.Add(dbParameter);
			}

			//Property mapsiz autoset columnlar için
			//ValueProvider deperlerine göre yapılacaklar:
			//AppFunctionDelegate = 2, //delegate invoke, bu değerle input dbparameter yarat, sqle kolonu koy
			//DBFunction = 4, //sqle dbfunction koy
			//DBTriggerredAutoValue = 5 //hiçbirşey yapma

			foreach (AutoSetColumnMapping autoSetColumnMapping in autoSetColumnMappingDictionary.Values) {
				if (!entityMapping.NestedTableColumnNameDictionaryAtMapper.Values.Contains(autoSetColumnMapping.ColumnName)) {
					DbParameter dbParameter = null;
					if (autoSetColumnMapping.ValueProviderSet.ValueProvider == ValueProvider.AppFunctionDelegate) {
						object value = DB.tryInvokeAutoSetFunction(autoSetColumnMapping.ValueProviderSet.FunctionDelegate);
						//TODO: mapsiz autoset column için type ve isDBNullableValueType bilgileri yok, parametre direkt set et, ancak çeşitli tiplerde sonucu test et
						dbParameter = entityMapping.ConnectionMapping.CreateDbParameter(autoSetColumnMapping.ColumnName, value);
					}
					else if (autoSetColumnMapping.ValueProviderSet.ValueProvider == ValueProvider.DBFunction) {
					}
					if (dbParameter != null)
						dbParameterList.Add(dbParameter);
				}
			}

			return dbParameterList.ToArray();
		}

		protected override void initialize() {
			if (entity == null)
				throw new CommandException("Entity instance cannot be null.");

			Delegate validatorFunctionDelegate = null;
			if (entityMapping.ValidatorFunctionDelegateDictionary.TryGetValue(before, out validatorFunctionDelegate))
				DB.tryDynamicInvokeValidatorFunction(validatorFunctionDelegate, entity);

			Delegate beforeActionDelegate = null;
			if (entityMapping.TriggerActionDelegateDictionary.TryGetValue(whenBefore, out beforeActionDelegate))
				DB.tryDynamicInvokeTriggerAction(beforeActionDelegate, entity);

			DB.throwIfNullOrEmpty<CommandException>(entityMapping.TableName, "Table name");
			DB.throwIfNullOrEmpty<CommandException>(entityMapping.SchemaName, "Schema name");

		}

		private void fillReturnValuePropertiesAfterDbCommand() {
			Dictionary<string, string> returningColumnNamePropertyNameDictionary = new Dictionary<string, string>();
			if (fillBackCommandMode == FillBackCommandMode.Insert)
				returningColumnNamePropertyNameDictionary = entityMapping.insertReturningColumnNamePropertyNameCacheAtMapper;
			else if (fillBackCommandMode == FillBackCommandMode.Update)
				returningColumnNamePropertyNameDictionary = QueryMapping.updateReturningColumnNamePropertyNameCacheAtMapper;
			else if (fillBackCommandMode == FillBackCommandMode.LogicalDelete)
				returningColumnNamePropertyNameDictionary = entityMapping.logicalDeleteReturningColumnNamePropertyNameCacheAtMapper;

			foreach (KeyValuePair<string, string> returningColumnNamePropertyNamePair in returningColumnNamePropertyNameDictionary) {
				PropertyInfo nestedPropertyInfo = entityMapping.NestedPropertyInfoCacheAtMapper[returningColumnNamePropertyNamePair.Value];
				PropertySetter setter = entityMapping.NestedPropertySetterCacheAtMapper[returningColumnNamePropertyNamePair.Value];

				foreach (var p in dbCommand.Parameters) {
					DbParameter dbParameter = (p as DbParameter);
					if (dbParameter.ParameterName == returningColumnNamePropertyNamePair.Key) {
						object columnValue = dbParameter.Value;
						if (columnValue == DBNull.Value) {
							columnValue = TypeConverter.GetDefaultValue(nestedPropertyInfo.PropertyType);
						}
						else {
							// tablo dışından gelen değerlerde tipler uyuşmayabiliyor 
							// (mesela 99 as a şeklinde select edilen değer, datareaderdan decimal olarak geliyor
							// bunu propertyde int tanımlandıysa inte cast etmek gerekiyor
							if (columnValue.GetType() != nestedPropertyInfo.PropertyType)
								//oracle da timestamp kolon datetime olarak geliyor
								if (entityMapping.TimestampMapping.ExistsForNestedPropertyName(returningColumnNamePropertyNamePair.Value)) {
									columnValue = BitConverter.GetBytes(((DateTime)columnValue).Ticks);
								}
								else
									TypeConverter.ConvertToPropertyType(columnValue, nestedPropertyInfo.PropertyType);
						}
						entityMapping.SetNestedPropertyValue(entityMapping.EntityType, entity, returningColumnNamePropertyNamePair.Value, columnValue, setter);
					}
				}
			}
		}

	}

	internal class InsertCommand : FillBackCommand {

		protected InsertCommand(object entity, EntityMapping entityMapping) {
			this.entity = entity;
			this.entityMapping = entityMapping;
			fillBackCommandMode = FillBackCommandMode.Insert;
			checkRowCount = false;

			before = Before.Insert;
			whenBefore = When.BeforeInsertForEachRow;
			whenAfter = When.AfterInsertForEachRow;
		}

		protected override IList<string> getNestedPropertyNameListToInsertUpdate() {
			return entityMapping.NestedTableColumnNameDictionaryAtMapper.Keys.ToList();
		}

		protected override DbCommand createOrGetDbCommand() {

			DbCommand dbCommand = null;
			if (DB.reuseDbCommands && entityMapping.InsertDbCommandCacheAtMapper != null) {
				dbCommand = entityMapping.InsertDbCommandCacheAtMapper;
				dbCommand.Parameters.Clear();
			}
			else {
				if (string.IsNullOrEmpty(entityMapping.InsertSqlCacheAtMapper))
					entityMapping.InsertSqlCacheAtMapper = createCommandText();
				dbCommand = entityMapping.ConnectionMapping.CreateTextDbCommand(entityMapping.InsertSqlCacheAtMapper);
				if (DB.reuseDbCommands)
					entityMapping.InsertDbCommandCacheAtMapper = dbCommand;
			}
			return dbCommand;
		}

		protected virtual string CreateCommandText() {
			throw new NotImplementedException();
		}

	}

	internal class UpdateCommand : FillBackCommand {

		protected UpdateCommand(object entity, EntityMapping entityMapping, QueryMapping queryMapping) {
			this.entity = entity;
			this.entityMapping = entityMapping;
			fillBackCommandMode = FillBackCommandMode.Update;
			QueryMapping = queryMapping;

			checkRowCount = true;

			before = Before.Update;
			whenBefore = When.BeforeUpdateForEachRow;
			whenAfter = When.AfterUpdateForEachRow;
		}

		protected override IList<string> getNestedPropertyNameListToInsertUpdate() {
			return QueryMapping.NestedPropertyNameListForUpdateCacheAtMapper;
		}

		protected virtual string CreateCommandText() {
			throw new NotImplementedException();
		}

		protected override DbCommand createDbCommand() {
			//aynı entity için her farklı propertyNamesToUpdate (yani farklı setclause) için farklı update dbcommandler olacak,
			//bunları dictionaryde ayırdetmek için queryname dictionary key yerine setclause kullanıyoruz

			DbCommand dbCommand = null;
			if (DB.reuseDbCommands && QueryMapping.UpdateDbCommandCache != null) {
				dbCommand = QueryMapping.UpdateDbCommandCache;
				dbCommand.Parameters.Clear();
			}
			else {
				if (string.IsNullOrEmpty(QueryMapping.UpdateSqlCache))
					QueryMapping.UpdateSqlCache = createCommandText();
				dbCommand = entityMapping.ConnectionMapping.CreateTextDbCommand(QueryMapping.UpdateSqlCache);
				if (DB.reuseDbCommands)
					QueryMapping.UpdateDbCommandCache = dbCommand;
			}
			return dbCommand;
		}

		protected override DbCommand createOrGetDbCommand() {
			//aynı entity için her farklı propertyNamesToUpdate (yani farklı setclause) için farklı update dbcommandler olacak,
			//bunları dictionaryde ayırdetmek için queryname dictionary key yerine setclause kullanıyoruz
			DbCommand dbCommand = null;
			if (!DB.reuseDbCommands)
				return createDbCommand();
			else {
				dbCommand = QueryMapping.UpdateDbCommandCache;
				if (dbCommand != null) {
					dbCommand.Parameters.Clear();
				}
				else {
					if (string.IsNullOrEmpty(QueryMapping.UpdateSqlCache))
						QueryMapping.UpdateSqlCache = createCommandText();
					dbCommand = entityMapping.ConnectionMapping.CreateTextDbCommand(QueryMapping.UpdateSqlCache);
					QueryMapping.UpdateDbCommandCache = dbCommand;
				}
			}

			return dbCommand;
		}

	}

	internal class LogicalDeleteCommand : FillBackCommand {

		protected LogicalDeleteCommand(object entity, EntityMapping entityMapping) {
			this.entity = entity;
			this.entityMapping = entityMapping;
			fillBackCommandMode = FillBackCommandMode.LogicalDelete;
			checkRowCount = true;

			before = Before.LogicalDelete;
			whenBefore = When.BeforeDeleteForEachRow;
			whenAfter = When.AfterDeleteForEachRow;
		}

		protected override IList<string> getNestedPropertyNameListToInsertUpdate() {
			return entityMapping.NestedPropertyNameListForLogicalDeleteCacheAtMapper;
		}

		protected override DbCommand createOrGetDbCommand() {
			DbCommand dbCommand = null;
			if (DB.reuseDbCommands && entityMapping.LogicalDeleteDbCommandCache != null) {
				dbCommand = entityMapping.LogicalDeleteDbCommandCache;
				dbCommand.Parameters.Clear();
			}
			else {
				if (string.IsNullOrEmpty(entityMapping.LogicalDeleteSqlCache))
					entityMapping.LogicalDeleteSqlCache = createCommandText();
				dbCommand = entityMapping.ConnectionMapping.CreateTextDbCommand(entityMapping.LogicalDeleteSqlCache);
				if (DB.reuseDbCommands)
					entityMapping.LogicalDeleteDbCommandCache = dbCommand;
			}
			return dbCommand;
		}

	}

}
