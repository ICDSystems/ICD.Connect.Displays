using ICD.Connect.API.Nodes;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	
	public sealed class SamsungProVideoWallDisplay : AbstractSamsungProDisplay<SamsungProVideoWallDisplaySettings>
	{

		private const byte ALL_DISPLAYS_WALL_ID = 0xFE;


		private byte? InputWallId { get; set; }
		private byte? VolumeWallId { get; set; }

		protected override byte GetWallIdForPowerCommand()
		{
			return ALL_DISPLAYS_WALL_ID;
		}

		protected override byte GetWallIdForInputCommand()
		{
			return InputWallId ?? ALL_DISPLAYS_WALL_ID;
		}

		protected override byte GetWallIdForVolumeCommand()
		{
			return VolumeWallId ?? ALL_DISPLAYS_WALL_ID;
		}

		protected override byte GetWallIdForScalingCommand()
		{
			return ALL_DISPLAYS_WALL_ID;
		}

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			InputWallId = ALL_DISPLAYS_WALL_ID;
			VolumeWallId = ALL_DISPLAYS_WALL_ID;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SamsungProVideoWallDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.InputWallId = InputWallId;
			settings.VolumeWallId = VolumeWallId;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SamsungProVideoWallDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			InputWallId = settings.InputWallId;
			VolumeWallId = settings.VolumeWallId;
			Trust = true;
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

			addRow("Input Wall ID", InputWallId);
			addRow("Volume Wall ID", VolumeWallId);
		}

		#endregion
	}
}