using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Devices.Controls.Power;

namespace ICD.Connect.Displays.Devices
{
	public static class DisplayConsole
	{
		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleNodeBase> GetConsoleNodes(IDisplay instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="addRow"></param>
		public static void BuildConsoleStatus(IDisplay instance, AddStatusRowDelegate addRow)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			addRow("Powered", instance.PowerState);
			addRow("Active Input", instance.ActiveInput);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		public static IEnumerable<IConsoleCommand> GetConsoleCommands(IDisplay instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");

			yield return new ConsoleCommand("PowerOn", "Turns on the display", () => ConsolePowerOn(instance));
			yield return new ConsoleCommand("PowerOff", "Turns off the display", () => ConsolePowerOff(instance));
			yield return new GenericConsoleCommand<int>("SetActiveInput", "SetActiveInput <ADDRESS>", i => instance.SetActiveInput(i));
		}

		private static void ConsolePowerOn(IDisplay instance)
		{
			if(instance == null)
				return;

			IPowerDeviceControl powerControl = instance.Controls.GetControl<IPowerDeviceControl>();
			if(powerControl == null)
				return;
			
			powerControl.PowerOn();
		}
		
		private static void ConsolePowerOff(IDisplay instance)
		{
			if(instance == null)
				return;

			IPowerDeviceControl powerControl = instance.Controls.GetControl<IPowerDeviceControl>();
			if(powerControl == null)
				return;
			
			powerControl.PowerOff();
		}
	}
}
