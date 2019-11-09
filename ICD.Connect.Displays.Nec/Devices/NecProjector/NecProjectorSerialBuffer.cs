using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
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

		#endregion

		#region Constructors

		public NecProjectorSerialBuffer()
		{
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

						//todo - if lentgh >= 2 and ExpectedLength is null, remove char from front of string
						if (!IsComplete(m_RxData.ToString()))
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

		private static bool IsComplete(string data)
		{
			int? expectedLength = NecProjectorCommand.GetResponseLengthFromHeaders(data);

			return expectedLength.HasValue && data.Length >= expectedLength.Value;
		}
	}
}
