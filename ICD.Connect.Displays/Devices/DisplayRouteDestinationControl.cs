using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Services;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.RoutingGraphs;

namespace ICD.Connect.Displays.Devices
{
	/// <summary>
	/// Simple IRouteDestinationControl for IDisplays.
	/// </summary>
	public sealed class DisplayRouteDestinationControl : AbstractRouteInputSelectControl<IDisplay>
	{
		/// <summary>
		/// Raised when an input source status changes.
		/// </summary>
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;

		private IRoutingGraph m_CachedRoutingGraph;

		/// <summary>
		/// Gets the routing graph.
		/// </summary>
		public IRoutingGraph RoutingGraph
		{
			get { return m_CachedRoutingGraph = m_CachedRoutingGraph ?? ServiceProvider.GetService<IRoutingGraph>(); }
		}

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
		public void SetActiveInput(int? input)
		{
			if (input.HasValue)
				Parent.SetActiveInput(input.Value);
		}

		/// <summary>
		/// Sets the current active input.
		/// </summary>
		public override void SetActiveInput(int? input, eConnectionType type)
		{
			SetActiveInput(input);
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
		/// Gets the input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override ConnectorInfo GetInput(int input)
		{
			Connection connection = RoutingGraph.Connections.GetInputConnection(new EndpointInfo(Parent.Id, Id, input));
			if (connection == null)
				throw new ArgumentOutOfRangeException("input");

			return new ConnectorInfo(connection.Destination.Address, connection.ConnectionType);
		}

		/// <summary>
		/// Returns true if the destination contains an input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override bool ContainsInput(int input)
		{
			return RoutingGraph.Connections.GetInputConnection(new EndpointInfo(Parent.Id, Id, input)) != null;
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			return RoutingGraph.Connections
			                   .GetInputConnections(Parent.Id, Id)
			                   .Select(c => new ConnectorInfo(c.Destination.Address, c.ConnectionType));
		}

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		private void Subscribe(IDisplay parent)
		{
			parent.OnActiveInputChanged += ParentOnActiveInputChanged;
			parent.OnPowerStateChanged += ParentOnPowerStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		private void Unsubscribe(IDisplay parent)
		{
			parent.OnActiveInputChanged -= ParentOnActiveInputChanged;
			parent.OnPowerStateChanged -= ParentOnPowerStateChanged;
		}

		/// <summary>
		/// Called when the parent is powered on/off.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ParentOnPowerStateChanged(object sender, DisplayPowerStateApiEventArgs args)
		{
			UpdateInputState();
		}

		/// <summary>
		/// Called when the parent switches HDMI inputs.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ParentOnActiveInputChanged(object sender, DisplayInputApiEventArgs args)
		{
			UpdateInputState();
		}

		private void UpdateInputState()
		{
			int? activeInput = Parent.PowerState != ePowerState.PowerOff ? Parent.ActiveInput : null;
			SetCachedActiveInput(activeInput, eConnectionType.Audio | eConnectionType.Video);
		}

		#endregion
	}
}
