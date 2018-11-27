using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Samsung.Devices.Consumer
{
	/// <summary>
	/// The Samsung display only ever returns one of two things: success or failure.
	/// </summary>
	public sealed class SamsungDisplaySerialBuffer : ISerialBuffer
	{
		/// <summary>
		/// Raised when a complete message has been buffered.
		/// </summary>
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		/// <summary>
		/// We don't get an ack or nack for power on, instead we get a series of status
		/// messages that don't fit any particular pattern.
		/// Raising this event is an attempt to inform the driver that the display is powered on.
		/// </summary>
		public event EventHandler OnJunkData; 

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
						// Clear leading nonsense
						if (c == SamsungDisplay.SUCCESS[0] || c == SamsungDisplay.FAILURE[0])
							m_RxData.Clear();

						m_RxData.Append(c);
						
						if (m_RxData.Length != 3)
							continue;

						string serial = m_RxData.Pop();

						if (serial == SamsungDisplay.SUCCESS || serial == SamsungDisplay.FAILURE)
							OnCompletedSerial.Raise(this, new StringEventArgs(serial));
						else
							OnJunkData.Raise(this);
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
