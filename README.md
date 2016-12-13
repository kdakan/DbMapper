# DbMapper
A fast mini ORM for .Net with native support for Oracle and Sql Server.

## Introduction
Hi, this article introduces DBMapper, a new ORM tool I have designed and developed. It's written on C#, and tested on Oracle 10g + and Sql Server 2005 +

## Main featues of DBMapper:

* No vendor lock-in. You can support both Oracle and Sql Server and many versions at the same time with the same source code/configuration, without changing anything. Provides vendor-independent mapping for vendor-dependent behavior like sequences, auto-increments, timestamp/rowversion, triggers, fetch-back command syntax, parameter naming and column/parameter types, 
* Can even swith from Oracle to Sql Server online without restarting your application (as demonstrated on the provided example code)
* Easy "fluent" interface and minimal configuration with visual studio intellisense.
* Runtime mapping-configuration changes without rebuilding or restarting application.
* Able to cache query results by query parameters, configurable logical delete and vendor-independent optimistic locking (timestamp) behavior,
* You can organize your entity properties under nested classes in unlimited depth.
* POCO's (you don't need to derive entity classes from a framework base class or mark attributes on your classes, your classes remain persistence-ignorant), therefore you can map any class with public properties, even third-party or system classes to db objects.
* Also provides an ActiveRecord base class if you want to use the Active Record pattern/syntax.
* You can host your logging/authorization, etc. triggers on .Net side, instead of using database triggers (without using PL/SQL or T/SQL)
* High performance, efficient algorithms, use of internal caching and Code.Emit native code generation for improved performance (does not use the slow Reflection api)
* It is open source, has as a clean and expandable code architecture for extensions or modifications, if necessary.

## So what is an ORM? 
It is a tool that frees the developer from writing the same database access codes again and again. With an ORM, you can map classes to tables/views/queries and easily fill your entity collection from db objects, and save them back to database tables. You can also call stored procedures and db functions using a syntax like calling native .Net methods.

## Mapping connections
So lets start mapping. First we define our connections. The following code defines vendor information for our connections with names OracleTest and SqlServerTest. We supply here the standard db parameter prefix as ":" to use in our queries. We could use any special character here, as it will be internally replaced with : when runnuin a query on Oracle and replaced with @ for Sql Server connections.

```
Connections 
  .Add(MyConnection.OracleTest, DBVendor.Oracle, ":") 
  .Add(MyConnection.SqlServerTest, DBVendor.Microsoft, ":");
```
Note that for all string definitions (like connection, schema, table/column, sp names etc.), DBMapper api expects you to pass an Enum type for intellisense names without typing errors, and less typing. We have declared our connection names in the following enum.

```
public enum MyConnection { 
  OracleTest, 
  SqlServerTest 
}
```
Also in the App.config file we supply our connection strings (connection strings can also be encrypted using standard .Net methods you can find on the web)

So where do we map our connections and entities? We map our connections in the MapConnection method of a class implementing the IMapConnection interface. We map our entities in the MapEntity method of any number of classes implementing the IMapEntity interface. App.config has two settings for DLL paths, one for our IMapConnection implementing class, and another for our IMapEntity implementing classes.

```
<?xml version="1.0" encoding="utf-8" ?> 
<configuration> 
  <connectionStrings> 
    <add name="OracleTest" connectionString="User Id=leas;Password=leas;Data Source=localhost" /> 
    <add name="SqlServerTest" connectionString="Integrated Security=SSPI;Data Source=
       localhost\SQLEXPRESS;Initial Catalog=DeneDB;Max Pool Size = 10000;Pooling = True" /> 
  </connectionStrings> 
  <appSettings> 
    <add key="IMapConnectionDllPath" value="..\..\..\Entity\bin\Debug\Common.dll"/> 
    <add key="IMapEntityDllPath" value="..\..\..\Entity\bin\Debug\Entity.dll"/> 
  </appSettings> 
</configuration>
```
## Mapping a basic class for querying
Next we define our entity classes. Entity classes do not need any specific base class, and can have nested or inner classes, with mapped properties in nested or inner classses. Below are two example entity classes, Test and TestReport. Test is a table-entity with both table and query-mapped properties, and also non-mapped properties in its inner classes. TestReport is a query-only entity (called view-entity in this framework). Lets first see the basic TestReport entity:

```
public class TestReport { 
  public string A { get; set; } 
  public string B { get; set; } 
}
```
And we supply the connection name, trigger actions as C# delegates, property to column mapping, and sql select queries and its parameter names in the following mapping statement:

