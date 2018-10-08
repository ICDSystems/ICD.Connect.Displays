using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Sharp.Devices.Commercial
{
	public sealed class SharpProSerialBuffer : ISerialBuffer
	{
		private readonly MultiDelimiterSerialBuffer m_Buffer;

		public event EventHandler<StringEventArgs> OnCompletedSerial;

		/// <summary>
		/// Constructor.
		/// </summary>
		public SharpProSerialBuffer()
		{
			m_Buffer = new MultiDelimiterSerialBuffer('\r', '\n');
			m_Buffer.OnCompletedSerial += BufferOnCompletedSerial;
		}

		/// <summary>
		/// Enqueues the serial data.
		/// </summary>
		/// <param name="data"></param>
		public void Enqueue(string data)
		{
			m_Buffer.Enqueue(data);
		}

		/// <summary>
		/// Clears all queued data in the buffer.
		/// </summary>
		public void Clear()
		{
			m_Buffer.Clear();
		}

		private void BufferOnCompletedSerial(object sender, StringEventArgs stringEventArgs)
		{
			string data = stringEventArgs.Data;

			// Ignore the "wait" response
			if (data == "WAIT")
				return;

			OnCompletedSerial.Raise(this, new StringEventArgs(data));
		}
	}
}
