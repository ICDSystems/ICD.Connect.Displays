using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Christie.Devices
{
	public sealed class ChristieDisplayBuffer : AbstractSerialBuffer
	{
		private static readonly IcdHashSet<char> s_Headers = new IcdHashSet<char>
		{
			ChristieDisplay.RESPONSE_SUCCESS,
			ChristieDisplay.RESPONSE_ERROR,
			ChristieDisplay.RESPONSE_BAD_COMMAND,
			ChristieDisplay.RESPONSE_DATA_REPLY
		};

		private readonly StringBuilder m_RxData;

		/// <summary>
		/// Constructor.
		/// </summary>
		public ChristieDisplayBuffer()
		{
			m_RxData = new StringBuilder();
		}

		/// <summary>
		/// Override to clear any current state.
		/// </summary>
		protected override void ClearFinal()
		{
			m_RxData.Clear();
		}

		/// <summary>
		/// Override to process the given item for chunking.
		/// </summary>
		/// <param name="data"></param>
		protected override IEnumerable<string> Process(string data)
		{
			foreach (char c in data)
			{
				bool isHeader = s_Headers.Contains(c);

				// Have to start with a header
				if (m_RxData.Length == 0 && !isHeader)
					continue;

				// We hit a second header
				if (m_RxData.Length > 0 && isHeader)
					yield return m_RxData.Pop();

				m_RxData.Append(c);

				if (!IsComplete(m_RxData.ToString()))
					continue;

				yield return m_RxData.Pop();
			}
		}

		/// <summary>
		/// Returns true if data is a complete response.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private static bool IsComplete(string data)
		{
			if (string.IsNullOrEmpty(data))
				return false;

			switch (data[0])
			{
				case ChristieDisplay.RESPONSE_SUCCESS:
				case ChristieDisplay.RESPONSE_BAD_COMMAND:
					return data.Length >= 1;

				case ChristieDisplay.RESPONSE_DATA_REPLY:
				case ChristieDisplay.RESPONSE_ERROR:
					return data.Length >= 3;

				default:
					return false;
			}
		}
	}
}
