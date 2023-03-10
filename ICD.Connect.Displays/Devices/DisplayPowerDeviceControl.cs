using ICD.Connect.Devices.Controls.Power;
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
        }

        /// <summary>
        /// Powers on the device.
        /// </summary>
        protected override void PowerOnFinal()
        {
            Parent.PowerOn();
        }

        /// <summary>
        /// Powers off the device.
        /// </summary>
        protected override void PowerOffFinal()
        {
            Parent.PowerOff();
        }

        #region Parent Callbacks

        /// <summary>
        /// Subscribe to the parent events.
        /// </summary>
        /// <param name="parent"></param>
        protected override void Subscribe(IDisplay parent)
        {
            parent.OnPowerStateChanged += ParentOnPowerStateChanged;
        }

        /// <summary>
        /// Unsubscribe from the parent events.
        /// </summary>
        /// <param name="parent"></param>
        protected override void Unsubscribe(IDisplay parent)
        {
            parent.OnPowerStateChanged -= ParentOnPowerStateChanged;
        }

        /// <summary>
        /// Called when the parent power state changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void ParentOnPowerStateChanged(object sender, DisplayPowerStateApiEventArgs eventArgs)
        {
            SetPowerState(eventArgs.Data.PowerState, eventArgs.Data.ExpectedDuration);
        }

	    #endregion
    }
}
