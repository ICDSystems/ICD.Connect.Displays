using System.Collections.Generic;
using System.Text;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.SerialQueues;

namespace ICD.Connect.Displays.Sony.BraviaSerial
{
    public class SonyBraviaSerialBuffer : AbstractSerialBuffer
    {

        private readonly ISerialQueue m_Queue;

        private readonly StringBuilder m_RxData;
        
        /// <summary>
        /// Turn debugging mode on/off
        /// Debugging on prints info to the console.
        /// </summary>
        public bool Debug { get; set; }

        public SonyBraviaSerialBuffer(ISerialQueue queue)
        {
            m_Queue = queue;
            m_RxData = new StringBuilder();
        }
        
        /// <summary>
        /// Override to clear any current state.
        /// </summary>
        protected override void ClearFinal()
        {
            m_RxData.Clear();
        }

        /// <summary>
        /// Override to process the given item for chunking.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected override IEnumerable<string> Process(string data)
        {
            if (Debug)
                IcdConsole.PrintLine(eConsoleColor.Magenta,"Adding data {0} to buffer and processing", StringUtils.ToHexLiteral(data));
            m_RxData.Append(data);

            while (m_RxData.Length > 0)
            {
                // Check to see if the first character is a header
                while (m_RxData[0] != SonyBraviaSerialResponse.HEADER)
                {
                    if (Debug)
                        IcdConsole.PrintLine(eConsoleColor.Magenta,"Looking for header byte, dropped byte {0}", StringUtils.ToHexLiteral(m_RxData[0]));
                    // Remove the first character in the buffer
                    m_RxData.Remove(0, 1);
                    // If the buffer is now empty, bail out and wait for additional characters
                    if (m_RxData.Length == 0)
                    {
                        if (Debug)
                            IcdConsole.PrintLine(eConsoleColor.Magenta,"Buffer empty after removing non-headers");
                        yield break;
                    }
                }
                
                // Check for character index 1 here, if not, bail out and wait for additional characters
                if (!(m_RxData.Length > 1))
                {
                    if (Debug)
                        IcdConsole.PrintLine(eConsoleColor.Magenta,"Buffer length {0}, waiting for additional data", m_RxData.Length);
                    yield break;
                }
                
                // Second character after the 0x70 should always be an answer
                SonyBraviaSerialResponse.eAnswer answer;
                if (!SonyBraviaSerialResponse.TryGetAnswerForChar(m_RxData[1], out answer))
                {
                    if (Debug)
                        IcdConsole.PrintLine(eConsoleColor.Magenta,"Byte after header invalid: {0}", StringUtils.ToHexLiteral(m_RxData.ToString(0,2)));
                    // Remove these two characters and continue the parsing loop
                    m_RxData.Remove(0, 2);
                    continue;
                }

                // Check for character index 2 here, if not, bail out and wait for additional responses
                if (!(m_RxData.Length > 2))
                {
                    if (Debug)
                        IcdConsole.PrintLine(eConsoleColor.Magenta,"Buffer length {0}, waiting for additional data", m_RxData.Length);
                    yield break;
                }
                
                // Check the third character, to see if it's checksum
                char checksum;
                checksum = SonyBraviaSerialCommand.GetChecksum(new [] {m_RxData[0], m_RxData[1]});
                if (m_RxData[2] == checksum)
                {
                    // Character is a checksum, so this is almost certainly a response to control request, or possibly some unsolicited response
                    string responseControl = m_RxData.ToString(0, 3);
                    if (Debug)
                        IcdConsole.PrintLine(eConsoleColor.Magenta,"Found valid checksum at third byte, assuming complete response: {0}", StringUtils.ToHexLiteral(responseControl));
                    
                    m_RxData.Remove(0, 3);
                    
                    // If the current command is a query, this is an unsolicited response
                    // Query response can also be Canceled or Parse Error
                    SonyBraviaSerialCommand command = m_Queue.CurrentCommand as SonyBraviaSerialCommand;
                    if (command != null && command.CommandType == SonyBraviaSerialCommand.eCommandType.Query && answer != SonyBraviaSerialResponse.eAnswer.Canceled && answer != SonyBraviaSerialResponse.eAnswer.ParseError)
                    {
                        // Unsolicited or nonsense response
                        // Ignore and continue parsing
                        if (Debug)
                            IcdConsole.PrintLine(eConsoleColor.Magenta,"Found valid command response, but current command is a query, ignoring");
                        continue;
                    }
                        
                    // Return as the current command and keep parsing
                    yield return responseControl;
                    continue;
                }
                
                // Third character is probably a length byte then
                int length = m_RxData[2] + 3;
                if (Debug)
                    IcdConsole.PrintLine(eConsoleColor.Magenta,"Assuming command length is {0}", length);
                
                // Total resposne needs to be length byte + 3 for the header
                if (m_RxData.Length < length)
                {
                    // Not long enough, break and wait for more data
                    if (Debug)
                        IcdConsole.PrintLine(eConsoleColor.Magenta,"Buffer doesn't contain enough bytes for command, current buffer: {0} bytes", m_RxData.Length);
                    yield break;
                }
                
                // Pull the string and return it, then loop restarts if there's data left
                string responseQuery = m_RxData.ToString(0, length);
                m_RxData.Remove(0, length);
                
                if (Debug)
                    IcdConsole.PrintLine(eConsoleColor.Magenta,"Found full length query response: {0}", responseQuery);

                yield return responseQuery;
            }

        }
    }
}