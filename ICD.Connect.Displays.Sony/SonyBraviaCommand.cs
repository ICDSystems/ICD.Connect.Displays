using ICD.Common.Utils;
using ICD.Connect.Protocol.Data;

namespace RSD.SimplSharp.Common.Displays.DisplayDevices.Sony
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
		private static readonly char[] s_Header = { '*', 'S' };

		public const char TYPE_CONTROL = 'C';
		public const char TYPE_ENQUIRY = 'E';
		public const char TYPE_ANSWER = 'A';
		public const char TYPE_NOTIFY = 'N';

		internal const char FOOTER = (char)0x0A;

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

		private readonly string m_Data;

		#region Properties

		public char Type { get { return m_Data[2]; } }

		public string Function { get { return m_Data.Substring(3, 4); } }

		public string Parameter { get { return m_Data.Substring(7, 16); } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SonyBraviaCommand(string data)
		{
			m_Data = data;
		}

		/// <summary>
		/// Creates a command for the given function and parameter.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="parameter"></param>
		/// <returns></returns>
		public static SonyBraviaCommand Control(string function, string parameter)
		{
			return Command(TYPE_CONTROL, function, parameter);
		}

		/// <summary>
		/// Creates a query for the given function.
		/// </summary>
		/// <param name="function"></param>
		/// <returns></returns>
		public static SonyBraviaCommand Enquiry(string function)
		{
			string parameter = StringUtils.Repeat(PARAMETER_NONE, 16);
			return Command(TYPE_ENQUIRY, function, parameter);
		}

		/// <summary>
		/// Creates a command with the given parameters.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="function"></param>
		/// <param name="parameter"></param>
		/// <returns></returns>
		public static SonyBraviaCommand Command(char type, string function, string parameter)
		{
			parameter = parameter.PadLeft(16, '0');

			string data = new string(s_Header) +
			              type +
			              function +
			              parameter;

			return new SonyBraviaCommand(data);
		}

		#endregion

		/// <summary>
		/// Serialize this instance to a string.
		/// </summary>
		/// <returns></returns>
		public string Serialize()
		{
			return m_Data;
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
