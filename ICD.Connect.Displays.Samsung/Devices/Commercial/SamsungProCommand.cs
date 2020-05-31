using System.Collections.Generic;
using System.Linq;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public abstract class AbstractSamsungProCommand : ISamsungProCommand
	{
		public const byte ID_ALL = 0xFE;
		public const byte HEADER = 0xAA;

		private readonly byte m_Command;
		private readonly byte m_Id;

		/// <summary>
		/// Gets the command code.
		/// </summary>
		public byte Command { get { return m_Command; } }

		/// <summary>
		/// Gets the command id.
		/// </summary>
		public byte Id { get { return m_Id; } }

		/// <summary>
		/// Creates the command for the given id.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="id"></param>
		protected AbstractSamsungProCommand(byte command, byte id)
		{
			m_Command = command;
			m_Id = id;
		}

		/// <summary>
		/// Calculates the checksum for the data.
		/// 
		/// All communications take place in hexadecimals. The checksum is calculated by adding up all
		/// values except the header. If a checksum adds up to be more than 2 digits as shown below
		/// (11+FF+01+01=112), the first digit is removed.
		/// E.g. Power On & ID=0
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static byte GetCheckSum(IEnumerable<byte> bytes)
		{
			int sum = bytes.Skip(1).Sum(b => b);

			while (sum > byte.MaxValue)
				sum -= byte.MaxValue + 1;

			return (byte)sum;
		}

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public abstract string Serialize();
	}

	public sealed class SamsungProCommand : AbstractSamsungProCommand
	{
		private readonly byte m_Data;

		/// <summary>
		/// Gets the command data.
		/// </summary>
		public byte Data { get { return m_Data; } }

		/// <summary>
		/// Creates the command for the given id.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="id"></param>
		/// <param name="data"></param>
		public SamsungProCommand(byte command, byte id, byte data)
			: base(command, id)
		{
			m_Data = data;
		}

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public override string Serialize()
		{
			byte[] bytes = {HEADER, Command, Id, 0x01, Data};
			byte checksum = GetCheckSum(bytes);

			return new string(bytes.Select(b => (char)b).ToArray()) + (char)checksum;
		}

		/// <summary>
		/// Converts this command to a query.
		/// </summary>
		/// <returns></returns>
		public SamsungProQuery ToQuery()
		{
			return new SamsungProQuery(Command, Id);
		}
	}

	public sealed class SamsungProQuery : AbstractSamsungProCommand
	{
		/// <summary>
		/// Creates the command for the given id.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="id"></param>
		public SamsungProQuery(byte command, byte id)
			: base(command, id)
		{
		}

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public override string Serialize()
		{
			byte[] bytes = {HEADER, Command, Id, 0x00};
			byte checksum = GetCheckSum(bytes);

			return new string(bytes.Select(b => (char)b).ToArray()) + (char)checksum;
		}
	}
}
