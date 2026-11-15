using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Mew.FeintPlan
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class FeintNodeAttribute : Attribute
	{
		public string Name { get; }

		public string Description { get; }

		public int TimeInSec { get; }

		public string Phase { get; }

		public uint SerialId { get; set; }

		public uint BaseId { get; }

		public int Order { get; set; }

		public int DisplayOrder { get; set; }

		public string SequenceName { get; set; } = "";

		public FeintNodeAttribute(string name, string description = "", string phase = "", uint baseId = 0u)
		{
			Name = name;
			Description = description;
			Phase = phase;
			BaseId = baseId;
			TimeInSec = ParseTimeFromDescription(description);
			SerialId = ParseSerialIdFromName(name);
		}

		private static uint ParseSerialIdFromName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return 0u;
			}
			int hashIndex = name.LastIndexOf('#');
			if (hashIndex >= 0 && hashIndex < name.Length - 1 && uint.TryParse(name.Substring(hashIndex + 1), out var serialId))
			{
				return serialId;
			}
			return 0u;
		}

		private static int ParseTimeFromDescription(string description)
		{
			if (string.IsNullOrEmpty(description))
			{
				return 0;
			}
			Match match = Regex.Match(description, "(\\d+):(\\d+)");
			if (match.Success)
			{
				int num = int.Parse(match.Groups[1].Value);
				int seconds = int.Parse(match.Groups[2].Value);
				return num * 60 + seconds;
			}
			return 0;
		}
	}
}
