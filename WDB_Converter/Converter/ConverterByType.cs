/* Coded by ClaudeNegm */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Text.RegularExpressions;
using Armageddon_WDB_Converter.Extensions;

namespace Armageddon_WDB_Converter.Converter
{
    public static class ConvertWDB
    {
        public static ArrayList ConvertbyType(string type, string value_name, BinaryReader rd, XmlElement el, XmlDocument structure_WDB)
        {
            ArrayList arr = new ArrayList();
            arr.Add(new object());
            try
            {
                switch (type)
                {
                    case "UINTEGER":
                        {
                            arr[0] = rd.ReadUInt32().ToString();
                            break;
                        }
                    case "INTEGER":
                        {
                            arr[0] = rd.ReadInt32().ToString();
                            break;
                        }
                    case "UINTEGER64":
                        {
                            arr[0] = rd.ReadUInt64().ToString();
                            break;
                        }
                    case "INTEGER64":
                        {
                            arr[0] = rd.ReadInt64().ToString();
                            break;
                        }
                    case "FLOAT":
                        {
                            arr[0] = rd.ReadSingle().ToString("F", CultureInfo.InvariantCulture);
                            break;
                        }
                    case "STRING":
                        {
                            arr[0] = Regex.Replace(rd.ReadCString(), @"'", @"\'");
                            arr[0] = Regex.Replace(arr[0].ToString(), "\"", "\\\"");
                            arr[0] = "'" + arr[0] + "'";
                            break;
                        }
                    case "SMALLINT":
                        {
                            arr[0] = rd.ReadInt16().ToString();
                            break;
                        }
                    case "TINYINT":
                        {
                            arr[0] = rd.ReadSByte().ToString();
                            break;
                        }
                    case "COUNT":
                        {
                            arr[0] = new ArrayList();
                            arr.Add(new ArrayList());

                            int maxcount = Convert.ToInt32(el.Attributes["maxcount"].Value);
                            int beginfrom = Convert.ToInt32(el.Attributes["beginfrom"].Value);

                            int count_value = rd.ReadInt32(); // for items, number of stats

                            // Add the numberstats_count to the list.
                            if (value_name != "")
                            {
                                ((ArrayList)arr[0]).Add(value_name);
                                ((ArrayList)arr[1]).Add(count_value.ToString());
                            }

                            int i = 0;
                            XmlNodeList countNodes = el.GetElementsByTagName("countElement");
                            while (beginfrom <= maxcount)
                            {
                                foreach (XmlElement countElem in countNodes)
                                {
                                    string sturct_name = "";
                                    if (countElem.Attributes["name"] != null)
                                        sturct_name = Regex.Replace(countElem.Attributes["name"].Value, "##", beginfrom.ToString());

                                    if (sturct_name != "")
                                        ((ArrayList)arr[0]).Add(sturct_name);

                                    if ((count_value > 0) && (i < count_value))
                                    {
                                        string count_type = countElem.Attributes["type"].Value;
                                        /* Begin switch */
                                        switch (count_type.ToUpper())
                                        {
                                            case "INTEGER":
                                                {
                                                    var value_bytype = rd.ReadInt32();
                                                    if (sturct_name != "")
                                                        ((ArrayList)arr[1]).Add(value_bytype.ToString());
                                                    break;
                                                }
                                            case "UINTEGER":
                                                {
                                                    var value_bytype = rd.ReadUInt32().ToString();
                                                    if (sturct_name != "")
                                                        ((ArrayList)arr[1]).Add(value_bytype);
                                                    break;
                                                }
                                        }
                                        /* End Switch */
                                    }
                                    else
                                    {
                                        // get default attributes
                                        if (sturct_name != "")
                                            ((ArrayList)arr[1]).Add(countElem.Attributes["default"].Value);
                                    }
                                }
                                i++;
                                beginfrom++;
                            }
                            break;
                        }
                    case "SWITCH":
                        {
                            arr[0] = new ArrayList();
                            arr.Add(new ArrayList());

                            int switch_value = rd.ReadInt32();
                            XmlNodeList switchNodes = el.GetElementsByTagName("SwitchElement");
                            foreach (XmlElement switchElem in switchNodes)
                            {
                                string switch_name = "";
                                if (switchElem.Attributes["name"] != null)
                                    switch_name = switchElem.Attributes["name"].Value;

                                if (switch_name != "")
                                    ((ArrayList)arr[0]).Add(switch_name);

                                string switch_case = switchElem.Attributes["case"].Value.ToUpper();
                                string switch_default_value = switchElem.Attributes["default"].Value;
                                if (switch_name != "")
                                {
                                    if (switch_case == "POSITIVE")
                                    {
                                        if (switch_value > 0)
                                            ((ArrayList)arr[1]).Add(switch_value.ToString());
                                        else
                                            ((ArrayList)arr[1]).Add(switch_default_value);
                                    }
                                    else if (switch_case == "NEGATIVE")
                                    {
                                        if (switch_value < 0)
                                            ((ArrayList)arr[1]).Add((switch_value * -1).ToString());
                                        else
                                            ((ArrayList)arr[1]).Add(switch_default_value);
                                    }
                                }
                            }
                            break;
                        }
                    case "FAKE":
                        {
                            arr[0] = "'" + Regex.Replace(el.Attributes["default"].Value, @"'", @"\'") + "'";
                            break;
                        }
                    case "UINTFLAGS":
                        {
                            uint value = rd.ReadUInt32();
                            if (value > 2147483648)
                                arr[0] = ((value - 2147483648) * (-1)).ToString();
                            else
                                arr[0] = value.ToString();
                            break;
                        }
                    default:
                        Console.WriteLine("Unknown type in 'definitions.xml' \"" + type.ToLower() + "\"");
                        break;
                }
            }
            catch
            {
                // Return an empty array list, which has Count = 0.
                arr.Clear();
            }
            return arr;
        }
    }
}
/* Coded by ClaudeNegm */