using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp; // For Basic SIMPL# Classes
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
        private readonly List<byte> m_RxData;
        private readonly SafeCriticalSection m_QueueSection;
        private readonly SafeCriticalSection m_ParseSection;

        public PanasonicDisplaySerialBuffer()
        {
            m_Queue = new Queue<string>();
            m_RxData = new List<byte>();
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
            {
                return;
            }
            try
            {
                string data = string.Empty;
                while (m_QueueSection.Execute(() => m_Queue.Dequeue(out data)))
                {
                    byte[] bytes = StringUtils.ToBytes(data);
                    m_RxData.AddRange(bytes);

                    while (m_RxData.Any() && m_RxData[0] != 0x02)
                    {
                        m_RxData.RemoveAt(0);
                    }

                    if (!m_RxData.Any())
                    {
                        continue;
                    }

                    int idx;
                    for (idx = 1; idx < m_RxData.Count; idx++)
                    {
                        if (m_RxData[idx] != 0x03)
                        {
                            continue;
                        }
                        var command = Encoding.ASCII.GetString(m_RxData.ToArray(), 0, idx + 1);
                        OnCompletedSerial.Raise(this, new StringEventArgs(command));
                        m_RxData.RemoveRange(0, idx + 1);
                        break;
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
