using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Mew.FeintPlan
{
	public class FeintPreset
	{
		public string Name { get; set; } = "";

		public List<string> Nodes { get; set; } = new List<string>();

		public List<FeintPresetNode> PhaseNodes { get; set; } = new List<FeintPresetNode>();
	}
}
