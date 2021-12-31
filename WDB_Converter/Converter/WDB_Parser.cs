/* Coded by ClaudeNegm */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;
using Armageddon_WDB_Converter.Extensions;
using System.Windows.Forms;

namespace Armageddon_WDB_Converter.Converter
{
    public enum QueryTypes
    {
        INSERT,
        INSERT_IGNORE,
        REPLACE,
        UPDATE
    }

    // need to update limits on INSERT queries.
    class WDB_Parser
    {
        #region Defines
        QueryTypes Query_Type = (QueryTypes)2;
        XmlDocument definition_xml = new XmlDocument();
        int Build_Required = 0;
        bool Logging = false;
        bool SQL_Logging = false;
        bool AddFakesToUpdateQueries = false;
        bool DynamicDBComparer = false;
        string SQL_Path = "";

        string MySQL_host = "";
        string MySQL_username = "";
        string MySQL_password = "";
        string MySQL_database = "";
        #endregion
        #region Functions
        /// <summary>
        /// Checks if Logging is enabled, if it is, it writes the text into the SQL path, in a file named `log.txt`.
        /// </summary>
        /// <param name="input">The input string.</param>
        public void WriteToLogFile(string input)
        {
            if (!Logging)
                return;

            FileStream file = new FileStream(SQL_Path + "log.txt", FileMode.Append, FileAccess.Write);
            StreamWriter stream = new StreamWriter(file);
            stream.Write(input + "\r\n");
            stream.Close();
            file.Close();
        }

        /// <summary>
        /// Gets the initial settings needed by the program to generate a nicely presented SQL file.
        /// </summary>
        private void GetXMLSettings()
        {
            foreach (XmlElement el in definition_xml.GetElementsByTagName("wdbDef"))
            {
                if (el.Attributes["logging"] != null)
                {
                    if (el.Attributes["logging"].Value.ToUpper() == "TRUE")
                    {
                        Logging = true;
                        StreamWriter wri = new StreamWriter(SQL_Path + "log.txt");
                        wri.Write("=================================\r\n== ArmageddonWDB v" + Application.ProductVersion + " log! ==\r\n=================================\r\n");
                        wri.Close();
                    }
                    else
                        Logging = false;
                }

                if (el.Attributes["AddFakesToUpdateQueries"] != null)
                {
                    if (el.Attributes["AddFakesToUpdateQueries"].Value.ToUpper() == "TRUE")
                        AddFakesToUpdateQueries = true;
                    else
                        AddFakesToUpdateQueries = false;
                }

                if (el.Attributes["query"] != null)
                    Query_Type = (QueryTypes)int.Parse(el.Attributes["query"].Value);

                if (el.Attributes["build"] != null)
                    Build_Required = el.Attributes["build"].Value.ToInt32();

                // Will only work for WhyDB developers.
                if (File.Exists("ArmageddonWDB_MySQL_Comparer.dll"))
                {
                    if (el.Attributes["MySQL_host"] != null)
                        MySQL_host = el.Attributes["MySQL_host"].Value;
                    if (el.Attributes["MySQL_username"] != null)
                        MySQL_username = el.Attributes["MySQL_username"].Value;
                    if (el.Attributes["MySQL_password"] != null)
                        MySQL_password = el.Attributes["MySQL_password"].Value;
                    if (el.Attributes["MySQL_database"] != null)
                        MySQL_database = el.Attributes["MySQL_database"].Value;

                    if ((MySQL_host == "") || (MySQL_username == "") || (MySQL_password == "") || (MySQL_database == ""))
                    {
                        Console.Clear();
                        Console.WriteLine("Please enter your MySQL information!\n");
                        if (MySQL_host == "")
                        {
                            Console.Write("MySQL Server Address (e.g. localhost): ");
                            MySQL_host = Console.ReadLine();
                        }
                        else
                            Console.WriteLine("MySQL Server Address (e.g. localhost): " + MySQL_host);
                        if (MySQL_username == "")
                        {
                            Console.Write("\nMySQL Username: ");
                            MySQL_username = Console.ReadLine();
                        }
                        else
                            Console.WriteLine("\nMySQL Username: " + MySQL_username);
                        if (MySQL_password == "")
                        {
                            Console.Write("MySQL Password: ");
                            MySQL_password = Console.ReadLine();
                        }
                        else
                            Console.WriteLine("MySQL Password: " + MySQL_password);
                        if (MySQL_database == "")
                        {
                            Console.Write("\nWorld database: ");
                            MySQL_database = Console.ReadLine();
                        }
                        else
                            Console.WriteLine("\nWorld database: " + MySQL_database);
                    }
                    Console.Write("\nEnable Logging in SQL file? [1 for yes, 0 for no] : ");
                    if (Console.ReadLine() == "1")
                        SQL_Logging = true;
                    Console.Clear();
                    DynamicDBComparer = true;
                    Query_Type = QueryTypes.UPDATE;
                }
                else
                    DynamicDBComparer = false;

                break;
            }

            if (((int)Query_Type > 3) || ((int)Query_Type < 0))
                Query_Type = (QueryTypes)2;
        }

