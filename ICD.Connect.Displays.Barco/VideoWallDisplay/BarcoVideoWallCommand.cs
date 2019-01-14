using System;
using System.Text;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Displays.Barco.VideoWallDisplay
{
// ReSharper disable UnusedMember.Global
	public enum eCommandKeyword
	{
		Get,
		Set
	};

	public enum eCommand
	{
		OpState,
		ActiveInput,
		SelInput,
		Tiling,
		DisplayMode,
// ReSharper disable InconsistentNaming
		EDID,
		EDIDList,
// ReSharper restore InconsistentNaming
		BezCorr,
		DispOrientation,
		WallSize,
		WallModules,
		TargetBrightness,
		TargetBrightnesRange
	};
// ReSharper restore UnusedMember.Global

	public sealed class BarcoVideoWallCommand : ISerialData, IEquatable<BarcoVideoWallCommand>
	{
		public string WallDisplayId { get; set; }

		public eCommandKeyword CommandKeyword { get; set; }

		public eCommand Command { get; set; }

		public string Device { get; set; }

		public string Attribute { get; set; }

		/// <summary>
		/// Serialize this command to a string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			StringBuilder cmd = new StringBuilder();
			cmd.AppendFormat("{1}{0}{2}{0}{3}",BarcoVideoWallDisplay.DELIMITER, WallDisplayId, CommandKeyword, Command);
			if (!String.IsNullOrEmpty(Device))
			{
				cmd.AppendFormat("{0}{1}", BarcoVideoWallDisplay.DELIMITER, Device);
				if (!String.IsNullOrEmpty(Attribute))
					cmd.AppendFormat("{0}{1}",BarcoVideoWallDisplay.DELIMITER, Attribute);
			}
			cmd.Append(BarcoVideoWallDisplay.TERMINATOR);
			return cmd.ToString();
		}

		public bool Equals(BarcoVideoWallCommand other)
		{
			return other != null && (other.WallDisplayId == WallDisplayId && other.CommandKeyword == CommandKeyword && other.Command == Command &&
			                         String.Equals(other.Device, Device, StringComparison.OrdinalIgnoreCase));
		}

		public static bool Equals(BarcoVideoWallCommand a, BarcoVideoWallCommand b)
		{
			return a.Equals(b);
		}
	}
}