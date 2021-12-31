/* Coded by ClaudeNegm */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Armageddon_WDB_Converter
{
    class CompareMySQL
    {
        public static void Start(string sql_path, string filename, string server, string username, string password, string db, bool Logging, bool SQL_Logging, int limit_xml)
        {
            try
            {
                /* Will only work if you have `ArmageddonWDB_MySQL_Comparer.dll`. */
                //new Armageddon_WDB_Converter.MySQL_Comparer(sql_path, filename, server, username, password, db, Logging, SQL_Logging, limit_xml);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n");
            }
        }
    }
}
/* Coded by ClaudeNegm */