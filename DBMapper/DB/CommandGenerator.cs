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

	internal class ValueProviderInfo {
		internal string columnName;
		internal ValueProviderSet valueProviderSet;
		internal ValueProvider valueProvider;
		internal bool isForPrimaryKey;
		internal bool isForTimestamp;
		internal bool isDBNullableValueType;
	}

	internal class BaseCommandGenerator {

		protected EntityMapping entityMapping;

		protected virtual void generateDbCommand() {
			throw new NotImplementedException();
		}

		protected virtual string generateCommandText() {
			throw new NotImplementedException();
		}

		protected virtual void generateDbParameterList() {
			throw new NotImplementedException();
		}

		internal virtual void Generate() {
			throw new NotImplementedException();
		}

	}

	internal class SelectCommandGenerator : BaseCommandGenerator {

		internal QueryMapping QueryMapping;

		internal SelectCommandGenerator(EntityMapping entityMapping, QueryMapping queryMapping) {
			this.entityMapping = entityMapping;
			QueryMapping = queryMapping;
		}

		internal override void Generate() {
			generateDbCommand();
			entityMapping.generateQueryNestedPropertyNameListAndPositionToCaches(entityMapping, QueryMapping);
		}

		protected override void generateDbCommand() {
			string sql = entityMapping.ConnectionMapping.replaceUsedDBParameterPrefix(QueryMapping.SelectSql, QueryMapping.inputParameterNameTypes);
			DbCommand dbCommand = entityMapping.ConnectionMapping.CreateTextDbCommand(QueryMapping.SelectSql);
			QueryMapping.SelectDbCommandCacheAtMapper = dbCommand;
		}

	}

	internal class DeleteCommandGenerator : BaseCommandGenerator {

		internal DeleteCommandGenerator(EntityMapping entityMapping) {
			this.entityMapping = entityMapping;
		}

		internal override void Generate() {

			if (entityMapping.LogicalDeleteAutoSetColumnMappingDictionary.Count != 0) {
				entityMapping.ConnectionMapping.CommandGeneratorFactory.CreateLogicalDeleteCommandGenerator(entityMapping).Generate();
			}
			else {

				generateDbParameterList();

				generateDbCommand();

			}

		}

		protected override void generateDbParameterList() {
		}

		protected override void generateDbCommand() {
			entityMapping.DeleteSqlCache = generateCommandText();
			DbCommand dbCommand = entityMapping.ConnectionMapping.CreateTextDbCommand(entityMapping.DeleteSqlCache);
			entityMapping.DeleteDbCommandCache = dbCommand;
		}

		protected override string generateCommandText() {
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

	internal class FillBackCommandGenerator : BaseCommandGenerator {

		protected FillBackCommandMode fillBackCommandMode;

		internal QueryMapping QueryMapping;

		internal override void Generate() {
			generateDbParameterList();
			generateDbCommand();
		}

		protected virtual IList<string> getNestedPropertyNameListToInsertUpdate() {
			throw new NotImplementedException();
		}

		protected virtual string getSequenceNextValueSqlString(string sequenceName) {
			return null;
		}

		protected override void generateDbParameterList() {

			IList<string> nestedPropertyNameListToInsertUpdate = getNestedPropertyNameListToInsertUpdate();

			Dictionary<string, AutoSetColumnMapping> autoSetColumnMappingDictionary = null;

			Dictionary<string, string> returningColumnNamePropertyNameCache = null;
			Dictionary<string, string> insertUpdateColumnNameValueNameCacheForNonPrimaryKey = null;
			Dictionary<string, string> insertUpdateColumnNameValueNameCacheForPrimaryKey = null;

			Dictionary<string, ValueProviderInfo> nestedPropertyValueProviderInfoCacheAtMapper = null;

			if (fillBackCommandMode == FillBackCommandMode.Insert) {
				autoSetColumnMappingDictionary = entityMapping.InsertAutoSetColumnMappingDictionary;
				insertUpdateColumnNameValueNameCacheForNonPrimaryKey = entityMapping.insertColumnNameValueNameForNonPrimaryKeyCacheAtMapper;
				insertUpdateColumnNameValueNameCacheForPrimaryKey = entityMapping.insertColumnNameValueNameForPrimaryKeyCacheAtMapper;
				returningColumnNamePropertyNameCache = entityMapping.insertReturningColumnNamePropertyNameCacheAtMapper;
				nestedPropertyValueProviderInfoCacheAtMapper = entityMapping.nestedPropertyValueProviderInfoForInsertCacheAtMapper;
			}
			else if (fillBackCommandMode == FillBackCommandMode.Update) {
				autoSetColumnMappingDictionary = entityMapping.UpdateAutoSetColumnMappingDictionary;
				insertUpdateColumnNameValueNameCacheForNonPrimaryKey = QueryMapping.updateColumnNameValueNameForNonPrimaryKeyCacheAtMapper;
				insertUpdateColumnNameValueNameCacheForPrimaryKey = QueryMapping.updateColumnNameValueNameForPrimaryKeyCacheAtMapper;
				returningColumnNamePropertyNameCache = QueryMapping.updateReturningColumnNamePropertyNameCacheAtMapper;
				nestedPropertyValueProviderInfoCacheAtMapper = QueryMapping.nestedPropertyValueProviderInfoForUpdateCacheAtMapper;
			}
			else if (fillBackCommandMode == FillBackCommandMode.LogicalDelete) {
				autoSetColumnMappingDictionary = entityMapping.LogicalDeleteAutoSetColumnMappingDictionary;
				insertUpdateColumnNameValueNameCacheForNonPrimaryKey = entityMapping.logicalDeleteColumnNameValueNameForNonPrimaryKeyCacheAtMapper;
				insertUpdateColumnNameValueNameCacheForPrimaryKey = entityMapping.logicalDeleteColumnNameValueNameForPrimaryKeyCacheAtMapper;
				returningColumnNamePropertyNameCache = entityMapping.logicalDeleteReturningColumnNamePropertyNameCacheAtMapper;
				nestedPropertyValueProviderInfoCacheAtMapper = entityMapping.nestedPropertyValueProviderInfoForLogicalDeleteCacheAtMapper;
			}

			insertUpdateColumnNameValueNameCacheForNonPrimaryKey.Clear();
			insertUpdateColumnNameValueNameCacheForPrimaryKey.Clear();
			returningColumnNamePropertyNameCache.Clear();
			nestedPropertyValueProviderInfoCacheAtMapper.Clear();

			foreach (string nestedPropertyName in nestedPropertyNameListToInsertUpdate) {
				string columnName = entityMapping.NestedTableColumnNameDictionaryAtMapper[nestedPropertyName];
				ValueProviderSet valueProviderSet = null;
				ValueProvider valueProvider = 0;
				AutoSetColumnMapping autoSetColumnMapping = null;
				bool isForTimestamp = false;
				bool isForPrimaryKey = false;
				bool isDBNullableValueType = false;

				if (autoSetColumnMappingDictionary.TryGetValue(columnName, out autoSetColumnMapping)) {
					valueProviderSet = autoSetColumnMapping.ValueProviderSet;
					valueProvider = valueProviderSet.ValueProvider;
					isDBNullableValueType = entityMapping.ColumnMappingDictionary[columnName].IsDBNullableValueType;
				}
				else if (entityMapping.PrimaryKeyMapping.NestedPrimaryKeyPropertyNameList.Contains(nestedPropertyName)) {
					valueProviderSet = entityMapping.PrimaryKeyMapping.ValueProviderSet;
					if (fillBackCommandMode == FillBackCommandMode.Insert)
						valueProvider = valueProviderSet.ValueProvider;
					else //update ve logicalDelete'de primary key yeni değer almayacağı için App gibi davranacak
						valueProvider = ValueProvider.App;

					isForPrimaryKey = true;
				}
				else if (entityMapping.TimestampMapping.ExistsForNestedPropertyName(nestedPropertyName)) {
					valueProviderSet = entityMapping.TimestampMapping.ValueProviderSet;
					valueProvider = valueProviderSet.ValueProvider;
					isForTimestamp = true;
				}
				else {// diğer table column'lar için de default App, ancak logical delete'de bunlarla işimiz olmadığından es geç
					if (fillBackCommandMode == FillBackCommandMode.LogicalDelete)
						continue;
					else {
						valueProvider = ValueProvider.App;
						isDBNullableValueType = entityMapping.ColumnMappingDictionary[columnName].IsDBNullableValueType;
					}
				}

				ValueProviderInfo valueProviderInfo = new ValueProviderInfo();
				valueProviderInfo.columnName = columnName;
				valueProviderInfo.isDBNullableValueType = isDBNullableValueType;
				valueProviderInfo.isForPrimaryKey = isForPrimaryKey;
				valueProviderInfo.isForTimestamp = isForTimestamp;
				valueProviderInfo.valueProvider = valueProvider;
				valueProviderInfo.valueProviderSet = valueProviderSet;

				nestedPropertyValueProviderInfoCacheAtMapper.Add(nestedPropertyName, valueProviderInfo);

				if (valueProvider == ValueProvider.App) {
					if (isForPrimaryKey)
						insertUpdateColumnNameValueNameCacheForPrimaryKey.Add(columnName, entityMapping.ConnectionMapping.DBParameterPrefix + columnName);
					else
						insertUpdateColumnNameValueNameCacheForNonPrimaryKey.Add(columnName, entityMapping.ConnectionMapping.DBParameterPrefix + columnName);
				}
				else if (valueProvider == ValueProvider.AppFunctionDelegate) {
					if (isForPrimaryKey)
						insertUpdateColumnNameValueNameCacheForPrimaryKey.Add(columnName, entityMapping.ConnectionMapping.DBParameterPrefix + columnName);
					else
						insertUpdateColumnNameValueNameCacheForNonPrimaryKey.Add(columnName, entityMapping.ConnectionMapping.DBParameterPrefix + columnName);

					returningColumnNamePropertyNameCache.Add(columnName, nestedPropertyName);
				}
				else if (valueProvider == ValueProvider.Sequence) {
					if (isForPrimaryKey)
						insertUpdateColumnNameValueNameCacheForPrimaryKey.Add(columnName, getSequenceNextValueSqlString(valueProviderSet.SequenceName));
					else
						insertUpdateColumnNameValueNameCacheForNonPrimaryKey.Add(columnName, getSequenceNextValueSqlString(valueProviderSet.SequenceName));

					returningColumnNamePropertyNameCache.Add(columnName, nestedPropertyName);
				}
				else if (valueProvider == ValueProvider.DBFunction) {
					if (isForPrimaryKey)
						insertUpdateColumnNameValueNameCacheForPrimaryKey.Add(columnName, DB.schemaDotName(valueProviderSet.SchemaName, valueProviderSet.DBFunctionName) + "()");
					else
						insertUpdateColumnNameValueNameCacheForNonPrimaryKey.Add(columnName, DB.schemaDotName(valueProviderSet.SchemaName, valueProviderSet.DBFunctionName) + "()");

					returningColumnNamePropertyNameCache.Add(columnName, nestedPropertyName);
				}
				else if (valueProvider == ValueProvider.DBTriggerredAutoValue) {
					returningColumnNamePropertyNameCache.Add(columnName, nestedPropertyName);
				}

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
						insertUpdateColumnNameValueNameCacheForNonPrimaryKey.Add(autoSetColumnMapping.ColumnName, entityMapping.ConnectionMapping.DBParameterPrefix + autoSetColumnMapping.ColumnName);
					}
					else if (autoSetColumnMapping.ValueProviderSet.ValueProvider == ValueProvider.DBFunction) {
						insertUpdateColumnNameValueNameCacheForNonPrimaryKey.Add(autoSetColumnMapping.ColumnName, DB.schemaDotName(autoSetColumnMapping.ValueProviderSet.SchemaName, autoSetColumnMapping.ValueProviderSet.DBFunctionName) + "()");
					}

				}
			}

			if (fillBackCommandMode == FillBackCommandMode.Insert) {
				entityMapping.insertColumnNameCacheAtMapper.AddRange(entityMapping.insertColumnNameValueNameForPrimaryKeyCacheAtMapper.Keys);
				entityMapping.insertColumnNameCacheAtMapper.AddRange(entityMapping.insertColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Keys);

				entityMapping.insertValueNameCacheAtMapper.AddRange(entityMapping.insertColumnNameValueNameForPrimaryKeyCacheAtMapper.Values);
				entityMapping.insertValueNameCacheAtMapper.AddRange(entityMapping.insertColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Values);
			}

		}

	}

	internal class InsertCommandGenerator : FillBackCommandGenerator {

		protected InsertCommandGenerator(EntityMapping entityMapping) {
			this.entityMapping = entityMapping;
			fillBackCommandMode = FillBackCommandMode.Insert;
		}

		protected override IList<string> getNestedPropertyNameListToInsertUpdate() {
			return entityMapping.NestedTableColumnNameDictionaryAtMapper.Keys.ToList();
		}

		protected override void generateDbCommand() {
			entityMapping.InsertSqlCacheAtMapper = generateCommandText();
			DbCommand dbCommand = entityMapping.ConnectionMapping.CreateTextDbCommand(entityMapping.InsertSqlCacheAtMapper);
			entityMapping.InsertDbCommandCacheAtMapper = dbCommand;
		}

		protected virtual string CreateCommandText() {
			throw new NotImplementedException();
		}

	}

	internal class UpdateCommandGenerator : FillBackCommandGenerator {

		protected UpdateCommandGenerator(EntityMapping entityMapping, QueryMapping queryMapping) {
			this.entityMapping = entityMapping;
			fillBackCommandMode = FillBackCommandMode.Update;
			QueryMapping = queryMapping;
		}

		protected override IList<string> getNestedPropertyNameListToInsertUpdate() {
			List<string> nestedPropertyNameListForUpdate = QueryMapping.QueryNestedPropertyNameListCacheAtMapper.Intersect(entityMapping.NestedTableColumnNameDictionaryAtMapper.Keys).ToList();
			QueryMapping.NestedPropertyNameListForUpdateCacheAtMapper.Clear();
			QueryMapping.NestedPropertyNameListForUpdateCacheAtMapper.AddRange(nestedPropertyNameListForUpdate);
			return QueryMapping.NestedPropertyNameListForUpdateCacheAtMapper;
		}

		protected virtual string CreateCommandText() {
			throw new NotImplementedException();
		}

		protected override void generateDbCommand() {
			QueryMapping.UpdateSqlCache = generateCommandText();
			DbCommand dbCommand = entityMapping.ConnectionMapping.CreateTextDbCommand(QueryMapping.UpdateSqlCache);
			QueryMapping.UpdateDbCommandCache = dbCommand;
		}

	}

	internal class LogicalDeleteCommandGenerator : FillBackCommandGenerator {

		protected LogicalDeleteCommandGenerator(EntityMapping entityMapping) {
			this.entityMapping = entityMapping;
			fillBackCommandMode = FillBackCommandMode.LogicalDelete;
		}

		protected override IList<string> getNestedPropertyNameListToInsertUpdate() {
			List<string> nestedPropertyNameListToInsertUpdate = new List<string>();
			nestedPropertyNameListToInsertUpdate.AddRange(entityMapping.PrimaryKeyMapping.NestedPrimaryKeyPropertyNameList);
			if (entityMapping.TimestampMapping.Exists())
				nestedPropertyNameListToInsertUpdate.Add(entityMapping.TimestampMapping.NestedPropertyName);
			foreach (AutoSetColumnMapping logicalDeleteAutoSetColumnMapping in entityMapping.LogicalDeleteAutoSetColumnMappingDictionary.Values) {
				ColumnMapping columnMapping = null;
				if (entityMapping.ColumnMappingDictionary.TryGetValue(logicalDeleteAutoSetColumnMapping.ColumnName, out columnMapping))
					nestedPropertyNameListToInsertUpdate.Add(columnMapping.NestedPropertyName);
			}
			entityMapping.NestedPropertyNameListForLogicalDeleteCacheAtMapper.Clear();
			entityMapping.NestedPropertyNameListForLogicalDeleteCacheAtMapper.AddRange(nestedPropertyNameListToInsertUpdate);

			return entityMapping.NestedPropertyNameListForLogicalDeleteCacheAtMapper;
		}

		protected override void generateDbCommand() {
			entityMapping.LogicalDeleteSqlCache = generateCommandText();
			DbCommand	dbCommand = entityMapping.ConnectionMapping.CreateTextDbCommand(entityMapping.LogicalDeleteSqlCache);
			entityMapping.LogicalDeleteDbCommandCache = dbCommand;
		}

	}


}
