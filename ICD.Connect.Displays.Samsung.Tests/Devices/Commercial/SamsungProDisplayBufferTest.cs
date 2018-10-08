using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Displays.Samsung.Devices.Commercial;
using NUnit.Framework;

namespace ICD.Connect.Displays.Samsung.Tests.Devices.Commercial
{
	[TestFixture]
	public sealed class SamsungProDisplayBufferTest
	{
		[Test]
		public void Enqueue()
		{
			List<StringEventArgs> feedback = new List<StringEventArgs>();

			SamsungProDisplayBuffer buffer = new SamsungProDisplayBuffer();
			buffer.OnCompletedSerial += (sender, args) => feedback.Add(args);

			// Power off
			buffer.Enqueue("\xAA\xFF\x00\x03\x41\x11\x00\x54");
			Assert.AreEqual(1, feedback.Count);
			Assert.AreEqual("\xAA\xFF\x00\x03\x41\x11\x00\x54", feedback[0].Data);
			feedback.Clear();

			// Cooldown ?
			buffer.Enqueue("\xAA\xE1\x00\x03\xA1\x21\x01\xA7");
			Assert.AreEqual(1, feedback.Count);
			Assert.AreEqual("\xAA\xE1\x00\x03\xA1\x21\x01\xA7", feedback[0].Data);
			feedback.Clear();

			// Sometimes get a rogue 0x00 after power off
			buffer.Enqueue("\x00");
			Assert.AreEqual(0, feedback.Count);

			// Power on
			buffer.Enqueue("\xAA\xFF\x00\x03\x41\x11\x01\x55");
			Assert.AreEqual(1, feedback.Count);
			Assert.AreEqual("\xAA\xFF\x00\x03\x41\x11\x01\x55", feedback[0].Data);
			feedback.Clear();

			// Warmup ?
			buffer.Enqueue("\xFF\x1C\xC4\x8C\xD6\xC6\xF9");
			Assert.AreEqual(1, feedback.Count);
			Assert.AreEqual("\xFF\x1C\xC4\x8C\xD6\xC6\xF9", feedback[0].Data);
			feedback.Clear();

			// Input query
			buffer.Enqueue("\xAA\xFF\x00\x03\x41\x14\x22\x79");
			Assert.AreEqual(1, feedback.Count);
			Assert.AreEqual("\xAA\xFF\x00\x03\x41\x14\x22\x79", feedback[0].Data);
			feedback.Clear();
		}

		[Test]
		public void Clear()
		{
			Assert.Inconclusive();
		}
	}
}
