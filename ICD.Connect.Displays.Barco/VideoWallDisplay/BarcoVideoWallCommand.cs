using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Displays.Barco.VideoWallDisplay
{
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
		EDID,
		EDIDList,
		BezCorr,
		DispOrientation,
		WallSize,
		WallModules,
		TargetBrightness,
		TargetBrightnesRange
	};

	public class BarcoVideoWallCommand : ISerialData, IEquatable<BarcoVideoWallCommand>
	{
		public string WallDisplayId { get; set; }

		public eCommandKeyword CommandKeyword { get; set; }

		public eCommand Command { get; set; }

		public string Device { get; set; }

		public string Attribute { get; set; }

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			StringBuilder cmd = new StringBuilder();
			cmd.AppendFormat("{0} {1} {2}", WallDisplayId, CommandKeyword, Command);
			if (!String.IsNullOrEmpty(Device))
			{
				cmd.AppendFormat(" {0}",Device);
				if (!String.IsNullOrEmpty(Attribute))
					cmd.AppendFormat(" {0}", Attribute);
			}
			cmd.Append(BarcoVideoWallDisplay.TERMINATOR);
			return cmd.ToString();
		}

		public bool Equals(BarcoVideoWallCommand other)
		{
			return other.WallDisplayId == WallDisplayId && other.CommandKeyword == CommandKeyword && other.Command == Command &&
			       String.Equals(other.Device, Device, StringComparison.OrdinalIgnoreCase);
		}

		public static bool Equals(BarcoVideoWallCommand a, BarcoVideoWallCommand b)
		{
			return a.Equals(b);
		}
	}
}