```
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
```
## Entity with nested properties and ActiveRecord
Here is a massive table-entity example with inner classes, table and view mapped columns, auto-set column value providers for primary key, timestamp and other columns, etc. Note that you don't have to provide all this information if you don't support both database vendors, or you suport both vendors throug a common subset of features. This example is massive because it is designed to demonstrate and test most features of DBMapper.

```
public class Test : ActiveRecord { 
  public decimal Price { get; set; } 
  public int Quantity { get; set; } 
  public TestEnum ItemType { get; set; } 
  public DateTime? OrderDate { get; set; } 
  public string Name { get; set; } 
  public string Text { get; set; } 
  public TestInner Inner { get; set; } 
  public List<TestDetail> Details { get; set; } 
  // no column in table for this, but mapped to on some query columns 
  public int WhatIsThis { get; set; }
  private DateTime What1; //not a public property, not mapped to db 
  public DateTime What2; //not a public property, not mapped to db 
  private string NonExistent1 { get; set; } //not a public property, not mapped to db 
  private string NonExistent2; //not a public property, not mapped to db 

  public static Test SelectByTestId(int testId) { 
    return Test.SelectFirst<Test>(Query.SelectByTestId, new InputParameter("testId", testId)); 
  } 
  public static Test SelectByTestIdAndQuantity(int testId, int quantity) { 
    return Test.SelectFirst<Test>(Query.SelectByTestIdAndQuantity,
                                  new InputParameter("testId", testId), 
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
  //this property need not exist, bu if it exists, it will be filled back on logical delete 
  public DateTime DeleteDate { get; set; }
  //this property could be removed, bu if it exists, it will be filled back on insert 
  public string InsertOSUserName { get; set; }
  //this property could be removed, bu if it exists, it will be filled back on update 
  public string UpdateOSUserName { get; set; }
  //this property could be removed, bu if it exists, it will be filled back on logical delete 
  public string DeleteOSUserName { get; set; }
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
```
## Mapping a class for all CRUD operations and trigger actions
Then we start mapping these classes and public properties to tables and queries, as follows (first we define a few function delegates to use in custom triggers or as auto-value providers):

```
Func<object> userNameFunction = delegate() { return Environment.UserName; }; 
Func<object> nowFunction = delegate() { return DateTime.Now; }; 
Func<object> dbNowFunction = delegate() { return DB.CallFunction<DateTime>(
  CommonDefinitions.Connection, CommonDefinitions.Schema, DBFunction.FGET_DATE); }; 
Func<object> dbTimestampFunction = delegate() { DateTime dbNow = DB.CallFunction<DateTime>(
  CommonDefinitions.Connection, CommonDefinitions.Schema, DBFunction.FGET_DATE); 
  if (CommonDefinitions.Connection == MyConnection.OracleTest) return dbNow; 
  else return BitConverter.GetBytes(dbNow.Ticks); }; 
Func<object> newTestIdFunction = delegate() { return DB.CallFunction<int>(
  CommonDefinitions.Connection, CommonDefinitions.Schema, DBFunction.FTEST_ID_NEXTVAL); };
```
And here is the table-entity mappings:

