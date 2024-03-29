﻿using System.Linq;
using ICD.Connect.Displays.Samsung.Devices.Commercial;
using NUnit.Framework;

namespace ICD.Connect.Displays.Samsung.Tests.Devices.Commercial
{
	[TestFixture]
	public sealed class CommonSamsungProCommandTest
	{
		[TestCase(new byte[] { 0xAA, 0x11, 0x00, 0x01, 0x01 }, 0x13)] // Power on
		[TestCase(new byte[] { 0xAA, 0xE1, 0x00, 0x03, 0xA1, 0x21, 0x01 }, 0xA7)] // Cool down
		public void GetCheckSumTest(byte[] bytes, byte expected)
		{
			Assert.AreEqual(expected, AbstractSamsungProCommand.GetCheckSum(bytes));
		}
	}

	public abstract class AbstractSamsungProCommandTest
	{
		protected abstract AbstractSamsungProCommand Instantiate(byte command, byte id);

		[TestCase(0x11)]
		public void CommandTest(byte command)
		{
			AbstractSamsungProCommand instance = Instantiate(command, 0x00);
			Assert.AreEqual(command, instance.Command);
		}

		[TestCase(0x05)]
		public void IdTest(byte id)
		{
			AbstractSamsungProCommand instance = Instantiate(0x00, id);
			Assert.AreEqual(id, instance.Id);
		}

		[Test]
		public abstract void SerializeTest();
	}

	[TestFixture]
	public sealed class SamsungProCommandTest : AbstractSamsungProCommandTest
	{
		[TestCase((byte)0x01)]
		public void DataTest(params byte[] data)
		{
			SamsungProCommand instance = new SamsungProCommand(0x00, 0x00, null, data);
			Assert.IsTrue(data.SequenceEqual(instance.Data));
		}

		protected override AbstractSamsungProCommand Instantiate(byte command, byte id)
		{
			return new SamsungProCommand(command, id, 0x00);
		}

		public override void SerializeTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void ToQueryTest()
		{
			Assert.Inconclusive();
		}
	}

	[TestFixture]
	public sealed class SamsungProQueryTest : AbstractSamsungProCommandTest
	{
		protected override AbstractSamsungProCommand Instantiate(byte command, byte id)
		{
			return new SamsungProQuery(command, id);
		}

		public override void SerializeTest()
		{
			Assert.Inconclusive();
		}
	}
}
