using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.EventArguments;

namespace ICD.Connect.Displays.Devices
{
	/// <summary>
	/// Simple IRouteDestinationControl for IDisplays.
	/// </summary>
	public sealed class DisplayRouteDestinationControl : AbstractRouteInputSelectControl<IDisplay>
	{
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public DisplayRouteDestinationControl(IDisplay parent, int id)
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
			OnSourceDetectionStateChange = null;

			base.DisposeFinal(disposing);

			Unsubscribe(Parent);
		}

		#region Methods

		/// <summary>
		/// Sets the current active input.
		/// </summary>
		/// <param name="input"></param>
		public override void SetActiveInput(int? input)
		{
			if (input.HasValue)
				Parent.SetHdmiInput(input.Value);
		}

		/// <summary>
		/// Returns true if a signal is detected at the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			return true;
		}

		/// <summary>
		/// Returns the true if the input is actively being used by the source device.
		/// For example, a display might true if the input is currently on screen,
		/// while a switcher may return true if the input is currently routed.
		/// </summary>
		public override bool GetInputActiveState(int input, eConnectionType type)
		{
			return ActiveInput == input;
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			return Enumerable.Range(1, Parent.InputCount)
			                 .Select(i => new ConnectorInfo(i, eConnectionType.Audio | eConnectionType.Video));
		}

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		private void Subscribe(IDisplay parent)
		{
			parent.OnHdmiInputChanged += ParentOnHdmiInputChanged;
			parent.OnIsPoweredChanged += ParentOnIsPoweredChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		private void Unsubscribe(IDisplay parent)
		{
			parent.OnHdmiInputChanged -= ParentOnHdmiInputChanged;
			parent.OnIsPoweredChanged -= ParentOnIsPoweredChanged;
		}

		/// <summary>
		/// Called when the parent is powered on/off.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ParentOnIsPoweredChanged(object sender, DisplayPowerStateApiEventArgs args)
		{
			UpdateInputState();
		}

		/// <summary>
		/// Called when the parent switches HDMI inputs.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ParentOnHdmiInputChanged(object sender, DisplayHmdiInputApiEventArgs args)
		{
			UpdateInputState();
		}

		private void UpdateInputState()
		{
			ActiveInput = Parent.IsPowered ? Parent.HdmiInput : null;
		}

		#endregion
	}
}