```
TableEntity<test /> 
  .Table(Table.TEST2, CommonDefinitions.Schema, CommonDefinitions.Connection, 
    CommonDefinitions.PrimaryKeyValueProvider, CommonDefinitions.TimestampValueProvider) 
  .PrimaryKeys 
    .Add(TEST.TEST_ID, x => x.Inner.TestId, Sequence.S_TEST, 
      CommonDefinitions.Schema, DBFunction.FTEST_ID_NEXTVAL, null) 
  .Timestamp(TEST.LOCK_TIMESTAMP, x => x.Inner.InnerInner.Timestamp, null, null, dbTimestampFunction) 
  .AutoSetColumns 
    .Add(Before.Insert, TEST.INS_DATE, CommonDefinitions.Schema, 
         DBFunction.FGET_DATE, null, AutoSetValueProvider.DBFunction) 
    .Add(Before.Insert, TEST.INS_OS_USER, null, null, 
         userNameFunction, AutoSetValueProvider.AppFunctionDelegate) 
    .Add(Before.Update, TEST.UPD_DATE, CommonDefinitions.Schema, 
         DBFunction.FGET_DATE, null, AutoSetValueProvider.DBFunction) 
    .Add(Before.Update, TEST.UPD_OS_USER, null, null, userNameFunction, AutoSetValueProvider.AppFunctionDelegate) 
    .Add(Before.LogicalDelete, TEST.DEL_DATE, CommonDefinitions.Schema, 
         DBFunction.FGET_DATE, null, AutoSetValueProvider.DBFunction) 
    .Add(Before.LogicalDelete, TEST.DEL_OS_USER, null, null, 
         userNameFunction, AutoSetValueProvider.AppFunctionDelegate) 
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
    //this mapping need not exist, bu if it exists, it will be filled back on logical delete 
    .Add(x => x.Inner.InnerInner.DeleteDate, TEST.DEL_DATE)
    //this mapping need not exist, bu if it exists, it will be filled back on insert 
    .Add(x => x.Inner.InnerInner.InsertOSUserName, TEST.INS_OS_USER)
    //this mapping need not exist, bu if it exists, it will be filled back on update 
    .Add(x => x.Inner.InnerInner.UpdateOSUserName, TEST.UPD_OS_USER)
    //this mapping need not exist, bu if it exists, it will be filled back on logical delete 
    .Add(x => x.Inner.InnerInner.DeleteOSUserName, TEST.DEL_OS_USER)
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
        WHERE TEST_ID <= 200"
        ) 
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
      ROW_NUMBER() OVER (ORDER BY t1.name desc) AS row_number, 
      t2.TEST_ID,t2.INS_DATE,t2.NAME,t2.TEXT 
      FROM test2 t1, test2 t2 
      where t1.TEST_ID = t2.TEST_ID and t1.INS_DATE = t2.INS_DATE 
      and t1.LOCK_TIMESTAMP = t2.LOCK_TIMESTAMP 
      ) 
      SELECT * FROM r 
      where row_number between :pageSize*(:pageNumber-1) + 1 and :pageSize*:pageNumber 
      ORDER BY name desc", 
      new InputParameterNameType("pageSize", typeof(int)), 
      new InputParameterNameType("pageNumber", typeof(int)) 
      );
```
You can see that all db object name strings are provided by enums. I find this more manageable than spreading strings all around the code, and easier with less typing than declaring and initializing string constants.
```
public enum ValueProvider { 
  App = 1, 
  AppFunctionDelegate = 2, 
  Sequence = 3, 
  DBFunction = 4, 
  DBTriggerredAutoValue = 5 
}
```
Primary keys, as well as timestamp and autoset columns can be set by the following ValueProvider options:

Use App option if the class property will be set in your application code and not fetched back after db command execution.

Use AppFunctionDelegate option if the property will be set by the function delegate you provide before db command execution.

Use Sequence option if the column wil get its value from the sequence and fetched back to fill the class property after db command execution.

DBFunction is similar to the Sequence option, except the column will get its value from the db function.

Use DBTriggerredAutoValue option if the column is filled at the database side, inside a trigger or as an auto-increment column in Sql Server. This column will not be inserted/updated, but only be fetched back to fill the class property after db command execution.

Autoset columns will be filled before insert, update or delete, if you provide them in the mapping. If there is an auto-set column mapping for before logical delete, then delete statements will become logical deletes (ie updating a column like delete_date, etc. instead of deleting the record)

