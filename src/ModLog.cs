using System;
using System.IO;
using UnityEngine;

namespace DuckovBetterActionCancel
{
	internal static class ModLog
	{
		private const string FileName = "DuckovBetterActionCancel.log";
		private const string Prefix = "[BetterActionCancel]";
		private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";

		private static bool sessionStarted;

		internal static void Info(string message)
		{
			Write("INFO", message, LogType.Log);
		}

		internal static void Warn(string message)
		{
			Write("WARN", message, LogType.Warning);
		}

		internal static void Error(string message)
		{
			Write("ERROR", message, LogType.Error);
		}

		private static void Write(string level, string message, LogType logType)
		{
			string text = Prefix + " [" + level + "] " + message;

			try
			{
				switch (logType)
				{
					case LogType.Warning:
						Debug.LogWarning(text);
						break;
					case LogType.Error:
						Debug.LogError(text);
						break;
					default:
						Debug.Log(text);
						break;
				}
			}
			catch
			{
			}

			try
			{
				string directory = Path.GetDirectoryName(typeof(ModLog).Assembly.Location);
				if (string.IsNullOrEmpty(directory))
				{
					return;
				}

				string line = "[" + DateTime.Now.ToString(TimestampFormat) + "] [" + level + "] " + message + Environment.NewLine;
				string path = Path.Combine(directory, FileName);

				if (sessionStarted)
				{
					File.AppendAllText(path, line);
					return;
				}

				File.WriteAllText(path, line);
				sessionStarted = true;
			}
			catch
			{
			}
		}
	}
}
