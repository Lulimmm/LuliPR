using System.Runtime.CompilerServices;

namespace Mew.FeintPlan
{
	public class FeintNodeInfo
	{
		public uint Id { get; set/*init*/; }

		public uint ZoneId { get; set/*init*/; }

		public string Name { get; set/*init*/; } = "";

		public string Description { get; set/*init*/; } = "";

		public int TimeInSec { get; set/*init*/; }

		public string Phase { get; set/*init*/; } = "";

		public IFeintNode Node { get; set/*init*/; }

		public uint SerialId { get; set/*init*/; }

		public string SequenceName { get; set/*init*/; } = "";

		public int DisplayOrder { get; set/*init*/; }

		public bool EnabledByDefault { get; set/*init*/; } = true;

		public uint BaseId { get; set/*init*/; }
	}
}
