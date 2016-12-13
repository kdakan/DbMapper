using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBMapper {
	public class ActiveRecord {
		public static IList<T> Select<T>(Enum queryName, params InputParameter[] inputParameters) where T : class, new() {
			return DB.Select<T>(queryName, inputParameters);
		}

		public static T SelectFirst<T>(Enum queryName, params InputParameter[] inputParameters) where T : class, new() {
			return DB.SelectFirst<T>(queryName, inputParameters);
		}

		public void Insert() {
			DB.Insert(this);
		}

		public void Update(Enum queryName) {
			DB.Update(this, queryName);
		}

		public void Delete() {
			DB.Delete(this);
		}
	}

}