        public void UpdateQueryAndCheckForOuput(ref string query_header, ref string query_content, ref string where_statements, ref  string outp, bool GenerateQueryHeader, bool Output, bool Key, string column_name, string value)
        {
            if (column_name != "")
            {
                // If it's not an update query, then we'll need headers, and normal queries.
                if (Query_Type != QueryTypes.UPDATE)
                {
                    // If `GenerateQueryHeader` is true, then it's still not fully completed.
                    if (GenerateQueryHeader)
                        query_header += "`" + column_name + "`,";

                    // Add the value of the column to the query.
                    query_content += value + ",";
                }
                else
                {
                    // If the column is a key, add it in the WHERE clause.
                    if (Key)
                    {
                        if (where_statements == "")
                            where_statements += "(`" + column_name + "`=" + value + ")";
                        else
                            where_statements += " AND (`" + column_name + "`=" + value + ")";
                    }
                    else // If it's not, add it to the normal SET clause.
                        query_content += "`" + column_name + "`=" + value + ",";
                }
            }

            /* If the column has output rights, update the outp string.
             * I didn't add a check if the `column_name` isn't empty, because it could be used
             * for debugging.
            */
            if (Output)
            {
                outp += "[";
                if (column_name != "")
                    outp += column_name.Replace("`", "");
                else
                    outp += "UNK";
                outp += "] " + value.ToString().Replace("\\'", "\'").Replace("\\\"", "\"") + " ";
            }
        }
        #endregion

