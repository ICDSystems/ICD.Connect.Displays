using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.EventArguments;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Samsung
{
	/// <summary>
	/// The Samsung display only ever returns one of two things: success or failure.
	/// </summary>
	public sealed class SamsungDisplaySerialBuffer : ISerialBuffer
	{
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly StringBuilder m_RxData;
		private readonly Queue<string> m_Queue;

		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public SamsungDisplaySerialBuffer()
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
					// Success and failure are both 3 character codes
					foreach (char c in data)
					{
						m_RxData.Append(c);
						if (m_RxData.Length > 3)
							m_RxData.Remove(0, 1);

						if (m_RxData.Length != 3)
							continue;

						string serial = m_RxData.ToString();
						if (serial != SamsungDisplay.SUCCESS && serial != SamsungDisplay.FAILURE)
							continue;

						m_RxData.Clear();
						OnCompletedSerial.Raise(this, new StringEventArgs(serial));
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
