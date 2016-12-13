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
using System.Configuration;
using System.Threading;

namespace Test {
	class Program {

		static void Main(string[] args) {

			//for (int i = 0; i < 10; i++) {
			//  Thread thread = new Thread(ts);
			//  thread.Start();
			//}

			//Console.ReadKey();
			//return;


			//DB.ConnectionStringSettingsCollection = ConfigurationManager.ConnectionStrings;

			string iMapConnectionDllPath = ConfigurationManager.AppSettings["IMapConnectionDllPath"];
			string[] iMapConnectionDllPaths = iMapConnectionDllPath.Split(new char[] { ';' });

			foreach (string path in iMapConnectionDllPaths) {
				Assembly assembly = Assembly.LoadFrom(path);
				foreach (Type type in assembly.GetExportedTypes()) {
					if (type.GetInterface("DBMapper.IMapConnection") != null) {
						IMapConnection mapper = (IMapConnection)Activator.CreateInstance(type);
						mapper.MapConnection();
					}
				}
			}

			string iMapEntityDllPath = ConfigurationManager.AppSettings["IMapEntityDllPath"];
			string[] iMapEntityDllPaths = iMapEntityDllPath.Split(new char[] { ';' });

			foreach (string path in iMapEntityDllPaths) {
				Assembly assembly = Assembly.LoadFrom(path);
				foreach (Type type in assembly.GetExportedTypes()) {
					if (type.GetInterface("DBMapper.IMapEntity") != null) {
						IMapEntity mapper = (IMapEntity)Activator.CreateInstance(type);
						mapper.MapEntity();
					}
				}
			}

			try {
				Map.GenerateCommands();
			}
			catch (Exception ex) {
				throw;
			}

			Console.WriteLine("Mapping complete.");

			//servis çağrısı simule et..
			Assembly businessAssembly = Assembly.LoadFrom(@"..\..\..\Business\bin\Debug\Business.dll");
			foreach (Type type in businessAssembly.GetExportedTypes())
				if (type.FullName == "Business.TestBusiness") {
					object businessObject = Activator.CreateInstance(type);
					type.InvokeMember("RunTests", BindingFlags.InvokeMethod, null, businessObject, null);
				}

			//Console.WriteLine("Tests done.");
			//Console.ReadKey();
		}

	}

}
