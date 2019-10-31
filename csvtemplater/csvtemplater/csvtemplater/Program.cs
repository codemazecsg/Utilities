using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Reflection;
using Newtonsoft.Json;

namespace csvtemplater
{

    /*
    #
    #  CSV Data Generator Template Creator (csvtemplater)
    #  author: Jay Askew  2019 (c)
    #  
    #
    #  v1.0.0.1     -  05/28/2019   Base Version
    #  v1.0.0.2     -  05/28/2019   Added code for numeric and bit data types
    #  v1.0.0.3     -  10/31/2019   fixed bug in string formatting
    #
    #
    */

    class Program
    {

        static bool displayHelp = false;
        static ConsoleColor defaultColor = Console.ForegroundColor;
        static string serverName = string.Empty;
        static string databaseName = string.Empty;
        static string tableName = string.Empty;
        static string userName = string.Empty;
        static string passWord = string.Empty;
        static bool useTrustedConnection = false;
        static string outputFile = string.Empty;
        static bool printHeaders = false;
        static bool appendNew = false;
        static Int64 numberOfRows = 100;
        static int numberOfFiles = 1;
        static CsvFormatter _csv;

        static void Main(string[] args)
        {

            // first process cmd line args
            if (!ProcessCmdLineArgs())
            {
                sendToConsole(ConsoleColor.Red, "Failed to process command line arguments.");
                return;
            }

            // did we just show help?
            if (displayHelp)
            {
                return;
            }

            // check for required items
            if (serverName == string.Empty || databaseName == string.Empty || tableName == string.Empty || outputFile == string.Empty)
            {
                sendToConsole(ConsoleColor.Red, "You must provide the minimal parameters to connect to the database server.  Please check csvtemplater /? for more information on [REQUIRED] parameters.");
                return;
            }

            // Do we have everything necessary to access the database
            if (!useTrustedConnection && userName == string.Empty)
            {
                sendToConsole(ConsoleColor.Red, "You must provide a username when not using a trusted connection.  Please check csvtemplater /? for more information.");
                return;
            }

            // check for password and authentication info
            if (!useTrustedConnection && passWord == string.Empty)
            {
                sendToConsole(defaultColor, string.Format(@"Please enter the password for user '{0}': ", userName.ToString()), false, false);
                passWord = Console.ReadLine();
            }

            // initalize CsvFormatter ref
            _csv = new CsvFormatter();

            _csv.numberOfRows = numberOfRows;
            _csv.numberOfFiles = numberOfFiles;
            _csv.printColumnNames = printHeaders;
            _csv.tableName = tableName;
            _csv.version = "v1.0.0.1";

            // get column collection
            if (!getColumns())
            {
                sendToConsole(ConsoleColor.Red, string.Format(@"Failed to get column collection on table '{0}].  Please check for previous errors.  Terminating without saving.", tableName.ToString()));
                return;
            }

            // write the file
            if (!writeFile())
            {
                sendToConsole(ConsoleColor.Red, string.Format(@"Failed to save file to location '{0}'.  Please check for previous errors."));
                return;
            }

            // signal success
            sendToConsole(ConsoleColor.Green, "Template generation finished successfully.", true, true);

        }

