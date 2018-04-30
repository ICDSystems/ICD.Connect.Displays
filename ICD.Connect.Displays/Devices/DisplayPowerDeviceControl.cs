using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays.Devices
{
	public sealed class DisplayPowerDeviceControl : AbstractPowerDeviceControl<IDisplay>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public DisplayPowerDeviceControl(IDisplay parent, int id)
			: base(parent, id)
		{
			Subscribe(parent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			Unsubscribe(Parent);

			base.DisposeFinal(disposing);
		}

		/// <summary>
		/// Powers on the device.
		/// </summary>
		public override void PowerOn()
		{
			Parent.PowerOn();
		}

		/// <summary>
		/// Powers off the device.
		/// </summary>
		public override void PowerOff()
		{
			Parent.PowerOff();
		}

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		private void Subscribe(IDisplay parent)
		{
			parent.OnIsPoweredChanged += ParentOnIsPoweredChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		private void Unsubscribe(IDisplay parent)
		{
			parent.OnIsPoweredChanged -= ParentOnIsPoweredChanged;
		}

		/// <summary>
		/// Called when the parent power state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ParentOnIsPoweredChanged(object sender, DisplayPowerStateApiEventArgs eventArgs)
		{
			IsPowered = Parent.IsPowered;
		}

		#endregion
	}
}
