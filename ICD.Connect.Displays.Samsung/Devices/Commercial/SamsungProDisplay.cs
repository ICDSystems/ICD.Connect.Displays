using ICD.Common.Properties;
using ICD.Connect.API.Nodes;
using ICD.Connect.Settings;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public sealed class SamsungProDisplay : AbstractSamsungProDisplay<SamsungProDisplaySettings>
	{
		#region Properties

		/// <summary>
		/// Gets/sets the ID of this tv.
		/// </summary>
		[PublicAPI]
		public byte WallId { get; set; }

		#endregion

		#region Methods

		protected override byte GetWallIdForPowerCommand()
		{
			return WallId;
		}

		protected override byte GetWallIdForInputCommand()
		{
			return WallId;
		}

		protected override byte GetWallIdForVolumeCommand()
		{
			return WallId;
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			WallId = 0;
		}

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(SamsungProDisplaySettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.WallId = WallId;
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(SamsungProDisplaySettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			WallId = settings.WallId;
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

			addRow("Wall ID", WallId);
		}

		#endregion
	}
}