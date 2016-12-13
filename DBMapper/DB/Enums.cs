using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DBMapper {
	//Primary key, timestamp, property mapli autoset column ve diğer table columnlar için
	//ValueProvider değerine göre yapılacaklar:
	//(update'de primary key sqle koy denen yerlerde..vsvsvsvs
	//App = 1, //property değeriyle input dbparameter yarat, sqle kolonu koy
	//AppFunctionDelegate = 2, //delegate invoke et, bu değerle inputoutput dbparameter yarat, sqle kolonu koy, sqlde return et/property set et
	//Sequence = 3, //boş değerle inputoutput dbparameter yarat, sqle sequence koy, sqlde return et/property set et
	//DBFunction = 4, //boş değerle inputoutput dbparameter yarat, sqle dbfunction koy, sqlde return et/property set et
	//DBTriggerredAutoValue = 5 //boş değerle inputoutput dbparameter yarat, sqle kolonu koyma, sqlde return et/property set et

	//Property mapsiz autoset columnlar için
	//ValueProvider deperlerine göre yapılacaklar:
	//AppFunctionDelegate = 2, //delegate invoke, bu değerle input dbparameter yarat, sqle kolonu koy
	//DBFunction = 4, //sqle dbfunction koy
	//DBTriggerredAutoValue = 5 //hiçbirşey yapma
	
	//Bu en geneli, bunu dışarıya sadece readonly olarak aç, map işlemini diğer enumlarla yaptır..
	public enum ValueProvider {
		App = 1,
		AppFunctionDelegate = 2,
		Sequence = 3,
		DBFunction = 4,
		DBTriggerredAutoValue = 5
	}

	//kod ve mapping değiştirmeden db vendor switch edilmek isteniyorsa, özel kolon değerleri set ederken 3 seçenek var
	//1) dblerin desteklediği özelliklerin ortak kesiti kullanılmalı, 
	//2) app tarafından veya db trigger/autoincrement/timestamp-rowversion vs gibi uygulama dışında db tarafından halledilmeli
	//(bu iki seçenek için mappingler ValueProvider vermeden gayet sadeleşiyor ve tavsiye edilen bu iki seçenek, özellikle performans açısından ikincisi..)
	//3)entity bazında PrimaryKeyValueProvider ve TimestampValueProvider ve autoset koon bazında 
	//  iki db vendorun farklı farklı desteklediği özelliklerin ikisi de birlikte map edilmeli ve 
	//  entity ve autoset kolon bazında ValueProvider lar tanımlanmalı
	public enum PrimaryKeyValueProvider {
		App = 1,
		AppFunctionDelegate = 2,
		Sequence = 3,
		DBFunction = 4,
		DBTriggerredAutoValue = 5
	}

	public enum TimestampValueProvider {
		AppFunctionDelegate = 2,
		DBFunction = 4,
		DBTriggerredAutoValue = 5
	}

	public enum AutoSetValueProvider {
		AppFunctionDelegate = 2,
		DBFunction = 4,
		DBTriggerredAutoValue = 5
	}

	public enum Before {
		Insert,
		Update,
		LogicalDelete,
	}

	public enum FillBackCommandMode {
		Insert,
		Update,
		LogicalDelete,
	}

	public enum When {
		AfterSelectForEachRow,
		BeforeInsertForEachRow,
		AfterInsertForEachRow,
		BeforeUpdateForEachRow,
		AfterUpdateForEachRow,
		BeforeDeleteForEachRow,
		AfterDeleteForEachRow,
	}

	public enum DBVendor {
		Oracle,
		Microsoft
	}

}
