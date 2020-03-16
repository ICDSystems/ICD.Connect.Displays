using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Displays.DisplayLift
{
	public static class RelayDisplayLiftConsole
	{
		public static void BuildConsoleStatus(RelayDisplayLiftDevice device, AddStatusRowDelegate addRow)
		{
			addRow("Extend Relay", device.ExtendRelay);
			addRow("Retract Relay", device.RetractRelay);
			addRow("Extend Time", device.ExtendTime + "ms");
			addRow("Retract Time", device.RetractTime + "ms");
			addRow("Latch Mode", device.LatchRelay ? "Latch Mode" : "Unlatched Mode");
		}

		public static IEnumerable<IConsoleCommand> GetConsoleCommands(RelayDisplayLiftDevice device)
		{
			yield return new ParamsConsoleCommand("SetBootDelay", "Sets a new boot delay, in ms", p => SetExtendTime(device, p));
			yield return
				new ParamsConsoleCommand("SetCoolingDelay", "Sets a new cooling delay, in ms", p => SetRetractTime(device, p));
			yield return new ConsoleCommand("EnableLatchedMode",
			                                "Sets the relays to latched mode, meaning they will be held closed until the direction is reversed.",
			                                () => SetLatchedMode(device, true));

			yield return new ConsoleCommand("DisableLatchedMode",
			                                "Sets the relays to unlatched mode, meaning they will be held closed for a set time.",
			                                () => SetLatchedMode(device, false));
		}

		private static void SetLatchedMode(RelayDisplayLiftDevice device, bool enable)
		{
			device.LatchRelay = enable;
		}

		private static string SetExtendTime(RelayDisplayLiftDevice device, string[] parameters)
		{
			int delayLength;
			if (parameters.Length == 0 || !StringUtils.TryParse(parameters[0], out delayLength) || delayLength < 0)
				return "Could not set extend time. Please specify a valid time in ms.";

			device.ExtendTime = delayLength;

			return "Extend time set to " + delayLength;
		}

		private static string SetRetractTime(RelayDisplayLiftDevice device, string[] parameters)
		{
			int delayLength;
			if (parameters.Length == 0 || !StringUtils.TryParse(parameters[0], out delayLength) || delayLength < 0)
				return "Could not set retract time. Please specify a valid time in ms.";

			device.RetractTime = delayLength;

			return "Retract time set to " + delayLength;
		}
	}
}
