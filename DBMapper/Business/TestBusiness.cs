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
using Entity;
using Common;
using System.Threading;

namespace Business {
	public class TestBusiness {

		public void RunTests() {
			try {
				Console.WriteLine("Display mappings begin");

				Console.WriteLine("Connection mappings:");
				foreach (ConnectionMapping connectionMapping in Map.ConnectionMappings) {
					Console.WriteLine("ConnectionName:" + connectionMapping.ConnectionName);
					Console.WriteLine("ConnectionString:" + connectionMapping.ConnectionString);
					Console.WriteLine("DBParameterPrefix:" + connectionMapping.DBParameterPrefix);
					Console.WriteLine("DBVendor:" + connectionMapping.DBVendor);
					Console.WriteLine("ProviderInvariantName:" + connectionMapping.ProviderInvariantName);
					Console.WriteLine("UsedDBParameterPrefix:" + connectionMapping.UsedDBParameterPrefix);
					Console.WriteLine("-------------------------------------------------------------------");
				}

				Console.WriteLine("Entity mappings:");
				foreach (EntityMapping entityMapping in Map.EntityMappings) {
					Console.WriteLine("EntityType:" + entityMapping.EntityType);
					Console.WriteLine("ConnectionName:" + entityMapping.ConnectionName);
					Console.WriteLine("TableName:" + entityMapping.TableName);
					Console.WriteLine("SchemaName:" + entityMapping.SchemaName);
					if (entityMapping.PrimaryKeyColumnNames != null) {
						Console.WriteLine("PrimaryKeyColumnNames:");
						foreach (string primaryKeyColumnName in entityMapping.PrimaryKeyColumnNames) {
							Console.WriteLine("PrimaryKeyColumnName:" + primaryKeyColumnName);
						}
					}
					if (entityMapping.TimestampMapping != null) {
						Console.WriteLine("TimestampMapping:");
						Console.WriteLine("ColumnName:" + entityMapping.TimestampMapping.ColumnName);
						Console.WriteLine("NestedPropertyName:" + entityMapping.TimestampMapping.NestedPropertyName);
						Console.WriteLine("ValueProviderSet:");
						Console.WriteLine("SchemaName:" + entityMapping.TimestampMapping.ValueProviderSet.SchemaName);
						Console.WriteLine("DBFunctionName:" + entityMapping.TimestampMapping.ValueProviderSet.DBFunctionName);
						Console.WriteLine("SequenceName:" + entityMapping.TimestampMapping.ValueProviderSet.SequenceName);
						Console.WriteLine("FunctionDelegate:" + entityMapping.TimestampMapping.ValueProviderSet.FunctionDelegate);
						Console.WriteLine("ValueProvider:" + entityMapping.TimestampMapping.ValueProviderSet.ValueProvider);
					}
					Console.WriteLine("TriggerActions:");
					foreach (When when in entityMapping.TriggerActions) {
						Console.WriteLine("When:" + when);
					}
					Console.WriteLine("insertTestAutoSetColumnMappings:");
					foreach (AutoSetColumnMapping autoSetColumnMapping in entityMapping.InsertAutoSetColumnMappings) {
						Console.WriteLine("ColumnName:" + autoSetColumnMapping.ColumnName);
						Console.WriteLine("ValueProviderSet:");
						Console.WriteLine("SchemaName:" + autoSetColumnMapping.ValueProviderSet.SchemaName);
						Console.WriteLine("DBFunctionName:" + autoSetColumnMapping.ValueProviderSet.DBFunctionName);
						Console.WriteLine("SequenceName:" + autoSetColumnMapping.ValueProviderSet.SequenceName);
						Console.WriteLine("FunctionDelegate:" + autoSetColumnMapping.ValueProviderSet.FunctionDelegate);
						Console.WriteLine("ValueProvider:" + autoSetColumnMapping.ValueProviderSet.ValueProvider);
					}
					Console.WriteLine("updateTestAutoSetColumnMappings:");
					foreach (AutoSetColumnMapping autoSetColumnMapping in entityMapping.UpdateAutoSetColumnMappings) {
						Console.WriteLine("ColumnName:" + autoSetColumnMapping.ColumnName);
						Console.WriteLine("ValueProviderSet:");
						Console.WriteLine("SchemaName:" + autoSetColumnMapping.ValueProviderSet.SchemaName);
						Console.WriteLine("DBFunctionName:" + autoSetColumnMapping.ValueProviderSet.DBFunctionName);
						Console.WriteLine("SequenceName:" + autoSetColumnMapping.ValueProviderSet.SequenceName);
						Console.WriteLine("FunctionDelegate:" + autoSetColumnMapping.ValueProviderSet.FunctionDelegate);
						Console.WriteLine("ValueProvider:" + autoSetColumnMapping.ValueProviderSet.ValueProvider);
					}
					Console.WriteLine("LogicaldeleteTestAutoSetColumnMappings:");
					foreach (AutoSetColumnMapping autoSetColumnMapping in entityMapping.LogicalDeleteAutoSetColumnMappings) {
						Console.WriteLine("ColumnName:" + autoSetColumnMapping.ColumnName);
						Console.WriteLine("ValueProviderSet:");
						Console.WriteLine("SchemaName:" + autoSetColumnMapping.ValueProviderSet.SchemaName);
						Console.WriteLine("DBFunctionName:" + autoSetColumnMapping.ValueProviderSet.DBFunctionName);
						Console.WriteLine("SequenceName:" + autoSetColumnMapping.ValueProviderSet.SequenceName);
						Console.WriteLine("FunctionDelegate:" + autoSetColumnMapping.ValueProviderSet.FunctionDelegate);
						Console.WriteLine("ValueProvider:" + autoSetColumnMapping.ValueProviderSet.ValueProvider);
					}
					Console.WriteLine("ColumnMappings:");
					foreach (ColumnMapping columnMapping in entityMapping.ColumnMappings) {
						Console.WriteLine("ColumnName:" + columnMapping.ColumnName);
						Console.WriteLine("NestedPropertyName:" + columnMapping.NestedPropertyName);
						Console.WriteLine("IsDBNullableValueType:" + columnMapping.IsDBNullableValueType);
					}
					Console.WriteLine("QueryMappings:");
					foreach (QueryMapping queryMapping in entityMapping.QueryMappings) {
						Console.WriteLine("QueryName:" + queryMapping.QueryName);
						Console.WriteLine("selectTestSql:" + queryMapping.SelectSql);
						Console.WriteLine("IsQueryResultCached:" + queryMapping.IsQueryResultCached);
						Console.WriteLine("MaxNumberOfQueriesToHoldInCache:" + queryMapping.MaxNumberOfQueriesToHoldInCache);
					}
					Console.WriteLine("-------------------------------------------------------------------");
				}
				Console.WriteLine("Display mappings complete");

				Console.WriteLine("Press a key to continue");
				Console.ReadKey();

				Console.WriteLine("Singlethread test 1 begin");
				benchmark(1, "allBenchmarks", delegate { allBenchmarks(); });
				Console.WriteLine("Singlethread test 1 complete");

				Console.WriteLine("Press a key to continue");
				Console.ReadKey();

				Console.WriteLine("Change mappings test begin");
				EntityMapping entityMappingToChange = Map.EntityMappings[0];
				entityMappingToChange.ChangeConnectionName("SqlServerTest");
				entityMappingToChange.ChangeSchemaName("DBO");
				entityMappingToChange.ChangeTableName("TEST2");
				entityMappingToChange.PrimaryKeyMapping.ChangeSchemaName("DBO");
				entityMappingToChange.TimestampMapping.ChangeSchemaName("DBO");
				foreach (AutoSetColumnMapping autoSetColumnMapping in entityMappingToChange.InsertAutoSetColumnMappings) {
					autoSetColumnMapping.ChangeSchemaName("DBO");
				}
				foreach (AutoSetColumnMapping autoSetColumnMapping in entityMappingToChange.UpdateAutoSetColumnMappings) {
					autoSetColumnMapping.ChangeSchemaName("DBO");
				}				
				foreach (AutoSetColumnMapping autoSetColumnMapping in entityMappingToChange.LogicalDeleteAutoSetColumnMappings) {
					autoSetColumnMapping.ChangeSchemaName("DBO");
				}
				
				CommonDefinitions.Connection = (MyConnection)Enum.Parse(typeof(MyConnection), "SqlServerTest");
				CommonDefinitions.Schema = (MySchema)Enum.Parse(typeof(MySchema), "DBO");

				entityMappingToChange.GenerateCommands();


				benchmark(1, "allBenchmarks", delegate { allBenchmarks(); });

				Console.WriteLine("Change mappings test complete");

				Console.WriteLine("Press a key to continue");
				Console.ReadKey();

				Console.WriteLine("Singlethread test 2 begin");
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();

				int k;
				for (k = 0; k < 1000; k++) {
					callFunctionProcedureTest();
					selectTest();
					insertTest();
					updateTest();
					deleteTest();
				}
				stopwatch.Stop();
				Console.WriteLine("Count:" + k * 5 + " Timespan(ms):" + stopwatch.ElapsedMilliseconds + " avg ms: " + stopwatch.ElapsedMilliseconds / k / 5);
				Console.WriteLine("Singlethread test 2 complete");

				Console.WriteLine("Press a key to continue");
				Console.ReadKey();

				Console.WriteLine("Multithread test 1 begin");
				stopwatch = new Stopwatch();
				stopwatch.Start();
				for (k = 0; k < 1000; k++) {
					Thread thread = new Thread(callFunctionProcedureTest);
					thread.Start();
					thread = new Thread(selectTest);
					thread.Start();
					thread = new Thread(insertTest);
					thread.Start();
					thread = new Thread(updateTest);
					thread.Start();
					thread = new Thread(deleteTest);
					thread.Start();
					//Thread.Sleep(lag);
					//thread.Join();
				}

				stopwatch.Stop();
				Console.WriteLine("Count:" + k * 5 + " Timespan(ms):" + stopwatch.ElapsedMilliseconds + " avg ms: " + stopwatch.ElapsedMilliseconds / k / 5);
				Console.WriteLine("Multithread test 1 complete");

				Console.WriteLine("Press a key to continue");
				Console.ReadKey();

				Console.WriteLine("Multithread test 2 begin");
				Console.WriteLine("callFunctionProcedureTest");
				stopwatch = new Stopwatch();
				stopwatch.Start();

				for (k = 0; k < 1000; k++) {
					Thread thread = new Thread(callFunctionProcedureTest);
					thread.Start();
					//Thread.Sleep(20);
				}
				stopwatch.Stop();
				Console.WriteLine("Count:" + k + " Timespan(ms):" + stopwatch.ElapsedMilliseconds + " avg ms: " + stopwatch.ElapsedMilliseconds / k);

				Console.WriteLine("selectTest");
				stopwatch = new Stopwatch();
				stopwatch.Start();

				for (k = 0; k < 1000; k++) {
					Thread thread = new Thread(selectTest);
					thread.Start();
					//Thread.Sleep(20);
				}
				stopwatch.Stop();
				Console.WriteLine("Count:" + k + " Timespan(ms):" + stopwatch.ElapsedMilliseconds + " avg ms: " + stopwatch.ElapsedMilliseconds / k);

				Console.WriteLine("insertTest");
				stopwatch = new Stopwatch();
				stopwatch.Start();

				for (k = 0; k < 1000; k++) {
					Thread thread = new Thread(insertTest);
					thread.Start();
					//Thread.Sleep(20);
				}
				stopwatch.Stop();
				Console.WriteLine("Count:" + k + " Timespan(ms):" + stopwatch.ElapsedMilliseconds + " avg ms: " + stopwatch.ElapsedMilliseconds / k);

				Console.WriteLine("updateTest");
				stopwatch = new Stopwatch();
				stopwatch.Start();

				for (k = 0; k < 1000; k++) {
					Thread thread = new Thread(updateTest);
					thread.Start();
					//Thread.Sleep(20);
				}
				stopwatch.Stop();
				Console.WriteLine("Count:" + k + " Timespan(ms):" + stopwatch.ElapsedMilliseconds + " avg ms: " + stopwatch.ElapsedMilliseconds / k);

				Console.WriteLine("deleteTest");
				stopwatch = new Stopwatch();
				stopwatch.Start();

				for (k = 0; k < 1000; k++) {
					Thread thread = new Thread(deleteTest);
					thread.Start();
					//Thread.Sleep(20);
				}
				stopwatch.Stop();
				Console.WriteLine("Count:" + k + " Timespan(ms):" + stopwatch.ElapsedMilliseconds + " avg ms: " + stopwatch.ElapsedMilliseconds / k);

				Console.WriteLine("Multithread test 2 complete");

				Console.WriteLine("Press a key to exit");
				Console.ReadKey();
			}
			catch (Exception ex) {
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine("--------------------------------------------------------------------");
				Console.WriteLine(ex.InnerException.Message);
				Console.WriteLine(ex.InnerException.StackTrace);
			}

		}

