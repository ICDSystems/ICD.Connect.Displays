using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Christie.Devices
{
	public sealed class ChristieDisplayBuffer : ISerialBuffer
	{
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly StringBuilder m_RxData;
		private readonly Queue<string> m_Queue;
		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

		private static readonly char[] s_Headers =
		{
			ChristieDisplay.RESPONSE_SUCCESS,
			ChristieDisplay.RESPONSE_ERROR,
			ChristieDisplay.RESPONSE_BAD_COMMAND,
			ChristieDisplay.RESPONSE_DATA_REPLY
		};

		/// <summary>
		/// Constructor.
		/// </summary>
		public ChristieDisplayBuffer()
		{
			m_RxData = new StringBuilder();
			m_Queue = new Queue<string>();

			m_QueueSection = new SafeCriticalSection();
			m_ParseSection = new SafeCriticalSection();
		}

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

		/// <summary>
		/// Searches the enqueued serial data for the delimiter character.
		/// Complete strings are raised via the OnCompletedString event.
		/// </summary>
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
						bool isHeader = s_Headers.Contains(c);

						// Have to start with a header
						if (m_RxData.Length == 0 || !isHeader)
							continue;

						// We hit a second header
						if (m_RxData.Length > 0)
							OnCompletedSerial.Raise(this, new StringEventArgs(m_RxData.Pop()));

						m_RxData.Append(c);

						if (!IsComplete(m_RxData.ToString()))
							continue;

						OnCompletedSerial.Raise(this, new StringEventArgs(m_RxData.Pop()));
					}
				}
			}
			finally
			{
				m_ParseSection.Leave();
			}
		}

		/// <summary>
		/// Returns true if data is a complete response.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private static bool IsComplete(string data)
		{
			IcdConsole.PrintLine(StringUtils.ToHexLiteral(data));

			switch (data.First())
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
