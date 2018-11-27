using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Displays.Nec.Devices
{
	public sealed class NecDisplayCommand : ISerialData
	{
		public const byte START_HEADER = 0x01;
		private const byte RESERVED = 0x30;
		public const byte MONITOR_ID_ALL = 0x2A;
		private const byte CONTROLLER_ID = 0x30;

		public const byte COMMAND = 0x41;
		public const byte COMMAND_REPLY = 0x42;
		public const byte GET_PARAMETER = 0x43;
		public const byte GET_PARAMETER_REPLY = 0x44;
		public const byte SET_PARAMETER = 0x45;
		public const byte SET_PARAMETER_REPLY = 0x46;

		private const byte START_MESSAGE = 0x02;
		public const byte END_MESSAGE = 0x03;

		private const byte NULL_RESPONSE = 0x87;
		private const byte ERROR_RESPONSE = 0x01;

		public const byte DELIMITER = 0x0D;

		private readonly byte[] m_Header;
		private readonly byte[] m_Message;

		public ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

		#region Properties

		/// <summary>
		/// Gets the message type.
		/// </summary>
		public byte MessageType { get { return GetMessageType(m_Header); } }

		/// <summary>
		/// Returns true if this command is a response from the display.
		/// </summary>
		public bool IsResponse
		{
			get
			{
				return MessageType == GET_PARAMETER_REPLY ||
					   MessageType == SET_PARAMETER_REPLY ||
					   MessageType == COMMAND_REPLY;
			}
		}

		/// <summary>
		/// To tell the controller that the monitor does not have any answer to give to the host (not
		/// ready or not expected)
		/// </summary>
		public bool IsNullResponse
		{
			get
			{
				if (!IsResponse)
					return false;
				return FromAsciiCharacters8(new[] { m_Message[1], m_Message[2] }) == NULL_RESPONSE;
			}
		}

		/// <summary>
		/// Unsupported operation with this monitor or unsupported operation under current condition.
		/// </summary>
		public bool IsErrorMessage
		{
			get
			{
				if (!IsResponse)
					return false;
				return FromAsciiCharacters8(new[] { m_Message[1], m_Message[2] }) == ERROR_RESPONSE;
			}
		}

		/// <summary>
		/// Operation page.
		/// </summary>
		public byte OpCodePage
		{
			get
			{
				int offset = IsResponse ? 2 : 0;
				return FromAsciiCharacters8(new[] { m_Message[1 + offset], m_Message[2 + offset] });
			}
		}

		/// <summary>
		/// Operation code.
		/// </summary>
		public byte OpCode
		{
			get
			{
				int offset = IsResponse ? 2 : 0;
				return FromAsciiCharacters8(new[] { m_Message[3 + offset], m_Message[4 + offset] });
			}
		}

		/// <summary>
		/// Operation type code.
		/// </summary>
		public byte OperationType
		{
			get
			{
				if (!IsResponse)
					throw new InvalidOperationException();
				return FromAsciiCharacters8(new[] { m_Message[7], m_Message[8] });
			}
		}

		/// <summary>
		/// The maximum value the display can accept for this command.
		/// </summary>
		public ushort MaxValue
		{
			get
			{
				if (!IsResponse)
					throw new InvalidOperationException();
				return FromAsciiCharacters16(new[] { m_Message[9], m_Message[10], m_Message[11], m_Message[12] });
			}
		}

		/// <summary>
		/// The current value assigned to the display for this command.
		/// </summary>
		public ushort CurrentValue
		{
			get
			{
				switch (MessageType)
				{
					case SET_PARAMETER:
						return FromAsciiCharacters16(new[] { m_Message[5], m_Message[6], m_Message[7], m_Message[8] });

					case GET_PARAMETER_REPLY:
					case SET_PARAMETER_REPLY:
						return FromAsciiCharacters16(new[] { m_Message[13], m_Message[14], m_Message[15], m_Message[16] });

					default:
						throw new InvalidOperationException();
				}
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="header"></param>
		/// <param name="message"></param>
		private NecDisplayCommand(byte[] header, byte[] message)
		{
			m_Header = header;
			m_Message = message;
		}

		/// <summary>
		/// Instantiates the command from data received from the display.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static NecDisplayCommand FromData(ISerialData data)
		{
			return FromData(data.Serialize());
		}

		/// <summary>
		/// Instantiates the command from data received from the display.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static NecDisplayCommand FromData(string data)
		{
			return FromData(Encoding.ASCII.GetBytes(data));
		}

		/// <summary>
		/// Instantiates the command from data received from the display.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static NecDisplayCommand FromData(byte[] data)
		{
			if (data.Last() == DELIMITER)
				data = data.Take(data.Length - 1).ToArray();

			byte[] header = data.Take(7).ToArray();
			byte[] message = data.Skip(7).ToArray();

			return new NecDisplayCommand(header, message);
		}

		/// <summary>
		/// Instantiates a command.
		/// </summary>
		/// <param name="monitorId"></param>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static NecDisplayCommand Command(byte monitorId, IEnumerable<byte> bytes)
		{
			byte[] message = new[] { START_MESSAGE }.Concat(bytes)
												  .Concat(new[] { END_MESSAGE })
												  .ToArray();
			byte[] header = GetHeader(monitorId, COMMAND, (byte)message.Length);

			return new NecDisplayCommand(header, message);
		}

		/// <summary>
		/// Instantiates a command to get the given parameter.
		/// </summary>
		/// <param name="monitorId"></param>
		/// <param name="page"></param>
		/// <param name="code"></param>
		/// <returns></returns>
		public static NecDisplayCommand GetParameterCommand(byte monitorId, byte page, byte code)
		{
			byte[] message = GetParameterMessage(page, code);
			byte[] header = GetHeader(monitorId, GET_PARAMETER, (byte)message.Length);

			return new NecDisplayCommand(header, message);
		}

		/// <summary>
		/// Instantiates a command to set the given parameter.
		/// </summary>
		/// <param name="monitorId"></param>
		/// <param name="page"></param>
		/// <param name="code"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static NecDisplayCommand SetParameterCommand(byte monitorId, byte page, byte code, ushort value)
		{
			byte[] message = SetParameterMessage(page, code, value);
			byte[] header = GetHeader(monitorId, SET_PARAMETER, (byte)message.Length);

			return new NecDisplayCommand(header, message);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Serializes the command to a string for communication with the display.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			byte[] bytes = ToBytes().ToArray();
			return Encoding.ASCII.GetString(bytes, 0, bytes.Length);
		}

		/// <summary>
		/// Gets the command as a sequence of bytes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<byte> ToBytes()
		{
			byte checksum = GetChecksum(m_Header.Concat(m_Message));

			return m_Header.Concat(m_Message)
						   .Append(checksum)
						   .Append(DELIMITER);
		}

		/// <summary>
		/// Gets the message bytes without the start and end codes.
		/// </summary>
		public IEnumerable<byte> GetMessageWithoutStartEndCodes()
		{
			int startIndex = m_Message.FindIndex(b => b == START_MESSAGE);
			int endIndex = m_Message.FindIndex(b => b == END_MESSAGE);

			if (startIndex != -1 && endIndex != -1)
				return m_Message.Skip(startIndex + 1).Take(endIndex - startIndex - 1).ToArray();

			Logger.AddEntry(eSeverity.Error,
			                "Command message {0} is missing start and/or end character.",
			                StringUtils.ArrayFormat(m_Message));
			return null;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Builds the message to get a given parameter.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="code"></param>
		/// <returns></returns>
		private static byte[] GetParameterMessage(byte page, byte code)
		{
			byte[] pageChars = ToAsciiCharacters(page);
			byte[] codeChars = ToAsciiCharacters(code);

			return new[]
			{
				START_MESSAGE,
				pageChars[0],
				pageChars[1],
				codeChars[0],
				codeChars[1],
				END_MESSAGE
			};
		}

		/// <summary>
		/// Builds the message to set a given parameter.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="code"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		private static byte[] SetParameterMessage(byte page, byte code, ushort value)
		{
			byte[] pageChars = ToAsciiCharacters(page);
			byte[] codeChars = ToAsciiCharacters(code);
			byte[] valueChars = ToAsciiCharacters(value);

			return new[]
			{
				START_MESSAGE,
				pageChars[0],
				pageChars[1],
				codeChars[0],
				codeChars[1],
				valueChars[0],
				valueChars[1],
				valueChars[2],
				valueChars[3],
				END_MESSAGE
			};
		}

		/// <summary>
		/// Creates the header string.
		/// </summary>
		/// <param name="monitorId"></param>
		/// <param name="messageType"></param>
		/// <param name="messageLength"></param>
		/// <returns></returns>
		private static byte[] GetHeader(byte monitorId, byte messageType, byte messageLength)
		{
			byte[] lengthBytes = ToAsciiCharacters(messageLength);

			return new[]
			{
				START_HEADER,
				RESERVED,
				monitorId,
				CONTROLLER_ID,
				messageType,
				lengthBytes[0],
				lengthBytes[1]
			};
		}

		/// <summary>
		/// Returns the message type for the given header.
		/// </summary>
		/// <param name="header"></param>
		/// <returns></returns>
		private static byte GetMessageType(IList<byte> header)
		{
			return header[4];
		}

		/// <summary>
		/// Returns the message length for the given header.
		/// </summary>
		/// <param name="header"></param>
		/// <returns></returns>
		private static byte GetMessageLength(IEnumerable<byte> header)
		{
			byte[] lengthBytes = header.Skip(5).Take(2).ToArray();
			return FromAsciiCharacters8(lengthBytes);
		}

		/// <summary>
		/// Byte representation of values are encoded as two separate ascii characters.
		/// E.g. 0x3A becomes '3' (0x33) and 'A' (0x41).
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static byte[] ToAsciiCharacters(byte value)
		{
			string hex = string.Format("{0:X2}", value);
			return Encoding.ASCII.GetBytes(hex);
		}

		/// <summary>
		/// Byte representation of values are encoded as four separate ascii characters.
		/// E.g. 0x0123 becomes '0', '1', '2', '3'.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static byte[] ToAsciiCharacters(ushort value)
		{
			string hex = string.Format("{0:X4}", value);
			return Encoding.ASCII.GetBytes(hex);
		}

		/// <summary>
		/// Decodes an 8 bit value stored as separate ascii characters.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		private static byte FromAsciiCharacters8(byte[] bytes)
		{
			string hex = Encoding.ASCII.GetString(bytes, 0, 2);
			return byte.Parse(hex, NumberStyles.HexNumber);
		}

		/// <summary>
		/// Decodes a 16 bit value stores as separate ascii characters.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		private static ushort FromAsciiCharacters16(byte[] bytes)
		{
			string hex = Encoding.ASCII.GetString(bytes, 0, 4);
			return ushort.Parse(hex, NumberStyles.HexNumber);
		}

		/// <summary>
		/// Xors all of the bytes except the first.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		private static byte GetChecksum(IEnumerable<byte> data)
		{
			return data.Skip(1).Aggregate((byte)0, (current, next) => (byte)(current ^ next));
		}

		#endregion
	}
}
