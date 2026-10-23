using AEAssist.Extension;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AEAssist;
using AEAssist.Helper;

namespace Ninja.PR.Methods
{
	public class NinjaUpdater
	{
		public static void UpdateFiles()
		{
			string baseDir = Share.CurrentDirectory;
			if (string.IsNullOrEmpty(baseDir))
			{
				LogHelper.Print("无法获取当前目录");
				return;
			}
			string timelinesDir = Path.Combine(baseDir, "..", "..", "ACR", "Congratulation", "NinjaTimelines");
			string triggerlinesDir = Path.Combine(baseDir, "..", "..", "Triggerlines");
			timelinesDir = Path.GetFullPath(timelinesDir);
			triggerlinesDir = Path.GetFullPath(triggerlinesDir);
			if (!Directory.Exists(timelinesDir))
			{
				LogHelper.Print("Timelines 目录不存在: " + timelinesDir);
				return;
			}
			if (!Directory.Exists(triggerlinesDir))
			{
				LogHelper.Print("Triggerlines 目录不存在: " + triggerlinesDir);
				return;
			}
			try
			{
				string[] files = Directory.GetFiles(timelinesDir);
				foreach (string timelineFilePath in files)
				{
					string timelineFileName = Path.GetFileName(timelineFilePath);
					string prefix = GetPrefix(timelineFileName);
					int timelineVersion = GetVersionNumber(timelineFileName);
					string[] matchingTriggerFiles = Directory.GetFiles(triggerlinesDir, prefix + "*");
					if (matchingTriggerFiles.Length == 0)
					{
						string destinationPath = Path.Combine(triggerlinesDir, timelineFileName);
						File.Copy(timelineFilePath, destinationPath, overwrite: true);
						LogHelper.Print("为您安装了伊甸时间轴: " + timelineFileName);
						continue;
					}
					bool updated = false;
					string[] array = matchingTriggerFiles;
					for (int j = 0; j < array.Length; j++)
					{
						int triggerVersion = GetVersionNumber(Path.GetFileName(array[j]));
						if (timelineVersion <= triggerVersion)
						{
							continue;
						}
						string[] array2 = matchingTriggerFiles;
						foreach (string fileToDelete in array2)
						{
							try
							{
								File.Delete(fileToDelete);
								LogHelper.Print("删除旧时间轴: " + Path.GetFileName(fileToDelete));
							}
							catch (Exception ex)
							{
								LogHelper.Print("删除文件 " + fileToDelete + " 时发生异常: " + ex.Message);
							}
						}
						string destinationPath2 = Path.Combine(triggerlinesDir, timelineFileName);
						File.Copy(timelineFilePath, destinationPath2, overwrite: true);
						LogHelper.Print("检测到时间轴更新，已更新时间轴: " + timelineFileName);
						updated = true;
						break;
					}
					if (!updated)
					{
						LogHelper.Print("无需更新: " + prefix);
					}
				}
			}
			catch (Exception ex2)
			{
				LogHelper.Print("发生错误: " + ex2.Message);
			}
		}

		private static string GetPrefix(string fileName)
		{
			int verIndex = fileName.IndexOf("_Ver", StringComparison.OrdinalIgnoreCase);
			if (verIndex >= 0)
			{
				return fileName.Substring(0, verIndex);
			}
			return Path.GetFileNameWithoutExtension(fileName);
		}

		private static int GetVersionNumber(string fileName)
		{
			Match match = new Regex("_Ver(\\d+)", RegexOptions.IgnoreCase).Match(fileName);
			if (match.Success && int.TryParse(match.Groups[1].Value, out var version))
			{
				return version;
			}
			return 0;
		}
	}
}
