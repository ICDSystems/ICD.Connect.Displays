using System;
using System.Text;
using System.Text.RegularExpressions;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Displays.Planar.Devices.PlanarQe
{
	/// <summary>
	/// CommandOperator options for the PlanarQe Command
	/// Includes both operators to send to the display,
	/// and operators returned from the display.
	/// </summary>
	public enum eCommandOperator
	{
		Set,
		GetName,
		GetNumeric,
		Increment,
		Decrement,
		Response, //Response Only
		Ack, //Response Only
		Nak, //Response Only
		Err //Response Only
	}

	public sealed class PlanarQeCommand : ISerialData
	{
		public const char TERMINATOR = '\x0d';
		private const string SEPARATOR = "\x20";

		private static readonly Regex s_ResponseMatcher = new Regex(@"([^:\(@\^\!]+)(\([^\)]+\))*([:\@\^\!])(.*)");

		[PublicAPI]
		public string CommandCode { get; private set; }

		[PublicAPI]
		public string[] Modifiers { get; private set; }

		[PublicAPI]
		public eCommandOperator? CommandOperator { get; private set; }

		[PublicAPI]
		public string[] Operands { get; private set; }

		[PublicAPI]
		public PlanarQeCommand([NotNull] string commandCode, [CanBeNull] string[] modifiers, [CanBeNull] eCommandOperator? commandOperator,
							   [CanBeNull] string[] operands)
		{
			if (commandCode == null)
				throw new ArgumentNullException("commandCode");

			CommandCode = commandCode;
			Modifiers = modifiers;
			CommandOperator = commandOperator;
			Operands = operands;
		}

		[PublicAPI]
		public PlanarQeCommand([NotNull] string commandCode) : 
			this(commandCode, null, null, null)
		{
		}

		[PublicAPI]
		public PlanarQeCommand([NotNull] string commandCode, eCommandOperator commandOperator) :
			this(commandCode, null, commandOperator, null)
		{
		}

		[PublicAPI]
		public PlanarQeCommand([NotNull] string commandCode, eCommandOperator commandOperator, [CanBeNull] string operand) :
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

			//If no operator, return string now without operands
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

		/// <summary>
		/// Gets the operator code char for the given operator
		/// Only accepts operators we can send to the display
		/// </summary>
		/// <param name="commandOperator"></param>
		/// <returns></returns>
		public static char GetOperatorCode(eCommandOperator commandOperator)
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

		/// <summary>
		/// Returns the operator for a given character
		/// Works for send and response operators
		/// </summary>
		/// <param name="operatorCode"></param>
		/// <returns></returns>
		public static eCommandOperator GetOperatorFromCode(char operatorCode)
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

		/// <summary>
		/// Compares commands
		/// To match, commands must have the same CommandCode,
		/// the same modifiers, and equivalent operators
		/// Set, Increment, Decrement are considered equivalent operators
		/// </summary>
		/// <param name="commandA"></param>
		/// <param name="commandB"></param>
		/// <returns></returns>
		public static bool CommandComparer([NotNull] PlanarQeCommand commandA, [NotNull] PlanarQeCommand commandB)
		{
			if (commandA == null)
				throw new ArgumentNullException("commandA");

			if (commandB == null)
				throw new ArgumentNullException("commandB");

			if (commandA.CommandCode != commandB.CommandCode)
				return false;

			// Is null must be the same for both
			if (commandA.Modifiers == null ^ commandB.Modifiers == null)
				return false;

			if (commandA.Modifiers != null && commandB.Modifiers != null)
			{
				if (!commandA.Modifiers.SequenceEqual(commandB.Modifiers, string.Equals))
					return false;
			}

			
			
			// ReSharper disable PossibleInvalidOperationException
			return CommandOperatorComparer(commandA.CommandOperator.Value, commandB.CommandOperator.Value);
			// ReSharper restore PossibleInvalidOperationException
		}

		/// <summary>
		/// Compares operators
		/// Set, Increment, Decrement are considered equivalent operators
		/// </summary>
		/// <param name="commandOperatorA"></param>
		/// <param name="commandOperatorB"></param>
		/// <returns></returns>
		public static bool CommandOperatorComparer(eCommandOperator? commandOperatorA, eCommandOperator? commandOperatorB)
		{
			// HasValue must be the same for both
			if (commandOperatorA.HasValue ^ commandOperatorB.HasValue)
				return false;

			// No value to compare = true
			if (!commandOperatorA.HasValue)
				return true;

			if (commandOperatorA.Value == commandOperatorB.Value)
				return true;

			// Set, Increment, Decrement are the same command
			if ((commandOperatorA.Value == eCommandOperator.Set || commandOperatorA.Value == eCommandOperator.Increment ||
			     commandOperatorA.Value == eCommandOperator.Decrement) &&
			    (commandOperatorB.Value == eCommandOperator.Set || commandOperatorB.Value == eCommandOperator.Increment ||
			     commandOperatorB.Value == eCommandOperator.Decrement))
				return true;

			return false;
		}

		/// <summary>
		/// Attempts to parse the given string as a PlanarQeCommand
		/// This will parse both responses and commands being sent
		/// Will return null if unable to parse the string
		/// </summary>
		/// <param name="command"></param>
		/// <returns>Command, or null if unable to parse</returns>
		[CanBeNull]
		public static PlanarQeCommand ParseCommand([CanBeNull] string command)
		{
			if (string.IsNullOrEmpty(command))
				return null;

			Match match = s_ResponseMatcher.Match(command);
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

		/// <summary>
		/// Parses the modifiers string of a command
		/// If string is empty, returns null
		/// </summary>
		/// <param name="modifiers"></param>
		/// <returns>array of modifiers, or null if no modifiers</returns>
		[CanBeNull]
		public static string[] ParseModifiers([NotNull] string modifiers)
		{
			if (modifiers == null)
				throw new ArgumentNullException("modifiers");

			modifiers = modifiers.Trim(new[] {' ', '(', ')'});

			return string.IsNullOrEmpty(modifiers) ? null : modifiers.Split(' ');
		}
	}
}
