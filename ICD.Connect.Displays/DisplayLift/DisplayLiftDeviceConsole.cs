using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Controls.Power;

namespace ICD.Connect.Displays.DisplayLift
{
	public static class DisplayLiftDeviceConsole
	{
		private static ILoggerService Logger { get { return ServiceProvider.GetService<ILoggerService>(); } }

		public static void BuildConsoleStatus(IDisplayLiftDevice device, AddStatusRowDelegate addRow)
		{
			addRow("Display", device.Display);

			bool isBootDelaying = device.LiftState == eLiftState.BootDelay;
			bool isCooling = device.LiftState == eLiftState.CooldownDelay;

			addRow("Lift State", device.LiftState);

			addRow("Boot Delay", device.BootDelay + "ms");
			if (isBootDelaying)
				addRow("Boot Delay Remaining", device.BootDelayRemaining + "ms");

			addRow("Cooling Delay", device.CoolingDelay + "ms");
			if (isCooling)
				addRow("Cooling Delay Remaining", device.CoolingDelayRemaining + "ms");
		}

		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IDisplayLiftDevice device)
		{
			yield return new ConsoleCommand("Extend", "Extends this lift", () => device.ExtendLift(null));
			yield return new ConsoleCommand("Retract", "Retracts this lift", () => device.RetractLift(null));
			yield return new ConsoleCommand("PowerOn", "Extends the lift and powers the display", () => PowerOn(device));
			yield return new ConsoleCommand("PowerOff", "Powers off the display and retracts the lift", () => PowerOff(device));
			yield return new ParamsConsoleCommand("SetBootDelay", "Sets a new boot delay, in ms", p => SetBootDelay(device, p));
			yield return
				new ParamsConsoleCommand("SetCoolingDelay", "Sets a new cooling delay, in ms", p => SetCoolingDelay(device, p));
		}

		private static void PowerOn(IDisplayLiftDevice device)
		{
			if (device.Display == null)
				throw new InvalidOperationException("Lift has no associated Display");

			IPowerDeviceControl control = device.Display.Controls.GetControl<IPowerDeviceControl>();
			if (control == null)
				return;

			control.PowerOn();
		}

		private static void PowerOff(IDisplayLiftDevice device)
		{
			if (device.Display == null)
				throw new InvalidOperationException("Lift has no associated Display");

			IPowerDeviceControl control = device.Display.Controls.GetControl<IPowerDeviceControl>();
			if (control == null)
				return;

			control.PowerOff();
		}

		private static void SetBootDelay(IDisplayLiftDevice device, string[] parameters)
		{
			int delayLength;
			if (parameters.Length == 0 || !StringUtils.TryParse(parameters[0], out delayLength) || delayLength < 0)
			{
				Logger.AddEntry(eSeverity.Warning, "Could not set boot delay. Please specify a valid delay length.");
				return;
			}

			device.BootDelay = delayLength;
		}

		private static void SetCoolingDelay(IDisplayLiftDevice device, string[] parameters)
		{
			int delayLength;
			if (parameters.Length == 0 || !StringUtils.TryParse(parameters[0], out delayLength) || delayLength < 0)
			{
				Logger.AddEntry(eSeverity.Warning, "Could not set cooling delay. Please specify a valid delay length.");
				return;
			}

			device.CoolingDelay = delayLength;
		}

		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IDisplayLiftDevice device)
		{
			yield break;
		}
	}
}
