using System.Collections.Generic;
using System.Linq;
using ICD.Connect.Displays.Christie.Devices;
using NUnit.Framework;

namespace ICD.Connect.Displays.Christie.Tests
{
	[TestFixture]
	public sealed class ChristieDisplayBufferTest
	{
		[Test]
		public void CompletedSerialFeedbackTest()
		{
			Assert.Inconclusive();
		}

		[TestCase("\x06\x06\x06", "\x06", "\x06", "\x06")]
		[TestCase("\x1D\x01\x00", "\x1D\x01\x00")]
		public void Enqueue(string data, params string[] expected)
		{
			List<string> feedback = new List<string>();

			ChristieDisplayBuffer buffer = new ChristieDisplayBuffer();
			buffer.OnCompletedSerial += (sender, args) => feedback.Add(args.Data);

			buffer.Enqueue(data);

			Assert.IsTrue(feedback.SequenceEqual(expected));
		}

		[Test]
		public void Clear()
		{
			Assert.Inconclusive();
		}
	}
}
