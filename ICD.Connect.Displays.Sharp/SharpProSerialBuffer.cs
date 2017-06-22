using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.EventArguments;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Sharp
{
	public sealed class SharpProSerialBuffer : ISerialBuffer
	{
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly StringBuilder m_RxData;
		private readonly SafeCriticalSection m_ParseSection;

		private readonly Queue<string> m_Queue;
		private readonly SafeCriticalSection m_QueueSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		public SharpProSerialBuffer()
		{
			m_RxData = new StringBuilder();
			m_ParseSection = new SafeCriticalSection();

			m_Queue = new Queue<string>();
			m_QueueSection = new SafeCriticalSection();
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
				m_Queue.Clear();
				m_RxData.Clear();
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
						m_RxData.Append(c);

						string stringData = m_RxData.ToString();

						// Ignore the "wait" response
						stringData = stringData.Replace("WAIT" + SharpProDisplay.RETURN, string.Empty);

						if (string.IsNullOrEmpty(stringData) || !stringData.EndsWith(SharpProDisplay.RETURN))
							continue;

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
	}
}
