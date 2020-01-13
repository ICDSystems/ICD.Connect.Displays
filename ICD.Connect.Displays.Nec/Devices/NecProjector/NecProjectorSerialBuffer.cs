using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Nec.Devices.NecProjector
{
	public sealed class NecProjectorSerialBuffer : ISerialBuffer
	{
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		#region Fields

		private readonly StringBuilder m_RxData;
		private readonly Queue<string> m_Queue;

		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

		private readonly NecProjector m_Parent;

		#endregion

		#region Constructors

		public NecProjectorSerialBuffer(NecProjector parent)
		{
			m_Parent = parent;

			m_RxData = new StringBuilder();
			m_Queue = new Queue<string>();

			m_QueueSection = new SafeCriticalSection();
			m_ParseSection = new SafeCriticalSection();
		}

		#endregion

		/// <summary>
		/// Enqueues the serial data.
		/// </summary>
		/// <param name="data"></param>
		public void Enqueue(string data)
		{
			m_QueueSection.Execute(() => m_Queue.Enqueue(data));
			Parse();
		}

		/// <summary>
		/// Clears all queued data in the buffer.
		/// </summary>
		public void Clear()
		{
			m_ParseSection.Enter();
			m_QueueSection.Enter();

			try
			{
				m_RxData.Clear();
				m_Queue.Clear();
			}
			finally
			{
				m_ParseSection.Leave();
				m_QueueSection.Leave();
			}
		}

		private void Parse()
		{
			if (!m_ParseSection.TryEnter())
				return;

			try
			{
				string data = null;
				while (m_QueueSection.Execute(() => m_Queue.Dequeue(out data)))
				{
					foreach (char c in data)
					{
						m_RxData.Append(c);

						if (!IsComplete(m_RxData.ToString()))
						{
							// If the length is < 2, or we have an expected length,
							// just wait for the command
							if (m_RxData.Length < NecProjectorCommand.MinimumResponseLength || NecProjectorCommand.GetResponseLengthFromHeaders(m_RxData.ToString()).HasValue)
								continue;

							// If we get here, the command length is unknown when it should be known
							// Likely have unexpected/junk in the buffer, let's try to remove it.
							
							m_Parent.Log(eSeverity.Warning, "Unknown response in buffer, scrubbing: {0}", StringUtils.ToHexLiteral(m_RxData.ToString()));

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

						string output = m_RxData.Pop();
						OnCompletedSerial.Raise(this, new StringEventArgs(output));
					}
				}
			}
			finally
			{
				m_ParseSection.Leave();
			}
		}

		private static bool IsComplete(string data)
		{
			int? expectedLength = NecProjectorCommand.GetResponseLengthFromHeaders(data);

			return expectedLength.HasValue && data.Length >= expectedLength.Value;
		}
	}
}
