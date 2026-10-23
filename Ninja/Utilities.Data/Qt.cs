using AEAssist.Extension;
using System.Runtime.CompilerServices;

namespace Ninja.PR.Utilities.Data
{
	public static class Qt
	{
		public static bool Get(string qtName)
		{
			return NinjaRotationEntry.QT.GetQt(qtName);
		}

		public static bool Set(string qtName, bool qtValue)
		{
			return NinjaRotationEntry.QT.SetQt(qtName, qtValue);
		}
	}
}
