using System;
using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;

namespace ICD.Connect.Displays.Samsung.Devices.Consumer
{
	/// <summary>
	/// The Samsung display only ever returns one of two things: success or failure.
	/// </summary>
	public sealed class SamsungDisplaySerialBuffer : AbstractSerialBuffer
	{
		/// <summary>
		/// We don't get an ack or nack for power on, instead we get a series of status
		/// messages that don't fit any particular pattern.
		/// Raising this event is an attempt to inform the driver that the display is powered on.
		/// </summary>
		public event EventHandler OnJunkData; 

		private readonly StringBuilder m_RxData;

		/// <summary>
		/// Constructor.
		/// </summary>
		public SamsungDisplaySerialBuffer()
		{
			m_RxData = new StringBuilder();
		}

		/// <summary>
		/// Override to clear any current state.
		/// </summary>
		protected override void ClearFinal()
		{
			m_RxData.Clear();
		}

		/// <summary>
		/// Override to process the given item for chunking.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		protected override IEnumerable<string> Process(string data)
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
					yield return serial;
				else
					OnJunkData.Raise(this);
			}
		}
	}
}