        // get columns of table
        static bool getColumns()
        {

            // we'll first use a List to get the column collection
            List<CsvFormatter.column> cols = new List<CsvFormatter.column>();

            // we need to get the columns
            SqlConnectionStringBuilder cnString = new SqlConnectionStringBuilder();

            cnString.DataSource = serverName;
            cnString.InitialCatalog = databaseName;
            
            if (useTrustedConnection)
            {
                cnString.IntegratedSecurity = true;
            }
            else
            {
                cnString.UserID = userName;
                cnString.Password = passWord;
            }

            // create conn obj
            SqlConnection cn = new SqlConnection(cnString.ToString());

            try
            {
                // open conn
                cn.Open();

                // set the query
                string query = string.Format(@"select name, system_type_id, max_length, precision, scale from sys.columns where object_id = object_id('{0}');", tableName.ToString());

                // build command obj
                SqlCommand cmd = new SqlCommand(query, cn);

                // get reader
                SqlDataReader dr;

                // get records
                dr = cmd.ExecuteReader();

                // get the columns
                while (dr.Read())
                {
                    CsvFormatter.column col = new CsvFormatter.column();

                    // get name
                    col.columnName = dr["name"].ToString();

                    // get data type
                    int _dataType;

                    if (!int.TryParse(dr["system_type_id"].ToString(), out _dataType))
                    {
                        sendToConsole(ConsoleColor.Red, string.Format(@"Error getting data type for column '{0}'", col.columnName.ToString()));
                        return false;
                    }

                    if (_dataType == 48 || _dataType == 52 || _dataType == 56 || _dataType ==  127 || _dataType == 104)
                    {
                        // integer data
                        if (_dataType == 104)
                        {
                            col.dataType = CsvFormatter.dataTypes.Integer;
                            col.minvalue = "1";
                            col.maxValue = "3";   // it never picks the max value
                        }
                        else
                        {
                            col.dataType = CsvFormatter.dataTypes.Integer;
                            col.minvalue = "1";
                            col.maxValue = int.MaxValue.ToString();
                        }
                    }
                    else if (_dataType == 167 || _dataType == 175 || _dataType == 231 || _dataType == 239 || _dataType == 36)
                    {
                        // string or char data
                        col.dataType = CsvFormatter.dataTypes.String;

                        if (_dataType == 36)
                        {
                            col.minvalue = "32";
                            col.maxValue = "32";
                        }
                        else
                        {
                            col.minvalue = "1";

                            int length = 2500;
                            if (!int.TryParse(dr["max_length"].ToString(), out length))
                            {
                                sendToConsole(ConsoleColor.Red, string.Format(@"Invalid max length for column '{0}'.", col.columnName.ToString()));
                                return false;
                            }

                            if (length > 2500 || length < 1)
                            {
                                sendToConsole(ConsoleColor.Yellow, string.Format(@"Column length truncated to 2500 bytes for column '{0}'", col.columnName.ToString()));
                                col.maxValue = "2500";
                            }
                            else
                            {
                                col.maxValue = length.ToString();
                            }
                        }
                    }
                    else if (_dataType == 106 || _dataType == 62 || _dataType == 60 || _dataType == 59 || _dataType == 122 || _dataType == 108)
                    {
                        // decimal
                        col.dataType = CsvFormatter.dataTypes.Decimal;

                        int scale = 8;
                        if (!int.TryParse(dr["scale"].ToString(), out scale))
                        {
                            sendToConsole(ConsoleColor.Red, string.Format(@"Invalid scale for column '{0}'.", col.columnName.ToString()));
                            return false;
                        }

                        if (scale < 1 || scale > 8)
                        {
                            sendToConsole(ConsoleColor.Yellow, string.Format(@"Scale for column '{0}' not supported.  Defaulting to a scale of 2.", col.columnName));
                            col.minvalue = "1";
                            col.maxValue = int.MaxValue.ToString();
                            col.mantissa = 2;
                        }
                        else
                        {
                            col.minvalue = "1";
                            col.maxValue = int.MaxValue.ToString();
                            col.mantissa = (byte)scale;
                        }

                    }
                    else if (_dataType == 40 || _dataType == 41 || _dataType == 42 || _dataType == 58 || _dataType == 61)
                    {
                        // date
                        col.dataType = CsvFormatter.dataTypes.Date;
                        col.minvalue = "01/01/1900";
                        col.maxValue = DateTime.Now.ToString("MM/dd/yyyy");
                    }
                    else
                    {
                        sendToConsole(ConsoleColor.Red, string.Format(@"Unsupported data type for column '{0}'", col.columnName.ToString()));
                        return false;
                    }


                    // add to collection
                    cols.Add(col);
                }

                _csv.numberOfCols = cols.Count;
                _csv.columns = cols.ToArray();

            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return false;
            }

            return true;
        }
        