		private static void allBenchmarks() {
			//benchmark(1, "map", delegate { map(); });
			benchmark(1, "callFunctionProcedureTest", delegate { callFunctionProcedureTest(); });
			benchmark(1, "callFunctionProcedureTest", delegate { callFunctionProcedureTest(); });
			benchmark(1, "callFunctionProcedureTest", delegate { callFunctionProcedureTest(); });
			benchmark(1, "callFunctionProcedureTest", delegate { callFunctionProcedureTest(); });
			benchmark(1, "callFunctionProcedureTest", delegate { callFunctionProcedureTest(); });
			benchmark(1, "callFunctionProcedureTest", delegate { callFunctionProcedureTest(); });
			benchmark(1, "callFunctionProcedureTest", delegate { callFunctionProcedureTest(); });
			benchmark(1, "callFunctionProcedureTest", delegate { callFunctionProcedureTest(); });
			benchmark(1, "callFunctionProcedureTest", delegate { callFunctionProcedureTest(); });
			benchmark(1, "callFunctionProcedureTest", delegate { callFunctionProcedureTest(); });
			benchmark(1, "insertTest", delegate { insertTest(); });
			benchmark(1, "insertTest", delegate { insertTest(); });
			benchmark(1, "insertTest", delegate { insertTest(); });
			benchmark(1, "insertTest", delegate { insertTest(); });
			benchmark(1, "insertTest", delegate { insertTest(); });
			benchmark(1, "insertTest", delegate { insertTest(); });
			benchmark(1, "insertTest", delegate { insertTest(); });
			benchmark(1, "insertTest", delegate { insertTest(); });
			benchmark(1, "insertTest", delegate { insertTest(); });
			benchmark(1, "insertTest", delegate { insertTest(); });
            benchmark(1, "selectTest", delegate { selectTest(); });
			benchmark(1, "selectTest", delegate { selectTest(); });
			benchmark(1, "selectTest", delegate { selectTest(); });
			benchmark(1, "selectTest", delegate { selectTest(); });
			benchmark(1, "selectTest", delegate { selectTest(); });
			benchmark(1, "selectTest", delegate { selectTest(); });
			benchmark(1, "selectTest", delegate { selectTest(); });
			benchmark(1, "selectTest", delegate { selectTest(); });
			benchmark(1, "selectTest", delegate { selectTest(); });
			benchmark(1, "selectTest", delegate { selectTest(); });
			benchmark(1, "updateTest", delegate { updateTest(); });
			benchmark(1, "updateTest", delegate { updateTest(); });
			benchmark(1, "updateTest", delegate { updateTest(); });
			benchmark(1, "updateTest", delegate { updateTest(); });
			benchmark(1, "updateTest", delegate { updateTest(); });
			benchmark(1, "updateTest", delegate { updateTest(); });
			benchmark(1, "updateTest", delegate { updateTest(); });
			benchmark(1, "updateTest", delegate { updateTest(); });
			benchmark(1, "updateTest", delegate { updateTest(); });
			benchmark(1, "updateTest", delegate { updateTest(); });
			benchmark(1, "deleteTest", delegate { deleteTest(); });
			benchmark(1, "deleteTest", delegate { deleteTest(); });
			benchmark(1, "deleteTest", delegate { deleteTest(); });
			benchmark(1, "deleteTest", delegate { deleteTest(); });
			benchmark(1, "deleteTest", delegate { deleteTest(); });
			benchmark(1, "deleteTest", delegate { deleteTest(); });
			benchmark(1, "deleteTest", delegate { deleteTest(); });
			benchmark(1, "deleteTest", delegate { deleteTest(); });
			benchmark(1, "deleteTest", delegate { deleteTest(); });
			benchmark(1, "deleteTest", delegate { deleteTest(); });
		}

