using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Samsung.Devices.Commercial
{
	public sealed class SamsungProDisplayBuffer : AbstractSerialBuffer
	{
		private readonly StringBuilder m_RxData;

		/// <summary>
		/// Constructor.
		/// </summary>
		public SamsungProDisplayBuffer()
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
		/// <returns></returns>
		protected override IEnumerable<string> Process(string data)
		{
			foreach (char c in data)
			{
				// Reached a new header
				if (c == AbstractSamsungProCommand.HEADER)
					m_RxData.Clear();

				m_RxData.Append(c);

				if (IsComplete(m_RxData.ToString()))
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
			// Hack - warmup response doesn't fit the pattern
			if ((data.StartsWith('\xFF') || data.StartsWith('\x1C')) && data.EndsWith('\xF9'))
				return true;

			return new SamsungProResponse(data).IsValid;
		}
	}
}
