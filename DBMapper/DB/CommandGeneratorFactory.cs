using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Reflection;

namespace DBMapper {

	internal abstract class CommandGeneratorFactory {

		internal abstract InsertCommandGenerator CreateInsertCommandGenerator(EntityMapping entityMapping);

		internal abstract UpdateCommandGenerator CreateUpdateCommandGenerator(EntityMapping entityMapping, QueryMapping queryMapping);

		internal abstract LogicalDeleteCommandGenerator CreateLogicalDeleteCommandGenerator(EntityMapping entityMapping);

	}

	internal class OracleCommandGeneratorFactory : CommandGeneratorFactory {

		internal override InsertCommandGenerator CreateInsertCommandGenerator(EntityMapping entityMapping) {
			return new OracleInsertCommandGenerator(entityMapping);
		}

		internal override UpdateCommandGenerator CreateUpdateCommandGenerator(EntityMapping entityMapping, QueryMapping queryMapping) {
			return new OracleUpdateCommandGenerator(entityMapping, queryMapping);
		}

		internal override LogicalDeleteCommandGenerator CreateLogicalDeleteCommandGenerator(EntityMapping entityMapping) {
			return new OracleLogicalDeleteCommandGenerator(entityMapping);
		}

	}

	internal class SqlServerCommandGeneratorFactory : CommandGeneratorFactory {

		internal override InsertCommandGenerator CreateInsertCommandGenerator(EntityMapping entityMapping) {
			return new SqlServerInsertCommandGenerator(entityMapping);
		}

		internal override UpdateCommandGenerator CreateUpdateCommandGenerator(EntityMapping entityMapping, QueryMapping queryMapping) {
			return new SqlServerUpdateCommandGenerator(entityMapping, queryMapping);
		}

		internal override LogicalDeleteCommandGenerator CreateLogicalDeleteCommandGenerator(EntityMapping entityMapping) {
			return new SqlServerLogicalDeleteCommandGenerator(entityMapping);
		}

	}

	internal class OracleInsertCommandGenerator : InsertCommandGenerator {

		internal OracleInsertCommandGenerator(EntityMapping entityMapping)
			: base(entityMapping) {
		}

		protected override string getSequenceNextValueSqlString(string sequenceName) {
			return entityMapping.SchemaName + "." + sequenceName + ".NEXTVAL";
		}

		protected override string generateCommandText() {

			List<string> insertColumnNameList = new List<string>();
			insertColumnNameList.AddRange(entityMapping.insertColumnNameValueNameForPrimaryKeyCacheAtMapper.Keys);
			insertColumnNameList.AddRange(entityMapping.insertColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Keys);

			List<string> insertValueNameList = new List<string>();
			insertValueNameList.AddRange(entityMapping.insertColumnNameValueNameForPrimaryKeyCacheAtMapper.Values);
			insertValueNameList.AddRange(entityMapping.insertColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Values);

			//oracle için:
			//Sequence, DBFunction veya	DBTriggerredAutoValue ile dolan mapli kolonlar varsa
			//(mesela LOCK_TS, DENEMENO, INS_TARIH), o zaman aşağıdaki gibi returning ifadeleri olacak:
			//INSERT INTO LEAS.DENEME (RAKAM, YAZI, INS_TARIH) 
			//VALUES (:RAKAM, :YAZI, LEAS.GET_DATE()) 
			//RETURNING LOCK_TS, DENEMENO, INS_TARIH INTO :LOCK_TS, :DENEMENO, :INS_TARIH;

			StringBuilder sqlStringBuilder = new StringBuilder();

			sqlStringBuilder.Append("INSERT INTO ");
			sqlStringBuilder.Append(entityMapping.SchemaName);
			sqlStringBuilder.Append(".");
			sqlStringBuilder.Append(entityMapping.TableName);
			sqlStringBuilder.Append("(");
			sqlStringBuilder.Append(string.Join(",", insertColumnNameList.ToArray()));
			sqlStringBuilder.Append(")");

			sqlStringBuilder.Append(" VALUES (");
			sqlStringBuilder.Append(string.Join(",", insertValueNameList.ToArray()));

			sqlStringBuilder.Append(")");

			if (entityMapping.insertReturningColumnNamePropertyNameCacheAtMapper.Count != 0) {
				sqlStringBuilder.Append(" RETURNING ");
				sqlStringBuilder.Append(string.Join(", ", entityMapping.insertReturningColumnNamePropertyNameCacheAtMapper.Keys.ToArray()));
				sqlStringBuilder.Append(" INTO ");
				sqlStringBuilder.Append(entityMapping.ConnectionMapping.DBParameterPrefix);
				sqlStringBuilder.Append(string.Join(", " + entityMapping.ConnectionMapping.DBParameterPrefix, entityMapping.insertReturningColumnNamePropertyNameCacheAtMapper.Keys.ToArray()));
			}

			return sqlStringBuilder.ToString();
		}

	}

