using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using DBMapper;
using System.Diagnostics;
using Common;

namespace Entity {

	public class MapTestEntities : IMapEntity {

		private static void logTrigger(Test x) {
			//Console.WriteLine("Logging for test id: " + x.Inner.TestId);
		}

		private static void logTrigger(TestReport x) {
			//Console.WriteLine("Logging for testReport a, b: " + x.A, x.B);
		}

		//Action<Deneme> testAuhorizationTriggerAction = x => { Console.WriteLine("Authorization control."); };
		private static void authorizationTrigger(Test x) {
			//Console.WriteLine("Authorization control.");
		}

		private static void authorizationTrigger() {
			//Console.WriteLine("Authorization control.");
		}

		public void MapEntity() {

			Func<object> userNameFunction = delegate() { return Environment.UserName; };
			Func<object> nowFunction = delegate() { return DateTime.Now; };
			Func<object> dbNowFunction = delegate() { return DB.CallFunction<DateTime>(CommonDefinitions.Connection, CommonDefinitions.Schema, DBFunction.FGET_DATE); };
			Func<object> dbTimestampFunction = delegate() { DateTime dbNow = DB.CallFunction<DateTime>(CommonDefinitions.Connection, CommonDefinitions.Schema, DBFunction.FGET_DATE); if (CommonDefinitions.Connection == MyConnection.OracleTest) return dbNow; else return BitConverter.GetBytes(dbNow.Ticks); };

			Func<object> newTestIdFunction = delegate() { return DB.CallFunction<int>(CommonDefinitions.Connection, CommonDefinitions.Schema, DBFunction.FTEST_ID_NEXTVAL); };

			TableEntity<Test>
				.Table(Table.TEST2, CommonDefinitions.Schema, CommonDefinitions.Connection, CommonDefinitions.PrimaryKeyValueProvider, CommonDefinitions.TimestampValueProvider)
				.PrimaryKeys
					.Add(TEST.TEST_ID, x => x.Inner.TestId, Sequence.S_TEST, CommonDefinitions.Schema, DBFunction.FTEST_ID_NEXTVAL, null)
				.Timestamp(TEST.LOCK_TIMESTAMP, x => x.Inner.InnerInner.Timestamp, null, null, dbTimestampFunction)
				.AutoSetColumns
					.Add(Before.Insert, TEST.INS_DATE, CommonDefinitions.Schema, DBFunction.FGET_DATE, null, AutoSetValueProvider.DBFunction)
					.Add(Before.Insert, TEST.INS_OS_USER, null, null, userNameFunction, AutoSetValueProvider.AppFunctionDelegate)
					.Add(Before.Update, TEST.UPD_DATE, CommonDefinitions.Schema, DBFunction.FGET_DATE, null, AutoSetValueProvider.DBFunction)
					.Add(Before.Update, TEST.UPD_OS_USER, null, null, userNameFunction, AutoSetValueProvider.AppFunctionDelegate)
					.Add(Before.LogicalDelete, TEST.DEL_DATE, CommonDefinitions.Schema, DBFunction.FGET_DATE, null, AutoSetValueProvider.DBFunction)
					.Add(Before.LogicalDelete, TEST.DEL_OS_USER, null, null, userNameFunction, AutoSetValueProvider.AppFunctionDelegate)
				.TriggerActions
					.AddForBeforeSelectCommand(authorizationTrigger)
					.Add(When.AfterSelectForEachRow, logTrigger)
					.Add(When.BeforeInsertForEachRow, authorizationTrigger)
					.Add(When.AfterInsertForEachRow, logTrigger)
					.Add(When.BeforeUpdateForEachRow, authorizationTrigger)
					.Add(When.AfterUpdateForEachRow, logTrigger)
					.Add(When.BeforeDeleteForEachRow, authorizationTrigger)
					.Add(When.AfterDeleteForEachRow, logTrigger)
				.TableColumns
					.Add(x => x.Name, TEST.NAME)
					.Add(x => x.Text, TEST.TEXT)
					.Add(x => x.Price, TEST.PRICE)
					.Add(x => x.Quantity, TEST.QUANTITY)
					.Add(x => x.OrderDate, TEST.ORDER_DATE, true)
					.Add(x => x.ItemType, TEST.ITEM_TYPE)
					.Add(x => x.Inner.InnerInner.InnerDescription, TEST.DESCRIPTION)
					.Add(x => x.Inner.InnerInner.DeleteDate, TEST.DEL_DATE) //bu tanımlanmasa da olur tanımlanmışsa logical delete'de doldurulur
					.Add(x => x.Inner.InnerInner.InsertOSUserName, TEST.INS_OS_USER) //bu tanımlanmasa da olur tanımlanmışsa insert'de doldurulur
					.Add(x => x.Inner.InnerInner.UpdateOSUserName, TEST.UPD_OS_USER) //bu tanımlanmasa da olur tanımlanmışsa update'de doldurulur
					.Add(x => x.Inner.InnerInner.DeleteOSUserName, TEST.DEL_OS_USER) //bu tanımlanmasa da olur tanımlanmışsa delete'de doldurulur
				.ViewColumns
					.Add(x => x.Inner.InnerDescription1, TEST.DESCRIPTION1)
					.Add(x => x.Inner.InnerDescription2, TEST.DESCRIPTION2)
					.Add(x => x.WhatIsThis, TEST.WHAT_IS_THIS)
				.Queries
					.Add(Query.SelectAll,
							@"SELECT 1 AS WHAT, T.QUANTITY, -99 AS WHAT_IS_THIS, T.DESCRIPTION, 
							T.DESCRIPTION AS DESCRIPTION1, T.DESCRIPTION AS DESCRIPTION2, 
							T.TEST_ID, T.TEXT, T.PRICE, T.LOCK_TIMESTAMP, T.ORDER_DATE, T.ITEM_TYPE 
							FROM TEST2 T 
							WHERE TEST_ID <= 200")
					.Add(Query.SelectByTestId,
							@"SELECT T.UPD_OS_USER, T.QUANTITY, -99 AS WHAT_IS_THIS, T.DESCRIPTION, 
							T.DESCRIPTION AS DESCRIPTION1, T.DESCRIPTION AS DESCRIPTION2, 
							T.TEST_ID, T.TEXT, T.PRICE, T.LOCK_TIMESTAMP, T.ORDER_DATE, T.ITEM_TYPE 
							FROM TEST2 T 
							WHERE TEST_ID = :testId", true,
							new InputParameterNameType("testId", typeof(int))
							)
					.Add(Query.SelectByTestIdForFoo,
							@"SELECT T.QUANTITY, -99 AS WHAT_IS_THIS, T.DESCRIPTION, 
							T.TEST_ID, T.TEXT, T.PRICE, T.LOCK_TIMESTAMP
							FROM TEST2 T 
							WHERE TEST_ID = :testId", true,
							new InputParameterNameType("testId", typeof(int))
							)
					.Add(Query.SelectByTestIdAndQuantity,
							@"SELECT * 
							FROM TEST2 
							WHERE TEST_ID = :testId 
							AND QUANTITY = :quantity",
							new InputParameterNameType("testId", typeof(int)),
							new InputParameterNameType("quantity", typeof(int))
							)
					.Add(Query.SelectWithPaging,
							@"WITH r AS 
							(
							SELECT
							ROW_NUMBER() OVER (ORDER BY t1.name desc) AS row_number, t2.TEST_ID,t2.INS_DATE,t2.NAME,t2.TEXT 
							FROM test2 t1, test2 t2
							where t1.TEST_ID = t2.TEST_ID and t1.INS_DATE = t2.INS_DATE and t1.LOCK_TIMESTAMP = t2.LOCK_TIMESTAMP
							)
							SELECT * FROM r
							where row_number between :pageSize*(:pageNumber-1) + 1 and :pageSize*:pageNumber
							ORDER BY name desc",
							new InputParameterNameType("pageSize", typeof(int)),
							new InputParameterNameType("pageNumber", typeof(int))
							);

			ViewEntity<TestReport>
				.Connection(CommonDefinitions.Connection)
				.TriggerActions
					.AddForBeforeSelectCommand(authorizationTrigger)
					.Add(When.AfterSelectForEachRow, logTrigger)
				.ViewColumns
					.Add(x => x.A, TEST_REPORT.A)
					.Add(x => x.B, TEST_REPORT.B)
				.Queries
					.Add(Query.SelectReport,
							@"SELECT :a AS A, GETDATE() AS B 
							FROM DUAL 
							WHERE :a = :aaa 
							AND 1 in (:list)", true,
							new InputParameterNameType("a", typeof(string)),
							new InputParameterNameType("aaa", typeof(string)),
							new InputParameterNameType("list", typeof(string))
							);
		}

	}


