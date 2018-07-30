using System.Linq;
using ICD.Connect.Displays.Samsung.Devices.Commercial;
using NUnit.Framework;

namespace ICD.Connect.Displays.Samsung.Tests.Devices.Commercial
{
	[TestFixture]
	public sealed class SamsungProResponseTest
	{
		[TestCase("\xAA\xFF\x00\x03\x41\x15\x10\x68", 0x00)]
		public void IdTest(string response, byte expected)
		{
			Assert.AreEqual(expected, new SamsungProResponse(response).Id);
		}

		[TestCase("\xAA\xFF\x00\x03\x41\x15\x10\x68", true)]
		public void SuccessTest(string response, bool expected)
		{
			Assert.AreEqual(expected, new SamsungProResponse(response).Success);
		}

		[TestCase("\xAA\xFF\x00\x03\x41\x15\x10\x68", 0x15)]
		public void CommandTest(string response, byte expected)
		{
			Assert.AreEqual(expected, new SamsungProResponse(response).Command);
		}

		[TestCase("\xAA\xFF\x00\x03\x41\x15\x10\x68", new byte[] {0x15})]
		public void ValuesTest(string response, params byte[] expected)
		{
			byte[] values = new SamsungProResponse(response).Values;
			Assert.IsTrue(values.SequenceEqual(expected));
		}

		[TestCase("\xAA\xFF\x00\x03\x41\x15\x10\x68", true)]
		public void IsValidTest(string response, bool expected)
		{
			Assert.AreEqual(expected, new SamsungProResponse(response).IsValid);
		}
	}
}
