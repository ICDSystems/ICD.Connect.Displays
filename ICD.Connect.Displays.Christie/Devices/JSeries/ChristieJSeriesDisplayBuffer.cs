using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Christie.Devices.JSeries
{
	public sealed class ChristieJSeriesDisplayBuffer : AbstractSerialBuffer
	{
		private string m_RxData;

		/// <summary>
		/// Constructor.
		/// </summary>
		public ChristieJSeriesDisplayBuffer()
		{
			m_RxData = string.Empty;
		}

		/// <summary>
		/// Override to clear any current state.
		/// </summary>
		protected override void ClearFinal()
		{
			m_RxData = string.Empty;
		}

		/// <summary>
		/// Override to process the given item for chunking.
		/// </summary>
		/// <param name="data"></param>
		protected override IEnumerable<string> Process(string data)
		{
			bool escape = m_RxData.EndsWith('\\');

			foreach (char c in data)
			{
				// Data starts with a (
				if (m_RxData.Length == 0 && c != '(')
					continue;

				m_RxData += c;

				// Data ends with a )
				if (c == ')' && !escape)
				{
					yield return m_RxData;
					m_RxData = string.Empty;
				}

				escape = c == '\\';
			}
		}
	}
}
