using AEAssist.Extension;
using System.Runtime.CompilerServices;
using AEAssist;
using AEAssist.CombatRoutine.Trigger;
using AEAssist.GUI;

namespace Ninja.PR.Utilities.Triggers
{
	public class TriggerCondition_Ninki : ITriggerCond, ITriggerBase
	{
		[LabelName("忍气")]
		public int Red { get; set; }

		public string DisplayName => "忍气";

		public string Remark { get; set; }

		public bool Draw()
		{
			return false;
		}

		public bool Handle(ITriggerCondParams triggerCondParams)
		{
			return Core.Resolve<JobApi_Ninja>().Ninki >= Red;
		}
	}
}
