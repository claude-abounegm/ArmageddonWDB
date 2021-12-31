/* Coded by ClaudeNegm */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Globalization;
using MySql.Data.MySqlClient;

namespace Armageddon_WDB_Converter
{
    static class Extensions
    {
        public static void WriteAndFlush(this StreamWriter writer, string text)
        {
            writer.Write(text);
            writer.Flush();
        }

        public static string RevertSQLFormatChanges(this String str)
        {
            return str.Replace("\\'", "'").Replace("\\\"", "\"");
        }
    }

    public class MySQL_Comparer
    {
        private static bool Logging = false;
        private static bool SQL_Logging = false;

        private static string filename_path = "";

        private static void WriteToLogFile(string input)
        {
            if (!Logging)
                return;

            FileStream file = new FileStream(filename_path + "_log.txt", FileMode.Append, FileAccess.Write);
            StreamWriter stream = new StreamWriter(file);
            stream.Write(input + "\r\n");
            stream.Close();
            file.Close();
        }

        private static string GetInitialOuputBySQLName()
        {
            if (SQL_Logging)
            {
                if (filename_path.EndsWith("questcache.sql"))
                    return "Title";
                else if (filename_path.EndsWith("creaturecache.sql"))
                    return "name";
                else if (filename_path.EndsWith("gameobjectcache.sql"))
                    return "Name";
                else if (filename_path.EndsWith("itemnamecache.sql"))
                    return "name";
                else if (filename_path.EndsWith("itemcache.sql"))
                    return "name1";
                //else if (filename_path.EndsWith("npccache.sql"))
                //    return "entry";
            }
            return "";
        }

        private static string GetInfoBySQLName()
        {
            if (SQL_Logging)
            {
                if (filename_path.EndsWith("questcache.sql"))
                    return "Quest";
                else if (filename_path.EndsWith("creaturecache.sql"))
                    return "NPC";
                else if (filename_path.EndsWith("gameobjectcache.sql"))
                    return "GO";
                else if (filename_path.EndsWith("itemnamecache.sql"))
                    return "Item";
                else if (filename_path.EndsWith("itemcache.sql"))
                    return "Item";
                //else if (filename_path.EndsWith("npccache.sql"))
                //    return "NPC Text id:";
            }
            return "";
        }

        private static string parse(string text, string x, string patternid, int matchid)
        {
            try
            {
                Regex expression = new Regex(x, RegexOptions.IgnoreCase);
                MatchCollection matchlist = expression.Matches(text);
                Match match = matchlist[matchid];
                string output = match.Value;
                output = Regex.Replace(output, x, "$" + patternid);
                return output;
            }
            catch
            {
                return "0";
            }
        }

