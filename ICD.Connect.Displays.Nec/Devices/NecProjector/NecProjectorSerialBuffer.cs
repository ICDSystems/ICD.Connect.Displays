using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Nec.Devices.NecProjector
{
	public sealed class NecProjectorSerialBuffer : AbstractSerialBuffer
	{
		private readonly StringBuilder m_RxData;
		private readonly NecProjector m_Parent;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		public NecProjectorSerialBuffer(NecProjector parent)
		{
			m_Parent = parent;

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
				m_RxData.Append(c);

				if (!IsComplete(m_RxData.ToString()))
				{
					// If the length is < 2, or we have an expected length,
					// just wait for the command
					if (m_RxData.Length < NecProjectorCommand.MinimumResponseLength ||
					    NecProjectorCommand.GetResponseLengthFromHeaders(m_RxData.ToString()).HasValue)
						continue;

					// If we get here, the command length is unknown when it should be known
					// Likely have unexpected/junk in the buffer, let's try to remove it.
					m_Parent.Logger.Log(eSeverity.Warning, "Unknown response in buffer, scrubbing: {0}",
					                    StringUtils.ToHexLiteral(m_RxData.ToString()));

					int? expectedLength = null;
					while (m_RxData.Length >= NecProjectorCommand.MinimumResponseLength && expectedLength == null)
					{
						m_RxData.Remove(0, 1);
						expectedLength = NecProjectorCommand.GetResponseLengthFromHeaders(m_RxData.ToString());
					}

					// After clearing out the buffer, see if we now have a complete command.  If not, continue
					if (!IsComplete(m_RxData.ToString()))
						continue;
				}

				yield return m_RxData.Pop();
			}
		}

		private static bool IsComplete(string data)
		{
			return data.Length >= NecProjectorCommand.GetResponseLengthFromHeaders(data);
		}
	}
}