        // write out the JSON
        static bool writeFile()
        {
            string fullPath = outputFile;

            if(appendNew)
            {
                fullPath = fullPath.Replace(".json", "_new.json");
            }

            try
            {
                // serialize it
                string json = JsonConvert.SerializeObject(_csv);

                // write it
                File.WriteAllText(fullPath, json);
            }
            catch(Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return false;
            }

            return true;
        }

        // process command line arguments
        static bool ProcessCmdLineArgs()
        {
            // get reference to the cmd line arguments
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 1; i < args.Length; i++)
            {
                if (!args[i].StartsWith("-") && !args[i].StartsWith("/"))
                {
                    sendToConsole(ConsoleColor.Red, "Incorrect parameter found.");
                    return false;
                }

                switch (args[i].Substring(1, 1).ToUpper())
                {
                    case "H":
                        displayHelp = true;
                        showHelp();

                        break;

                    case "?":
                        displayHelp = true;
                        showHelp();

                        break;

                    case "S":
                        // server name
                        serverName = args[i].Substring(2);

                        break;

                    case "D":
                        // database name
                        databaseName = args[i].Substring(2);

                        break;

                    case "U":
                        // user name
                        userName = args[i].Substring(2);

                        break;

                    case "P":
                        // password
                        passWord = args[i].Substring(2);

                        break;

                    case "Y":
                        // trusted connection
                        useTrustedConnection = true;

                        break;

                    case "O":
                        // output file
                        outputFile = args[i].Substring(2);

                        break;

                    case "T":
                        // table name
                        tableName = args[i].Substring(2);

                        break;

                    case "K":
                        // print headers
                        printHeaders = true;

                        break;

                    case "R":
                        // number of rows
                        string _rows = args[i].Substring(2);

                        if (!Int64.TryParse(_rows, out numberOfRows))
                        {
                            sendToConsole(ConsoleColor.Red, "Invalid parameter value provided for number of rows.");
                            return false;
                        }

                        break;

                    case "F":
                        // number of files
                        string _files = args[i].Substring(2);

                        if (!int.TryParse(_files, out numberOfFiles))
                        {
                            sendToConsole(ConsoleColor.Red, "Invalid parameter value provided for number of files.");
                            return false;
                        }

                        break;

                    case "X":
                        // append new tag
                        appendNew = true;

                        break;

                    default:

                        sendToConsole(ConsoleColor.Red, "Incorrect parameter provided.");
                        return false;

                } // switch
            } // for 

            return true;
        }

        // display help 
        static void showHelp()
        {
            StringBuilder help = new StringBuilder();

            help.Append(Environment.NewLine);
            help.Append("csvtemplater - CSV File Data Generation Tool Template Creator.");
            help.Append(Environment.NewLine);
            help.Append("Jay Askew - 2019(c)");
            help.Append(Environment.NewLine);
            help.Append("Version: ");
            help.Append(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            help.Append(Environment.NewLine);
            help.Append(Environment.NewLine);
            help.Append(Environment.NewLine);
            help.Append("/? or /H        This screen.");
            help.Append(Environment.NewLine);
            help.Append("/S              SQL Server server name. [REQUIRED]");
            help.Append(Environment.NewLine);
            help.Append("/D              Database name. [REQUIRED]");
            help.Append(Environment.NewLine);
            help.Append("/U              User name.");
            help.Append(Environment.NewLine);
            help.Append("/P              User password (If not provided and not a trusted connection, will be prompted).");
            help.Append(Environment.NewLine);
            help.Append("/Y              Trusted connection.");
            help.Append(Environment.NewLine);
            help.Append("/O              Output filename for template file. [REQUIRED]");
            help.Append(Environment.NewLine);
            help.Append("/T              Table name to inspect. [REQUIRED]");
            help.Append(Environment.NewLine);
            help.Append("/K              Print row headers at the start of each file.");
            help.Append(Environment.NewLine);
            help.Append("/R              Number of rows.");
            help.Append(Environment.NewLine);
            help.Append("/F              Number of files.");
            help.Append(Environment.NewLine);
            help.Append("/X              Append '_new' tag to the end of the filename.");
            help.Append(Environment.NewLine);
            help.Append(Environment.NewLine);


            sendToConsole(defaultColor, help.ToString());

        }

        // write to console
        static void sendToConsole(ConsoleColor color, string message, bool skipLine = false, bool carriageReturn = true)
        {
            if (skipLine)
            {
                Console.WriteLine();
            }

            Console.ForegroundColor = color;

            if (carriageReturn)
            {
                Console.WriteLine(message);
            }
            else
            {
                Console.Write(message);
            }

            Console.ForegroundColor = defaultColor;
        }

    }