	internal class OracleUpdateCommandGenerator : UpdateCommandGenerator {

		internal OracleUpdateCommandGenerator(EntityMapping entityMapping, QueryMapping queryMapping)
			: base(entityMapping, queryMapping) {
		}

		protected override string generateCommandText() {
			StringBuilder sqlStringBuilder = new StringBuilder();

			//oracle için:
			//Sequence, DBFunction veya	DBTriggerredAutoValue ile dolan mapli kolonlar varsa
			//(mesela LOCK_TS, DENEMENO, INS_TARIH), o zaman aşağıdaki gibi returning ifadeleri olacak:
			//UPDATE LEAS.DENEME SET RAKAM = :RAKAM, YAZI=:YAZI, UPD_TARIH=LEAS.GETDATE()
			//WHERE DENEMENO = :DENEMENO AND LOCK_TS = :OLD_LOCK_TS 
			//RETURNING LOCK_TS, UPD_TARIH INTO :LOCK_TS, :UPD_TARIH;

			sqlStringBuilder.Append("UPDATE ");
			sqlStringBuilder.Append(entityMapping.SchemaName);
			sqlStringBuilder.Append(".");
			sqlStringBuilder.Append(entityMapping.TableName);
			sqlStringBuilder.Append(" SET ");

			bool isFirstSetColumn = true;
			foreach (string columnName in QueryMapping.updateColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Keys) {
				if (!isFirstSetColumn)
					sqlStringBuilder.Append(", ");

				sqlStringBuilder.Append(columnName);
				sqlStringBuilder.Append(" = ");
				sqlStringBuilder.Append(QueryMapping.updateColumnNameValueNameForNonPrimaryKeyCacheAtMapper[columnName]);

				isFirstSetColumn = false;
			}

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

			if (QueryMapping.updateColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Keys.Contains(entityMapping.TimestampMapping.ColumnName)) {
				sqlStringBuilder.Append(" AND ");
				sqlStringBuilder.Append(entityMapping.TimestampMapping.ColumnName);
				sqlStringBuilder.Append(" = ");
				sqlStringBuilder.Append(entityMapping.ConnectionMapping.DBParameterPrefix);
				sqlStringBuilder.Append("OLD_");
				sqlStringBuilder.Append(entityMapping.TimestampMapping.ColumnName);
			}

			if (QueryMapping.updateReturningColumnNamePropertyNameCacheAtMapper.Count != 0) {
				sqlStringBuilder.Append(" RETURNING ");
				sqlStringBuilder.Append(string.Join(", ", QueryMapping.updateReturningColumnNamePropertyNameCacheAtMapper.Keys.ToArray()));
				sqlStringBuilder.Append(" INTO ");
				sqlStringBuilder.Append(entityMapping.ConnectionMapping.DBParameterPrefix);
				sqlStringBuilder.Append(string.Join(", " + entityMapping.ConnectionMapping.DBParameterPrefix, QueryMapping.updateReturningColumnNamePropertyNameCacheAtMapper.Keys.ToArray()));
			}

			return sqlStringBuilder.ToString();
		}

	}

	internal class OracleLogicalDeleteCommandGenerator : LogicalDeleteCommandGenerator {

		internal OracleLogicalDeleteCommandGenerator(EntityMapping entityMapping)
			: base(entityMapping) {
		}