If you provide a timestamp mapping, updates (as well as delete and logical delete) will check if your version is the most current and fail if not (i.e. you read the record, someone else updates that record, and when you try to update it you will get an error, stating you must select it again before updating, for data consistency.

Not every database vendor and every version of the same database support the same feature set, like Oracle does not have auto-increment columns, Sql Server 2005 and 2008 don't have sequences (while 2012 has it), Also, timestamp type is a binary counter in SQL Server, whereas it has date/time semantics in Oracle. So the table-mapping section has a global Primary key value provider and Timestamp column value provider "strategy" section that can be easily changed for the vendor/version of database you or your client wants to use. Then you can define all alternative value providers for that column, and change the global strategy from one place. One thing I'm thinking of doing is, moving this value provider strategy option to the connection definitions as an entity default, and not repeat it for every entity mapping.. Yet another thing I will be adding is, mapping properties for common base entity classes, so those mappings will be "inherited" by all child entity classes. This way, you would not repeat all the common autoset column mappings (assuming those column names are same on all tables mapped to the child entities. 

## CRUD (Create/Read/Update/Delete) statements
An example select statement:

```
IList<TestReport> testReportList = DB.Select<TestReport>(Query.SelectReport, 
  new InputParameter("a", "x"), 
  new InputParameter("aaa", "x"), new InputParameter("list", list));
```
Or something like this, if you use ActiveRecord as a base class:
```
public static Test SelectByTestId(int testId) {
    return Test.SelectFirst<Test>(Query.SelectByTestId, new InputParameter("testId", testId)); 
}
```
An example insert statement:
```
DB.Insert(test); 
```
Or if you use ActiveRecord as a base class:
```
test.Insert(); 
```
Update: 
```
DB.Update(test, Query.SelectByTestId); 
```
Or ActiveRecord version:
```
test.Update(Query.SelectByTestId); 
```
Delete is similar. To call a db function or procedure (last one uses output parameters to return values);
```
DateTime dbNow = DB.CallFunction<DateTime>(
    CommonDefinitions.Connection, CommonDefinitions.Schema, DBFunction.FGET_DATE); 
DB.CallProcedure(CommonDefinitions.Connection, CommonDefinitions.Schema, StoredProcedure.PTEST); 
OutputParameter[] outputParameters = new OutputParameter[] { 
new OutputParameter("P_SOUT", null, typeof(string)) }; 
DB.CallProcedure(CommonDefinitions.Connection, CommonDefinitions.Schema, 
          StoredProcedure.PTEST1, outputParameters, new InputParameter("P_NIN", 123)); 
```
Only the table-columns are inserted and updated, not the view-columns. Only the columns that were queried should be updated for data consistency, so Update statement updates only the table-columns that are in the given query. Queries only fill the properties matching the table-column or view-column mappings and query column names. If there is a column in the query that does not exist in table-colum or view-column mappings, then naturally it does not fill ant property. To use the example test project, you will need to have access to Sql Server and Oracle databases, and the following database objects: SQL Server (two versions of the same table, with and without auto-increment (identity) and with different timestamp types (timestamp/rowversion with counter semantics vs binary for date/time semantics like on Oracle):
```
CREATE TABLE [dbo].[TEST]( 
[TEST_ID] [numeric](9, 0) IDENTITY(1,1) NOT NULL, 
[NAME] [nchar](50) NULL, 
[TEXT] [text] NULL, 
[PRICE] [numeric](22, 2) NULL, 
[QUANTITY] [numeric](9, 0) NULL, 
[ORDER_DATE] [datetime] NULL, 
[LOCK_TIMESTAMP] [timestamp] NOT NULL, 
[DEL_DATE] [datetime] NULL, 
[DESCRIPTION] [nvarchar](100) NULL, 
[ITEM_TYPE] [numeric](2, 0) NULL, 
[INS_DATE] [datetime] NULL, 
[UPD_DATE] [datetime] NULL, 
[INS_OS_USER] [nvarchar](50) NULL, 
[UPD_OS_USER] [nvarchar](50) NULL, 
[DEL_OS_USER] [nvarchar](50) NULL 
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY] 
CREATE NONCLUSTERED INDEX [I_TEST2_TEST_ID] ON [dbo].[TEST2] 
( 
[TEST_ID] ASC 
)
```
Also the DUAL table is used for identical select statements for both Sql Server and Oracle in our queries:
```
CREATE TABLE [dbo].[DUAL]( 
[DUMMY] [nvarchar](1) NULL 
) ON [PRIMARY] 
CREATE PROCEDURE [dbo].[PTEST] 
AS 
BEGIN 
select 1 
END 
CREATE PROCEDURE [dbo].[PTEST1] 
@P_NIN numeric, 
@P_SOUT nvarchar(100) output 
AS 
BEGIN 
SELECT @P_SOUT = 'SSS' + cast(@P_NIN as nvarchar(30)) 
END 
CREATE FUNCTION [dbo].[FGET_DATE]() 
RETURNS DATETIME 
AS 
BEGIN 
RETURN GETDATE(); 
END 
CREATE FUNCTION [dbo].[FTEST_ID_NEXTVAL]() 
RETURNS int 
AS 
BEGIN 
DECLARE @i int; 
SELECT @i = max(TEST_ID) from TEST2; 
RETURN isnull(@i, 0) + 1; 
END
```
For Oracle:
```
create table test2 ( 
TEST_ID NUMBER(9) NOT NULL, 
NAME VARCHAR2(50), 
TEXT CLOB, 
PRICE NUMBER(22,2), 
QUANTITY NUMBER(9), 
ORDER_DATE DATE, 
LOCK_TIMESTAMP TIMESTAMP(6) NOT NULL, 
DEL_DATE DATE, 
DESCRIPTION VARCHAR2(100), 
ITEM_TYPE NUMBER(2), 
INS_DATE DATE, 
UPD_DATE DATE, 
INS_OS_USER VARCHAR2(50), 
UPD_OS_USER VARCHAR2(50), 
DEL_OS_USER VARCHAR2(50) 
) 
create function fget_date return date as 
begin return sysdate; end; 
create function ftest_id_nextval return number is 
begin 
return s_test.nextval; 
end; 
create function getdate return date as 
begin return sysdate; end; 
create procedure ptest as 
begin null; end; 
create procedure ptest1(p_nin number, p_sout out varchar2) as 
begin null; p_sout := 'sss'||to_char(p_nin); end; 
CREATE INDEX I_TEST2_TEST_ID ON TEST2 (TEST_ID);
```
For both Oracle and Sql Server 2012 (only version 2012 has sequences)
```
create sequence s_test; 
```
Schema names I used for my test code are leas for Oracle, and dbo for Sql Server. You can create these objects and run the code to test DBMapper. Important note: Multi-thread (concurrent command) tests for update and logical delete are done using the same record. Normally DBMapper would throw "Record changed, please requery before saving" exception for data consistency, but to carry out tests more realistically, I have commented out the lines that throw this exception for this test. The method is ExecuteNonQuery on ConnectionMapping class:
```
//if (rowCount == 0 && checkRowCount) 
// throw new RecordNotFoundOrRecordHasBeenChangedException(
//  "Record not found or record has been updated since your last query. " + 
//  "Please re-query this record and try again.");
```
Before using it for real-world production systems, you SHOULD remove the comment on this if block and throw this exception! And one last thing, DBMapper uses version 2.112.2.0 of Oracle Data Access Components for .NET, remember to install ODAC on your machine or you will not be able to build or use DBMApper.
## Points of Interest
I learned a lot while writing this ORM tool. I became more proficient in T-SQL, profiling, refactoring, object-oriented design and patterns. This project first started as a very basic prototype for testing dynamic property getter/setter code I found on the web, and grew into a multi-vendor ORM with many features, mostly in my spare time in these last couple of months. Once it grew into a procedural mess, then I refactored and redesigned it using template methods and abstract factory patterns, from then on it became manageable and less-buggy. Up to last couple of weeks, it wasn't tested in multi-thread (concurrent user) mode, and was generating internal caches lazily (on the first need for that cache, but not before). However, this meant I had to use lots and lots of reader/writer locks on these caches for too long intervals, and this caused a great performance decrease when used in concurrent mode. Then I transferred the internal cache/command/Code Emit generators to do all work at the start-up just after the mapping finishes, then I didn't need to lock anything and then came the concurrent performance boost. In its present form, hundreds of concurrent commands perform as fast as a single command execution.

## Organization of the source code
The provided DBMapper.zip file contains the source code for the entire DBMapper engine and my test projects that runs benchmarks and demonstrates the usage.
DBMapper project contains all classes for DBMapper engine and its public mapping and command api.
Fluent.cs contains the fluent mapping api, Mapper.cs contains the private inner mapping api called from fluent interface.
Mappings.cs contain the mapping classes, dictionaries and caches that are used at runtime command execution, and also contains the public api for changing mappings at runtime.
DB.cs contains the public CRUD/SP Call command api.
Command.cs has the template command classes executed on CRUD and SP calls.
CommandFactory.cs has the vendor-specific override command classes for some CRUD commands.
CommandGenerator.cs resembles Command.cs, but contains only classes for generating the necessary information/caches/Code.Emit native code that are used by command classes.
CommandGeneratorFactory.cs classes resemble CommandFactory.cs, and again generate info/caches/Code.Emit native code for vendor specific commands.
ActiveRecord.cs is a basic wrapper on the CRUD command api, for use as a base class if you want to use it.
MultiKeyDictionary.cs contains Dictionary classes with multiple key implementation.
Entity project contains the entity classes used for testing, and also the mapping class for these entities, implementing IMapEntity interface.
Business project contains classes using these test entity classes for CRUD operations, and calling SP's.
Finally, Test project loads IMApConenction and IMApEntity implementing classes, runs MapConnection and MapEntity methods on these classes, then runs GenerateCommands method to prepare the system before its use. Finally, it runs tests that are on the business project.
The tests demonstrate using the public api for running CRUD db commands on entity objects, calling sp's, and then runtime changing schema, connection and DB-vendor for an entity mapping, generating commands for the affected entity, and running benchmarks and the same tests again.. No rebuild or restart necessary for even switching DB vendors, Wow!!!
## History
This is a full working beta version of DBMapper, which dates back to April 2013. It has not been tested by lots of users. I have tested it under simulated heavy load (hundreds of concurrent db commands/users) and it performs exceptionally well. I will be glad to provide help for people who want to use and test it. The source code provided also comes with the test project that I used for my tests. If you have any questions or suggestions, or to report a bug, please email me at kdakan@yahoo.com.
