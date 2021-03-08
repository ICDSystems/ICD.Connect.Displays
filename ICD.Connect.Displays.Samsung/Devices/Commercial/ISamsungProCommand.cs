using System.Collections.Generic;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public interface ISamsungProCommand : ISerialData
	{
		byte Command { get; }

		byte Id { get; }
	}

	public class SamsungProCommandEqualityComparer : IEqualityComparer<ISamsungProCommand>
	{
		public bool Equals(ISamsungProCommand x, ISamsungProCommand y)
		{
			if (x == null || y == null)
				return false;

			// If one is a query and the other is not, the commands are different.
			if (x.GetType() != y.GetType())
				return false;

			if (x.Id != y.Id)
				return false;

			// Are the command types the same?
			if (x.Command != y.Command)
				return false;

			SamsungProCommand commandX = x as SamsungProCommand;
			SamsungProCommand commandY = y as SamsungProCommand;

			if (commandX == null || commandY == null)
				return true;

			return commandX.Data == commandY.Data;
		}

		public int GetHashCode(ISamsungProCommand obj)
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + obj.Id.GetHashCode();
				hash = hash * 23 + obj.Command.GetHashCode();

				SamsungProCommand command = obj as SamsungProCommand;
				if (command != null)
					hash = hash * 23 + command.Data.GetHashCode();

				return hash;
			}
		}
	}
}