        public MySQL_Comparer(string sql_path, string filename, string server, string username, string password, string db, bool logging, bool SQL_logging, int limit_xml)
        {
            filename_path = sql_path + filename;
            Logging = logging;
            SQL_Logging = SQL_logging;

            StreamReader rd = new StreamReader(filename_path);
            string SQL_Content = rd.ReadToEnd();
            rd.Close();

            Console.WriteLine("Gathering important infos for comparing, please wait!");
            string[] SQL_Split = Regex.Split(SQL_Content.Replace("\r\n/* Finished", "\r\nUPDATE/* Finished"), "\r\nUPDATE");

            if ((SQL_Split.Length > 2) && (!SQL_Split[1].StartsWith("#")))
            {
                decimal time_required = Math.Round(Math.Round((decimal)(SQL_Split.Length / 95), 1) / 60, 1);

                MySqlConnection conn = new MySqlConnection(String.Format("server={0};user id={1}; password={2}; database={3}; pooling=false", server, username, password, db));
                conn.Open();

                if (Logging)
                {
                    StreamWriter wri = new StreamWriter(filename_path + "_log.txt");
                    string test = "== `" + filename + "` comparing log! ==";
                    string test2 = "";
                    for (int i = 0; i < test.Length; i++)
                        test2 += "=";
                    wri.Write(test2 + "\r\n" + test + "\r\n" + test2 + "\r\n\r\n");
                    wri.Close();
                }

                Console.WriteLine("Comparing started, the process will take ~ " + time_required.ToString() + " minutes to finish.");

                StreamWriter wr = new StreamWriter(filename_path + ".tmp");
                int queries_added_count = 0;
                int update_queries = 0;
                //int progress_count = 0;

                try
                {
                    ArrayList insert_queries = new ArrayList();
                    wr.WriteAndFlush("/* Generating `" + filename + "` started on '" + DateTime.Now.ToLocalTime() + "' using ArmageddonWDB Comparer */");
                    
                    foreach (string x in SQL_Split)
                    {
                        if (x.StartsWith("/*"))
                        {
                            if (!x.Contains("ArmageddonWDB"))
                                wr.WriteAndFlush(x + "\r\n");
                            continue;
                        }

                        // count of queries added.
                        queries_added_count++;

                        // Debugging.
                        // Console.WriteLine("[" + progress_count++.ToString() + "/" + SQL_Split.Length + "]");

                        // format: `table`
                        string table_name = Regex.Split(x, " SET ")[0].Substring(1);

                        /* query_content_split[0] = query_contents.
                         * query_content_split[1] = where statements.
                         */
                        string[] query_content_split = Regex.Split(x.Substring(6 + table_name.Length), " WHERE ");

                        if (query_content_split[0].Length > 2)
                        {
                            // contains all the values and the columns names.
                            string[] name_values = Regex.Split(query_content_split[0], ",`");
                            name_values[0] = name_values[0].Substring(1);

                            string select_query = "";
                            ArrayList column_names = new ArrayList();
                            foreach (string column in name_values)
                            {
                                string column_name = Regex.Split(column, "`=")[0];
                                column_names.Add(column_name);
                                if (select_query != "")
                                    select_query += ",";
                                select_query += "`" + column_name + "`";
                            }
                            select_query = String.Format("SELECT {0} FROM {1} WHERE {2}", select_query, table_name, query_content_split[1]);

                            MySqlDataReader reader = new MySqlCommand(select_query, conn).ExecuteReader();

                            // The UPDATE or INSERT query generated by the application.
                            string query = "";

                            // UPDATE queries.
                            if (reader.HasRows)
                            {
                                reader.Read();
                                int values_added_count = 0;
                                ArrayList columns_affected = new ArrayList();
                                string outp = "";

                                for (int i = 0; i < column_names.Count; i++)
                                {
                                    // the name of the column
                                    string name = (string)column_names[i];
                                    // the value, from WDB.
                                    string wdb_value = name_values[i].Substring(name.Length + 2);
                                    // the value, from DB.
                                    string db_value = reader[column_names[i].ToString()].ToString();

                                    // Add the initial info. for output.
                                    if ((name == GetInitialOuputBySQLName()) && (!query_content_split[1].Contains("`" + GetInitialOuputBySQLName() + "`")))
                                        outp = String.Format("[{0}] {1}", name, wdb_value.RevertSQLFormatChanges());

                                    // String conversion
                                    if (wdb_value.StartsWith("'") && wdb_value.EndsWith("'"))
                                        wdb_value = wdb_value.Substring(1).Remove(wdb_value.Length - 2).RevertSQLFormatChanges();
                                    // Float conversion
                                    else if (wdb_value.Contains("."))
                                        db_value = float.Parse(db_value).ToString("F", CultureInfo.InvariantCulture);

                                    if (wdb_value != db_value)
                                    {
                                        // Add the columns affected for output.
                                        if ((!columns_affected.Contains(name)) && (Logging))
                                            columns_affected.Add(name);

                                        // name_values contains the query in this format: column`=value
                                        if (values_added_count > 0)
                                            query += ",";
                                        query += "`" + name_values[i];

                                        // Count the values added.
                                        values_added_count++;
                                    }
                                }
                                // values_added_count > 0 then there were values added.
                                if (values_added_count > 0)
                                {
                                    string log = "";
                                    if (Logging)
                                    {
                                        /* format the ouput for _log.txt file */
                                        string _outp = "";
                                        _outp = query_content_split[1].Replace("(", "[").Replace(")", "]").Replace(";", "").Replace("] AND [", " [").Replace("=", "] ").Replace("`", "");
                                        _outp = _outp.Remove(_outp.Length - 1) + " " + outp;
                                        //Console.WriteLine(_outp);
                                        _outp += "\r\n";
                                        _outp += "  ** Updated columns: ";
                                        foreach (string column in columns_affected)
                                            _outp += column + ", ";
                                        _outp = _outp.Remove(_outp.Length - 2);
                                        _outp += ".\r\n";
                                        WriteToLogFile(_outp);
                                        /* finish formating the txt */

                                        /* SQL log */
                                        if (GetInitialOuputBySQLName() != "" && GetInfoBySQLName() != "")
                                        {
                                            if (query_content_split[1].Contains(GetInitialOuputBySQLName()))
                                                outp = "[" + GetInitialOuputBySQLName() + "] " + parse(query_content_split[1], "[(]`" + GetInitialOuputBySQLName() + "`=(.*?)[)]", "1", 0);
                                            log = string.Format("-- Update {0}: {1} \r\n", GetInfoBySQLName(), outp.Substring(GetInitialOuputBySQLName().Length + 3));
                                        }
                                    }

                                    query = log + String.Format("UPDATE {0} SET {1} WHERE {2}", table_name, query, query_content_split[1]);

                                    wr.WriteAndFlush("\r\n" + query);
                                    update_queries++;
                                }
                                else // if values_added_count == 0 then no values were added.
                                    queries_added_count--;
                            }
                            // INSERT queries.
                            else if (!reader.HasRows)
                            {
                                query = "";
                                string query_header = "";
                                string query_values = "";

                                foreach (string statement in Regex.Split(query_content_split[1].Remove(query_content_split[1].Length - 1).Replace("(", "").Replace(")", ""), (" AND ")))
                                {
                                    string[] values = Regex.Split(statement, "`=");
                                    if (query_header != "" && query_values != "")
                                    {
                                        query_header += ",";
                                        query_values += ",";
                                    }
                                    query_header += values[0] + "`";
                                    query_values += values[1];
                                }

                                string outp = "";
                                for (int i = 0; i < column_names.Count; i++)
                                {
                                    string name = (string)column_names[i];
                                    if ((name == GetInitialOuputBySQLName()) && (!query_content_split[1].Contains("`" + GetInitialOuputBySQLName() + "`")))
                                        outp = String.Format("[{0}] {1}", name, name_values[i].Substring(name.Length + 2).RevertSQLFormatChanges());

                                    if (query_header != "" && query_values != "")
                                    {
                                        query_header += ",";
                                        query_values += ",";
                                    }

                                    query_header += "`" + name + "`";
                                    query_values += name_values[i].Substring(name.Length + 2);
                                }

                                string log = "";
                                if (Logging)
                                {
                                    /* format the ouput for _log.txt file */
                                    string _outp = "";
                                    _outp = query_content_split[1].Replace("(", "[").Replace(")", "]").Replace(";", "").Replace("] AND [", " [").Replace("=", "] ").Replace("`", "");
                                    _outp = _outp.Remove(_outp.Length - 1);
                                    _outp += " " + outp + "\r\n  ** Entry was missing, generated INSERT query.\r\n";
                                    WriteToLogFile(_outp);
                                    /* finish formating the txt */

                                    /* SQL log */
                                    if (GetInitialOuputBySQLName() != "" && GetInfoBySQLName() != "")
                                    {
                                        if (query_content_split[1].Contains(GetInitialOuputBySQLName()))
                                            outp = "[" + GetInitialOuputBySQLName() + "] " + parse(query_content_split[1], "[(]`" + GetInitialOuputBySQLName() + "`=(.*?)[)]", "1", 0);
                                        log = string.Format("-- Add {0}: {1} \r\n", GetInfoBySQLName(), outp.Substring(GetInitialOuputBySQLName().Length + 3));
                                    }
                                }

                                if (insert_queries.Count == 0)
                                    insert_queries.Add(String.Format("INSERT INTO {0} ({1}) VALUES", table_name, query_header));
                                insert_queries.Add(log + "(" + query_values + ")");
                            }
                            reader.Close();
                        }
                    }
                    // Insert queries
                    if (insert_queries.Count > 1)
                    {
                        int limit = 0;
                        bool limit_passed = false;

                        if (update_queries > 0)
                            wr.WriteAndFlush("\r\n");
                        for (int i = 1; i < insert_queries.Count; i++)
                        {
                            if (limit == 0)
                            {
                                if (!limit_passed)
                                {
                                    if (((limit_xml > 1) || (GetInfoBySQLName() != "" && GetInitialOuputBySQLName() != "" && limit_xml > 1)) && insert_queries.Count > 2)
                                        wr.WriteAndFlush("\r\n" + insert_queries[0] + "\r\n" + insert_queries[i]);
                                    else
                                    {
                                        if (GetInfoBySQLName() != "" && GetInitialOuputBySQLName() != "")
                                        {
                                            string query_log = Regex.Split(insert_queries[i].ToString(), "\r\n[(](.*?)[)]")[0];
                                            string query_content = insert_queries[i].ToString().Substring(query_log.Length + 2);
                                            wr.WriteAndFlush("\r\n" + query_log + "\r\n" + insert_queries[0] + " " + query_content);
                                        }
                                        else
                                            wr.WriteAndFlush("\r\n" + insert_queries[0] + " " + insert_queries[i].ToString());
                                    }
                                }
                                else
                                {
                                    wr.WriteAndFlush(",");
                                    wr.WriteAndFlush("\r\n" + insert_queries[i]);
                                    limit++;
                                }
                            }
                            else if (limit == limit_xml)
                            {
                                wr.WriteAndFlush(";");
                                if (((limit_xml > 1) || (GetInfoBySQLName() != "" && GetInitialOuputBySQLName() != "" && limit_xml > 1)) && insert_queries.Count > 2)
                                    wr.WriteAndFlush("\r\n" + insert_queries[0] + "\r\n" + insert_queries[i]);
                                else
                                {
                                    if (GetInfoBySQLName() != "" && GetInitialOuputBySQLName() != "")
                                    {
                                        string query_log = Regex.Split(insert_queries[i].ToString(), "\r\n[(](.*?)[)]")[0];
                                        string query_content = insert_queries[i].ToString().Substring(query_log.Length + 2);
                                        wr.WriteAndFlush("\r\n" + query_log + "\r\n" + insert_queries[0] + " " + query_content);
                                    }
                                    else
                                        wr.WriteAndFlush("\r\n" + insert_queries[0] + " " + insert_queries[i].ToString());
                                }
                                limit = 0;
                                limit_passed = true;
                            }
                            else
                                wr.WriteAndFlush(",\r\n" + insert_queries[i]);

                            limit++;
                        }
                        wr.WriteAndFlush(";");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    queries_added_count = -1;
                }

                wr.WriteAndFlush("\r\n/* Generating `" + filename + "` finished on '" + DateTime.Now.ToLocalTime() + "' using ArmageddonWDB Comparer */");
                
                wr.Close();
                conn.Close();

                if (queries_added_count > 0)
                {
                    File.Delete(filename_path);
                    File.Copy(filename_path + ".tmp", filename_path);
                    File.Delete(filename_path + ".tmp");
                    WriteToLogFile("There were `" + queries_added_count + "` querie(s) that have been found different from DB.");
                    Console.WriteLine("** There were `" + queries_added_count + "` querie(s) that have been found different from DB.");
                }
                else
                {
                    File.Delete(filename_path + ".tmp");
                    File.Delete(filename_path + "_log.txt");
                    if (queries_added_count == 0)
                    {
                        Console.WriteLine("** No values were found different from the ones in DB.");
                        File.Delete(filename_path);
                    }
                }
                Console.WriteLine("Finished comparing `" + filename + "` with DB.\n");
            }
        }
    }
}
/* Coded by ClaudeNegm */