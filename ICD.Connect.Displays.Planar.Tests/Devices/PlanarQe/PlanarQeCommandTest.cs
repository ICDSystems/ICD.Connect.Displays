using ICD.Common.Utils.Extensions;
using ICD.Connect.Displays.Planar.Devices.PlanarQe;
using NUnit.Framework;

namespace ICD.Connect.Displays.Planar.Tests.Devices.PlanarQe
{
	[TestFixture]
	public sealed class PlanarQeCommandTest
	{
		[TestCase("BRIGHTNESS", null, eCommandOperator.Set, new[] { "100" }, "BRIGHTNESS=100\x0d")]
		[TestCase("GAIN", null, eCommandOperator.Set, new[] { "101", "102", "103" }, "GAIN=101 102 103\x0d")]
		[TestCase("GAIN", new[] { "CURRENT", "RED" }, eCommandOperator.Set, new[] { "102" }, "GAIN(CURRENT RED)=102\x0d")]
		[TestCase("GAIN", new[] { "ZONE.1", "ALL" }, eCommandOperator.Set, new[] { "104", "105", "106" }, "GAIN(ZONE.1 ALL)=104 105 106\x0d")]
		[TestCase("BRIGHTNESS", null, eCommandOperator.Increment, null, "BRIGHTNESS+\x0d")]
		[TestCase("BRIGHTNESS", null, eCommandOperator.Decrement, null, "BRIGHTNESS-\x0d")]
		[TestCase("RESET", new[]{"USER"} , null, null, "RESET(USER)\x0d")]
		[TestCase("ASPECT", null, eCommandOperator.GetName, null, "ASPECT?\x0d")]
		[TestCase("ASPECT", null, eCommandOperator.GetNumeric, null, "ASPECT#\x0d")]
		public void SerializeTest(string commandCode, string[] modifiers, eCommandOperator? commandOperator,
		                          string[] operands, string expectedResult)
		{
			var command = new PlanarQeCommand(commandCode, modifiers, commandOperator, operands);

			StringAssert.AreEqualIgnoringCase(expectedResult, command.Serialize());
		}

		[Test]
		public void CommandComparerTest()
		{
			Assert.True(PlanarQeCommand.CommandComparer(new PlanarQeCommand("GAIN", eCommandOperator.Set, "25"), new PlanarQeCommand("GAIN", eCommandOperator.Set, "35")));
			Assert.True(PlanarQeCommand.CommandComparer(new PlanarQeCommand("GAIN", eCommandOperator.Set, "25"), new PlanarQeCommand("GAIN", eCommandOperator.Increment)));
			Assert.True(PlanarQeCommand.CommandComparer(new PlanarQeCommand("GAIN", eCommandOperator.Set, "25"), new PlanarQeCommand("GAIN", eCommandOperator.Decrement)));
			Assert.False(PlanarQeCommand.CommandComparer(new PlanarQeCommand("AUDIO.VOLUME", eCommandOperator.Set, "25"), new PlanarQeCommand("GAIN", eCommandOperator.Set, "35")));
			Assert.False(PlanarQeCommand.CommandComparer(new PlanarQeCommand("AUDIO.VOLUME", eCommandOperator.GetName), new PlanarQeCommand("AUDIO.VOLUME", eCommandOperator.Set, "35")));
			Assert.False(PlanarQeCommand.CommandComparer(new PlanarQeCommand("GAIN", new []{"ZONE.1", "RED"}, eCommandOperator.Set, new []{"25"}), new PlanarQeCommand("GAIN", new[] { "ZONE.1", "GREEN" }, eCommandOperator.Set, new[] { "25" })));
		}

		[TestCase(true, null, null)]
		[TestCase(true, eCommandOperator.Response, eCommandOperator.Response)]
		[TestCase(false, null, eCommandOperator.Set)]
		[TestCase(false, eCommandOperator.Decrement, null)]
		[TestCase(false, eCommandOperator.Increment, eCommandOperator.GetName)]
		[TestCase(true, eCommandOperator.Set, eCommandOperator.Increment)]
		[TestCase(true, eCommandOperator.Decrement, eCommandOperator.Set)]
		[TestCase(true, eCommandOperator.Decrement, eCommandOperator.Increment)]
		public void CommandOperatorComparerTest(bool expectedResult, eCommandOperator? commandOperatorA, eCommandOperator? commandOperatorB)
		{
			Assert.AreEqual(expectedResult, PlanarQeCommand.CommandOperatorComparer(commandOperatorA, commandOperatorB));
		}

