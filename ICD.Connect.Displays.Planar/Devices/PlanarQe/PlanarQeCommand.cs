using System;
using System.Text;
using System.Text.RegularExpressions;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Displays.Planar.Devices.PlanarQe
{
	public enum eCommandOperator
	{
		Set,
		GetName,
		GetNumeric,
		Increment,
		Decrement,
		Response,
		Ack,
		Nak,
		Err
	}

	public sealed class PlanarQeCommand : ISerialData
	{
		public const char TERMINATOR = '\x0d';
		private const string SEPARATOR = "\x20";

		private static readonly Regex s_ResponseMatcher = new Regex(@"([^:\(@\^\!]+)(\([^\)]+\))*([:\@\^\!])(.*)");

		public string CommandCode { get; private set; }

		public string[] Modifiers { get; private set; }

		public eCommandOperator? CommandOperator { get; private set; }

		public string[] Operands { get; private set; }

		public PlanarQeCommand(string commandCode, string[] modifiers, eCommandOperator? commandOperator,
		                       string[] operands)
		{
			CommandCode = commandCode;
			Modifiers = modifiers;
			CommandOperator = commandOperator;
			Operands = operands;
		}

		public PlanarQeCommand(string commandCode) : 
			this(commandCode, null, null, null)
		{
		}

		public PlanarQeCommand(string commandCode, eCommandOperator commandOperator) :
			this(commandCode, null, commandOperator, null)
		{
		}

		public PlanarQeCommand(string commandCode, eCommandOperator commandOperator, string operand) :
			this(commandCode, null, commandOperator,new[] {operand} )
		{
		}

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			if (string.IsNullOrEmpty(CommandCode))
				throw new InvalidOperationException("Cannot serialize without command code");

			StringBuilder command = new StringBuilder();

			command.Append(CommandCode);

			if (Modifiers != null)
				command.AppendFormat("({0})", string.Join(SEPARATOR, Modifiers));

			if (!CommandOperator.HasValue)
			{
				command.Append(TERMINATOR);
				return command.ToString();
			}

			command.Append(GetOperatorCode(CommandOperator.Value));

			if (Operands != null)
				command.Append(string.Join(SEPARATOR, Operands));

			command.Append(TERMINATOR);
			return command.ToString();
		}

		private static char GetOperatorCode(eCommandOperator commandOperator)
		{
			switch (commandOperator)
			{
				case eCommandOperator.Set:
					return '=';
				case eCommandOperator.GetName:
					return '?';
				case eCommandOperator.GetNumeric:
					return '#';
				case eCommandOperator.Increment:
					return '+';
				case eCommandOperator.Decrement:
					return '-';
				default:
					throw new ArgumentOutOfRangeException("commandOperator");
			}
		}

		private static eCommandOperator GetOperatorFromCode(char operatorCode)
		{
			switch (operatorCode)
			{
				case '=':
					return eCommandOperator.Set;
				case '?':
					return eCommandOperator.GetName;
				case '#':
					return eCommandOperator.GetNumeric;
				case '+':
					return eCommandOperator.Increment;
				case '-':
					return eCommandOperator.Decrement;
				case ':':
					return eCommandOperator.Response;
				case '@':
					return eCommandOperator.Ack;
				case '^':
					return eCommandOperator.Nak;
				case '!':
					return eCommandOperator.Err;
				default:
					throw new ArgumentOutOfRangeException("operatorCode");
			}
		}

		public static bool CommandComparer([NotNull] PlanarQeCommand commandA, [NotNull] PlanarQeCommand commandB)
		{
			if (commandA == null)
				throw new ArgumentNullException("commandA");

			if (commandB == null)
				throw new ArgumentNullException("commandB");

			if (commandA.CommandCode != commandB.CommandCode)
				return false;

			if (commandA.Modifiers == null ^ commandB.Modifiers == null)
				return false;

			if (commandA.Modifiers != null && commandB.Modifiers != null)
			{
				if (!commandA.Modifiers.SequenceEqual(commandB.Modifiers, string.Equals))
					return false;
			}

			// different value are false
			if (commandA.CommandOperator.HasValue  ^ commandB.CommandOperator.HasValue)
				return false;
			
			// No value to compare = true
			if (!commandA.CommandOperator.HasValue && !commandB.CommandOperator.HasValue)
				return true;
			
			// ReSharper disable PossibleInvalidOperationException
			return CommandOperatorComparer(commandA.CommandOperator.Value, commandB.CommandOperator.Value);
			// ReSharper restore PossibleInvalidOperationException
		}

		private static bool CommandOperatorComparer(eCommandOperator commandOperatorA, eCommandOperator commandOperatorB)
		{
			if (commandOperatorA == commandOperatorB)
				return true;

			// Set, Increment, Decrement are the same command

			if ((commandOperatorA == eCommandOperator.Set || commandOperatorA == eCommandOperator.Increment ||
			     commandOperatorA == eCommandOperator.Decrement) &&
			    (commandOperatorB == eCommandOperator.Set || commandOperatorB == eCommandOperator.Increment ||
			     commandOperatorB == eCommandOperator.Decrement))
				return true;

			return false;
		}

		public static PlanarQeCommand ParseResponse(string response)
		{
			if (string.IsNullOrEmpty(response))
				return null;
			//Regex for response: ([^:\(@\^]+)(\([^\)]+\))*([:\@\^])(.*)

			var match = s_ResponseMatcher.Match(response);
			if (!match.Success)
				return null;

			string commandCode = match.Groups[1].Value;

			string[] modifiers = null;
			if (match.Groups[2].Success)
				modifiers = ParseModifiers(match.Groups[2].Value);

			eCommandOperator? commandOperator = null;
			if (match.Groups[3].Success)
				commandOperator = GetOperatorFromCode(match.Groups[3].Value[0]);

			string[] operands = null;
			if (match.Groups[4].Success)
				operands = match.Groups[4].Value.Trim().Split(' ');

			return new PlanarQeCommand(commandCode, modifiers,commandOperator,operands);
		}

		public static string[] ParseModifiers(string modifiers)
		{
			if (modifiers == null)
				throw new ArgumentNullException("modifiers");

			return modifiers.Trim(new [] { ' ', '(', ')' }).Split(' ');
		}
	}
}
