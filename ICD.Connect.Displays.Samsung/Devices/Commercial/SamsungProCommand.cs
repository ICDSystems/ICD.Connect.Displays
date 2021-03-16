using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public abstract class AbstractSamsungProCommand : ISamsungProCommand
	{
		public const byte ID_ALL = 0xFE;
		public const byte HEADER = 0xAA;

		private readonly byte m_Command;
		private readonly byte? m_SubCommand;
		private readonly byte m_Id;

		/// <summary>
		/// Gets the command code.
		/// </summary>
		public byte Command { get { return m_Command; } }

		/// <summary>
		/// Gets the sub-command data.
		/// </summary>
		public byte? SubCommand { get { return m_SubCommand; } }

		/// <summary>
		/// Gets the command id.
		/// </summary>
		public byte Id { get { return m_Id; } }

		/// <summary>
		/// Creates the command for the given id.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="id"></param>
		/// <param name="subCommand"></param>
		protected AbstractSamsungProCommand(byte command, byte id, byte? subCommand)
		{
			m_Command = command;
			m_SubCommand = subCommand;
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
		private readonly byte[] m_Data;

		/// <summary>
		/// Gets the command data.
		/// </summary>
		public byte[] Data { get { return m_Data; } }

		/// <summary>
		/// Returns the length of the data plus the optional subcommand byte.
		/// </summary>
		private byte CommandLength
		{
			get
			{
				int output = Data.Length;
				if (SubCommand != null)
					output += 1;

				return (byte)output;
			}
		}

		/// <summary>
		/// Creates the command for the given id.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="id"></param>
		/// <param name="data"></param>
		public SamsungProCommand(byte command, byte id, byte data)
			: this(command, id, null, new[] {data})
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="id"></param>
		/// <param name="subCommand"></param>
		/// <param name="data"></param>
		public SamsungProCommand(byte command, byte id, byte? subCommand, byte[] data)
			: base(command, id, subCommand)
		{
			m_Data = data;
		}

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public override string Serialize()
		{
			List<byte> bytes = new List<byte>
			{
				HEADER,
				Command,
				Id,
				CommandLength
			};

			if (SubCommand.HasValue)
				bytes.Add(SubCommand.Value);

			bytes.AddRange(Data);

			byte checksum = GetCheckSum(bytes);
			bytes.Add(checksum);

			return StringUtils.ToString(bytes);
		}

		/// <summary>
		/// Converts this command to a query.
		/// </summary>
		/// <returns></returns>
		public SamsungProQuery ToQuery()
		{
			return new SamsungProQuery(Command, Id, SubCommand);
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
			: this(command, id, null)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="id"></param>
		/// <param name="subCommand"></param>
		public SamsungProQuery(byte command, byte id, byte? subCommand)
			: base(command, id, subCommand)
		{
		}

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public override string Serialize()
		{
			List<byte> bytes = new List<byte>
			{
				HEADER,
				Command,
				Id,
				// Command length
				SubCommand.HasValue ? (byte)1 : (byte)0
			};

			if (SubCommand.HasValue)
				bytes.Add(SubCommand.Value);

			byte checksum = GetCheckSum(bytes);
			bytes.Add(checksum);

			return StringUtils.ToString(bytes);
		}
	}
}
