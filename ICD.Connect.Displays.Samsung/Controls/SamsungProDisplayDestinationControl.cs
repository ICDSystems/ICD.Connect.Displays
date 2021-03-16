using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices.Controls.Power;
using ICD.Connect.Displays.EventArguments;
using ICD.Connect.Displays.Samsung.Devices.Commercial;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.Controls.Streaming;
using ICD.Connect.Routing.Endpoints;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.RoutingGraphs;

namespace ICD.Connect.Displays.Samsung.Controls
{
	public sealed class SamsungProDisplayDestinationControl : AbstractRouteInputSelectControl<ISamsungProDisplay>, IStreamRouteDestinationControl
	{
		#region Events

		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;
		public event EventHandler<StreamUriEventArgs> OnInputStreamUriChanged;

		#endregion

		public const int URL_LAUNCHER_INPUT = 100;

		private IRoutingGraph m_CachedRoutingGraph;

		/// <summary>
		/// Gets the routing graph.
		/// </summary>
		public IRoutingGraph RoutingGraph
		{
			get { return m_CachedRoutingGraph = m_CachedRoutingGraph ?? ServiceProvider.GetService<IRoutingGraph>(); }
		}

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public SamsungProDisplayDestinationControl(ISamsungProDisplay parent, int id)
			: base(parent, id)
		{
		}

		#endregion

		#region Methods

		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			return true;
		}

		public override ConnectorInfo GetInput(int input)
		{
			Connection connection = RoutingGraph.Connections.GetInputConnection(new EndpointInfo(Parent.Id, Id, input));
			if (connection == null)
				throw new ArgumentOutOfRangeException("input");

			return new ConnectorInfo(connection.Destination.Address, connection.ConnectionType);
		}

		public override bool ContainsInput(int input)
		{
			return RoutingGraph.Connections.GetInputConnection(new EndpointInfo(Parent.Id, Id, input)) != null;
		}

		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			return RoutingGraph.Connections
			                   .GetInputConnections(Parent.Id, Id)
			                   .Select(c => new ConnectorInfo(c.Destination.Address, c.ConnectionType));
		}

		public override void SetActiveInput(int? input, eConnectionType type)
		{
			SetActiveInput(input);
		}

		/// <summary>
		/// Sets the current active input.
		/// </summary>
		/// <param name="input"></param>
		public void SetActiveInput(int? input)
		{
			if (input.HasValue)
				Parent.SetActiveInput(input.Value);
		}

		#region URL Launcher

		public bool SetStreamForInput(int input, Uri stream)
		{
			if (input != URL_LAUNCHER_INPUT)
				return false;

			Parent.SetUrlLauncherSource(stream);

			return true;
		}

		public Uri GetStreamForInput(int input)
		{
			return input != URL_LAUNCHER_INPUT ? null : Parent.LauncherUri;
		}

		#endregion

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(ISamsungProDisplay parent)
		{
			base.Subscribe(parent);

			parent.OnActiveInputChanged += ParentOnActiveInputChanged;
			parent.OnPowerStateChanged += ParentOnPowerStateChanged;
			parent.OnUrlLauncherSourceChanged += ParentOnUrlLauncherSourceChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(ISamsungProDisplay parent)
		{
			base.Unsubscribe(parent);

			parent.OnActiveInputChanged -= ParentOnActiveInputChanged;
			parent.OnPowerStateChanged -= ParentOnPowerStateChanged;
			parent.OnUrlLauncherSourceChanged -= ParentOnUrlLauncherSourceChanged;
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

		private void ParentOnUrlLauncherSourceChanged(object sender, GenericEventArgs<Uri> args)
		{
			OnInputStreamUriChanged.Raise(this,
			                              new StreamUriEventArgs(eConnectionType.Audio | eConnectionType.Video,
			                                                     URL_LAUNCHER_INPUT, args.Data));
		}

		private void UpdateInputState()
		{
			int? activeInput = Parent.PowerState != ePowerState.PowerOff ? Parent.ActiveInput : null;
			SetCachedActiveInput(activeInput, eConnectionType.Audio | eConnectionType.Video);
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			StreamRouteDestinationControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in StreamRouteDestinationControlConsole.GetConsoleCommands(this))
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
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in StreamRouteDestinationControlConsole.GetConsoleNodes(this))
				yield return node;
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