		[TestCase('=', eCommandOperator.Set)]
		[TestCase('?', eCommandOperator.GetName)]
		[TestCase('#', eCommandOperator.GetNumeric)]
		[TestCase('+', eCommandOperator.Increment)]
		[TestCase('-', eCommandOperator.Decrement)]
		public void GetOperatorCodeTest(char expectedResult, eCommandOperator operatorCode)
		{
			StringAssert.AreEqualIgnoringCase(expectedResult.ToString(), PlanarQeCommand.GetOperatorCode(operatorCode).ToString());
		}

		[TestCase(eCommandOperator.Response, ':')]
		[TestCase(eCommandOperator.Ack, '@')]
		[TestCase(eCommandOperator.Nak, '^')]
		[TestCase(eCommandOperator.Err, '!')]
		[TestCase(eCommandOperator.Set, '=')]
		[TestCase(eCommandOperator.GetName, '?')]
		[TestCase(eCommandOperator.GetNumeric, '#')]
		[TestCase(eCommandOperator.Increment, '+')]
		[TestCase(eCommandOperator.Decrement, '-')]
		public void GetOperatorForCodeTest(eCommandOperator expectedResult, char code)
		{
			Assert.AreEqual(expectedResult, PlanarQeCommand.GetOperatorFromCode(code));
		}

		[TestCase("(CURRENT RED)", new [] {"CURRENT", "RED"})]
		[TestCase("(ZONE.1 ALL)", new[] { "ZONE.1", "ALL"})]
		[TestCase("(1 2 3)", new[] { "1", "2", "3"})]
		[TestCase("()", null)]
		public void ParseModifiersTest(string input, string[] expectedResult)
		{
			string[] output = PlanarQeCommand.ParseModifiers(input);
			if (output == null)
			{
				if (expectedResult == null)
					Assert.Pass();
				else
					Assert.Fail();
				return;
			}

			Assert.True(expectedResult.SequenceEqual(output, string.Equals));
		}

		[TestCase("BRIGHTNESS:100\x0d", "BRIGHTNESS", null, eCommandOperator.Response, new[] {"100"})]
		[TestCase("GAIN:101 102 103", "GAIN", null, eCommandOperator.Response, new[] {"101", "102", "103"})]
		[TestCase("GAIN(CURRENT RED):102\x0d", "GAIN", new []{"CURRENT", "RED"}, eCommandOperator.Response, new []{"102"})]
		[TestCase("GAIN(ZONE.1 ALL):104 105 106", "GAIN", new []{"ZONE.1", "ALL"}, eCommandOperator.Response, new []{"104", "105", "106"})]
		[TestCase("RESET(USER)@ACK\x0d", "RESET", new []{"USER"}, eCommandOperator.Ack, new []{"ACK"})]
		[TestCase("RESET(USER)^NAK", "RESET", new[] { "USER" }, eCommandOperator.Nak, new[] { "NAK" })]
		[TestCase("FAKE.COMMAND:ERR 3\x0d", "FAKE.COMMAND", null, eCommandOperator.Response, new []{"ERR", "3"})]
		[TestCase("BRIGHTNESS(ZONE.999)!ERR 4", "BRIGHTNESS", new []{"ZONE.999"}, eCommandOperator.Err, new []{"ERR", "4"})]
		public void ParseCommandTest(string input, string expectedCommandCode, string[] expectedModifiers,
		                              eCommandOperator? expectedCommandOperator, string[] expectedOperands)
		{
			var parsedCommand = PlanarQeCommand.ParseCommand(input);

			StringAssert.AreEqualIgnoringCase(expectedCommandCode, parsedCommand.CommandCode, "Command Code");
			Assert.AreEqual(expectedCommandOperator, parsedCommand.CommandOperator, "Command Operator");
			if (expectedModifiers == null)
				Assert.True(parsedCommand.Modifiers == null, "Modifiers");
			else
				Assert.True(expectedModifiers.SequenceEqual(parsedCommand.Modifiers, string.Equals), "Modifiers");
			if (expectedOperands == null)
				Assert.True(parsedCommand.Operands == null, "Operands");
			else
				Assert.True(expectedOperands.SequenceEqual(parsedCommand.Operands, string.Equals), "Operands");


		}
	}
}
