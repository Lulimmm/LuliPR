using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mew.FeintPlan.Events;
using Mew.FeintPlan.Scripts.DMU;
using Mew.FeintPlan.Scripts.FRU;

namespace Mew.FeintPlan
{
	public static class FeintZoneConfig
	{
		public static readonly Dictionary<string, uint> ZoneIds = new Dictionary<string, uint>
		{
			{ "M9S", 1321u },
			{ "M10S", 1323u },
			{ "M11S", 1325u },
			{ "M12S", 1327u },
			{ "FRU", 1238u },
			{ "TOP", 1122u },
			{ "DMU", 1363u }
		};

		public static readonly Dictionary<uint, Func<IMewFeintEvent?, string?>> PhaseEntryDetectors = new Dictionary<uint, Func<IMewFeintEvent?, string?>> {
		{
			ZoneIds["DMU"],
			DMUData.DetectPhaseEntry
		},
		{
			ZoneIds["FRU"],
			FRUData.DetectPhaseEntry
		} };

		public static bool HasPhaseEntryDetector(uint zoneId)
		{
			return PhaseEntryDetectors.ContainsKey(zoneId);
		}

		public static string? DetectPhaseEntry(uint zoneId, IMewFeintEvent? condParams)
		{
			if (!PhaseEntryDetectors.TryGetValue(zoneId, out var detector))
			{
				return null;
			}
			return detector(condParams);
		}
	}
}
