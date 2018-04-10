using ICD.Common.Utils;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.EventArguments
{
	public sealed class DisplayHmdiInputApiEventArgs : AbstractApiEventArgs
	{
		private readonly int m_HdmiInput;
		private readonly bool m_Active;

		/// <summary>
		/// Gets the HDMI input address for this event.
		/// </summary>
		public int HdmiInput { get { return m_HdmiInput; } }

		/// <summary>
		/// Gets the active state of the HDMI input.
		/// </summary>
		public bool Active { get { return m_Active; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hdmiInput"></param>
		/// <param name="active"></param>
		public DisplayHmdiInputApiEventArgs(int hdmiInput, bool active)
			: base(DisplayApi.EVENT_HDMI_INPUT)
		{
			m_HdmiInput = hdmiInput;
			m_Active = active;
		}

		/// <summary>
		/// Override to add additional properties to the string representation.
		/// </summary>
		/// <param name="addProperty"></param>
		protected override void ToString(AddReprPropertyDelegate addProperty)
		{
			base.ToString(addProperty);

			addProperty("HdmiInput", m_HdmiInput);
			addProperty("Active", m_Active);
		}
	}
}
