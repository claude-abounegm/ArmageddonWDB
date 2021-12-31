/* Coded by ClaudeNegm */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Armageddon_WDB_Converter.Extensions
{
    public static class StringExtension
    {
        /// <summary>
        /// Parses the given string, by returning the parsed integer if it succeeds, else will return 0.
        /// </summary>
        /// <param name="string_int"></param>
        /// <returns></returns>
        public static int ToInt32(this string string_int)
        {
            try
            {
                return Convert.ToInt32(string_int);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the name of the WDB file, by excluding it's extension(.wdb).
        /// </summary>
        /// <param name="wdb_name"></param>
        /// <returns></returns>
        public static string GetWDBName(this string wdb_name)
        {
            if ((wdb_name.ToUpper().EndsWith(".WDB")) && (wdb_name.Length > 4))
            {
                return wdb_name.Remove(wdb_name.Length - 4);
            }
            else
                return "";
        }
    }
}
/* Coded by ClaudeNegm */