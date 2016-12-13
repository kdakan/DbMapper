using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace DBMapper {

	#region Fluent interface for connection mapping

	public class Connections {

		internal Connections() {
		}

		public static Connection Add(Enum connectionName, DBVendor dbVendor, string usedDBParameterPrefix) {
			lock (Map.lockerForMap) {
				ConnectionMapper.NewConnection(connectionName, dbVendor, usedDBParameterPrefix);
				return new Connection();
			}
		}
	}

	public class Connection {

		internal Connection() {
		}

		public Connection Add(Enum connectionName, DBVendor dbVendor, string usedDBParameterPrefix) {
			lock (Map.lockerForMap) {
				ConnectionMapper.NewConnection(connectionName, dbVendor, usedDBParameterPrefix);
				return new Connection();
			}
		}

	}

	#endregion Fluent interface for connection mapping

	#region Fluent interface for table entity mapping

	public class TableEntity<T> {

		internal TableEntity() {
		}

		public static TableEntityMapper<T> Table(object tableName, Enum schemaName, Enum connectionName) {
			lock (Map.lockerForMap) {
				return Table(tableName, schemaName, connectionName, 0, 0);
			}
		}

		public static TableEntityMapper<T> Table(object tableName, Enum schemaName, Enum connectionName, PrimaryKeyValueProvider defaultPrimaryKeyValueProvider, TimestampValueProvider defaultTimestampValueProvider) {
			lock (Map.lockerForMap) {
				EntityMapper entityMapper = EntityMapper.NewTableEntity<T>(tableName, schemaName, connectionName, defaultPrimaryKeyValueProvider, defaultTimestampValueProvider);
				return new TableEntityMapper<T>(entityMapper);
			}
		}

	}

	public class TableEntityMapper<T> {

		protected EntityMapper entityMapper;

		internal TableEntityMapper(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		protected TableEntityMapper()
			: base() {
		}

		//public DerivedTableEntities<T> DerivedTableEntities { get { return new DerivedTableEntities<T>(); } private set { } }

		public PrimaryKeyColumns<T> PrimaryKeys { get { return new PrimaryKeyColumns<T>(entityMapper); } private set { } }

		public TableColumns<T> TableColumns { get { return new TableColumns<T>(entityMapper); } private set { } }

		public TableEntityViewColumns<T> ViewColumns { get { return new TableEntityViewColumns<T>(entityMapper); } private set { } }

		public AutoSetColumns<T> AutoSetColumns { get { return new AutoSetColumns<T>(entityMapper); } private set { } }

		public TableEntityTriggerActions<T> TriggerActions { get { return new TableEntityTriggerActions<T>(entityMapper); } private set { } }

		public TableEntityValidators<T> Validators { get { return new TableEntityValidators<T>(entityMapper); } private set { } }

		public TableEntityQueries<T> Queries { get { return new TableEntityQueries<T>(entityMapper); } private set { } }

		public TableEntityMapper<T> Timestamp(Enum columnName, Expression<Func<T, object>> memberExpression) {
			entityMapper.NewTimestamp<T>(columnName, memberExpression, null, null, null, TimestampValueProvider.DBTriggerredAutoValue);
			return this;
		}

		public TableEntityMapper<T> Timestamp(Enum columnName, Expression<Func<T, object>> memberExpression, Enum schemaName, Enum dbFunctionName) {
			entityMapper.NewTimestamp<T>(columnName, memberExpression, schemaName, dbFunctionName, null, TimestampValueProvider.DBFunction);
			return this;
		}

		public TableEntityMapper<T> Timestamp(Enum columnName, Expression<Func<T, object>> memberExpression, Func<object> functionDelegate) {
			entityMapper.NewTimestamp<T>(columnName, memberExpression, null, null, functionDelegate, TimestampValueProvider.AppFunctionDelegate);
			return this;
		}

		public TableEntityMapper<T> Timestamp(Enum columnName, Expression<Func<T, object>> memberExpression, Enum schemaName, Enum dbFunctionName, Func<object> functionDelegate) {
			entityMapper.NewTimestamp<T>(columnName, memberExpression, schemaName, dbFunctionName, functionDelegate, entityMapper.DefaultTimestampValueProvider);
			return this;
		}

	}

	public class PrimaryKeyColumns<T> {

		EntityMapper entityMapper;

		internal PrimaryKeyColumns(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		public PrimaryKeyColumn<T> Add(Enum columnName, Expression<Func<T, object>> memberExpression, bool isValueAppProvided) {
			if (isValueAppProvided)
				entityMapper.NewPrimaryKey<T>(columnName, memberExpression, null, null, null, null, PrimaryKeyValueProvider.App);
			else
				entityMapper.NewPrimaryKey<T>(columnName, memberExpression, null, null, null, null, PrimaryKeyValueProvider.DBTriggerredAutoValue);

			return new PrimaryKeyColumn<T>(entityMapper);
		}

		public PrimaryKeyColumn<T> Add(Enum columnName, Expression<Func<T, object>> memberExpression, Enum sequenceName) {
			entityMapper.NewPrimaryKey<T>(columnName, memberExpression, sequenceName, null, null, null, PrimaryKeyValueProvider.Sequence);
			return new PrimaryKeyColumn<T>(entityMapper);
		}

		public PrimaryKeyColumn<T> Add(Enum columnName, Expression<Func<T, object>> memberExpression, Enum schemaName, Enum dbFunctionName) {
			entityMapper.NewPrimaryKey<T>(columnName, memberExpression, null, schemaName, dbFunctionName, null, PrimaryKeyValueProvider.DBFunction);
			return new PrimaryKeyColumn<T>(entityMapper);
		}

		public PrimaryKeyColumn<T> Add(Enum columnName, Expression<Func<T, object>> memberExpression, Func<object> functionDelegate) {
			entityMapper.NewPrimaryKey<T>(columnName, memberExpression, null, null, null, functionDelegate, PrimaryKeyValueProvider.AppFunctionDelegate);
			return new PrimaryKeyColumn<T>(entityMapper);
		}

		public PrimaryKeyColumn<T> Add(Enum columnName, Expression<Func<T, object>> memberExpression, Enum sequenceName, Enum schemaName, Enum dbFunctionName, Func<object> functionDelegate) {
			entityMapper.NewPrimaryKey<T>(columnName, memberExpression, sequenceName, schemaName, dbFunctionName, functionDelegate, entityMapper.DefaultPrimaryKeyValueProvider);
			return new PrimaryKeyColumn<T>(entityMapper);
		}

	}

	public class PrimaryKeyColumn<T> : TableEntityMapper<T> {

		internal PrimaryKeyColumn(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		internal PrimaryKeyColumn()
			: base() {
		}

		public PrimaryKeyColumn<T> Add(Enum columnName, Expression<Func<T, object>> memberExpression, bool isValueAppProvided) {
			if (isValueAppProvided)
				entityMapper.NewPrimaryKey<T>(columnName, memberExpression, null, null, null, null, PrimaryKeyValueProvider.App);
			else
				entityMapper.NewPrimaryKey<T>(columnName, memberExpression, null, null, null, null, PrimaryKeyValueProvider.DBTriggerredAutoValue);

			return this;
		}

		public PrimaryKeyColumn<T> Add(Enum columnName, Expression<Func<T, object>> memberExpression, Enum sequenceName) {
			entityMapper.NewPrimaryKey<T>(columnName, memberExpression, sequenceName, null, null, null, PrimaryKeyValueProvider.Sequence);
			return this;
		}

		public PrimaryKeyColumn<T> Add(Enum columnName, Expression<Func<T, object>> memberExpression, Enum schemaName, Enum dbFunctionName) {
			entityMapper.NewPrimaryKey<T>(columnName, memberExpression, null, schemaName, dbFunctionName, null, PrimaryKeyValueProvider.DBFunction);
			return this;
		}

		public PrimaryKeyColumn<T> Add(Enum columnName, Expression<Func<T, object>> memberExpression, Func<object> functionDelegate) {
			entityMapper.NewPrimaryKey<T>(columnName, memberExpression, null, null, null, functionDelegate, PrimaryKeyValueProvider.AppFunctionDelegate);
			return this;
		}

		public PrimaryKeyColumn<T> Add(Enum columnName, Expression<Func<T, object>> memberExpression, Enum sequenceName, Enum schemaName, Enum dbFunctionName, Func<object> functionDelegate) {
			entityMapper.NewPrimaryKey<T>(columnName, memberExpression, sequenceName, schemaName, dbFunctionName, functionDelegate, entityMapper.DefaultPrimaryKeyValueProvider);
			return this;
		}

	}

	public class TableColumns<T> {

		EntityMapper entityMapper;

		internal TableColumns(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		public TableColumn<T> Add(Expression<Func<T, object>> memberExpression, Enum columnName) {
			entityMapper.NewTableColumn<T>(memberExpression, columnName);
			return new TableColumn<T>(entityMapper);
		}

		public TableColumn<T> Add(Expression<Func<T, object>> memberExpression, Enum columnName, bool isDBNullableValueType) {
			entityMapper.NewTableColumn<T>(memberExpression, columnName, isDBNullableValueType);
			return new TableColumn<T>(entityMapper);
		}

	}

	public class TableColumn<T> : TableEntityMapper<T> {

		internal TableColumn(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		protected TableColumn()
			: base() {
		}

		public TableColumn<T> Add(Expression<Func<T, object>> memberExpression, Enum columnName) {
			entityMapper.NewTableColumn<T>(memberExpression, columnName);
			return this;
		}

		public TableColumn<T> Add(Expression<Func<T, object>> memberExpression, Enum columnName, bool isDBNullableValueType) {
			entityMapper.NewTableColumn<T>(memberExpression, columnName, isDBNullableValueType);
			return this;
		}

	}

	public class AutoSetColumns<T> {

		EntityMapper entityMapper;

		internal AutoSetColumns(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		public AutoSetColumn<T> Add(Before before, Enum columnName) {
			entityMapper.NewAutoSetColumn<T>(before, columnName, null, null, null, AutoSetValueProvider.DBTriggerredAutoValue);
			return new AutoSetColumn<T>(entityMapper);
		}

		public AutoSetColumn<T> Add(Before before, Enum columnName, Enum schemaName, Enum dbFunctionName) {
			entityMapper.NewAutoSetColumn<T>(before, columnName, schemaName, dbFunctionName, null, AutoSetValueProvider.DBFunction);
			return new AutoSetColumn<T>(entityMapper);
		}

		public AutoSetColumn<T> Add(Before before, Enum columnName, Func<object> functionDelegate) {
			entityMapper.NewAutoSetColumn<T>(before, columnName, null, null, functionDelegate, AutoSetValueProvider.AppFunctionDelegate);
			return new AutoSetColumn<T>(entityMapper);
		}

		public AutoSetColumn<T> Add(Before before, Enum columnName, Enum schemaName, Enum dbFunctionName, Func<object> functionDelegate, AutoSetValueProvider autoSetValueProvider) {
			entityMapper.NewAutoSetColumn<T>(before, columnName, schemaName, dbFunctionName, functionDelegate, autoSetValueProvider);
			return new AutoSetColumn<T>(entityMapper);
		}

	}

	public class AutoSetColumn<T> : TableEntityMapper<T> {

		internal AutoSetColumn(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		internal AutoSetColumn()
			: base() {
		}

		public AutoSetColumn<T> Add(Before before, Enum columnName) {
			entityMapper.NewAutoSetColumn<T>(before, columnName, null, null, null, AutoSetValueProvider.DBTriggerredAutoValue);
			return this;
		}

		public AutoSetColumn<T> Add(Before before, Enum columnName, Enum schemaName, Enum dbFunctionName) {
			entityMapper.NewAutoSetColumn<T>(before, columnName, schemaName, dbFunctionName, null, AutoSetValueProvider.DBFunction);
			return this;
		}

		public AutoSetColumn<T> Add(Before before, Enum columnName, Func<object> functionDelegate) {
			entityMapper.NewAutoSetColumn<T>(before, columnName, null, null, functionDelegate, AutoSetValueProvider.AppFunctionDelegate);
			return this;
		}

		public AutoSetColumn<T> Add(Before before, Enum columnName, Enum schemaName, Enum dbFunctionName, Func<object> functionDelegate, AutoSetValueProvider autoSetValueProvider) {
			entityMapper.NewAutoSetColumn<T>(before, columnName, schemaName, dbFunctionName, functionDelegate, autoSetValueProvider);
			return this;
		}

	}

	public class TableEntityTriggerActions<T> {

		EntityMapper entityMapper;

		internal TableEntityTriggerActions(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		public TableEntityTriggerAction<T> Add(When when, Action<T> triggerActionDelegate) {
			entityMapper.NewTriggerAction<T>(when, triggerActionDelegate);
			return new TableEntityTriggerAction<T>(entityMapper);
		}

		public TableEntityTriggerAction<T> AddForBeforeSelectCommand(Action triggerActionDelegate) {
			entityMapper.NewTriggerActionBeforeSelectCommand<T>(triggerActionDelegate);
			return new TableEntityTriggerAction<T>(entityMapper);
		}

	}

	public class TableEntityTriggerAction<T> : TableEntityMapper<T> {

		internal TableEntityTriggerAction(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		internal TableEntityTriggerAction()
			: base() {
		}

		public TableEntityTriggerAction<T> Add(When when, Action<T> triggerActionDelegate) {
			entityMapper.NewTriggerAction<T>(when, triggerActionDelegate);
			return this;
		}

		public TableEntityTriggerAction<T> AddForBeforeSelectCommand(Action triggerActionDelegate) {
			entityMapper.NewTriggerActionBeforeSelectCommand<T>(triggerActionDelegate);
			return this;
		}

	}

	public class TableEntityValidators<T> {

		EntityMapper entityMapper;

		internal TableEntityValidators(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		public TableEntityValidator<T> Add(Before before, Func<T, bool> validatorFunctionDelegate) {
			entityMapper.NewValidatorFunction<T>(before, validatorFunctionDelegate);
			return new TableEntityValidator<T>(entityMapper);
		}

	}

	public class TableEntityValidator<T> : TableEntityMapper<T> {

		internal TableEntityValidator(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		internal TableEntityValidator()
			: base() {
		}

		public TableEntityValidator<T> Add(Before before, Func<T, bool> validatorFunctionDelegate) {
			entityMapper.NewValidatorFunction<T>(before, validatorFunctionDelegate);
			return this;
		}

	}


	public class TableEntityViewColumns<T> {

		EntityMapper entityMapper;

		internal TableEntityViewColumns(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		public TableEntityViewColumn<T> Add(Expression<Func<T, object>> memberExpression, Enum columnName) {
			entityMapper.NewViewColumn<T>(memberExpression, columnName);
			return new TableEntityViewColumn<T>(entityMapper);
		}

	}

	public class TableEntityViewColumn<T> : TableEntityMapper<T> {

		internal TableEntityViewColumn(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		internal TableEntityViewColumn()
			: base() {
		}

		public TableEntityViewColumn<T> Add(Expression<Func<T, object>> memberExpression, Enum columnName) {
			entityMapper.NewViewColumn<T>(memberExpression, columnName);
			return this;
		}

	}

	public class TableEntityQueries<T> {

		EntityMapper entityMapper;

		internal TableEntityQueries(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		public TableEntityQuery<T> Add(Enum queryName, string sql, params InputParameterNameType[] inputParameterNameTypes) {
			entityMapper.NewQuery<T>(queryName, sql, inputParameterNameTypes);
			return new TableEntityQuery<T>(entityMapper);
		}

		public TableEntityQuery<T> Add(Enum queryName, string sql, bool isQueryResultCached, params InputParameterNameType[] inputParameterNameTypes) {
			entityMapper.NewQuery<T>(queryName, sql, isQueryResultCached, inputParameterNameTypes);
			return new TableEntityQuery<T>(entityMapper);
		}

		public TableEntityQuery<T> Add(Enum queryName, string sql, int maxNumberOfQueriesToHoldInCache, params InputParameterNameType[] inputParameterNameTypes) {
			entityMapper.NewQuery<T>(queryName, sql, maxNumberOfQueriesToHoldInCache, inputParameterNameTypes);
			return new TableEntityQuery<T>(entityMapper);
		}

	}

	public class TableEntityQuery<T> : TableEntityMapper<T> {

		internal TableEntityQuery(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		internal TableEntityQuery()
			: base() {
		}

		public TableEntityQuery<T> Add(Enum queryName, string sql, params InputParameterNameType[] inputParameterNameTypes) {
			entityMapper.NewQuery<T>(queryName, sql, inputParameterNameTypes);
			return this;
		}

		public TableEntityQuery<T> Add(Enum queryName, string sql, bool isQueryResultCached, params InputParameterNameType[] inputParameterNameTypes) {
			entityMapper.NewQuery<T>(queryName, sql, isQueryResultCached, inputParameterNameTypes);
			return this;
		}

		public TableEntityQuery<T> Add(Enum queryName, string sql, int maxNumberOfQueriesToHoldInCache, params InputParameterNameType[] inputParameterNameTypes) {
			entityMapper.NewQuery<T>(queryName, sql, maxNumberOfQueriesToHoldInCache, inputParameterNameTypes);
			return this;
		}

	}

	#endregion Fluent interface for table entity mapping

	#region Fluent interface for view entity mapping

	public class ViewEntity<T> {

		internal ViewEntity() {
		}

		public static ViewEntityMapper<T> Connection(Enum connectionName) {
			lock (Map.lockerForMap) {
				EntityMapper entityMapper = EntityMapper.NewViewEntity<T>(connectionName);
				return new ViewEntityMapper<T>(entityMapper);
			}
		}

	}

	public class ViewEntityMapper<T> {

		protected EntityMapper entityMapper;

		internal ViewEntityMapper(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		protected ViewEntityMapper()
			: base() {
		}

		//public DerivedViewEntities<T> DerivedViewEntities { get { return new DerivedViewEntities<T>(); } private set { } }

		public ViewEntityViewColumns<T> ViewColumns { get { return new ViewEntityViewColumns<T>(entityMapper); } private set { } }

		public ViewEntityTriggerActions<T> TriggerActions { get { return new ViewEntityTriggerActions<T>(entityMapper); } private set { } }

		public ViewEntityQueries<T> Queries { get { return new ViewEntityQueries<T>(entityMapper); } private set { } }

	}

	public class ViewEntityViewColumns<T> {

		EntityMapper entityMapper;

		internal ViewEntityViewColumns(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		public ViewEntityViewColumn<T> Add(Expression<Func<T, object>> memberExpression, Enum columnName) {
			entityMapper.NewViewColumn<T>(memberExpression, columnName);
			return new ViewEntityViewColumn<T>(entityMapper);
		}

	}

	public class ViewEntityViewColumn<T> : ViewEntityMapper<T> {

		internal ViewEntityViewColumn(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		internal ViewEntityViewColumn()
			: base() {
		}

		public ViewEntityViewColumn<T> Add(Expression<Func<T, object>> memberExpression, Enum columnName) {
			entityMapper.NewViewColumn<T>(memberExpression, columnName);
			return this;
		}

	}

	public class ViewEntityTriggerActions<T> {

		EntityMapper entityMapper;

		internal ViewEntityTriggerActions(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		public ViewEntityTriggerAction<T> Add(When when, Action<T> triggerActionDelegate) {
			entityMapper.NewTriggerAction<T>(when, triggerActionDelegate);
			return new ViewEntityTriggerAction<T>(entityMapper);
		}

		public ViewEntityTriggerAction<T> AddForBeforeSelectCommand(Action triggerActionDelegate) {
			entityMapper.NewTriggerActionBeforeSelectCommand<T>(triggerActionDelegate);
			return new ViewEntityTriggerAction<T>(entityMapper);
		}

	}

	public class ViewEntityTriggerAction<T> : ViewEntityMapper<T> {

		internal ViewEntityTriggerAction(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		internal ViewEntityTriggerAction()
			: base() {
		}

		public ViewEntityTriggerAction<T> Add(When when, Action<T> triggerActionDelegate) {
			entityMapper.NewTriggerAction<T>(when, triggerActionDelegate);
			return this;
		}

		public ViewEntityTriggerAction<T> AddForBeforeSelectCommand(Action triggerActionDelegate) {
			entityMapper.NewTriggerActionBeforeSelectCommand<T>(triggerActionDelegate);
			return this;
		}

	}

	public class ViewEntityQueries<T> {

		EntityMapper entityMapper;

		internal ViewEntityQueries(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		public ViewEntityQuery<T> Add(Enum queryName, string sql, params InputParameterNameType[] inputParameterNameTypes) {
			entityMapper.NewQuery<T>(queryName, sql, inputParameterNameTypes);
			return new ViewEntityQuery<T>(entityMapper);
		}

		public ViewEntityQuery<T> Add(Enum queryName, string sql, bool isQueryResultCached, params InputParameterNameType[] inputParameterNameTypes) {
			entityMapper.NewQuery<T>(queryName, sql, isQueryResultCached, inputParameterNameTypes);
			return new ViewEntityQuery<T>(entityMapper);
		}

		public ViewEntityQuery<T> Add(Enum queryName, string sql, int maxNumberOfQueriesToHoldInCache, params InputParameterNameType[] inputParameterNameTypes) {
			entityMapper.NewQuery<T>(queryName, sql, maxNumberOfQueriesToHoldInCache, inputParameterNameTypes);
			return new ViewEntityQuery<T>(entityMapper);
		}

	}

	public class ViewEntityQuery<T> : ViewEntityMapper<T> {

		internal ViewEntityQuery(EntityMapper entityMapper) {
			this.entityMapper = entityMapper;
		}

		internal ViewEntityQuery()
			: base() {
		}

		public ViewEntityQuery<T> Add(Enum queryName, string sql, params InputParameterNameType[] inputParameterNameTypes) {
			entityMapper.NewQuery<T>(queryName, sql, inputParameterNameTypes);
			return this;
		}

		public ViewEntityQuery<T> Add(Enum queryName, string sql, bool isQueryResultCached, params InputParameterNameType[] inputParameterNameTypes) {
			entityMapper.NewQuery<T>(queryName, sql, isQueryResultCached, inputParameterNameTypes);
			return this;
		}

		public ViewEntityQuery<T> Add(Enum queryName, string sql, int maxNumberOfQueriesToHoldInCache, params InputParameterNameType[] inputParameterNameTypes) {
			entityMapper.NewQuery<T>(queryName, sql, maxNumberOfQueriesToHoldInCache, inputParameterNameTypes);
			return this;
		}

	}

	#endregion Fluent interface for view entity mapping

}
