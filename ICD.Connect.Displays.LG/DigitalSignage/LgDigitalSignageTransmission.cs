using System.Text;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Displays.LG.DigitalSignage
{
	/// <summary>
	/// [Command1][Command2][ ][Set ID][ ][Data][Cr]
	/// 
	/// E.g. Power On: "ka 01 01\x0D"
	/// 
	/// * [Command1]: identifies between the factory setting and the user setting modes.
	/// * [Command2]: controls monitor sets.
	/// * [Set ID]: Used for selecting a set you want to control. A unique Set ID can be assigned to each set ranging
	///             from 1 to 1000(01H~3E8H) under Settings in the OSD menu.
	///	            Selecting '00H' for Set ID allows the simultaneous control of all connected monitors.
	///	            (The maximum value may differ depending on the model.)
	/// * [Data]: Transmits command data.
	///	          Data count may increase depending on the command.
	/// * [Cr]: Carriage Return. Corresponds to '0x0D' in ASCII code.
	/// * [ ]: White Space. Corresponds to '0x20' in ASCII code
	/// </summary>
	public sealed class LgDigitalSignageTransmission : ISerialData
	{
		public const string QUERY = "\0xFF";

		public char Command1 { get; set; }

		public char Command2 { get; set; }

		public int SetId { get; set; }

		public string Data { get; set; }

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			return new StringBuilder()
				.Append(Command1)
				.Append(Command2)
				.Append(' ')
				.Append(SetId)
				.Append(' ')
				.Append(Data)
				.Append((char)0x0D)
				.ToString();
		}

		public override string ToString()
		{
			return Serialize();
		}
	}

	/// <summary>
	/// [Command2][ ][Set ID][ ][OK/NG][Data][x]
	/// 
	/// * The Product transmits ACK (acknowledgement) based on this format when receiving normal data. At this
	/// time, if the data is FF, it indicates the present status data. If the data is in data write mode, it returns the data of
	/// the PC computer.
	/// * If a command is sent with Set ID '00' (=0x00), the data is reflected to all monitor sets and each monitor set
	/// does not send an acknowledgement (ACK).
	/// * If the data value 'FF' is sent in control mode via RS-232C, the current setting value of a function can be
	/// checked (only for some functions).
	/// *Some commands are not supported depending on the model.
	/// </summary>
	public sealed class LgDigitalSignageAcknowledgement
	{
		public enum eAck
		{
			Ok,
			Ng
		}

		private const string ACK_REGEX = @"(?'command2'\S) (?'setId'\d+) (?'ack'OK|NG)(?'data'[0-9a-fA-F]+)x";

		public char Command2 { get; private set; }

		public int SetId { get; private set; }

		public eAck Ack { get; private set; }

		public string Data { get; private set; }

		/// <summary>
		/// Attempts to deserialize the given data string into an acknowledgement.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="output"></param>
		/// <returns></returns>
		public static bool Deserialize(string data, out LgDigitalSignageAcknowledgement output)
		{
			output = null;

			Match match = Regex.Match(data, ACK_REGEX);
			if (!match.Success)
				return false;

			output = new LgDigitalSignageAcknowledgement
			{
				Command2 = match.Groups["command2"].Value[0],
				SetId = int.Parse(match.Groups["setId"].Value),
				Ack = EnumUtils.Parse<eAck>(match.Groups["ack"].Value, true),
				Data = match.Groups["data"].Value
			};

			return true;
		}
	}
}
