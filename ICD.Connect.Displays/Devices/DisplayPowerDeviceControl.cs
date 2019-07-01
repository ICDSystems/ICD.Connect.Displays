using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays.Devices
{
    public sealed class DisplayPowerDeviceControl : AbstractPowerDeviceControl<IDisplay>
    {
        public delegate void PrePowerOnDelegate();

        public delegate void PostPowerOffDelegate();

        public PrePowerOnDelegate   PrePowerOn   { get; set; }
        public PostPowerOffDelegate PostPowerOff { get; set; }

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
            if (PrePowerOn != null)
                PrePowerOn.Invoke();
            else
                Parent.PowerOn();
        }

        public void PowerOn(bool bypassPrePowerOn)
        {
            if(bypassPrePowerOn)
                Parent.PowerOn();
            else
                PowerOn();
        }

        /// <summary>
        /// Powers off the device.
        /// </summary>
        public override void PowerOff()
        {
            Parent.PowerOff();

            if (PostPowerOff != null)
                PostPowerOff.Invoke();
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