	public class Test : ActiveRecord {
		public decimal Price { get; set; }
		public int Quantity { get; set; }
		public TestEnum ItemType { get; set; }
		public DateTime? OrderDate { get; set; }
		public string Name { get; set; }
		public string Text { get; set; }

		public TestInner Inner { get; set; }

		public List<TestDetail> Details { get; set; }


		public int WhatIsThis { get; set; } // no column in table for this, but mapped to on some query columns

		private DateTime What1;				//not a public property, not mapped to db
		public DateTime What2;				//not a public property, not mapped to db
		private string NonExistant1 { get; set; } //not a public property, not mapped to db
		private string NonExistant2;							//not a public property, not mapped to db

		public static Test SelectByTestId(int testId) {
			return Test.SelectFirst<Test>(Query.SelectByTestId, new InputParameter("testId", testId));
		}

		public static Test SelectByTestIdAndQuantity(int testId, int quantity) {
			return Test.SelectFirst<Test>(Query.SelectByTestIdAndQuantity, new InputParameter("testId", testId),
																																		 new InputParameter("quantity", quantity));
		}

		public static IList<Test> SelectAll() {
			return Test.Select<Test>(Query.SelectAll);
		}

	}

	public class TestInner {
		public int TestId { get; set; }

		public string InnerDescription1 { get; set; }
		public string InnerDescription2 { get; set; }
		public TestInnerInner InnerInner { get; set; }
	}

