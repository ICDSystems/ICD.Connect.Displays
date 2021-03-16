using System.Linq;
using System.Text;
using ICD.Common.Utils.Collections;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public struct SamsungProResponse
	{
		private readonly byte[] m_Bytes;

		private static readonly IcdHashSet<byte> s_CommandsWithSubCommands = new IcdHashSet<byte>
		{
			0xC7,
			0x1B
		};

		public byte Header { get { return m_Bytes[0]; } }

		public byte Code { get { return m_Bytes[1]; } }

		/// <summary>
		/// Gets the id of the display.
		/// </summary>
		public byte Id { get { return m_Bytes[2]; } }

		/// <summary>
		/// Returns true if the command was successful.
		/// </summary>
		public bool Success { get { return (char)m_Bytes[4] == 'A'; } }

		/// <summary>
		/// The command code.
		/// </summary>
		public byte Command { get { return m_Bytes[5]; } }

		/// <summary>
		/// The sub command code. Only applicable for certain commands.
		/// </summary>
		public byte Subcommand { get { return s_CommandsWithSubCommands.Contains(Command) ? m_Bytes[6] : (byte)0; } }

		/// <summary>
		/// The command result.
		/// </summary>
		public byte[] Values
		{
			get
			{
				// Responses with a sub-command byte shift the index of the value by 1
				if (s_CommandsWithSubCommands.Contains(Command))
				{
					int length = m_Bytes[3] - 3;
					byte[] output = new byte[length];

					for (int index = 0; index < length; index++)
						output[index] = m_Bytes[7 + index];

					return output;
				}
				else
				{
					int length = m_Bytes[3] - 2;
					byte[] output = new byte[length];

					for (int index = 0; index < length; index++)
						output[index] = m_Bytes[6 + index];

					return output;
				}
			}
		}

		/// <summary>
		/// Returns true if the checksum is valid.
		/// </summary>
		public bool IsValid
		{
			get
			{
				if (m_Bytes.Length < 7)
					return false;

				byte expected = m_Bytes[m_Bytes.Length - 1];
				byte actual = AbstractSamsungProCommand.GetCheckSum(m_Bytes.Take(m_Bytes.Length - 1));

				return expected == actual;
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="response"></param>
		public SamsungProResponse(string response)
		{
			m_Bytes = Encoding.GetEncoding(28591).GetBytes(response);
		}
	}
}
