using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bubblegum
{
	public static class Logging
	{
		private static Mutex _Mutex = new Mutex();

		private static void WriteTime()
		{
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write("[{0}] ", DateTime.Now.ToString("HH:mm:ss"));
		}

		private static void LogLine(string key, ConsoleColor color, string str, object[] args)
		{
			_Mutex.WaitOne();
			WriteTime();
			Console.ForegroundColor = color;
			Console.Write("[" + key + "] " + str, args);
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine();
			_Mutex.ReleaseMutex();
		}

		public static void Info(string str, params object[] args)
		{
			LogLine("INFO", ConsoleColor.White, str, args);
		}

		public static void Warn(string str, params object[] args)
		{
			LogLine("WARN", ConsoleColor.Yellow, str, args);
		}

		public static void Error(string str, params object[] args)
		{
			LogLine("EROR", ConsoleColor.Red, str, args);
		}

		public static void Debug(string str, params object[] args)
		{
			LogLine("DEBG", ConsoleColor.Red, str, args);
		}
	}
}
