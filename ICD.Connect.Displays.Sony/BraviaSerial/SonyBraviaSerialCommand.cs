using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Connect.Protocol.Data;

namespace ICD.Connect.Displays.Sony.BraviaSerial
{
    /// <summary>
    /// Represents a command to be sent to Sony Bravia displays over the RS-232 interface
    /// </summary>
    public sealed class SonyBraviaSerialCommand : ISerialData
    {
        /// <summary>
        /// Command Type, Query vs Control
        /// </summary>
        public enum eCommandType
        {
            Control,
            Query
        };

        /// <summary>
        /// Command function
        /// </summary>
        public enum eCommandFunction
        {
            Power,
            Standby,
            InputSelect,
            VolumeControl,
            Muting,
            Sircs //Remote Emulation
        }
        
        #region Command Components
        
        private const char CHAR_ZERO = '\x00';
        private const char CHAR_ONE = '\x01';

        private const char HEADER_CONTROL = '\x8C';
        private const char HEADER_QUERY = '\x83';

        private const char CATEGORY = '\x00';

        private const char COMMAND_POWER = '\x00';
        private const char COMMAND_STANDBY = '\x01';
        private const char COMMAND_INPUT_SELECT = '\x02';
        private const char COMMAND_VOLUME_CONTROL = '\x05';
        private const char COMMAND_MUTING = '\x06';
        private const char COMMAND_SIRCS = '\x67';

        /// <summary>
        /// Used as the first data byte for many toggle and increment/decrement commands
        /// </summary>
        public const char DATA_TOGGLE = CHAR_ZERO;
        
        /// <summary>
        /// Used as the first data byte for many direct set commands
        /// </summary>
        public const char DATA_DIRECT = CHAR_ONE;

        /// <summary>
        /// Data byte for most "off" functions
        /// </summary>
        public const char DATA_OFF = CHAR_ZERO;

        /// <summary>
        /// Data byte for most "on" functions
        /// </summary>
        public const char DATA_ON = CHAR_ONE;
        
        #endregion
        
        
        /// <summary>
        /// Maps enum to command type codes
        /// </summary>
        private static readonly BiDictionary<eCommandType, char> s_CommandTypeCodes =
            new BiDictionary<eCommandType, char>
            {
                { eCommandType.Control, HEADER_CONTROL },
                { eCommandType.Query, HEADER_QUERY }
            };

        /// <summary>
        /// Maps enum to command function codes
        /// </summary>
        private static readonly BiDictionary<eCommandFunction, char> s_CommandFunctionCodes =
            new BiDictionary<eCommandFunction, char>
            {
                { eCommandFunction.Power, COMMAND_POWER },
                { eCommandFunction.Standby, COMMAND_STANDBY },
                { eCommandFunction.InputSelect, COMMAND_INPUT_SELECT },
                { eCommandFunction.VolumeControl, COMMAND_VOLUME_CONTROL },
                { eCommandFunction.Muting, COMMAND_MUTING },
                { eCommandFunction.Sircs , COMMAND_SIRCS}
            };

        /// <summary>
        /// Command Type
        /// </summary>
        private readonly eCommandType m_CommandType;

        /// <summary>
        /// Command Function
        /// </summary>
        private readonly eCommandFunction m_CommandFunction;

        /// <summary>
        /// Data Payload for command
        /// </summary>
        [NotNull]
        private readonly char[] m_Data;

        /// <summary>
        /// Command Type
        /// </summary>
        public eCommandType CommandType
        {
            get { return m_CommandType; }
        }

        /// <summary>
        /// Command Function
        /// </summary>
        public eCommandFunction CommandFunction
        {
            get { return m_CommandFunction; }
        }

        /// <summary>
        /// Data payload for set commands
        /// </summary>
        [NotNull]
        public char[] Data
        {
            get { return m_Data; }
        }

        /// <summary>
        /// Constructor for commands with no data payload
        /// </summary>
        /// <param name="type"></param>
        /// <param name="function"></param>
        private SonyBraviaSerialCommand(eCommandType type, eCommandFunction function) : this(type, function, Enumerable.Empty<char>())
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type"></param>
        /// <param name="function"></param>
        /// <param name="data"></param>
        /// <exception cref="ArgumentNullException"></exception>
        private SonyBraviaSerialCommand(eCommandType type, eCommandFunction function, [NotNull] IEnumerable<char> data)
        {
            if (data == null) 
                throw new ArgumentNullException("data");
            
            m_CommandType = type;
            m_CommandFunction = function;
            m_Data = data.ToArray();
        }

        /// <summary>
        /// Serialize this instance to a string.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            StringBuilder builder = new StringBuilder();

            char headerCode;
            if (!s_CommandTypeCodes.TryGetValue(CommandType, out headerCode))
            {
                throw new InvalidOperationException(string.Format("Can't serialize command type of {0}",
                    CommandType));
            }

