using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Displays.Sony
{
	/// <summary>
	/// Sony bravia communication is done with fixed length commands:
	/// 
	/// 0 - Header
	/// 1
	/// 2 - Type
	/// 3 - Function
	/// 4
	/// 5
	/// 6
	/// 7 - Parameter
	/// 8
	/// 9
	/// 10
	/// 11
	/// 12
	/// 13
	/// 14
	/// 15
	/// 16
	/// 17
	/// 18
	/// 19
	/// 20
	/// 21
	/// 22
	/// 23 - Footer
	/// </summary>
	public sealed class SonyBraviaCommand : ISerialData
	{
		private const string HEADER = "*S";

		private const char TYPE_CONTROL = 'C';
		private const char TYPE_ENQUIRY = 'E';
		private const char TYPE_ANSWER = 'A';
		private const char TYPE_NOTIFY = 'N';

		public const char FOOTER = (char)0x0A;

		private const char PARAMETER_NONE = '#';

		public const string ERROR = "FFFFFFFFFFFFFFFF";
		public const string SUCCESS = "0000000000000000";

		public enum eCommand
		{
			Control,
			Enquiry,
			Answer,
			Notify
		}

		private static readonly BiDictionary<eCommand, char> s_CommandCodes =
			new BiDictionary<eCommand, char>
			{
				{ eCommand.Control, TYPE_CONTROL },
				{ eCommand.Enquiry, TYPE_ENQUIRY },
				{ eCommand.Answer, TYPE_ANSWER },
				{ eCommand.Notify, TYPE_NOTIFY }
			};

		#region Properties

		public eCommand Type { get; set; }

		public string Function { get; set; }

		public string Parameter { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="function"></param>
		/// <param name="parameter"></param>
		private SonyBraviaCommand(eCommand type, string function, string parameter)
		{
			Type = type;
			Function = function;
			Parameter = parameter;
		}

		/// <summary>
		/// Creates a command for the given function and parameter.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="parameter"></param>
		/// <returns></returns>
		public static SonyBraviaCommand Control(string function, string parameter)
		{
			return new SonyBraviaCommand(eCommand.Control, function, parameter);
		}

		/// <summary>
		/// Creates a query for the given function.
		/// </summary>
		/// <param name="function"></param>
		/// <returns></returns>
		public static SonyBraviaCommand Enquiry(string function)
		{
			string parameter = StringUtils.Repeat(PARAMETER_NONE, 16);
			return new SonyBraviaCommand(eCommand.Enquiry, function, parameter);
		}

		/// <summary>
		/// Deserializes the response into a command.
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		public static SonyBraviaCommand Response(string response)
		{
			// Type
			char typeCode = response[2];
			eCommand type = s_CommandCodes.GetKey(typeCode);

			// Function
			string function = response.Substring(3, 4);

			// Parameter
			string parameter = response.Substring(7, 16);

			return new SonyBraviaCommand(type, function, parameter);
		}

		#endregion

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			StringBuilder builder = new StringBuilder();

			// Header
			builder.Append(HEADER);

			// Type
			char typeCode = s_CommandCodes.GetValue(Type);
			builder.Append(typeCode);

			// Function
			builder.Append(Function);

			// Parameter
			string parameter = (Parameter ?? string.Empty).PadLeft(16, '0');
			builder.Append(parameter);

			// Footer
			builder.Append(FOOTER);

			return builder.ToString();
		}

		/// <summary>
		/// Builds a HDMI input parameter from the given HDMI input address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public static string SetHdmiInputParameter(int address)
		{
			return "10000" + address.ToString().PadLeft(4, '0');
		}

		/// <summary>
		/// Returns the HDMI input described by the given parameter.
		/// </summary>
		/// <param name="parameter"></param>
		/// <returns></returns>
		public static int? GetHdmiInputParameter(string parameter)
		{
			// 0000000M0000XXXX
			// M0000XXXX
			char mode = parameter[parameter.Length - 9];
			if (mode != '1')
				return null;

			string inputString = parameter.Substring(parameter.Length - 4);
			int input;
			if (!StringUtils.TryParse(inputString, out input))
				return null;

			return input;
		}
	}
}
