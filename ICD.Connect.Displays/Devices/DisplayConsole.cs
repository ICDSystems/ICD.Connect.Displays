using System;
using System.Collections.Generic;
using ICD.Common.Utils;
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
			addRow("Hdmi Input", instance.HdmiInput);
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

			string hdmiRange = StringUtils.RangeFormat(1, instance.InputCount);
			yield return new GenericConsoleCommand<int>("SetHdmiInput", "SetHdmiInput x " + hdmiRange, i => instance.SetHdmiInput(i));

			yield return new EnumConsoleCommand<eScalingMode>("SetScalingMode", a => instance.SetScalingMode(a));
		}
	}
}