		protected override string generateCommandText() {
			StringBuilder sqlStringBuilder = new StringBuilder();

			//oracle için:
			//UPDATE MYS.TEST SET DEL_DATE = :DEL_DATE 
			//WHERE TEST_ID = :TEST_ID AND LOCK_TIMESTAMP = :OLD_LOCK_TIMESTAMP
			//RETURNING LOCK_TIMESTAMP INTO :LOCK_TIMESTAMP

			sqlStringBuilder.Append("UPDATE ");
			sqlStringBuilder.Append(entityMapping.SchemaName);
			sqlStringBuilder.Append(".");
			sqlStringBuilder.Append(entityMapping.TableName);
			sqlStringBuilder.Append(" SET ");

			bool isFirstSetColumn = true;
			foreach (string columnName in entityMapping.logicalDeleteColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Keys) {
				if (!isFirstSetColumn)
					sqlStringBuilder.Append(", ");

				sqlStringBuilder.Append(columnName);
				sqlStringBuilder.Append(" = ");
				sqlStringBuilder.Append(entityMapping.logicalDeleteColumnNameValueNameForNonPrimaryKeyCacheAtMapper[columnName]);

				isFirstSetColumn = false;
			}

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

			if (entityMapping.logicalDeleteColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Keys.Contains(entityMapping.TimestampMapping.ColumnName)) {
				sqlStringBuilder.Append(" AND ");
				sqlStringBuilder.Append(entityMapping.TimestampMapping.ColumnName);
				sqlStringBuilder.Append(" = ");
				sqlStringBuilder.Append(entityMapping.ConnectionMapping.DBParameterPrefix);
				sqlStringBuilder.Append("OLD_");
				sqlStringBuilder.Append(entityMapping.TimestampMapping.ColumnName);
			}

			if (entityMapping.logicalDeleteReturningColumnNamePropertyNameCacheAtMapper.Count != 0) {
				sqlStringBuilder.Append(" RETURNING ");
				sqlStringBuilder.Append(string.Join(", ", entityMapping.logicalDeleteReturningColumnNamePropertyNameCacheAtMapper.Keys.ToArray()));
				sqlStringBuilder.Append(" INTO ");
				sqlStringBuilder.Append(entityMapping.ConnectionMapping.DBParameterPrefix);
				sqlStringBuilder.Append(string.Join(", " + entityMapping.ConnectionMapping.DBParameterPrefix, entityMapping.logicalDeleteReturningColumnNamePropertyNameCacheAtMapper.Keys.ToArray()));
			}

			return sqlStringBuilder.ToString();
		}

	}

	internal class SqlServerInsertCommandGenerator : InsertCommandGenerator {

		internal SqlServerInsertCommandGenerator(EntityMapping entityMapping)
			: base(entityMapping) {
		}

		protected override string getSequenceNextValueSqlString(string sequenceName) {
			return "NEXT VALUE FOR " + entityMapping.SchemaName + "." + sequenceName;
		}

