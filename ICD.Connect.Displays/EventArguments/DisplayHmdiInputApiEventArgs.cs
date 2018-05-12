using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.EventArguments
{
	[Serializable]
	public sealed class DisplayHdmiInputState
	{
		public int HdmiInput { get; set; }
		public bool Active { get; set; }
	}

	public sealed class DisplayHmdiInputApiEventArgs : AbstractGenericApiEventArgs<DisplayHdmiInputState>
	{
		/// <summary>
		/// Gets the HDMI input address for this event.
		/// </summary>
		[PublicAPI]
		public int HdmiInput { get { return Data.HdmiInput; } }

		/// <summary>
		/// Gets the active state of the HDMI input.
		/// </summary>
		[PublicAPI]
		public bool Active { get { return Data.Active; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hdmiInput"></param>
		/// <param name="active"></param>
		public DisplayHmdiInputApiEventArgs(int hdmiInput, bool active)
			: base(DisplayApi.EVENT_HDMI_INPUT, new DisplayHdmiInputState { HdmiInput = hdmiInput, Active = active})
		{
		}

		/// <summary>
		/// Override to add additional properties to the string representation.
		/// </summary>
		/// <param name="addProperty"></param>
		protected override void ToString(AddReprPropertyDelegate addProperty)
		{
			base.ToString(addProperty);

			addProperty("HdmiInput", HdmiInput);
			addProperty("Active", Active);
		}
	}
}
