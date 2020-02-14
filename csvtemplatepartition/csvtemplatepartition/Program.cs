using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace csvtemplatepartition
{
    /*
    #
    #  CSV Template Partitioning Tool
    #  author: Jay Askew  2020 (c)
    #  
    #
    #  v1.0.0.1     -  02/11/2020   Base Version
    #
    #
    #
    */

    class Program
    {

        static bool displayHelp = false;
        static ConsoleColor defaultColor = Console.ForegroundColor;
        static string inputFormatFile = string.Empty;
        static string outputDirectory = string.Empty;
        static string[] partitionColumns;
        static string[] partitionSizes;
        static string[] prevValue;
        static Int64[] values = new Int64[2];
        static Int64 templateFiles = 0;
        static bool useRangeNaming = false;
        static int parallelism = 1;
        
        // model ref
        static CsvFormatter _csv;
        static CsvFormatter _csvPartition;
        static CsvFormatter _shadowCSV;

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

            // we only support 2 partitions at this time
            if (partitionColumns.Length > 2 || partitionSizes.Length > 2)
            {
                sendToConsole(ConsoleColor.Red, "A maximum of two partitioning columns is supported.");
                return;
            }

            // first we must load the format file
            try
            {
                _csv = loadModelFormatFile();
            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, String.Format(@"Error occurred loading format file '{0}'", inputFormatFile.ToString()));
                return;
            }

            // initialize prevValue array
            prevValue = new string[partitionColumns.Length];

            for (int q = 0; q < partitionSizes.Length; q++)
                prevValue[q] = null;

            // at this point we have the model file loaded
            // now we must make sure we are using supported data types
            for (int i = 0; i < partitionColumns.Length; i++)
            {
                int _id;

                if (!int.TryParse(partitionColumns[i], out _id))
                {
                    sendToConsole(ConsoleColor.Red, String.Format(@"Unable to read column id for partition."));
                    return;
                }
                else
                {
                    if (_csv.columns[_id].dataType != CsvFormatter.dataTypes.Date && _csv.columns[_id].dataType != CsvFormatter.dataTypes.Integer)
                    {
                        sendToConsole(ConsoleColor.Red, String.Format(@"Column {0} is not a supported datatype for partitioning.  Supported data types are date and integer.", _id.ToString()));
                        return;
                    }
                }

                if (_csv.columns[_id].valueListFile != "" && _csv.columns[_id].defaultValue != "")
                {
                    sendToConsole(ConsoleColor.Red, String.Format(@"Default values and value lists are not supported for partitioning in column {0}", _id.ToString()));
                    return;
                }
            
            }

            // if using parallelism, we must make sure monotonics are not used.  Parallelism does not support monotonically increasing values because we do not coordinate across processes to make sure duplicates are not introduced.
            // we simply change the row count and file account to allow generation to happen in parallel across multiple cores.
            if (parallelism > 1)
            {
                // we check for monotonics in any column
                foreach (CsvFormatter.column c in _csv.columns)
                {
                    if (c.monotonic == true)
                    {
                        sendToConsole(ConsoleColor.Red, "The use of monotonically increasing values is not supported when parallelism is enabled for templates.");
                        return;
                    }

                    if (c.dataType == CsvFormatter.dataTypes.Date && (c.minvalue == c.maxValue))
                    {
                        sendToConsole(ConsoleColor.Red, "The use of monotonically increasing values is not supported when parallelism is enabled for templates.");
                        return;
                    }
                }
            }


            // validate that the ranges are equal multiples of each other
            // here we must calculate the number of files each range will produce and make sure it is a) a multiple of the total run files defined and b) a multiple of the total run rows defined
            // and c) a multiple of the other ranges.  It must be a multiple of other ranges because lower numbered ranges, those with a smaller defined size, will be duplicated and this must be done
            // on the template file boundary.  For example, if we have a range of integers in a column from 1 to 120,000,000 - then a partition size of 1,000,000 will produce 120 template files.  Other 
            // partitioned columns must align to this boundary.  For a date column that has a range of one year and is partitioned monthly (30 days), this would work fine as a size of 30 days would yield
            // 12 files and the boundaries between the two align -->  120 % 12 = 0.   If they did not align, for instance we used a range of 100,000,000 and wrote 100 template files on the first partition,
            // we'd have no way to align them correctly as 100 % 12 = 4 (misaligned as we'd need to have two covering ranges for 1 column in that template - not possible).

            // calculate the target file count
            for (int t = 0; t < partitionColumns.Length; t++)
            {
                if (_csv.columns[int.Parse(partitionColumns[t])].dataType == CsvFormatter.dataTypes.Date)
                {
                    // calculate range on dates
                    DateTime _max;
                    DateTime _min;
                    int _size;

                    if (!DateTime.TryParse(_csv.columns[int.Parse(partitionColumns[t])].maxValue, out _max))
                    {
                        sendToConsole(ConsoleColor.Red, String.Format(@"Unable to parse out max value for column '{0}' for alignment validation.", partitionColumns[t].ToString()));
                        return;
                    }

                    if (!DateTime.TryParse(_csv.columns[int.Parse(partitionColumns[t])].minvalue, out _min))
                    {
                        sendToConsole(ConsoleColor.Red, String.Format(@"Unable to parse out min value for column '{0}' for alignment validation.", partitionColumns[t].ToString()));
                        return;
                    }

                    // now we need the size passed
                    if (!int.TryParse(partitionSizes[t], out _size))
                    {
                        sendToConsole(ConsoleColor.Red, String.Format(@"Unable to parse out size value for column '{0}' for alignment validation.", partitionSizes[t].ToString()));
                        return;
                    }

                    // get time span
                    TimeSpan ts = (_max - _min);

                    // get normalized size
                    int ds = normalizeDate(_size);

                    switch(ds)
                    {
                        case 7:
                            values[t] = (Int64)(ts.TotalDays / 7);
                            break;
                        case 30:
                            values[t] = (Int64)(ts.TotalDays / 30);
                            break;
                        case 365:
                            values[t] = (Int64)(ts.TotalDays / 365);
                            break;
                    }

                }
                else
                {
                    // calculate range on ints
                    Int64 _max;
                    Int64 _min;
                    Int64 _size;

                    if (!Int64.TryParse(_csv.columns[int.Parse(partitionColumns[t])].maxValue, out _max))
                    {
                        sendToConsole(ConsoleColor.Red, String.Format(@"Unable to parse out max value for column '{0}' for alignment validation.", partitionColumns[t].ToString()));
                        return;
                    }

                    if (!Int64.TryParse(_csv.columns[int.Parse(partitionColumns[t])].minvalue, out _min))
                    {
                        sendToConsole(ConsoleColor.Red, String.Format(@"Unable to parse out min value for column '{0}' for alignment validation.", partitionColumns[t].ToString()));
                        return;
                    }

                    // now we need the size passed
                    if (!Int64.TryParse(partitionSizes[t], out _size))
                    {
                        sendToConsole(ConsoleColor.Red, String.Format(@"Unable to parse out size value for column '{0}' for alignment validation.", partitionSizes[t].ToString()));
                        return;
                    }

                    // now we get the file count
                    values[t] = (((_max - _min) + 1) / _size);
                }

            }

            // check for alignment and set file count
            if (partitionColumns.Length > 1)
            {
                bool aligned = true;

                // now check alignment
                if (values[0] > values[1])
                {
                    // set templateFiles
                    templateFiles = values[0];

                    if (values[0] % values[1] != 0)
                    {
                        // not aligned
                        aligned = false;
                    }
                }
                else
                {
                    // set templateFiles
                    templateFiles = values[1];

                    if (values[1] % values[0] != 0)
                    {
                        // not aligned
                        aligned = false;
                    }
                }

                if (!aligned)
                {
                    sendToConsole(ConsoleColor.Red, "The partition values supplied are not aligned.  Partition sizes and ranges must be aligned by being multiples of each other.");
                    return;
                }
            }
            else
            {
                // there is only one value
                templateFiles = values[0];
            }


            // generate templates
            for (Int64 f = 0; f < templateFiles; f++)
            {
                // loop once for each file that should be written

                // first get a copy of the model format file
                _csvPartition = loadModelFormatFile();

                _csvPartition.numberOfFiles = (_csv.numberOfFiles / (int)templateFiles);
                _csvPartition.numberOfRows = (_csv.numberOfRows / templateFiles);

                for (int w = 0; w < partitionColumns.Length; w++)
                {

                    if (values[w] == templateFiles || f % values[w] == 0)
                    {
                        // we only want to calculate new ranges *IF* the calculated number of templates files is eql to the templateFiles var - which means we are on the column controlling rows/files ratio or the largest 
                        // template count column
                        // --OR--
                        // if we are aligned on count with the number of files into templateFiles

                        // loop through partitioning columns
                        int _colid = int.Parse(partitionColumns[w]);
                        Int64 _size = Int64.Parse(partitionSizes[w]);

                        if (_csvPartition.columns[w].dataType == CsvFormatter.dataTypes.Date)
                        {
                            // we have a date
                            // calculate range on dates
                            DateTime _max;
                            DateTime _min;
                            int __sze;

                            if (!DateTime.TryParse(_csv.columns[w].minvalue, out _min))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"Unable to parse out min value for column '{0}' for alignment validation.", partitionColumns[w].ToString()));
                                return;
                            }

                            // now we need the size passed
                            if (!int.TryParse(partitionSizes[w], out __sze))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"Unable to parse out size value for column '{0}' for alignment validation.", partitionSizes[w].ToString()));
                                return;
                            }

                            // normalize
                            __sze = normalizeDate(__sze);

                            // if we have a previous value, use it instead
                            if (prevValue[w] != null)
                                _min = DateTime.Parse(prevValue[w]);


                            // now we add the size
                            switch (__sze)
                            {
                                case 7:
                                    _max = _min.AddDays(7);
                                    break;
                                case 30:
                                    _max = _min.AddMonths(1);
                                    break;
                                case 365:
                                    _max = _min.AddYears(1);
                                    break;
                                default:
                                    _max = _min.AddYears(1);
                                    break;
                            }

                            // now we set
                            _csvPartition.columns[_colid].minvalue = _min.ToString("MM/dd/yyyy");
                            _csvPartition.columns[_colid].maxValue = _max.AddDays(-1).ToString("MM/dd/yyyy");

                            // update previous Values
                            prevValue[w] = _max.ToString();

                        }
                        else
                        {
                            // we have an integer
                            Int64 _max;
                            Int64 _min;
                            Int64 __sze;

                            if (!Int64.TryParse(_csv.columns[int.Parse(partitionColumns[w])].minvalue, out _min))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"Unable to parse out min value for column '{0}' for alignment validation.", partitionColumns[w].ToString()));
                                return;
                            }

                            // now we need the size passed
                            if (!Int64.TryParse(partitionSizes[w], out __sze))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"Unable to parse out size value for column '{0}' for alignment validation.", partitionSizes[w].ToString()));
                                return;
                            }

                            // if we have a previous value, use it instead
                            if (prevValue[w] != null)
                                _min = Int64.Parse(prevValue[w]);

                            // now we add the size
                            _max = _min + __sze;

                            _csvPartition.columns[_colid].minvalue = _min.ToString();
                            _csvPartition.columns[_colid].maxValue = (_max - 1).ToString();

                            // update previous values
                            prevValue[w] = _max.ToString();
                        }

                    }
                    else
                    {
                        // if we don't calculate a new range, we must copy the old range - which we should save
                        _csvPartition.columns[w].minvalue = _shadowCSV.columns[w].minvalue;
                        _csvPartition.columns[w].maxValue = _shadowCSV.columns[w].maxValue;
                    }

                } // inner for

                if (parallelism > 1)
                {
                    _csvPartition.numberOfFiles = (_csvPartition.numberOfCols / parallelism);
                    _csvPartition.numberOfRows = (_csvPartition.numberOfRows / parallelism);

                    for (int p = 0; p < parallelism; p++)
                    {
                        if (!saveModelFormatFile((int)f, (int)p))
                        {
                            sendToConsole(ConsoleColor.Red, String.Format(@"Failed to save parallel model format file #{0}-{1}", f.ToString(), p.ToString()));
                            return;
                        }
                    }
                }
                else
                {
                    if (!saveModelFormatFile((int)f))
                    {
                        sendToConsole(ConsoleColor.Red, String.Format(@"Failed to save model format file #{0}", f.ToString()));
                        return;
                    }
                }

                // save shadow copy
                _shadowCSV = _csvPartition;                

            } // outer for
                                 
        } // main

        // normalize date values
        static int normalizeDate(int d)
        {
            switch (d)
            {
                case 6:
                    return 7;
                case 7:
                    return 7;
                case 8:
                    return 7;
                case 28:
                    return 30;
                case 29:
                    return 30;
                case 30:
                    return 30;
                case 31:
                    return 30;
                case 364:
                    return 365;
                case 365:
                    return 365;
                case 366:
                    return 365;
                default:
                    return 365;
            }
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

                    case "I":
                        // input format file
                        inputFormatFile = args[i].Substring(2);

                        if (!File.Exists(inputFormatFile))
                        {
                            sendToConsole(ConsoleColor.Red, "Input file not found.");
                            return false;
                        }

                        if (!inputFormatFile.Contains("\\"))
                        {
                            sendToConsole(ConsoleColor.Red, "Please provide the fully qualified path name.");
                            return false;
                        }

                        break;

                    case "O":
                        // output directory
                        outputDirectory = args[i].Substring(2);

                        if (!Directory.Exists(outputDirectory))
                        {
                            sendToConsole(ConsoleColor.Red, "Output directory not found.");
                            return false;
                        }

                        break;

                    case "C":
                        string _cols = args[i].Substring(2);
                        int _colId;

                        partitionColumns = _cols.Split(',');

                        for (int _i = 0; _i < partitionColumns.Length; _i++)
                        {
                            if (!int.TryParse(partitionColumns[_i], out _colId))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"Partition column value provided at ordinal position {0} is invalid.", _i.ToString()));
                                return false;
                            }
                        }

                        break;

                    case "S":
                        string _sizes = args[i].Substring(2);
                        Int64 _size;

                        partitionSizes = _sizes.Split(',');

                        for (int _j = 0; _j < partitionSizes.Length; _j++)
                        {
                            if (!Int64.TryParse(partitionSizes[_j], out _size))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"Partition size value provided at ordinal position {0} is invalid.", _j.ToString()));
                                return false;
                            }
                        }

                        break;

                    case "P":
                        string _parallel = args[i].Substring(2);

                        if (!int.TryParse(_parallel, out parallelism))
                        {
                            sendToConsole(ConsoleColor.Red, "Incorrect parallelism value provided.");
                            return false;
                        }

                        break;

                    case "N":
                        useRangeNaming = true;

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
            help.Append("csvtemplatepartition - CSVDatagen template partitioning tool.");
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
            help.Append("/I              Input template file that needs partitioning.");
            help.Append(Environment.NewLine);
            help.Append("/O              Output directory for new partitioned template files.");
            help.Append(Environment.NewLine);
            help.Append("/C              Comma separated list of partition columns - ordinal position in the provided template.  A maximum of two partitioning columns is supported.");
            help.Append(Environment.NewLine);
            help.Append("/S              Comma separated list of partition sizes aligned with the columns from /C.  The largest partition size will determine the row/file ratio and smaller partition sizes will be duplicated accross template files.");
            help.Append("                Date sizes are defined in number of days which are rounded to the nearest boundary.  A maximum of two partitioning columns is supported.");
            help.Append(Environment.NewLine);
            help.Append("/N              Use starting range from first listed column in /C parameter as suffix for file name (otherwise, an incrementing numeric value is used).");
            help.Append(Environment.NewLine);
            help.Append("/P              Sets the parallelism for a single partition.  This is disabled (set to 1) by default.  When set to x (usually to the number of cores), partitioned templates will be duplicated with the same ranges set to 1/xth the number of rows and files.");
            help.Append(Environment.NewLine);
            help.Append(Environment.NewLine);
            help.Append("NOTE: The original source template (provided to /i), should have the full ranges defined on partitioned columns as well as the full number of rows and files for the entire run in the header.  CSVTemplatePartition will calculate the ");
            help.Append("      correct number of rows and files on a per template basis for partitioning.  Additionally, partition sizes must be even multiples.  For example, if you are partitioning by a date range and by an integer ID, a common scenario, then the partition ");
            help.Append("      sizes of these must be multiples of each other.  If you are partitioning a one year range by a monthly basis you should provide a partition size of '30' (days).  This denotes a montly partitioning scheme for the range defined");
            help.Append("      in the source template and as such would be written to 12 files (one year range divided by 30 days or monthly = 12).  If you were then wanting to partition on an integer column, the range would need to yield the same number");
            help.Append("      of files when divided by the size.  In this case, the size and range would need to be a multiple of 12 for alignment:  12000, 120000, 1200000, 12000000, etc..");
            help.Append(Environment.NewLine);
            help.Append(Environment.NewLine);
            help.Append(Environment.NewLine);

            sendToConsole(defaultColor, help.ToString());

        }

        // load model from JSON file
        static CsvFormatter loadModelFormatFile()
        {
            CsvFormatter _csv;

            // get file contents
            try
            {
                // get contents
                string json = File.ReadAllText(inputFormatFile);

                // construct the csv formatter model
                _csv = JsonConvert.DeserializeObject<CsvFormatter>(json);

            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                throw ex;
            }

            return _csv;
        }

        // save model to disk
        static bool saveModelFormatFile(int ordinal = 0, int parallelOrdinal = 0)
        {
            // get the current name
            string currentFileName = inputFormatFile.Substring(inputFormatFile.LastIndexOf('\\'));

            // remove extension so we can supply the correct suffix
            currentFileName = currentFileName.Replace(".json", "");
            currentFileName = currentFileName.Replace("\\", "");

            // add the correct suffix with the extension
            if (useRangeNaming)
            {
                int _colRef;

                if (!int.TryParse(partitionColumns[0], out _colRef))
                {
                    sendToConsole(ConsoleColor.Red, String.Format(@"Unable to retrieve partition column value."));
                    return false;
                }

                // apply appropriate suffix
                if (_csvPartition.columns[_colRef].dataType == CsvFormatter.dataTypes.Integer)
                {
                    currentFileName = String.Format(@"{0}_{1}.json", currentFileName, _csvPartition.columns[_colRef].minvalue.ToString());
                }
                else if (_csvPartition.columns[_colRef].dataType == CsvFormatter.dataTypes.Date)
                {
                    DateTime _minDate;

                    if (!DateTime.TryParse(_csvPartition.columns[_colRef].minvalue.ToString(), out _minDate))
                    {
                        sendToConsole(ConsoleColor.Red, String.Format(@"Unable to retrieve date value from '{0}'", _csvPartition.columns[_colRef].minvalue.ToString()));
                        return false;
                    }

                    currentFileName = String.Format(@"{0}_{1}.json", currentFileName, _minDate.ToString("yyyyMM"));
                }
                else
                {
                    sendToConsole(ConsoleColor.Red, String.Format(@"Data type not supported for partitioning."));
                    return false;
                }
            }
            else
            {
                currentFileName = String.Format(@"{0}_{1}.json", currentFileName, ordinal.ToString("000"));
            }

            // change file name to denote parallelism
            if (parallelism > 1)
            {
                string newSuffix = String.Format(@"_p{0}-{1}.json", parallelism.ToString(), parallelOrdinal.ToString());
                currentFileName = currentFileName.Replace(".json", newSuffix);
            }

            // construct full path
            string fullPath;

            if (outputDirectory.Substring(outputDirectory.Length - 1) == "\\")
            {
                // we have a directory separator
                fullPath = String.Format(@"{0}{1}", outputDirectory, currentFileName);
            }
            else
            {
                fullPath = String.Format(@"{0}\{1}", outputDirectory, currentFileName);
            }
            
            // serialize Json
            string json = JsonConvert.SerializeObject(_csvPartition);

            // write to the provided path
            try
            {
                File.WriteAllText(fullPath, json);
            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return false;
            }

            return true;
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
            public Int64 monotonicSeed { get; set; }

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