		private static void benchmarkx(int count, string message, Action action) {
			for (int i = 0; i < count; i++)
				action.Invoke();
		}

		private static void benchmark(int adet, string mesaj, Action action) {
			try {
				Console.Write(mesaj + ": ");
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
				for (int i = 0; i < adet; i++)
					action.Invoke();
				stopwatch.Stop();
				Console.WriteLine("Timespan(ms):" + stopwatch.ElapsedMilliseconds);
				//Console.ReadKey();
				//Console.WriteLine("----------------------------------------------");
			}
			catch (Exception ex) {
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
				throw;

			}
		}

		private static void logTrigger(Test x) {
			//Console.WriteLine("Logging for test id: " + x.Inner.TestId);
		}

		private static void logTrigger(TestReport x) {
			//Console.WriteLine("Logging for testReport a, b: " + x.A, x.B);
		}

		//Action<Deneme> denemeAuhorizationTriggerAction = x => { throw new Exception("Bu işlem için yetkiniz yoktur."); };
		private static void authorizationTrigger(Test x) {
			//Console.WriteLine("Authorization control.");
		}

		private static void authorizationTrigger() {
			//Console.WriteLine("Authorization control.");
		}

		//create table deneme (denemeno number(9) not null, ad varchar2(30), yazi clob, tutar number(22,2), rakam number (9), ipt_tar date, kyt_tar timestamp);
		private static void selectTest() {
			IList<Test> testList = DB.Select<Test>(Query.SelectAll);
			//List<Deneme> denemeListe = Deneme.selectTestAll();

			//foreach (Deneme deneme in denemeListe) {
			//  Console.Write("{0}\t", deneme.Id);
			//  Console.Write("{0}\t", deneme.Ad);
			//  Console.Write("{0}\t", deneme.Yazi);
			//  Console.Write("{0}\t", deneme.Tutar);
			//  Console.Write("{0}\t", deneme.Rakam);
			//  Console.Write("{0}\t", deneme.Tarih);
			//  Console.Write("{0}\t", deneme.Tur);
			//  Console.Write("{0}\t", deneme.BuNedir);
			//  Console.Write("{0}\t", deneme.Ic.IcAciklama1);
			//  Console.Write("{0}\t", deneme.Ic.IcAciklama2);
			//  Console.Write("{0}\t", deneme.Ic.IcIc.IcIcAciklama);
			//  Console.WriteLine();
			//}

			//Console.WriteLine();
			//Console.WriteLine();
			//Console.WriteLine();

			Test test1 = DB.SelectFirst<Test>(Query.SelectByTestId, new InputParameter("testId", 4));
			Test test2 = DB.SelectFirst<Test>(Query.SelectByTestId, new InputParameter("testId", 4));
			if (test1 != test2 || test1 == null)
				throw new Exception("Expected same result object from same cached query, but results are different objects");

			List<string> list = new List<string>();
			list.Add("1");
			list.Add(null);
			list.Add("3");

			IList<TestReport> testReportList = DB.Select<TestReport>(Query.SelectReport, new InputParameter("a", "x"), new InputParameter("aaa", "x"), new InputParameter("list", list));
			//foreach (DenemeRapor denemeRapor in denemeRaporListe) {
			//  Console.Write("{0}\t", denemeRapor.a);
			//  Console.Write("{0}\t", denemeRapor.b);
			//  //  Console.WriteLine();
			//}


		}

