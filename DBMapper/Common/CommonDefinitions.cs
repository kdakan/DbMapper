using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBMapper;

namespace Common {
	public static class CommonDefinitions {

		//Oracle'dan sql server'a switch edilecekse sadece aşağıdaki satırlar commentlenip diğerleri açılacak..
		//istenirse bunlar app.config'de appsettings'e kaydedilerek oradan da okunabilir, o durumda tekrar build yerine restart yeterli olur..
		public static MyConnection Connection = MyConnection.OracleTest;
		public static MySchema Schema = MySchema.LEAS;
		public static PrimaryKeyValueProvider PrimaryKeyValueProvider = PrimaryKeyValueProvider.DBFunction;
		//private static PrimaryKeyValueProvider PrimaryKeyValueProvider = PrimaryKeyValueProvider.Sequence;
		public static TimestampValueProvider TimestampValueProvider = TimestampValueProvider.AppFunctionDelegate;

	}

	public class MapTestConnections : IMapConnection {
		public void MapConnection() {
			Connections
				.Add(MyConnection.OracleTest, DBVendor.Oracle, ":")
				.Add(MyConnection.SqlServerTest, DBVendor.Microsoft, ":");

		}
	}

	public enum MySchema {
		LEAS,
		DBO
	}

	public enum MyConnection {
		OracleTest,
		SqlServerTest
	}

}
