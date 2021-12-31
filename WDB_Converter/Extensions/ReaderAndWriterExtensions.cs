/* Coded by ClaudeNegm */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Armageddon_WDB_Converter.Extensions
{
    // BinaryReaderExtension from Wowtools
    public static class BinaryReaderExtension
    {
        /// <summary> Reads the NULL terminated string from 
        /// the current stream and advances the current position of the stream by string length + 1.
        /// <seealso cref="BinaryReader.ReadString"/>
        /// </summary>
        public static string ReadCString(this BinaryReader reader)
        {
            try
            {
                var bytes = new List<byte>();
                byte b;
                while ((b = reader.ReadByte()) != 0)
                {
                    bytes.Add(b);
                }
                return Encoding.UTF8.GetString(bytes.ToArray());
            }
            catch
            {
                return "0";
            }
        }
    }

    public static class StreamWriterExtension
    {
        /// <summary>
        /// Writes a string to the stream + clears all buffers for the current writer and causes any buffered data to be written to the underlying stream.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="text"></param>
        public static void WriteAndFlush(this StreamWriter writer, string text)
        {
            writer.Write(text);
            writer.Flush();
        }
    }
}
/* Coded by ClaudeNegm */