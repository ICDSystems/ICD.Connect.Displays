using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.LG.DigitalSignage
{
	public sealed class LgDigitalSignageSerialBuffer : AbstractSerialBuffer
	{
		private readonly StringBuilder m_RxData;

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public LgDigitalSignageSerialBuffer()
		{
			m_RxData = new StringBuilder();
		}

		#endregion

		#region Private Methods

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
			while (data.Length > 0)
			{
				int index = data.IndexOfAny(new [] {'x', '\n'});

				// Easy case
				if (index < 0)
				{
					m_RxData.Append(data);
					break;
				}

				char delimiter = data[index];
				string acks = m_RxData.Pop() + data.Substring(0, index + 1);
				data = data.Substring(index + 1);

				switch (delimiter)
                {
					// Command response
					case 'x':
						foreach (Match match in Regex.Matches(acks, LgDigitalSignageAcknowledgement.ACK_REGEX))
							yield return match.Value;
						break;

					// Unsolicited
					case '\n':
						yield return acks.Trim();
						break;
                }
			}
		}

		#endregion
	}
}
