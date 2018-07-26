using System.Linq;
using System.Text;

namespace ICD.Connect.Displays.Samsung
{
	public struct SamsungProResponse
	{
		private readonly byte[] m_Bytes;

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
		/// The command result.
		/// </summary>
		public byte[] Values
		{
			get
			{
				int length = m_Bytes[3] - 2;
				byte[] output = new byte[length];

				for (int index = 0; index < length; index++)
					output[index] = m_Bytes[6 + index];

				return output;
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

				// Getting unexpected messages with header 0xAA 0xE1
				if (m_Bytes[1] != 0xFF)
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