    // the CSV Formatter class represents the structure of the CSV file
    public class CsvFormatter
    {
        public enum dataTypes
        {
            String = 1,
            Date = 2,
            Integer = 3,
            Decimal = 4
        }

        public enum SpecialDataClasses
        {
            None = 1,
            FirstName = 2,
            LastName = 3,
            FullName = 4,
            StreetAddress = 5,
            City = 6,
            State = 7,
            Country = 8,
            ZipCode = 9,
            SSN = 10,
            Phone = 11
        }

        // describes the data structure of each row
        public class column
        {

            [JsonProperty(PropertyName = "columnName")]
            public string columnName { get; set; }

            [JsonProperty(PropertyName = "dataType")]
            public dataTypes dataType { get; set; }

            [JsonProperty(PropertyName = "specialDataClass")]
            public SpecialDataClasses specialDataClass { get; set; }

            [JsonProperty(PropertyName = "defaultValue")]
            public string defaultValue { get; set; }

            [JsonProperty(PropertyName = "valueListFile")]
            public string valueListFile { get; set; }

            [JsonProperty(PropertyName = "minValue")]
            public string minvalue { get; set; }

            [JsonProperty(PropertyName = "maxValue")]
            public string maxValue { get; set; }

            [JsonProperty(PropertyName = "monotonic")]
            public bool monotonic { get; set; }

            [JsonProperty(PropertyName = "monotonicSeed")]
            public int monotonicSeed { get; set; }

            [JsonProperty(PropertyName = "monotonicStep")]
            public int monotonicStep { get; set; }

            [JsonProperty(PropertyName = "mantissa")]
            public byte mantissa { get; set; }

            [JsonProperty(PropertyName = "selectivity")]
            public Int64 selectivity { get; set; }

            [JsonProperty(PropertyName = "selColumn")]
            public int selColumn { get; set; }

            // constructor
            public column()
            {
                this.columnName = "csvdatacolumn";
                this.dataType = dataTypes.String;
                this.specialDataClass = SpecialDataClasses.None;
                this.defaultValue = string.Empty;
                this.valueListFile = string.Empty;
                this.minvalue = "1";
                this.maxValue = int.MaxValue.ToString();
                this.monotonic = false;
                this.monotonicSeed = 1;
                this.monotonicStep = 1;
                this.mantissa = 2;
                this.selectivity = -1;
                this.selColumn = -1;
            }
        }


        [JsonProperty(PropertyName = "tableName")]
        public string tableName { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string version { get; set; }

        [JsonProperty(PropertyName = "numberOfCols")]
        public int numberOfCols { get; set; }

        [JsonProperty(PropertyName = "numberOfRows")]
        public Int64 numberOfRows { get; set; }

        [JsonProperty(PropertyName = "numberOfFiles")]
        public int numberOfFiles { get; set; }

        [JsonProperty(PropertyName = "printColumnNames")]
        public bool printColumnNames { get; set; }

        [JsonProperty(PropertyName = "cols")]
        public column[] columns { get; set; }

        // constructor
        public CsvFormatter()
        {
            this.numberOfCols = 0;
            this.numberOfRows = 0;
            this.numberOfFiles = 0;
            this.tableName = "csvdata";
            this.version = "1.0.0";
            this.printColumnNames = false;
            this.columns = null;
        }

    }
}