		protected override string generateCommandText() {

			List<string> insertColumnNameList = new List<string>();
			insertColumnNameList.AddRange(entityMapping.insertColumnNameValueNameForPrimaryKeyCacheAtMapper.Keys);
			insertColumnNameList.AddRange(entityMapping.insertColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Keys);

			List<string> insertValueNameList = new List<string>();
			insertValueNameList.AddRange(entityMapping.insertColumnNameValueNameForPrimaryKeyCacheAtMapper.Values);
			insertValueNameList.AddRange(entityMapping.insertColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Values);

			//oracle için:
			//Sequence, DBFunction veya	DBTriggerredAutoValue ile dolan mapli kolonlar varsa
			//(mesela LOCK_TS, DENEMENO, INS_TARIH), o zaman aşağıdaki gibi returning ifadeleri olacak:
			//INSERT INTO LEAS.DENEME (RAKAM, YAZI, INS_TARIH) 
			//VALUES (:RAKAM, :YAZI, LEAS.GET_DATE()) 
			//RETURNING LOCK_TS, DENEMENO, INS_TARIH INTO :LOCK_TS, :DENEMENO, :INS_TARIH;

			//sql server için:
			//Sequence, DBFunction veya	DBTriggerredAutoValue ile dolan mapli kolonlar varsa
			//(mesela LOCK_TS, DENEMENO, INS_TARIH), o zaman aşağıdaki gibi returning ifadeleri olacak:
			//(@RETURNING_TABLE içindeki kolon tipleri INFORMATION_SCHEMA'dan çekilerek cachelenecek)
			//DECLARE @RETURNING_TABLE TABLE (LOCK_TS BINARY(8), DENEMENO INTEGER, INS_TARIH DATE);
			//INSERT INTO DBO.DENEME (YAZI, INS_TARIH) 
			//OUTPUT INSERTED.LOCK_TS, INSERTED.DENEMENO, INSERTED.INS_TARIH INTO @RETURNING_TABLE
			//VALUES (@YAZI, GETDATE());
			//SELECT @LOCK_TS=LOCK_TS, @DENEMENO=DENEMENO, @INS_TARIH=INS_TARIH FROM @RETURNING_TABLE;
			//
			//INFORMATION_SCHEMA selecti:
			//select COLUMN_NAME, 
			//case when DATA_TYPE = 'timestamp' then 'byte(8)'
			//else DATA_TYPE + case when CHARACTER_MAXIMUM_LENGTH is not null then '(' + cast(CHARACTER_MAXIMUM_LENGTH as varchar) + ')' else '' end 
			//end as declare_type
			//from INFORMATION_SCHEMA.COLUMNS 
			//where TABLE_SCHEMA = 'dbo' 
			//and TABLE_NAME = 'DENEME'

			StringBuilder sqlStringBuilder = new StringBuilder();
			if (entityMapping.insertReturningColumnNamePropertyNameCacheAtMapper.Count != 0) {
				sqlStringBuilder.Append("DECLARE @RETURNING_TABLE TABLE (");
				bool isFirstColumn = true;
				foreach (string columnName in entityMapping.insertReturningColumnNamePropertyNameCacheAtMapper.Keys) {
					if (!isFirstColumn)
						sqlStringBuilder.Append(",");

					sqlStringBuilder.Append(columnName + " ");
					sqlStringBuilder.Append(entityMapping.sqlServerDBColumnTypeDictionaryAtMapper[columnName]);
					isFirstColumn = false;
				}
				sqlStringBuilder.Append("); ");
			}

			sqlStringBuilder.Append("INSERT INTO ");
			sqlStringBuilder.Append(entityMapping.SchemaName);
			sqlStringBuilder.Append(".");
			sqlStringBuilder.Append(entityMapping.TableName);
			sqlStringBuilder.Append("(");
			sqlStringBuilder.Append(string.Join(",", insertColumnNameList.ToArray()));
			sqlStringBuilder.Append(")");

			if (entityMapping.insertReturningColumnNamePropertyNameCacheAtMapper.Count != 0) {
				sqlStringBuilder.Append(" OUTPUT INSERTED.");
				sqlStringBuilder.Append(string.Join(", INSERTED.", entityMapping.insertReturningColumnNamePropertyNameCacheAtMapper.Keys.ToArray()));
				sqlStringBuilder.Append(" INTO @RETURNING_TABLE");
			}

			sqlStringBuilder.Append(" VALUES (");
			sqlStringBuilder.Append(string.Join(",", insertValueNameList.ToArray()));

			sqlStringBuilder.Append(")");

			if (entityMapping.insertReturningColumnNamePropertyNameCacheAtMapper.Count != 0) {
				sqlStringBuilder.Append("; SELECT ");
				bool isFirstColumn = true;
				foreach (string columnName in entityMapping.insertReturningColumnNamePropertyNameCacheAtMapper.Keys) {
					if (!isFirstColumn)
						sqlStringBuilder.Append(",");

					sqlStringBuilder.Append("@" + columnName + " = " + columnName);
					isFirstColumn = false;
				}
				sqlStringBuilder.Append(" FROM @RETURNING_TABLE;");
			}

			return sqlStringBuilder.ToString();
		}

	}

	internal class SqlServerUpdateCommandGenerator : UpdateCommandGenerator {

		internal SqlServerUpdateCommandGenerator(EntityMapping entityMapping, QueryMapping queryMapping)
	    : base(entityMapping, queryMapping) {
	  }