	public class TestInnerInner {
		public DateTime DeleteDate { get; set; }//this property could be removed, bu if it exists, it will be filled back on logical delete
		public string InsertOSUserName { get; set; }//this property could be removed, bu if it exists, it will be filled back on insert
		public string UpdateOSUserName { get; set; }//this property could be removed, bu if it exists, it will be filled back on update
		public string DeleteOSUserName { get; set; }//this property could be removed, bu if it exists, it will be filled back on logical delete

		public byte[] Timestamp { get; set; }

		public string InnerDescription { get; set; }
	}

	public class TestDetail {
		public int TestDetailId { get; set; }
		public int TestId { get; set; }
		public string DetailDescription { get; set; }
	}

	public enum TestEnum {
		Type1 = 1,
		Type2 = 2
	}

	public class TestReport {
		public string A { get; set; }
		public string B { get; set; }
	}


	public enum TEST {
		TEST_ID,
		NAME,
		TEXT,
		PRICE,
		QUANTITY,
		ORDER_DATE,
		ITEM_TYPE,
		DESCRIPTION,

		LOCK_TIMESTAMP,
		INS_OS_USER,
		INS_DATE,
		UPD_DATE,
		UPD_OS_USER,
		DEL_DATE,
		DEL_OS_USER,

		WHAT_IS_THIS,
		DESCRIPTION1,
		DESCRIPTION2,
	}

	public enum TEST_REPORT {
		A,
		B,
	}

	public enum Query {
		SelectAll,
		SelectByTestId,
		SelectByTestIdForFoo,
		SelectByTestIdAndQuantity,
		SelectWithPaging,
		SelectReport,
	}

	public enum Table {
		TEST2,
		TEST_DETAIL
	}

	public enum StoredProcedure {
		PTEST,
		PTEST1,
	}

	public enum DBFunction {
		FGET_DATE,
		FTEST_ID_NEXTVAL
	}

	public enum Sequence {
		S_TEST,
	}

}
