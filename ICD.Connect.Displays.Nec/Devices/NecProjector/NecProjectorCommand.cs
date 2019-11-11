﻿using System;
using System.Collections.Generic;
using ICD.Common.Utils.Collections;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Displays.Nec.Devices.NecProjector
{

	public enum eCommandType
	{
		PowerOn,
		PowerOff,
		InputSwitch,
		AspectAdjust,
		ErrorStatusRequest,
		RunningStatusRequest,
		InputStatusRequest
	}

	sealed class NecProjectorCommand : ISerialData
	{

		

		#region Commands and Responses

		/// <summary>
		/// Commands string formats for operations, minus checksums
		/// </summary>
		private static readonly Dictionary<eCommandType, string> s_CommandCodes = new Dictionary<eCommandType, string>()
		{
			{eCommandType.PowerOn, "\x02\x00\x00\x00\x00" },
			{eCommandType.PowerOff, "\x02\x01\x00\x00\x00" },
			{eCommandType.InputSwitch, "\x02\x03\x00\x00\x02\x01{0}" },
			{eCommandType.AspectAdjust, "\x03\x10\x00\x00\x05\x18\x00\x00{0}\x00" },
			{eCommandType.ErrorStatusRequest, "\x00\x88\x00\x00\x00" },
			{eCommandType.RunningStatusRequest, "\x00\x85\x00\x00\x01\x01" },
			{eCommandType.InputStatusRequest, "\x00\x85\x00\x00\x01\x02"}
		};

		private static readonly Dictionary<eCommandType, int> s_CommandCodesArgs = new Dictionary<eCommandType, int>()
		{
			{eCommandType.PowerOn, 0 },
			{eCommandType.PowerOff, 0 },
			{eCommandType.InputSwitch, 1 },
			{eCommandType.AspectAdjust, 1},
			{eCommandType.ErrorStatusRequest,0 },
			{eCommandType.RunningStatusRequest,0 },
			{eCommandType.InputStatusRequest, 0}
		};



		private static readonly BiDictionary<eCommandType, string> s_ResponseSuccessHeader = new BiDictionary<eCommandType, string>()
		{
			{eCommandType.PowerOn, "\x22\x00" },
			{eCommandType.PowerOff, "\x22\x01" },
			{eCommandType.InputSwitch, "\x22\x03" },
			{eCommandType.AspectAdjust, "\x23\x10" },
			{eCommandType.ErrorStatusRequest, "\x20\x88" },
			{eCommandType.RunningStatusRequest, "\x20\x85" }
		};

		private static readonly BiDictionary<eCommandType, string> s_ResponseFailHeader = new BiDictionary<eCommandType, string>()
		{
			{eCommandType.PowerOn, "\xA2\x00" },
			{eCommandType.PowerOff, "\xA2\x01" },
			{eCommandType.InputSwitch, "\xA2\x03" },
			{eCommandType.AspectAdjust, "\xA3\x10" },
			{eCommandType.ErrorStatusRequest, "\xA0\x88" },
			{eCommandType.RunningStatusRequest, "\xA0\x85" }
		};

		/// <summary>
		/// Total length of successful command responses, including checksum - this is used to find complete strings in the receive buffer
		/// </summary>
		private static readonly Dictionary<eCommandType, int> s_ResponseSuccessLength = new Dictionary<eCommandType, int>()
		{
			{eCommandType.PowerOn, 6 },
			{eCommandType.PowerOff, 6 },
			{eCommandType.InputSwitch, 7 },
			{eCommandType.AspectAdjust, 8 },
			{eCommandType.ErrorStatusRequest, 18 },
			{eCommandType.RunningStatusRequest, 22},
			{eCommandType.InputStatusRequest, 22 }
		};

		/// <summary>
		/// Total length of failed command responses, including checksum - this is used to find complete strings in the receive buffer
		/// </summary>
		private static readonly Dictionary<eCommandType, int> s_ResponseFailLength = new Dictionary<eCommandType, int>()
		{
			{eCommandType.PowerOn, 8 },
			{eCommandType.PowerOff, 8 },
			{eCommandType.InputSwitch, 8 },
			{eCommandType.AspectAdjust, 8 },
			{eCommandType.ErrorStatusRequest, 8 },
			{eCommandType.RunningStatusRequest, 8},
			{eCommandType.InputStatusRequest, 8 }
		};

		#endregion


		public eCommandType CommandType { get; private set; }

		public string[] CommandArgs { get; private set; }


		#region Constructor

		public NecProjectorCommand(eCommandType commandType, params string[] commandArgs)
		{
			CommandType = commandType;
			CommandArgs = commandArgs;

			if (CommandArgs.Length != s_CommandCodesArgs[CommandType])
				throw new
					ArgumentException(string.Format("Command Type {0} requires {1} parameters, {2} given", CommandType, s_CommandCodesArgs[CommandType], CommandArgs.Length),
					                  "commandArgs");
		}

		#endregion


		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			string command = string.Format(s_CommandCodes[CommandType], CommandArgs);

			char checksum = (char)CalculateChecksum(command);

			return command + checksum;
		}

		#region Static Methods.

		public static bool IsResponseSuccess(string response)
		{
			return response.Length >= 2 && s_ResponseSuccessHeader.ContainsValue(response.Substring(0, 2));
		}

		public static int? GetResponseLengthFromHeaders(string command)
		{
			// Need at least 2 characters to get expected length
			if (command.Length < 2)
				return null;

			eCommandType commandType;

			if (s_ResponseSuccessHeader.TryGetKey(command.Substring(0, 2), out commandType))
				return s_ResponseSuccessLength[commandType];

			if (s_ResponseFailHeader.TryGetKey(command.Substring(0, 2), out commandType))
				return s_ResponseFailLength[commandType];

			// Unknown command, unknown length
			return null;

		}

		private static byte CalculateChecksum(string command)
		{
			byte checksum = 0;
			unchecked
			{
				foreach (char c in command)
					checksum += (byte)c;
			}

			return checksum;
		}

		public static bool CommandComparer(NecProjectorCommand commandA, NecProjectorCommand commandB)
		{
			if (commandA.CommandType == commandB.CommandType)
				return true;

			if ((commandA.CommandType == eCommandType.PowerOn ||
			     commandA.CommandType == eCommandType.PowerOff) &&
			    (commandB.CommandType == eCommandType.PowerOn ||
			     commandB.CommandType == eCommandType.PowerOff))
				return true;

			return false;
		}

		#endregion
	}
}