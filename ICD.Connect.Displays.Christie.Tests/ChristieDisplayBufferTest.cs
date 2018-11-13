using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
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

		[TestCase("\x06\x06\x06", new[] { "\x06", "\x06", "\x06" })]
		public void Enqueue(string data, IEnumerable<string> expectedFeedback)
		{
			List<StringEventArgs> feedback = new List<StringEventArgs>();

			ChristieDisplayBuffer buffer = new ChristieDisplayBuffer();
			buffer.OnCompletedSerial += (sender, args) => feedback.Add(args);

			buffer.Enqueue(data);

			bool equals = feedback.Select(args => args.Data).SequenceEqual(expectedFeedback);

			Assert.IsTrue(equals);
		}

		[Test]
		public void Clear()
		{
			Assert.Inconclusive();
		}
	}
}
