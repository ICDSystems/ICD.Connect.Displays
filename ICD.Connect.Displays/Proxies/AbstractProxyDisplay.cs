using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Attributes;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Proxies;
using ICD.Connect.Displays.Devices;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays.Proxies
{
	public abstract class AbstractProxyDisplay : AbstractProxyDevice, IProxyDisplay
	{
		public event EventHandler<BoolEventArgs> OnIsPoweredChanged;

		public event DisplayHdmiInputDelegate OnHdmiInputChanged;

		public event EventHandler<ScalingModeEventArgs> OnScalingModeChanged;

		[ApiProperty("IsPowered", "Gets the powered state for the display.")]
		public bool IsPowered { get; private set; }

		[ApiProperty("InputCount", "Gets the HDMI input count for the display.")]
		public int InputCount { get; private set; }

		[ApiProperty("HdmiInput", "Gets the current HDMI input for the display.")]
		public int? HdmiInput { get; private set; }

		[ApiProperty("ScalingMode", "Gets the scaling mode for the display.")]
		public eScalingMode ScalingMode { get; private set; }

		[ApiMethod("PowerOn", "Powers the display.")]
		public void PowerOn()
		{
			throw new NotImplementedException();
		}

		[ApiMethod("PowerOff", "Powers off the display.")]
		public void PowerOff()
		{
			throw new NotImplementedException();
		}

		[ApiMethod("SetHdmiInput", "Sets the HDMI input for the display.")]
		public void SetHdmiInput(int address)
		{
			throw new NotImplementedException();
		}

		[ApiMethod("SetScalingMode", "Sets the scaling mode for the display.")]
		public void SetScalingMode(eScalingMode mode)
		{
			throw new NotImplementedException();
		}

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in DisplayConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			DisplayConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in DisplayConsole.GetConsoleCommands(this))
				yield return command;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
