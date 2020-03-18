using System;
using System.Collections.Generic;
using System.Text;
using ICD.Connect.Displays.Planar.Devices.PlanarQe;
using NUnit.Framework;

namespace ICD.Connect.Displays.Planar.Tests.Devices.PlanarQe
{
	[TestFixture]
	public sealed class PlanarQeDisplayTest
	{

		[Test]
		public void GetBoolOperandsTest()
		{
			Assert.True(PlanarQeDisplay.GetBoolOperands(new []{"ON"}));
			Assert.False(PlanarQeDisplay.GetBoolOperands(new[] { "OFF" }));
			Assert.Throws<ArgumentOutOfRangeException>(() => PlanarQeDisplay.GetBoolOperands(new List<string>()));
		}

		[TestCase(10,"DISPLAY.POWER", null, eCommandOperator.Set, new[] { "100" })]
		[TestCase(20,"DISPLAY.POWER", null, eCommandOperator.GetName, null)]
		[TestCase(20, "DISPLAY.POWER", null, eCommandOperator.GetNumeric, null)]
		[TestCase(30,"SOURCE.SELECT", null, eCommandOperator.Set, new[] { "HDMI.2" })]
		[TestCase(40, "SOURCE.SELECT", null, eCommandOperator.GetName, null)]
		[TestCase(40, "SOURCE.SELECT", null, eCommandOperator.GetNumeric, null)]
		[TestCase(50, "AUDIO.MUTE", null, eCommandOperator.Set, new[] { "ON" })]
		[TestCase(60, "AUDIO.MUTE", null, eCommandOperator.GetName, null)]
		[TestCase(60, "AUDIO.MUTE", null, eCommandOperator.GetNumeric, null)]
		[TestCase(50, "AUDIO.VOLUME", null, eCommandOperator.Set, new[] { "50" })]
		[TestCase(50, "AUDIO.VOLUME", null, eCommandOperator.Increment, null)]
		[TestCase(50, "AUDIO.VOLUME", null, eCommandOperator.Decrement, null)]
		[TestCase(60, "AUDIO.VOLUME", null, eCommandOperator.GetName, null)]
		[TestCase(60, "AUDIO.VOLUME", null, eCommandOperator.GetNumeric, null)]
		[TestCase(int.MaxValue, "GAIN", new[] { "ZONE.1", "ALL" }, eCommandOperator.Set, new[] { "104", "105", "106" })]
		[TestCase(int.MaxValue, "BRIGHTNESS", null, eCommandOperator.Increment, null)]
		[TestCase(int.MaxValue, "BRIGHTNESS", null, eCommandOperator.Decrement, null)]
		[TestCase(int.MaxValue,"RESET", new[] { "USER" }, null, null)]
		[TestCase(int.MaxValue,"ASPECT", null, eCommandOperator.GetName, null)]
		[TestCase(int.MaxValue, "ASPECT", null, eCommandOperator.GetNumeric, null)]
		public void GetPriorityForCommandTest(int expectedPriority, string commandCode, string[] modifiers,
		                                      eCommandOperator? commandOperator, string[] operands)
		{
			Assert.AreEqual(expectedPriority, PlanarQeDisplay.GetPriorityForCommand(new PlanarQeCommand(commandCode,modifiers,commandOperator,operands)));
		}


	}
}
