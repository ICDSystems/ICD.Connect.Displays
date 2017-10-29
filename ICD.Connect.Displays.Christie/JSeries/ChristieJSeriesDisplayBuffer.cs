using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Christie.JSeries
{
	public sealed class ChristieJSeriesDisplayBuffer : ISerialBuffer
	{
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly Queue<string> m_Queue;
		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

		private string m_RxData;

		/// <summary>
		/// Constructor.
		/// </summary>
		public ChristieJSeriesDisplayBuffer()
		{
			m_RxData = string.Empty;
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
				m_RxData = string.Empty;
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
							OnCompletedSerial.Raise(this, new StringEventArgs(m_RxData));
							m_RxData = string.Empty;
						}

						escape = c == '\\';
					}
				}
			}
			finally
			{
				m_ParseSection.Leave();
			}
		}
	}
}
