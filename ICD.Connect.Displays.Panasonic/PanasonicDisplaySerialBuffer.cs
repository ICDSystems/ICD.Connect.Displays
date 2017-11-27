using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Panasonic
{
	public sealed class PanasonicDisplaySerialBuffer : ISerialBuffer
	{
		public event EventHandler<StringEventArgs> OnCompletedSerial;

		private readonly Queue<string> m_Queue;
		private string m_RxData;
		private readonly SafeCriticalSection m_QueueSection;
		private readonly SafeCriticalSection m_ParseSection;

		public PanasonicDisplaySerialBuffer()
		{
			m_Queue = new Queue<string>();
			m_RxData = string.Empty;
			m_QueueSection = new SafeCriticalSection();
			m_ParseSection = new SafeCriticalSection();
		}

		public void Enqueue(string data)
		{
			m_QueueSection.Execute(() => m_Queue.Enqueue(data));
			Parse();
		}

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

		private void Parse()
		{
			if (!m_ParseSection.TryEnter())
				return;

			try
			{
				string data = string.Empty;
				while (m_QueueSection.Execute(() => m_Queue.Dequeue(out data)))
				{
				    m_RxData += data;

				    while (true)
				    {
				        // Find the header
				        int firstHeader = m_RxData.IndexOf((char)0x02);
				        if (firstHeader < 0)
				        {
				            m_RxData = string.Empty;
				            break;
				        }

				        if (firstHeader > 0)
				            m_RxData = m_RxData.Substring(firstHeader);

                        // Find the footer
				        int firstFooter = m_RxData.IndexOf((char)0x03);
				        if (firstFooter < 0)
				            break;

				        string command = m_RxData.Substring(0, firstFooter + 1);
                        OnCompletedSerial.Raise(this, new StringEventArgs(command));
                        
				        m_RxData = m_RxData.Substring(firstFooter + 1);
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