		private static void insertTest() {
			Test test = new Test() {
				//Id = 11,
				Name = "Name11",
				Inner = new TestInner() { InnerDescription1 = "InnerDesc1", InnerDescription2 = "InnerDesc2", InnerInner = new TestInnerInner() { InnerDescription = "InnerDesc" } },
				Quantity = 111,
				OrderDate = DateTime.Today,
				ItemType = TestEnum.Type1,
				Price = 111.11M,
				Text = "Text11",
			};

			//for (int i = 0; i < 100000; i++)
			//  test.Text += i;

			//deneme = DB.insertTest<Deneme>(deneme);
			test.Insert();
			//DB.Insert(test);
			//Console.WriteLine("insertTest OK.");
			Test test2 = new Test();

			//deneme = DB.insertTest<Deneme>(deneme);
			test2.Insert();

		}

		private static void updateTest() {
			//Deneme deneme = DB.selectTestFirst<Deneme>(DenemeQuery.selectTestById, new QueryParameter("testId", 1));

			Test test = Test.SelectByTestId(4);
			test.Name = null;
			test.WhatIsThis = 0;
			if (test.Inner != null && test.Inner.InnerInner != null)
				test.Inner.InnerInner.InnerDescription = null;
			if (test.Inner != null) {
				test.Inner.InnerDescription1 = null;
				test.Inner.InnerDescription2 = null;
			}
			test.Quantity = 0;
			test.OrderDate = null;// DateTime.MinValue;
			test.Price = 0;
			test.Text = null;
			test.ItemType = 0;

			DB.Update(test, Query.SelectByTestId);
			//test.Update(Query.SelectByTestId);

			Test test2 = Test.SelectFirst<Test>(Query.SelectByTestIdForFoo, new InputParameter("testId", 4));
			test2.Name = "Ad";
			test2.WhatIsThis = -9;
			test2.Inner.InnerInner.InnerDescription = "Description";
			test2.Inner.InnerDescription1 = null;
			test2.Inner.InnerDescription2 = null;
			test2.Quantity = 111;
			test2.OrderDate = DateTime.Now;
			test2.Price = 12345.67M;
			test2.Text = "Text";
			test2.ItemType = TestEnum.Type1;

			//deneme2.Yazi = "YazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazıYazı";
			DB.Update(test2, Query.SelectByTestIdForFoo);
			//deneme = DB.updateTestProperties<Deneme>(deneme, x => x.Ad);
			//deneme.updateTest();

			//Console.WriteLine("updateTest OK.");
		}

