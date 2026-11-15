using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Mew.FeintPlan
{
	public class ZoneFeintPlan
	{
		public uint ZoneId { get; set; }

		public List<string> EnabledNodes { get; set; } = new List<string>();

		public bool Enabled { get; set; } = true;

		public bool IsNodeEnabled(string phase, uint nodeId)
		{
			string key = new FeintNodeKey(phase, nodeId).ToString();
			return EnabledNodes.Contains(key);
		}

		public void SetNodeEnabled(string phase, uint nodeId, bool enabled)
		{
			string key = new FeintNodeKey(phase, nodeId).ToString();
			if (enabled && !EnabledNodes.Contains(key))
			{
				EnabledNodes.Add(key);
			}
			else if (!enabled)
			{
				EnabledNodes.Remove(key);
			}
		}

		public IEnumerable<FeintNodeKey> GetEnabledNodeKeys()
		{
			return EnabledNodes.Select(FeintNodeKey.Parse);
		}
	}
}
