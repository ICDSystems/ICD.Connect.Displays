using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;

namespace ICD.Connect.Displays.Sony.BraviaSerial
{
    /// <summary>
    /// Represents a response received from a Sony Bravia display over RS-232
    /// </summary>
    public sealed class SonyBraviaSerialResponse
    {
        /// <summary>
        /// Response answer code
        /// </summary>
        public enum eAnswer
        {
            Completed,
            LimitOver,
            LimitUnder,
            Canceled,
            ParseError
        }

        #region Consts
        
        /// <summary>
        /// Response header
        /// </summary>
        public const char HEADER = '\x70';

        private const char ANSWER_COMPLETED = '\x00';
        private const char ANSWER_LIMIT_OVER = '\x01';
        private const char ANSWER_LIMIT_UNDER = '\x02';
        private const char ANSWER_CANCELED = '\x03';
        private const char ANSWER_PARSE_ERROR = '\x04';
        
        #endregion

        #region Fields

        /// <summary>
        /// Response answer code
        /// </summary>
        private readonly eAnswer m_Answer;
        
        /// <summary>
        /// Response data payload
        /// </summary>
        private readonly char[] m_Data;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Response answer code
        /// </summary>
        public eAnswer Answer
        {
            get { return m_Answer; }
        }

        /// <summary>
        /// Response data payload
        /// </summary>
        [NotNull]
        public IEnumerable<char> Data
        {
            get { return m_Data; }
        }
        
        #endregion

        /// <summary>
        /// Constructor for response with a data payload
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="data"></param>
        private SonyBraviaSerialResponse(eAnswer answer, [CanBeNull] IEnumerable<char> data)
        {
            m_Answer = answer;
            m_Data = data == null ? Array.Empty<char>() : data.ToArray();
        }

        /// <summary>
        /// Constructor for response with no data payload
        /// </summary>
        /// <param name="answer"></param>
        private SonyBraviaSerialResponse(eAnswer answer) : this(answer, null)
        {
        }

        #region Static
        
        /// <summary>
        /// Map answer codes to enum
        /// </summary>
        private static readonly Dictionary<char, eAnswer> s_AnswerCodes = new Dictionary<char, eAnswer>()
        {
            { ANSWER_COMPLETED, eAnswer.Completed},
            { ANSWER_LIMIT_OVER, eAnswer.LimitOver},
            { ANSWER_LIMIT_UNDER, eAnswer.LimitUnder},
            { ANSWER_CANCELED, eAnswer.Canceled},
            { ANSWER_PARSE_ERROR, eAnswer.ParseError}
        };

        /// <summary>
        /// Try to convert an answer code char to eAnswer
        /// </summary>
        /// <param name="code">Answer code</param>
        /// <param name="answer">eAnswer</param>
        /// <returns>true if conversion successful, otherwise false</returns>
        public static bool TryGetAnswerForChar(char code, out eAnswer answer)
        {
            return s_AnswerCodes.TryGetValue(code, out answer);
        }

        /// <summary>
        /// Create a SonyBraviaSerialResponse from the serial response from the device
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static SonyBraviaSerialResponse FromResponse([NotNull] IEnumerable<char> response)
        {
            if (response == null) 
                throw new ArgumentNullException("response");
            
            char[] array = response.ToArray();

            if (array.Length < 3)
                throw new ArgumentException("Response is not long enough", "response");

            if (array[0] != HEADER)
                throw new ArgumentException("Response should start with the header byte", "response");

            eAnswer answer;
            if (!TryGetAnswerForChar(array[1], out answer))
                throw new ArgumentException("Response answer byte is invalid", "response");

            // If the response is only 3 bytes long, it doesn't have any data in it, just use the answer code
            if (array.Length == 3)
                return new SonyBraviaSerialResponse(answer);

            // If the response is longer, grab bytes at index 3 through the next to last
            // The last byte is the checksum
            List<char> data = new List<char>();
            for (int i = 3; i < array.Length - 1; i++)
                data.Add(array[i]);
            return new SonyBraviaSerialResponse(answer, data);
        }

        #endregion


    }
}