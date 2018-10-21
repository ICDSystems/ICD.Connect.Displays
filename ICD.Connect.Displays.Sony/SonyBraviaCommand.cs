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
		private const char PARAMETER_SUCCESS = '0';
		private const char PARAMETER_ERROR = 'F';

		public const string ERROR = "FFFFFFFFFFFFFFFF";

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
		/// Creates a command for the given function and parameter.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="parameter"></param>
		/// <returns></returns>
		public static SonyBraviaCommand Control(string function, string parameter)
		{
			return Command(eCommand.Control, function, parameter);
		}

		/// <summary>
		/// Creates a query for the given function.
		/// </summary>
		/// <param name="function"></param>
		/// <returns></returns>
		public static SonyBraviaCommand Enquiry(string function)
		{
			string parameter = StringUtils.Repeat(PARAMETER_NONE, 16);
			return Command(eCommand.Enquiry, function, parameter);
		}

		/// <summary>
		/// Creates a command with the given parameters.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="function"></param>
		/// <param name="parameter"></param>
		/// <returns></returns>
		public static SonyBraviaCommand Command(eCommand type, string function, string parameter)
		{
			return new SonyBraviaCommand
			{
				Type = type,
				Function = function,
				Parameter = parameter
			};
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
	}
}
