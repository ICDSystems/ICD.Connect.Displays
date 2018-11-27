using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.API.EventArguments;
using ICD.Connect.Displays.Proxies;

namespace ICD.Connect.Displays.EventArguments
{
	[Serializable]
	public sealed class DisplayInputState
	{
		public int Input { get; set; }
		public bool Active { get; set; }
	}

	public sealed class DisplayInputApiEventArgs : AbstractGenericApiEventArgs<DisplayInputState>
	{
		/// <summary>
		/// Gets the input address for this event.
		/// </summary>
		[PublicAPI]
		public int Input { get { return Data.Input; } }

		/// <summary>
		/// Gets the active state of the input.
		/// </summary>
		[PublicAPI]
		public bool Active { get { return Data.Active; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="active"></param>
		public DisplayInputApiEventArgs(int input, bool active)
			: base(DisplayApi.EVENT_ACTIVE_INPUT, new DisplayInputState { Input = input, Active = active})
		{
		}

		/// <summary>
		/// Override to add additional properties to the string representation.
		/// </summary>
		/// <param name="addProperty"></param>
		protected override void ToString(AddReprPropertyDelegate addProperty)
		{
			base.ToString(addProperty);

			addProperty("Input", Input);
			addProperty("Active", Active);
		}
	}
}