            builder.Append(headerCode);
            builder.Append(CATEGORY); // Category of 0 always

            char functionCode;
            if (!s_CommandFunctionCodes.TryGetValue(CommandFunction, out functionCode))
            {
                throw new InvalidOperationException(string.Format("Can't serialize function code of {0}",
                    CommandFunction));
            }

            builder.Append(functionCode);

            switch (CommandType)
            {
                case eCommandType.Query:
                    // Protocol defines next two bytes for queries
                    builder.Append('\xFF');
                    builder.Append('\xFF');
                    break;
                case eCommandType.Control:
                    int length = Data.Length + 1;
                    builder.Append((char)length);
                    builder.Append(Data);
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Can't serialize command type of {0}",
                        CommandType));
            }

            builder.Append(GetChecksum(builder));

            return builder.ToString();
        }

        /// <summary>
        /// Get the checksum for the command in the given string builder
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static char GetChecksum([NotNull] StringBuilder builder)
        {
            if (builder == null) 
                throw new ArgumentNullException("builder");

            char[] characters = new char[builder.Length];
            builder.CopyTo(0, characters, 0, builder.Length);

            return GetChecksum(characters);
        }

        /// <summary>
        /// Get the checksum for the given command
        /// </summary>
        /// <param name="characters"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static char GetChecksum([NotNull] IEnumerable<char> characters)
        {
            if (characters == null) 
                throw new ArgumentNullException("characters");

            byte checksum = 0;

            unchecked
            {
                foreach (char c in characters)
                {
                    checksum += (byte)c;
                }
            }

            return (char)checksum;
        }

        /// <summary>
        /// Get a query command for the given function
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static SonyBraviaSerialCommand GetQuery(eCommandFunction function)
        {
            // Standby can't query
            if (function == eCommandFunction.Standby || function == eCommandFunction.Sircs)
                throw new ArgumentOutOfRangeException("function", string.Format("Can't query the {0} function", function));
            
            return new SonyBraviaSerialCommand(eCommandType.Query, function);
        }

        /// <summary>
        /// Get a set power command
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static SonyBraviaSerialCommand SetPower(bool state)
        {
            char[] data = { state ? DATA_ON : DATA_OFF };
            return new SonyBraviaSerialCommand(eCommandType.Control, eCommandFunction.Power, data);
        }

        /// <summary>
        /// Get a set standby command
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static SonyBraviaSerialCommand SetStandby(bool state)
        {
            char[] data = { state ? DATA_ON : DATA_OFF };
            return new SonyBraviaSerialCommand(eCommandType.Control, eCommandFunction.Standby, data);
        }

        /// <summary>
        /// Get a set input command
        /// </summary>
        /// <param name="input">The 2 byte input code</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static SonyBraviaSerialCommand SetInput([NotNull] char[] input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            if (input.Length != 2)
                throw new ArgumentOutOfRangeException("input", "input must be array of length 2");

            return new SonyBraviaSerialCommand(eCommandType.Control, eCommandFunction.InputSelect, input);
        }

        /// <summary>
        /// Get a toggle input command
        /// </summary>
        /// <returns></returns>
        public static SonyBraviaSerialCommand ToggleInput()
        {
            char[] data = { DATA_TOGGLE };
            return new SonyBraviaSerialCommand(eCommandType.Control, eCommandFunction.InputSelect, data);
        }

        /// <summary>
        /// Get a set volume command
        /// </summary>
        /// <param name="volume">volume, from 0 to 100</param>
        /// <returns></returns>
        public static SonyBraviaSerialCommand SetVolume(int volume)
        {
            char[] data = { DATA_DIRECT, (char)volume };
            return new SonyBraviaSerialCommand(eCommandType.Control, eCommandFunction.VolumeControl, data);
        }

        /// <summary>
        /// Get an increment or decrement volume command
        /// </summary>
        /// <param name="up"></param>
        /// <returns></returns>
        public static SonyBraviaSerialCommand IncrementVolume(bool up)
        {
            char[] data = { DATA_TOGGLE, up ? '\x00' : '\x01' };
            return new SonyBraviaSerialCommand(eCommandType.Control, eCommandFunction.VolumeControl, data);
        }

        /// <summary>
        /// Get a set mute command
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static SonyBraviaSerialCommand SetMute(bool state)
        {
            char[] data = { DATA_DIRECT, state ? DATA_ON : DATA_OFF };
            return new SonyBraviaSerialCommand(eCommandType.Control, eCommandFunction.Muting, data);
        }

        /// <summary>
        /// Get a toggle mute command
        /// </summary>
        /// <returns></returns>
        public static SonyBraviaSerialCommand ToggleMute()
        {
            char[] data = { DATA_TOGGLE };
            return new SonyBraviaSerialCommand(eCommandType.Control, eCommandFunction.Muting, data);
        }
    }
}