		private static void deleteTest() {
			//Deneme deneme = DB.selectTestFirst<Deneme>(DenemeQuery.selectTestById, new QueryParameter("testId", 1));
			Test test = Test.SelectByTestId(7);
			//Test test = new Test();
			//test.Inner = new InnerTest();
			//test.Inner.TestId = 7;
			//deneme.deleteTest();
			DB.Delete(test);
			//deneme = DB.updateTestProperties<Deneme>(deneme, x => x.Ad);
			//deneme.updateTest();

			//Console.WriteLine("deleteTest OK.");
		}

		private static void callFunctionProcedureTest() {

			DateTime dbNow = DB.CallFunction<DateTime>(CommonDefinitions.Connection, CommonDefinitions.Schema, DBFunction.FGET_DATE);
			//Console.WriteLine("dbNow:" + dbNow);

			DB.CallProcedure(CommonDefinitions.Connection, CommonDefinitions.Schema, StoredProcedure.PTEST);

			OutputParameter[] outputParameters = new OutputParameter[] {
			  new OutputParameter("P_SOUT", null, typeof(string)) };

			DB.CallProcedure(CommonDefinitions.Connection, CommonDefinitions.Schema, StoredProcedure.PTEST1, outputParameters, new InputParameter("P_NIN", 123));
			//Console.WriteLine("p_sout output:" + outputParameters[0].Value);
		}

	}

}

