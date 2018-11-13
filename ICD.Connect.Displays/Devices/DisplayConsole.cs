using System;
using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Displays.EventArguments;

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

			addRow("Powered", instance.IsPowered);
			addRow("Active Input", instance.ActiveInput);
			addRow("Scaling Mode", instance.ScalingMode);
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

			yield return new ConsoleCommand("PowerOn", "Turns on the display", () => instance.PowerOn());
			yield return new ConsoleCommand("PowerOff", "Turns off the display", () => instance.PowerOff());
			yield return new GenericConsoleCommand<int>("SetActiveInput", "SetActiveInput <ADDRESS>", i => instance.SetActiveInput(i));
			yield return new EnumConsoleCommand<eScalingMode>("SetScalingMode", a => instance.SetScalingMode(a));
		}
	}
}
