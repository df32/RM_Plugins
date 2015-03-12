using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rainmeter
{
	public static class APIExtension
	{
		public static void Log(this API rm, params string[] texts)
		{
			API.Log(API.LogType.Notice, String.Join(" ", texts));
		}

		public static void Print(this API rm, params object[] values)
		{
			var builder = new StringBuilder();
			foreach (var val in values)
			{
				builder.Append(val.ToString());
				builder.Append(", ");
			}
			API.Log(API.LogType.Debug, builder.ToString());
		}

		public static void Error(this API rm, string message)
		{
			API.Log(API.LogType.Error, message);
		}

		public static void Warning(this API rm, string message)
		{
			API.Log(API.LogType.Warning, message);
		}
	}
}
