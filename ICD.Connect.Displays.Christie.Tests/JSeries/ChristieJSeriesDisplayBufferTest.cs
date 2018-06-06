using System.Collections.Generic;
using ICD.Connect.Displays.Christie.Devices.JSeries;
using NUnit.Framework;

namespace ICD.Connect.Displays.Christie.Tests.JSeries
{
	[TestFixture]
	public sealed class ChristieJSeriesDisplayBufferTest
	{
		[Test]
		public void CompletedSerialFeedbackTest()
		{
			Assert.Inconclusive();
		}

		[TestCase("(CON500)", "(CON500)")]
		[TestCase("(CON\\(\\)500)", "(CON\\(\\)500)")]
		[TestCase("ghfghf(CON500)fghfg", "(CON500)")]
		[TestCase("(CON500)(CON500)", "(CON500)", "(CON500)")]
		public void EnqueueTest(string data, params string[] expected)
		{
			List<string> results = new List<string>();

			ChristieJSeriesDisplayBuffer buffer = new ChristieJSeriesDisplayBuffer();
			buffer.OnCompletedSerial += (sender, args) => results.Add(args.Data);

			buffer.Enqueue(data);

			Assert.AreEqual(expected.Length, results.Count);
			for (int index = 0; index < expected.Length; index++)
				Assert.AreEqual(expected[index], results[index]);
		}

		[Test]
		public void ClearTest()
		{
			Assert.Inconclusive();
		}
	}
}
