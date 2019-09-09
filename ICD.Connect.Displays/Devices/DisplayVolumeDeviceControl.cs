using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Console.Mute;
using ICD.Connect.Audio.Controls.Mute;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Audio.EventArguments;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Displays.EventArguments;

namespace ICD.Connect.Displays.Devices
{
	public sealed class DisplayVolumeDeviceControl : AbstractVolumeLevelDeviceControl<IDisplayWithAudio>, IVolumeMuteFeedbackDeviceControl
	{
		#region Events

		/// <summary>
		/// Raised when the mute state changes.
		/// </summary>
		public event EventHandler<MuteDeviceMuteStateChangedApiEventArgs> OnMuteStateChanged;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the human readable name for this control.
		/// </summary>
		public override string Name { get { return string.Format("{0} Volume Control", Parent.Name); } }

		/// <summary>
		/// The min volume.
		/// </summary>
		protected override float VolumeRawMinAbsolute { get { return Parent.VolumeDeviceMin; } }

		/// <summary>
		/// The max volume.
		/// </summary>
		protected override float VolumeRawMaxAbsolute { get { return Parent.VolumeDeviceMax; } }

		/// <summary>
		/// Safety Min Volume Set on the device
		/// </summary>
		public override float? VolumeRawMin { get { return Parent.VolumeSafetyMin; }}

		/// <summary>
		/// Safety Max Volume Set on the device
		/// </summary>
		public override float? VolumeRawMax { get { return Parent.VolumeSafetyMax; }}

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		public override float VolumeLevel { get { return Parent.Volume; } }

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool VolumeIsMuted { get { return Parent.IsMuted; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public DisplayVolumeDeviceControl(IDisplayWithAudio parent, int id)
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
			OnMuteStateChanged = null;

			base.DisposeFinal(disposing);

			Unsubscribe(Parent);
		}

		#region Methods

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		public override void SetVolumeLevel(float volume)
		{
			Parent.SetVolume(volume);
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetVolumeMute(bool mute)
		{
			if (mute)
				Parent.MuteOn();
			else
				Parent.MuteOff();
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public void VolumeMuteToggle()
		{
			Parent.MuteToggle();
		}

		/// <summary>
		/// Increments the raw volume once.
		/// </summary>
		public override void VolumeIncrement()
		{
			Parent.VolumeUpIncrement();
		}

		/// <summary>
		/// Decrements the raw volume once.
		/// </summary>
		public override void VolumeDecrement()
		{
			Parent.VolumeDownIncrement();
		}

		protected override bool GetControlAvaliable()
		{
			return Parent.VolumeControlAvaliable;
		}

		#endregion

		#region Parent Callbacks

		protected override void Subscribe(IDisplayWithAudio parent)
		{
			if (parent == null)
				return;

			base.Subscribe(parent);

			parent.OnVolumeChanged += ParentOnVolumeChanged;
			parent.OnMuteStateChanged += ParentOnMuteStateChanged;
			parent.OnVolumeControlAvaliableChanged += ParentOnVolumeControlAvaliableChanged;
		}

		protected override void Unsubscribe(IDisplayWithAudio parent)
		{
			if (parent == null)
				return;

			base.Unsubscribe(parent);

			parent.OnVolumeChanged -= ParentOnVolumeChanged;
			parent.OnMuteStateChanged -= ParentOnMuteStateChanged;
			parent.OnVolumeControlAvaliableChanged -= ParentOnVolumeControlAvaliableChanged;
		}

		private void ParentOnVolumeChanged(object sender, DisplayVolumeApiEventArgs args)
		{
			IPowerDeviceControl senderAsPowerControl = sender as IPowerDeviceControl;
			if (senderAsPowerControl != null && senderAsPowerControl.PowerState == ePowerState.PowerOff)
				return;

			VolumeFeedback(args.Data);
		}

		private void ParentOnMuteStateChanged(object sender, DisplayMuteApiEventArgs args)
		{
			OnMuteStateChanged.Raise(this, new MuteDeviceMuteStateChangedApiEventArgs(args.Data));
		}

		private void ParentOnVolumeControlAvaliableChanged(object sender, DisplayVolumeControlAvaliableApiEventArgs e)
		{
			UpdateCachedControlAvaliable();
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

			VolumeMuteFeedbackDeviceControlConsole.BuildConsoleStatus(this, addRow);
			VolumeMuteDeviceControlConsole.BuildConsoleStatus(this, addRow);
			VolumeMuteBasicDeviceControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in VolumeMuteFeedbackDeviceControlConsole.GetConsoleCommands(this))
				yield return command;

			foreach (IConsoleCommand command in VolumeMuteDeviceControlConsole.GetConsoleCommands(this))
				yield return command;

			foreach (IConsoleCommand command in VolumeMuteBasicDeviceControlConsole.GetConsoleCommands(this))
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
			foreach (IConsoleNodeBase command in GetBaseConsoleNodes())
				yield return command;

			foreach (IConsoleNodeBase command in VolumeMuteFeedbackDeviceControlConsole.GetConsoleNodes(this))
				yield return command;

			foreach (IConsoleNodeBase command in VolumeMuteDeviceControlConsole.GetConsoleNodes(this))
				yield return command;

			foreach (IConsoleNodeBase command in VolumeMuteBasicDeviceControlConsole.GetConsoleNodes(this))
				yield return command;
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