        public WDB_Parser(XmlDocument structure_WDB, string initial_path, string SQL_path)
        {
            definition_xml = structure_WDB;
            SQL_Path = SQL_path;
            GetXMLSettings();

            foreach (XmlElement el in definition_xml.GetElementsByTagName("wdbId"))
            {
                string WDB_Name = el.Attributes["name"].Value + ".wdb";

                string WDB_Path;
                if (initial_path != "")
                    WDB_Path = initial_path + WDB_Name;
                else
                    WDB_Path = WDB_Name;

                if (File.Exists(WDB_Path))
                {
                    BinaryReader rd = new BinaryReader(new FileStream(WDB_Path, FileMode.Open, FileAccess.Read));

                    /* WDB Headers */
                    // Signature is the type of the file, like "WMOB" is creaturecache..
                    string signature = Encoding.ASCII.GetString(rd.ReadBytes(4).Reverse().ToArray());

                    // WoW build version
                    // If build in xml is same build in WDB and != to 0 then continue.
                    int WDB_Build = rd.ReadInt32();
                    if (((WDB_Build < Build_Required) || (WDB_Build > Build_Required)) && (Build_Required != 0))
                    {
                        Console.WriteLine("Couldn't convert '" + WDB_Name + "' because WDB build is [" + WDB_Build + "] and required build is [" + Build_Required + "].\n");
                        WriteToLogFile("** `" + WDB_Name + "` couldn't be converted because it's build [" + WDB_Build + "] isn't compatible with the one the converter requires [" + Build_Required + "].");
                    }
                    else
                    {
                        Console.WriteLine(WDB_Name + " conversion started!");

                        // Locale is like enGB, etc...
                        string locale = Encoding.ASCII.GetString(rd.ReadBytes(4).Reverse().ToArray());

                        // UNK?s
                        uint unk1 = rd.ReadUInt32();
                        uint unk2 = rd.ReadUInt32();
                        uint unk3 = rd.ReadUInt32();

                        // Create the SQL Path directory.
                        Directory.CreateDirectory(SQL_Path);
                        StreamWriter SQLwriter = new StreamWriter(SQL_Path + WDB_Name.GetWDBName() + ".sql");

                        WriteToLogFile("\r\n- " + WDB_Name + " {WDB Build: \"" + WDB_Build + "\", \"" + signature + "\", \"" + locale + "\"}");
                        SQLwriter.WriteAndFlush("/* Generating `" + WDB_Name.GetWDBName() + ".sql` started on '" + DateTime.Now.ToLocalTime() + "' using ArmageddonWDB {ClaudeNegm} */");

                        bool limit_passed = false;
                        int limit = 0;
                        int limit_xml = 2147483647;
                        if (el.Attributes["querylimit"] != null)
                            limit_xml = Convert.ToInt32(el.Attributes["querylimit"].Value);

                        string query_header = "";
                        bool GenerateQueryHeader = false;

                        if (Query_Type != QueryTypes.UPDATE)
                        {
                            GenerateQueryHeader = true;
                            query_header = Regex.Replace(Query_Type.ToString(), "_", " ") + " INTO `" + el.Attributes["tablename"].Value + "` (";
                        }

                        // Let's begin coding :P
                        while (rd.BaseStream.Position < rd.BaseStream.Length)
                        {
                            // ouput string.
                            string outp = "";
                            bool ParsedMoreThanOneTime = false;
                            string query_content = "";

                            // only used in UPDATE queries.
                            string where_statements = "";

                            foreach (XmlElement elem in el.GetElementsByTagName("wdbElement"))
                            {
                                // Begin Output [Check if the value has output rights or not]
                                bool Output = false;
                                if (elem.Attributes["output"] != null)
                                {
                                    string output = elem.Attributes["output"].Value.ToUpper();
                                    if (output.ToUpper() == "TRUE")
                                        Output = true;
                                    else
                                        Output = false;
                                }
                                // END Output

                                // Begin PrimaryKey [Check if the value is a a key for where clause or not]
                                bool Key = false;
                                if (Query_Type == QueryTypes.UPDATE)
                                {
                                    if (elem.Attributes["key"] != null)
                                    {
                                        string key = elem.Attributes["key"].Value.ToUpper();
                                        if (key.ToUpper() == "TRUE")
                                            Key = true;
                                        else
                                            Key = false;
                                    }
                                }
                                // END PrimaryKey

                                // ConvertWDB.byType() will check for the type of the element, and will return it's value in an array list...
                                string column_name = "";
                                if (elem.Attributes["name"] != null)
                                    column_name = elem.Attributes["name"].Value;
                                string type = elem.Attributes["type"].Value.ToUpper();

                                ArrayList Values = ConvertWDB.ConvertbyType(type, column_name, rd, elem, structure_WDB);

                                /* 1- If ConvertWDB.ConvertbyType() has failed, then it will return an empty ArrayList.
                                 * 2- If the value returned was 0 and `ParsedMoreThanOneTime` is false
                                 *    then we are at the beginning of the file, and the value was 0.
                                 *    break. 
                                 */
                                if ((Values.Count == 0) || (!ParsedMoreThanOneTime && Values[0].ToString() == "0"))
                                    break;

                                // If `AddFakesToUpdateQueries` is false, then skip FAKE types.
                                if ((Query_Type == QueryTypes.UPDATE) && (type == "FAKE") && (!AddFakesToUpdateQueries))
                                    continue;

                                /* ok, now comes the important part...
                                 * If you check ConvertWDB.ConvertbyType() function, you'll see that the count of the rows returned, are:
                                 * 0, 1 or 2, no more no less...
                                 * 
                                 * 0: if ConvertWDB.ConvertbyType() has failed to parse the value.
                                 * 
                                 * 1: if the type of the column isn't SWITCH or COUNT, only 1 row is returned
                                 *    containing the value of the column.
                                 *    
                                 * 2: if the type of the colum is SWITCH or COUNT, 2 rows are returned.
                                 *    each row, contains an ArrayList, that contains the values and the headers of the column. 
                                 */
                                if (Values.Count == 1)
                                {
                                    UpdateQueryAndCheckForOuput(ref query_header, ref query_content, ref where_statements, ref outp, GenerateQueryHeader, Output, Key, column_name, Values[0].ToString());
                                }
                                else if (Values.Count == 2)
                                {
                                    /* Usually, the values of Values[0] should be the same as Values[1].
                                     * it's a must, because query headers should have the same count 
                                     * as query values.. */
                                    if (((ArrayList)Values[0]).Count == ((ArrayList)Values[1]).Count)
                                    {
                                        // Loop through the ArrayList of `Values`.
                                        for (int d = 0; d < ((ArrayList)Values[0]).Count; d++)
                                        {
                                            // Values[0] is the array of column names.
                                            // Values[1] is the array of column values.
                                            string _column_name = (string)((ArrayList)Values[0])[d];
                                            string _value = (string)((ArrayList)Values[1])[d];
                                            UpdateQueryAndCheckForOuput(ref query_header, ref query_content, ref where_statements, ref outp, GenerateQueryHeader, Output, Key, _column_name, _value);
                                        }
                                    }
                                }

                                ParsedMoreThanOneTime = true;
                            } // foreach

                            if (outp != "")
                            {
                                // Print the output.
                                Console.WriteLine(outp);
                                // Write to log the output.
                                WriteToLogFile(outp);
                            }

                            /* If achieved to here, and `GenerateQueryHeader` is still true, then we
                             * should end it, and delete any extra "," */
                            if (GenerateQueryHeader)
                            {
                                if (query_header.EndsWith(","))
                                    query_header = query_header.Remove(query_header.Length - 1);

                                query_header += ") VALUES";
                                GenerateQueryHeader = false;
                            }

                            // For UPDATE and Non-UPDATE queries.
                            if (query_content.EndsWith(","))
                                query_content = query_content.Remove(query_content.Length - 1);

                            if ((Query_Type != QueryTypes.UPDATE) && (query_content != ""))
                            {
                                // Add "(" and ")" to the query.
                                query_content = "(" + query_content + ")";

                                if (limit == 0) // then the query headers weren't written still.
                                {
                                    /* If `limit_passed` is false, then this is the first query that will be written,
                                     * add query header with it. */
                                    if (!limit_passed)
                                    {
                                        SQLwriter.WriteAndFlush("\r\n" + query_header);
                                        if (limit_xml > 1)
                                            SQLwriter.WriteAndFlush("\r\n");
                                        else
                                            SQLwriter.WriteAndFlush(" ");
                                        SQLwriter.WriteAndFlush(query_content);
                                    }

                                    else
                                    {
                                        SQLwriter.WriteAndFlush(",");
                                        SQLwriter.WriteAndFlush("\r\n" + query_content);
                                        limit++;
                                    }
                                }
                                else if (limit == limit_xml)
                                {
                                    SQLwriter.WriteAndFlush(";");
                                    SQLwriter.WriteAndFlush("\r\n" + query_header);
                                    if (limit_xml > 1)
                                        SQLwriter.WriteAndFlush("\r\n");
                                    else
                                        SQLwriter.WriteAndFlush(" ");
                                    SQLwriter.WriteAndFlush(query_content);
                                    limit = 0;
                                    limit_passed = true;
                                }
                                else
                                {
                                    SQLwriter.WriteAndFlush(",");
                                    SQLwriter.WriteAndFlush("\r\n" + query_content);
                                }
                                limit++;
                            }
                            else if (Query_Type == QueryTypes.UPDATE && (query_content != ""))
                            {
                                limit++;
                                query_content = "UPDATE `" + el.Attributes["tablename"].Value + "` SET " + query_content;
                                if (where_statements != "")
                                {
                                    where_statements = " WHERE " + where_statements;
                                    query_content += where_statements;
                                }
                                query_content += ";";
                                SQLwriter.WriteAndFlush("\r\n" + query_content);
                            }

                            // reset `query_content` for next query.
                            query_content = "";
                        } // while()

                        if (Query_Type != QueryTypes.UPDATE)
                            SQLwriter.WriteAndFlush(";");

                        SQLwriter.WriteAndFlush("\r\n/* Finished generating `" + WDB_Name.GetWDBName() + ".sql` on '" + DateTime.Now.ToLocalTime() + "' using ArmageddonWDB {ClaudeNegm} */");
                        SQLwriter.Close();

                        if ((limit == 0) && (!limit_passed))
                        {
                            Console.WriteLine("  ** " + WDB_Name + "' doesn't contain any value!");
                            WriteToLogFile("[The wdb was empty, no values were parsed.]");
                            File.Delete(SQL_Path + WDB_Name.GetWDBName() + ".sql");
                        }

                        Console.WriteLine(WDB_Name + " conversion completed!\n");

                        try
                        {
                            if (DynamicDBComparer)
                                CompareMySQL.Start(SQL_path, WDB_Name.GetWDBName() + ".sql", MySQL_host, MySQL_username, MySQL_password, MySQL_database, Logging, SQL_Logging, limit_xml);
                        }
                        catch
                        {
                            if (!File.Exists("ArmageddonWDB_MySQL_Comparer.dll"))
                            {
                                Console.WriteLine("`ArmageddonWDB_MySQL_Comparer.dll` doesn't exist, it's essential for comparing databases.");
                                WriteToLogFile("`ArmageddonWDB_MySQL_Comparer.dll` doesn't exist, it's essential for comparing databases.");
                            }
                            else
                            {
                                Console.WriteLine("The version of `ArmageddonWDB_MySQL_Comparer.dll` is outdated, please update it and try again.");
                                WriteToLogFile("The version of `ArmageddonWDB_MySQL_Comparer.dll` is outdated, please update it and try again.");
                            }
                        }
                    }
                    rd.Close();
                }
                else
                {
                    Console.WriteLine("'" + WDB_Path + "' doesn't exist!\n");
                    WriteToLogFile("\r\n** `" + WDB_Name + "` doesn't exist.");
                }
            }
            WriteToLogFile("\r\nDone, by ClaudeNegm ;)");
        }
    }
}
/* Coded by ClaudeNegm */