		protected override string generateCommandText() {
	    //bool timestampColumnExists = entityMapping.TimestampMapping.Exists();
	    StringBuilder sqlStringBuilder = new StringBuilder();

	    //sql server için:
	    //Sequence, DBFunction veya	DBTriggerredAutoValue ile dolan mapli kolonlar varsa
	    //(mesela LOCK_TS, DENEMENO, INS_TARIH), o zaman aşağıdaki gibi returning ifadeleri olacak:
	    //(@RETURNING_TABLE içindeki kolon tipleri INFORMATION_SCHEMA'dan çekilerek cachelenecek)
	    //DECLARE @RETURNING_TABLE TABLE (LOCK_TS BINARY(8), UPD_TARIH DATETIME);
	    //UPDATE DBO.DENEME SET RAKAM = @RAKAM, YAZI=@YAZI, UPD_TARIH=GETDATE()
	    //OUTPUT INSERTED.LOCK_TS, INSERTED.UPD_TARIH INTO @RETURNING_TABLE 
	    //WHERE DENEMENO = @DENEMENO AND LOCK_TS = @OLD_LOCK_TS;
	    //SELECT @LOCK_TS = LOCK_TS, @UPD_TARIH = UPD_TARIH FROM @RETURNING_TABLE;
	    //
	    //INFORMATION_SCHEMA selecti:
	    //select COLUMN_NAME, 
	    //case when DATA_TYPE = 'timestamp' then 'byte(8)'
	    //else DATA_TYPE + case when CHARACTER_MAXIMUM_LENGTH is not null then '(' + cast(CHARACTER_MAXIMUM_LENGTH as varchar) + ')' else '' end 
	    //end as declare_type
	    //from INFORMATION_SCHEMA.COLUMNS 
	    //where TABLE_SCHEMA = 'dbo' 
	    //and TABLE_NAME = 'DENEME'

			if (QueryMapping.updateReturningColumnNamePropertyNameCacheAtMapper.Count != 0) {
	      sqlStringBuilder.Append("DECLARE @RETURNING_TABLE TABLE (");
	      bool isFirstColumn = true;
				foreach (string columnName in QueryMapping.updateReturningColumnNamePropertyNameCacheAtMapper.Keys) {
	        if (!isFirstColumn)
	          sqlStringBuilder.Append(",");

	        sqlStringBuilder.Append(columnName + " ");
	        sqlStringBuilder.Append(entityMapping.sqlServerDBColumnTypeDictionaryAtMapper[columnName]);
	        isFirstColumn = false;
	      }
	      sqlStringBuilder.Append("); ");
	    }

	    sqlStringBuilder.Append("UPDATE ");
	    sqlStringBuilder.Append(entityMapping.SchemaName);
	    sqlStringBuilder.Append(".");
	    sqlStringBuilder.Append(entityMapping.TableName);
	    sqlStringBuilder.Append(" SET ");

	    bool isFirstSetColumn = true;
			foreach (string columnName in QueryMapping.updateColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Keys) {
	      if (!isFirstSetColumn)
	        sqlStringBuilder.Append(", ");

	      sqlStringBuilder.Append(columnName);
	      sqlStringBuilder.Append(" = ");
				sqlStringBuilder.Append(QueryMapping.updateColumnNameValueNameForNonPrimaryKeyCacheAtMapper[columnName]);

	      isFirstSetColumn = false;
	    }

			if (QueryMapping.updateReturningColumnNamePropertyNameCacheAtMapper.Count != 0) {
	      sqlStringBuilder.Append(" OUTPUT INSERTED.");
				sqlStringBuilder.Append(string.Join(", INSERTED.", QueryMapping.updateReturningColumnNamePropertyNameCacheAtMapper.Keys.ToArray()));
	      sqlStringBuilder.Append(" INTO @RETURNING_TABLE");
	    }

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

			if (QueryMapping.updateColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Keys.Contains(entityMapping.TimestampMapping.ColumnName)) {
	      sqlStringBuilder.Append(" AND ");
	      sqlStringBuilder.Append(entityMapping.TimestampMapping.ColumnName);
	      sqlStringBuilder.Append(" = ");
	      sqlStringBuilder.Append(entityMapping.ConnectionMapping.DBParameterPrefix);
	      sqlStringBuilder.Append("OLD_");
	      sqlStringBuilder.Append(entityMapping.TimestampMapping.ColumnName);
	    }

			if (QueryMapping.updateReturningColumnNamePropertyNameCacheAtMapper.Count != 0) {
	      sqlStringBuilder.Append("; SELECT ");
	      bool isFirstColumn = true;
				foreach (string columnName in QueryMapping.updateReturningColumnNamePropertyNameCacheAtMapper.Keys) {
	        if (!isFirstColumn)
	          sqlStringBuilder.Append(",");

	        sqlStringBuilder.Append("@" + columnName + " = " + columnName);
	        isFirstColumn = false;
	      }
	      sqlStringBuilder.Append(" FROM @RETURNING_TABLE;");
	    }

	    return sqlStringBuilder.ToString();
	  }

	}

	internal class SqlServerLogicalDeleteCommandGenerator : LogicalDeleteCommandGenerator {

		internal SqlServerLogicalDeleteCommandGenerator(EntityMapping entityMapping)
	    : base(entityMapping) {
	  }

