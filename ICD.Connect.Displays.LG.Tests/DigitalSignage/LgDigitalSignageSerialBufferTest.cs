using System.Collections.Generic;
using ICD.Connect.Displays.LG.DigitalSignage;
using NUnit.Framework;

namespace ICD.Connect.Displays.LG.Tests.DigitalSignage
{
	[TestFixture]
	public sealed class LgDigitalSignageSerialBufferTest
	{
		[Test]
		public void CompletedSerialTest()
		{
			List<string> serials = new List<string>();

			LgDigitalSignageSerialBuffer buffer = new LgDigitalSignageSerialBuffer();
			buffer.OnCompletedSerial += (sender, args) => serials.Add(args.Data);

			const string junkA = "@R\xD2\xE8\x0F";
			const string junkB = "[  687.801566] SysRq : Emergency Remount R/O\x0D\x0A";
			const string response = "a 01 OK01x";

			buffer.Enqueue(junkA);
			buffer.Enqueue(response);
			buffer.Enqueue(junkA);
			buffer.Enqueue(junkB);

			Assert.AreEqual(1, serials.Count);
			Assert.AreEqual(serials[0], response);
		}
	}
}