		protected override string generateCommandText() {
	    //bool timestampColumnExists = entityMapping.TimestampMapping.Exists();
	    StringBuilder sqlStringBuilder = new StringBuilder();

	    //sql server için:
	    //Sequence, DBFunction veya	DBTriggerredAutoValue ile dolan mapli kolonlar varsa
	    //(mesela LOCK_TS, DENEMENO, INS_TARIH), o zaman aşağıdaki gibi returning ifadeleri olacak:
	    //(@RETURNING_TABLE içindeki kolon tipleri INFORMATION_SCHEMA'dan çekilerek cachelenecek)
	    //DECLARE @RETURNING_TABLE TABLE (LOCK_TIMESTAMP BINARY(8), UPD_DATE DATETIME);
	    //UPDATE DBO.TEST SET QUANTITY = @QUANTITY, TEXT=@TEXT, UPD_DATE=GETDATE()
	    //OUTPUT INSERTED.LOCK_TS, INSERTED.UPD_TARIH INTO @RETURNING_TABLE 
	    //WHERE TEST_ID = @TEST_ID AND LOCK_TIMESTAMP = @OLD_LOCK_TIMESTAMP;
	    //SELECT @LOCK_TIMESTAMP = LOCK_TIMESTAMP, @UPD_DATE = UPD_DATE FROM @RETURNING_TABLE;

	    if (entityMapping.logicalDeleteReturningColumnNamePropertyNameCacheAtMapper.Count != 0) {
	      sqlStringBuilder.Append("DECLARE @RETURNING_TABLE TABLE (");
	      bool isFirstColumn = true;
	      foreach (string columnName in entityMapping.logicalDeleteReturningColumnNamePropertyNameCacheAtMapper.Keys) {
	        if (!isFirstColumn)
	          sqlStringBuilder.Append(",");

	        sqlStringBuilder.Append(columnName + " ");
	        sqlStringBuilder.Append(entityMapping.sqlServerDBColumnTypeDictionaryAtMapper[columnName]);
	        isFirstColumn = false;
	      }
	      sqlStringBuilder.Append("); ");
	    }

	    sqlStringBuilder.Append("UPDATE ");
	    sqlStringBuilder.Append(entityMapping.SchemaName);
	    sqlStringBuilder.Append(".");
	    sqlStringBuilder.Append(entityMapping.TableName);
	    sqlStringBuilder.Append(" SET ");

	    bool isFirstSetColumn = true;
			foreach (string columnName in entityMapping.logicalDeleteColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Keys) {
	      if (!isFirstSetColumn)
	        sqlStringBuilder.Append(", ");

	      sqlStringBuilder.Append(columnName);
	      sqlStringBuilder.Append(" = ");
				sqlStringBuilder.Append(entityMapping.logicalDeleteColumnNameValueNameForNonPrimaryKeyCacheAtMapper[columnName]);

	      isFirstSetColumn = false;
	    }

			if (entityMapping.logicalDeleteReturningColumnNamePropertyNameCacheAtMapper.Count != 0) {
	      sqlStringBuilder.Append(" OUTPUT INSERTED.");
				sqlStringBuilder.Append(string.Join(", INSERTED.", entityMapping.logicalDeleteReturningColumnNamePropertyNameCacheAtMapper.Keys.ToArray()));
	      sqlStringBuilder.Append(" INTO @RETURNING_TABLE");
	    }

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

			if (entityMapping.logicalDeleteColumnNameValueNameForNonPrimaryKeyCacheAtMapper.Keys.Contains(entityMapping.TimestampMapping.ColumnName)) {
	      sqlStringBuilder.Append(" AND ");
	      sqlStringBuilder.Append(entityMapping.TimestampMapping.ColumnName);
	      sqlStringBuilder.Append(" = ");
	      sqlStringBuilder.Append(entityMapping.ConnectionMapping.DBParameterPrefix);
	      sqlStringBuilder.Append("OLD_");
	      sqlStringBuilder.Append(entityMapping.TimestampMapping.ColumnName);
	    }

			if (entityMapping.logicalDeleteReturningColumnNamePropertyNameCacheAtMapper.Count != 0) {
	      sqlStringBuilder.Append("; SELECT ");
	      bool isFirstColumn = true;
				foreach (string columnName in entityMapping.logicalDeleteReturningColumnNamePropertyNameCacheAtMapper.Keys) {
	        if (!isFirstColumn)
	          sqlStringBuilder.Append(",");

	        sqlStringBuilder.Append("@" + columnName + " = " + columnName);
	        isFirstColumn = false;
	      }
	      sqlStringBuilder.Append(" FROM @RETURNING_TABLE;");
	    }

	    return sqlStringBuilder.ToString();
	  }

	}

}
