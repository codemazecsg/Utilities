using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace csvdatagen
{

    /*
    #
    #  CSV Data Generator
    #  author: Jay Askew  2019 (c)
    #  
    #
    #  v1.0.0.1     -  05/15/2019   Base Version
    #  v1.0.0.2     -  05/19/2019   Changed scope of Random instance for performance improvements and fixed bugs with Date generation in the same year
    #  v1.0.0.3     -  05/19/2019   Changed help display to accurately reflect recent changes
    #  v1.0.0.4     -  05/21/2019   Added support for different encoding types: ASCII, UTF-8, UTF-16 (default), and UTF-32
    #  v1.0.0.5     -  05/21/2019   Made changes to selectivity handle to allow for uniqueness when selectivity == numberOfRows and added warnings
    #  v1.0.0.6     -  05/21/2019   Fixed bug with file creation w/o headers
    #  v1.0.0.7     -  05/22/2019   Changed value list to provide uniqueness when number of values < numberOfRows, added country support, and changed random string to support lower case
    #  v1.0.0.8     -  05/22/2019   Added console updates for selectivity data generation
    #  v1.0.0.9     -  05/22/2019   Added hold termination switch and fixed minor formatting in displayFormatFile()
    #  v1.0.0.10    -  05/23/2019   Fixed length of time issue for stats
    #  v1.0.0.11    -  05/23/2019   Added more detail on progress of creating selectivity sets and loading value lists
    #  v1.0.0.12    -  05/28/2019   Added special feature of string to generate GUIDs when string data types are set to 32 for both min and max
    #  v1.0.0.13    -  05/29/2019   Added ability to specify field and row terminators
    #  v1.0.0.14    -  05/30/2019   Fixed bug in getRandomIntegerAsString for number ranges that min/max values straddle the int32/Int64 boundary.
    #                               Added ability to specific the file extension with /E
    #  v1.0.0.15    -  06/06/2019   Fixed monotonic seed to support Int64
    #  v1.0.0.16    -  06/20/2019   Added special default functions (&ASN, &EQL, &GTR, &LSS) and fixed bug not writing a row terminator at the end of a file
    #  v1.0.0.17    -  07/05/2019   Added new special default &PRO to control the proportional mixture of values taken from more than one value list to set up ratios of values in a table
    #  v1.0.0.18    -  08/03/2019   Fixed bug in new PRO cmd that was causing a failure when evaluation default values < 4 bytes in length (invalid index reference into string)
    #  v1.0.0.19    -  10/25/2019   Fixed bug in encoding files to UTF-8 and others - default changed to UTF-8
    #  v1.0.0.20    -  10/25/2019   Fixed help documentation errors
    #
    #
    */

    class Program
    {
        // console state
        static bool displayHelp = false;
        static ConsoleColor defaultColor = Console.ForegroundColor;
        static bool WAIT_FLAG = false;
        static int WAIT_TIME = 20;
        static DateTime start;
        static DateTime end;
        static bool alternateDateFormat = false;
        static bool relaxSelectivity = false;
        static bool lowerString = false;
        static bool disableUniqueness = false;
        static bool holdTermination = false;

        // Random # generators
        static Random rand = new Random();
        static Random rString = new Random();

        // file reference
        static string FIELD_TERMINATOR = ",";
        static string ROW_TERMINATOR = Environment.NewLine;
        static string FILE_EXTENSION = "csv";
        static string inputFormatFile = string.Empty;
        static string outputDirectory = string.Empty;
        static string outputFileNameConvention = string.Empty;
        static Encoding encode = new UTF8Encoding(false);                    // default UTF-8

        // model ref
        static CsvFormatter _csv;

        // data cache
        static Int64 rowCount = 0;                  // current row count
        static Int64 cacheRows = 0;                 // current cache size
        static Int64 rowsCurrentlyWritten = 0;      // rows currently written to the current file
        static Int64 totalRowsWritten = 0;          // total rows written out
        static int fileCount = 0;                   // current file count
        static Int64 rowsPerFile = 0;               // rows written per file
        
        static int cacheFlushValue = 100000;        // cache flush value for when to flush cache to CSV file
        static int streamFlushValue = 100;          // flush stream buffer
        static string[] currentRow;                 // holds current row of data being generated
        static string[,] cache;                     // holds data to be serialized to CSV
        static string[][] selColumns;               // an array of arrays (jagged array) holding selectivity data per column
        static int currNextSelectIndex = 0;         // current top level index into selectivity j-array
        static string currCsvFile = string.Empty;   // current file for writing

        // special class data domains
        static string[] firstNames = null;      // common firstnames
        static string[] lastNames = null;       // common lastnames
        static string[] streetNames = null;     // common street names in the US
        static string[] cities = null;          // common cities in the US
        static string[] states = null;          // list of 50 US states
        static string[] countries = null;       // countries of the world

        // special class data files
        static string _stateData = string.Format(@"{0}\{1}", Directory.GetCurrentDirectory(), "states.txt");
        static string _cityData = string.Format(@"{0}\{1}", Directory.GetCurrentDirectory(), "cities.txt");
        static string _firstNameData = string.Format(@"{0}\{1}", Directory.GetCurrentDirectory(), "firstnames.txt");
        static string _lastNameData = string.Format(@"{0}\{1}", Directory.GetCurrentDirectory(), "lastnames.txt");
        static string _streetData = string.Format(@"{0}\{1}", Directory.GetCurrentDirectory(), "streets.txt");
        static string _countryData = string.Format(@"{0}\{1}", Directory.GetCurrentDirectory(), "countries.txt");

        // main entry point
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

            // did we get an input file?
            if (inputFormatFile == string.Empty)
            {
                // we must prompt for format
                sendToConsole(ConsoleColor.Yellow, "No format file provided, please provide data type and data range/domain information below.");

                if (defineModel())
                {
                    // we have built the model in memory now we should save it
                    if (!saveModelFormatFile())
                    {
                        sendToConsole(ConsoleColor.Red, "Failed to save model file to disk.");
                        return;
                    }
                    else
                    {
                        string _generateData = "true";

                        sendToConsole(defaultColor, string.Format(@"The model has been saved.  Would you like to generate the data now? [{0} - Enter to accept default]: ", _generateData.ToString().ToUpper()), true, false);
                        _generateData = Console.ReadLine();

                        if (_generateData.ToLower() == "false" || _generateData.ToLower() == "f")
                        {
                            return;
                        }
                    }
                }
                else
                {
                    sendToConsole(ConsoleColor.Red, "Building data model failed.  Exiting.");
                    return;
                }
            }
            else
            {
                // input format file
                if (!loadModelFormatFile())
                {
                    sendToConsole(ConsoleColor.Red, string.Format(@"Failed to load model file at '{0}'", inputFormatFile.ToString()));
                    return;
                }
            }

            // show model
            displayModelFormatFile();

            // check and build special class data domains that are file dependent
            foreach (CsvFormatter.column c in _csv.columns)
            {
                // do any have special domains?
                if (c.specialDataClass != CsvFormatter.SpecialDataClasses.None)
                {
                    // we must ensure we load
                    switch (c.specialDataClass)
                    {
                        case CsvFormatter.SpecialDataClasses.FirstName:

                            // check and get first name data loaded
                            if (firstNames == null)
                            {
                                if (!File.Exists(_firstNameData))
                                {
                                    sendToConsole(ConsoleColor.Red, "First name data not found in the local directory.");
                                    return;
                                }

                                if (!loadFirstNameData())
                                {
                                    sendToConsole(ConsoleColor.Red, "Failed to load required first name data.");
                                    return;
                                }
                            }

                            break;

                        case CsvFormatter.SpecialDataClasses.LastName:

                            // check and get last name data loaded
                            if (lastNames == null)
                            {
                                if (!File.Exists(_lastNameData))
                                {
                                    sendToConsole(ConsoleColor.Red, "Last name data not found in the local directory.");
                                    return;
                                }

                                if (!loadLastNameData())
                                {
                                    sendToConsole(ConsoleColor.Red, "Failed to load required last name data.");
                                    return;
                                }
                            }

                            break;

                        case CsvFormatter.SpecialDataClasses.FullName:

                            // gotta get first and last names
                            if (firstNames == null)
                            {
                                if (!File.Exists(_firstNameData))
                                {
                                    sendToConsole(ConsoleColor.Red, "First name data not found in the local directory.");
                                    return;
                                }

                                if (!loadFirstNameData())
                                {
                                    sendToConsole(ConsoleColor.Red, "Failed to load required first name data.");
                                    return;
                                }
                            }

                            if (lastNames == null)
                            {
                                if (!File.Exists(_lastNameData))
                                {
                                    sendToConsole(ConsoleColor.Red, "Last name data not found in the local directory.");
                                    return;
                                }

                                if (!loadLastNameData())
                                {
                                    sendToConsole(ConsoleColor.Red, "Failed to load required last name data.");
                                    return;
                                }
                            }

                            break;

                        case CsvFormatter.SpecialDataClasses.StreetAddress:

                            // check and get street data loaded
                            if (streetNames == null)
                            {
                                if (!File.Exists(_streetData))
                                {
                                    sendToConsole(ConsoleColor.Red, "Street name data not found in the local directory.");
                                    return;
                                }

                                if (!loadStreetData())
                                {
                                    sendToConsole(ConsoleColor.Red, "Failed to load required street name data.");
                                    return;
                                }
                            }

                            break;

                        case CsvFormatter.SpecialDataClasses.State:

                            // check and get state data loaded
                            if (states == null)
                            {
                                if (!File.Exists(_stateData))
                                {
                                    sendToConsole(ConsoleColor.Red, "State data not found in the local directory.");
                                    return;
                                }

                                if (!loadStateData())
                                {
                                    sendToConsole(ConsoleColor.Red, "Failed to load required state data.");
                                    return;
                                }
                            }

                            break;

                        case CsvFormatter.SpecialDataClasses.Country:

                            // check and get country data loaded
                            if (countries == null)
                            {
                                if (!File.Exists(_countryData))
                                {
                                    sendToConsole(ConsoleColor.Red, "Country data not found in the local directory.");
                                    return;
                                }

                                if (!loadCountryData())
                                {
                                    sendToConsole(ConsoleColor.Red, "Failed to load required country data.");
                                    return;
                                }
                            }

                            break;

                        case CsvFormatter.SpecialDataClasses.City:

                            // check and get city data loaded
                            if (cities == null)
                            {
                                if (!File.Exists(_cityData))
                                {
                                    sendToConsole(ConsoleColor.Red, "City data not found in the local directory.");
                                    return;
                                }

                                if (!loadCityData())
                                {
                                    sendToConsole(ConsoleColor.Red, "Failed to load required city data.");
                                    return;
                                }
                            }

                            break;

                        default:
                            // nothing

                            break;
                    }
                }
            }

            // take timestamp
            start = DateTime.Now;

            // check and build selectivity domains and value list files
            // first we have to determine if we have any special columns and then initialize the top level array
            int specialCount = 0;

            foreach (CsvFormatter.column c in _csv.columns)
            {
                if (c.valueListFile != string.Empty || c.selectivity > -1)
                {
                    specialCount++;
                }
            }

            if (specialCount > 0)
            {
                // initialize array to the correct top level size
                selColumns = new string[specialCount][];

                for (int i = 0; i < _csv.columns.Length; i++)
                {
                    // get reference
                    CsvFormatter.column c = _csv.columns[i];

                    // first, do we have a value list?
                    if (c.valueListFile != string.Empty)
                    {
                        int _idxV = loadValueList(c.valueListFile, c.defaultValue, _csv.numberOfRows);
                        if (_idxV == -1)
                        {
                            sendToConsole(ConsoleColor.Red, string.Format(@"Failed to load value list at location: '{0}'", c.valueListFile.ToString()));
                            return;
                        }
                        else
                        {
                            // data loaded - we set and continue as selectivity is set by the number of values in the list
                            c.selColumn = _idxV;

                            // auto-detect data type
                            string _v = selColumns[_idxV][0];

                            // detect data type
                            if (DateTime.TryParse(_v, out DateTime _date))
                            {
                                c.dataType = CsvFormatter.dataTypes.Date;
                            }
                            else if (Int64.TryParse(_v, out Int64 _int))
                            {
                                c.dataType = CsvFormatter.dataTypes.Integer;
                            }
                            else if (Decimal.TryParse(_v, out Decimal _decimal))
                            {
                                c.dataType = CsvFormatter.dataTypes.Decimal;
                            }
                            else
                            {
                                c.dataType = CsvFormatter.dataTypes.String;
                            }

                            continue;
                        }
                    }

                    // if not a value list, do we have a specified selectivity?
                    if (c.selectivity != -1 && c.selectivity > 0)
                    {
                        int _idxS = createSelectivitySet(i);
                        if (_idxS == -1)
                        {
                            sendToConsole(ConsoleColor.Red, string.Format(@"Failed to generate selectivity data set for column '{0}'", c.columnName.ToString()));
                            return;
                        }
                        else
                        {
                            // data was loaded - we set selColumn
                            c.selColumn = _idxS;
                        }
                    }

                }
            }

            // finally generate the data
            if (!generateCsvData())
            {
                sendToConsole(ConsoleColor.Red, "Failed to generate data.  Please check for previous error messages.");
            }

            // take timestamp
            end = DateTime.Now;

            // write summary
            writeSummary();

            // we're done.
            if (holdTermination)
            {
                sendToConsole(defaultColor, "Data generation has completed.  Press ENTER to terminate.", false, false);
                Console.ReadLine();
            }

        }

        // generate random INT64 as string
        static string getRandomIntegerAsString(Int64 minValue, Int64 maxValue)
        {

            // using the .NET random class has limitations as it only generates INT numbers by default.  If we need large numbers, then we need to use another method - we could move to the cryptography lib which I believe we could use for RNG.
            // so we will check to see what our max is to determine which method to use - the problem is that the large generator only generates large #s so we need to accommodate for that as well

            if (maxValue <= int.MaxValue)
            {
                // we have a basic int range - this wil probably handle 98% of the cases
                return rand.Next((int)minValue, (int)maxValue).ToString();
            }
            else if (maxValue > int.MaxValue && minValue < int.MaxValue)
            {
                // we have a range that covers both - here we don't strictly guarantee absolute min-max range but try to honor the magnitude of the number range requested
                Random selector = new Random();
                int k = minValue.ToString().Length;
                int j = selector.Next(k, 18);
                int v = (j % 2);

                // a basic switch here
                if (v == 1)
                {
                    // a low-order value
                    return rand.Next((int)minValue, int.MaxValue).ToString();
                }
                else
                {
                    // a high-order value
                    // however these always return full 8 bytes which result in 18 or 19 digit #s so we will randomize how much is returned
                    // ** see note below
                    byte[] b = new byte[8];

                    rand.NextBytes(b);
                    string s = BitConverter.ToUInt64(b, 0).ToString();

                    // in some cases the number returned can be shorter (less magnitude) than the random indexer for J
                    // in this case we just return the whole #
                    // we want to return something on the magnitude of the range requested
                    if (s.Length < j)
                    {
                        return s.ToString();
                    }
                    else
                    {
                        return s.Substring(0, j);
                    }
                }
            }
            else
            {
                // we have a range that covers both
                Random selector = new Random();

                int k = minValue.ToString().Length;
                int j = selector.Next(k, 18);

                // a high-order value
                // however these always return full 8 bytes which result in 18 or 19 digit #s so we will randomize how much is returned
                byte[] b = new byte[8];

                rand.NextBytes(b);
                string s = BitConverter.ToUInt64(b, 0).ToString();

                // in some cases the number returned can be shorter (less magnitude) than the random indexer for J
                // in this case we just return the whole #
                // we want to return something on the magnitude of the range requested
                if (s.Length < j)
                {
                    return s.ToString();
                }
                else
                {
                    return s.Substring(0, j);
                }

            }
        }

        // generate random DECIMAL as a string
        static string getRandomDecimal(Int64 minValue, Int64 maxValue, byte mantissa)
        {
            if (mantissa < 1 || mantissa > 8)
            {
                sendToConsole(ConsoleColor.Red, "Mantissa size invalid.  Setting to default value of 2.");
                mantissa = 2;
            }

            string _decimal = getRandomIntegerAsString(minValue, maxValue);
            string _mantissa = getRandomIntegerAsString(int.MaxValue, Int64.MaxValue);
            string _mantissa_reduced = _mantissa.Substring(0, mantissa);

            return string.Format(@"{0}.{1}", _decimal.ToString(), _mantissa_reduced.ToString());
        }

        // generate random STRING 
        static string generateRandomString(Int64 minValue, Int64 maxValue)
        {
            if (minValue < 1)
            {
                sendToConsole(ConsoleColor.Red, "Minumum string length invalid.  Resetting to default value of 10.");
                minValue = 10;
            }

            if (minValue > 2500)
            {
                sendToConsole(ConsoleColor.Red, "Maximum string length invalid.  Resetting to default value of 256.");
                maxValue = 256;
            }

            try
            {
                if (minValue == 32 && maxValue == 32)
                {
                    // special code for GUID
                    return Guid.NewGuid().ToString();
                }

                string size = getRandomIntegerAsString(minValue, maxValue);
                int _size = 16;

                if (!int.TryParse(size, out _size))
                {
                    sendToConsole(ConsoleColor.Yellow, "Failed to generate random value for string length.  Using default of 16.");
                }

                char c;
                StringBuilder b = new StringBuilder();

                for (int i = 0; i < _size; i++)
                {
                    c = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * rString.NextDouble() + 65)));
                    b.Append(c);
                }

                return b.ToString();
            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return "XXXXXXXXX";
            }

        }

        // generate random date
        static string generateRandomDateString(DateTime minValue, DateTime maxValue)
        {
            int minMonth = minValue.Month;
            int minYear = minValue.Year;
            int maxMonth = maxValue.Month;
            int maxYear = maxValue.Year;

            // get year
            string _year = getRandomIntegerAsString(minYear, maxYear);
            string _month;
            string _day;

            // get month
            if (minYear == maxYear)
            {
                // we have the same year
                _month = getRandomIntegerAsString(minMonth, (maxMonth + 1));
            }
            else if (minYear.ToString() == _year)
            {
                // in start year
                _month = getRandomIntegerAsString(minMonth, 12);
            }
            else if (maxYear.ToString() == _year)
            {
                // in ending year
                _month = getRandomIntegerAsString(1, maxMonth);
            }
            else
            {
                _month = getRandomIntegerAsString(1, 12);
            }

            // get the day
            if (_month == "2")
            {
                _day = getRandomIntegerAsString(1, 28);  // we won't worry about leap year
            }
            else if (_month == "4" || _month == "6" || _month == "9" || _month == "11")
            {
                _day = getRandomIntegerAsString(1, 30);
            }
            else
            {
                _day = getRandomIntegerAsString(1, 31);
            }

            if (alternateDateFormat)
            {
                return string.Format(@"{0}-{1}-{2}", _year.ToString(), _month.ToString().PadLeft(2, '0'), _day.ToString().PadLeft(2, '0'));
            }
            else
            {
                return string.Format(@"{0}/{1}/{2}", _month.ToString().PadLeft(2, '0'), _day.ToString().PadLeft(2, '0'), _year.ToString());
            }

        }

        // get random firstname
        static string getRandomFirstName()
        {
            string _index = getRandomIntegerAsString(0, (firstNames.Length - 1));
            int _idx = int.Parse(_index);
            return firstNames[_idx];
        }

        // get random lastname
        static string getRandomLastName()
        {
            string _index = getRandomIntegerAsString(0, (lastNames.Length - 1));
            int _idx = int.Parse(_index);
            return lastNames[_idx];
        }

        // get random full name
        static string getRandomFullName()
        {
            return string.Format(@"{0} {1}", getRandomFirstName(), getRandomLastName());
        }

        // get random street address
        static string getRandomStreetAddress()
        {
            string houseNumber = getRandomIntegerAsString(1, 9999);
            string _index = getRandomIntegerAsString(0, (streetNames.Length - 1));
            int _idx = int.Parse(_index);
            string _streetName = streetNames[_idx];

            return string.Format(@"{0} {1}", houseNumber, _streetName);
        }

        // get random city 
        static string getRandomCity()
        {
            string _index = getRandomIntegerAsString(0, (cities.Length - 1));
            int _idx = int.Parse(_index);
            return cities[_idx];
        }

        // get random state 
        static string getRandomState()
        {
            string _index = getRandomIntegerAsString(0, (states.Length - 1));
            int _idx = int.Parse(_index);
            return states[_idx];
        }

        // get random Country
        static string getRandomCountry()
        {
            string _index = getRandomIntegerAsString(0, (countries.Length - 1));
            int _idx = int.Parse(_index);
            return countries[_idx];
        }

        // get random zip code
        static string getRandomZipCode()
        {
            return getRandomIntegerAsString(10000, 99999);
        }

        // get random SSN
        static string getRandomSSN()
        {
            string n1 = getRandomIntegerAsString(100, 999);
            string n2 = getRandomIntegerAsString(10, 99);
            string n3 = getRandomIntegerAsString(1000, 9999);

            return string.Format(@"{0}-{1}-{2}", n1.ToString(), n2.ToString(), n3.ToString());
        }

        // get random phone #
        static string getRandomPhone()
        {
            string areacode = getRandomIntegerAsString(100, 999);
            string prefix = getRandomIntegerAsString(100, 999);
            string hNumber = getRandomIntegerAsString(1000, 9999);

            return string.Format(@"{0}-{1}-{2}", areacode.ToString(), prefix.ToString(), hNumber.ToString());
        }

        // create selectivity set
        static int createSelectivitySet(int colIndex)
        {
            // save off the current index to return
            int _idx = currNextSelectIndex;

            // we start with a List object to hold transient data
            List<string> _items = new List<string>();

            // get ref to column
            CsvFormatter.column c = _csv.columns[colIndex];

            // update
            sendToConsole(ConsoleColor.Yellow, string.Format(@"Generating selectivity date for column '{0}'", c.columnName.ToString()), true, true);
            sendToConsole(ConsoleColor.Yellow, string.Empty, true, true);

            // get location
            int Left = Console.CursorLeft;
            int Top = Console.CursorTop;

            // check to see if we have a special data type
            if (c.specialDataClass == CsvFormatter.SpecialDataClasses.None)
            {
                // regular data types
                if (c.dataType == CsvFormatter.dataTypes.Integer)
                {
                    Int64 min = Int64.Parse(c.minvalue);
                    Int64 max = Int64.Parse(c.maxValue);

                    while (_items.Count < c.selectivity)
                    {
                        string val = getRandomIntegerAsString(min, max);

                        if (!relaxSelectivity)
                        {
                            if (!_items.Contains(val))
                            {
                                _items.Add(val);
                            }
                        }
                        else
                        {
                            _items.Add(val);
                        }

                        // send update to console
                        selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                    }
                }
                else if (c.dataType == CsvFormatter.dataTypes.String)
                {
                    Int64 min = Int64.Parse(c.minvalue);
                    Int64 max = Int64.Parse(c.maxValue);

                    while (_items.Count < c.selectivity)
                    {
                        string val = generateRandomString(min, max);

                        if (!relaxSelectivity)
                        {
                            if (!_items.Contains(val))
                            {
                                _items.Add(val);
                            }
                        }
                        else
                        {
                            _items.Add(val);
                        }

                        // send update to console
                        selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                    }
                }
                else if (c.dataType == CsvFormatter.dataTypes.Decimal)
                {
                    Int64 min = Int64.Parse(c.minvalue);
                    Int64 max = Int64.Parse(c.maxValue);

                    while (_items.Count < c.selectivity)
                    {
                        string val = getRandomDecimal(min, max, c.mantissa);

                        if (!relaxSelectivity)
                        {
                            if (!_items.Contains(val))
                            {
                                _items.Add(val);
                            }
                        }
                        else
                        {
                            _items.Add(val);
                        }

                        // send update to console
                        selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                    }
                }
                else
                {
                    DateTime min = DateTime.Parse(c.minvalue);
                    DateTime max = DateTime.Parse(c.maxValue);

                    while (_items.Count < c.selectivity)
                    {
                        string val = generateRandomDateString(min, max);

                        if (!relaxSelectivity)
                        {
                            if (!_items.Contains(val))
                            {
                                _items.Add(val);
                            }
                        }
                        else
                        {
                            _items.Add(val);
                        }

                        // send update to console
                        selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                    }
                }
            }
            else
            {
                // we have a special data class
                switch (c.specialDataClass)
                {
                    case CsvFormatter.SpecialDataClasses.City:

                        if (c.selectivity > cities.Length)
                        {
                            sendToConsole(ConsoleColor.Yellow, "Selectivity value exceeds number of available cities.  Relaxing selectivity requirement (duplicates may result).");
                            relaxSelectivity = true;
                        }

                        while (_items.Count < c.selectivity)
                        {
                            string val = getRandomCity();

                            if (!relaxSelectivity)
                            {
                                if (!_items.Contains(val))
                                {
                                    _items.Add(val);
                                }
                            }
                            else
                            {
                                _items.Add(val);
                            }

                            // send update to console
                            selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                        }

                        break;

                    case CsvFormatter.SpecialDataClasses.Country:

                        if (c.selectivity > countries.Length)
                        {
                            sendToConsole(ConsoleColor.Red, "Selectivity value exceeds number of available countries.  Relaxing selectivity requirement (duplicates may result).");
                            relaxSelectivity = true;
                        }

                        while (_items.Count < c.selectivity)
                        {
                            string val = getRandomCountry();

                            if (!relaxSelectivity)
                            {
                                if (!_items.Contains(val))
                                {
                                    _items.Add(val);
                                }
                            }
                            else
                            {
                                _items.Add(val);
                            }

                            // send update to console
                            selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                        }

                        break;

                    case CsvFormatter.SpecialDataClasses.FirstName:

                        if (c.selectivity > firstNames.Length)
                        {
                            sendToConsole(ConsoleColor.Yellow, "Selectivity value exceeds number of available first names.  Relaxing selectivity requirement (duplicates may result).");
                            relaxSelectivity = true;
                        }

                        while (_items.Count < c.selectivity)
                        {
                            string val = getRandomFirstName();

                            if (!relaxSelectivity)
                            {
                                if (!_items.Contains(val))
                                {
                                    _items.Add(val);
                                }
                            }
                            else
                            {
                                _items.Add(val);
                            }

                            // send update to console
                            selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                        }

                        break;

                    case CsvFormatter.SpecialDataClasses.LastName:

                        if (c.selectivity > lastNames.Length)
                        {
                            sendToConsole(ConsoleColor.Yellow, "Selectivity value exceeds number of available last names.  Relaxing selectivity requirement (duplicates may result).");
                            relaxSelectivity = true;
                        }

                        while (_items.Count < c.selectivity)
                        {
                            string val = getRandomLastName();

                            if (!relaxSelectivity)
                            {
                                if (!_items.Contains(val))
                                {
                                    _items.Add(val);
                                }
                            }
                            else
                            {
                                _items.Add(val);
                            }

                            // send update to console
                            selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                        }

                        break;

                    case CsvFormatter.SpecialDataClasses.FullName:

                        while (_items.Count < c.selectivity)
                        {
                            string val = getRandomFullName();

                            if (!relaxSelectivity)
                            {
                                if (!_items.Contains(val))
                                {
                                    _items.Add(val);
                                }
                            }
                            else
                            {
                                _items.Add(val);
                            }

                            // send update to console
                            selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                        }

                        break;

                    case CsvFormatter.SpecialDataClasses.Phone:

                        while (_items.Count < c.selectivity)
                        {
                            string val = getRandomPhone();

                            if (!relaxSelectivity)
                            {
                                if (!_items.Contains(val))
                                {
                                    _items.Add(val);
                                }
                            }
                            else
                            {
                                _items.Add(val);
                            }

                            // send update to console
                            selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                        }

                        break;

                    case CsvFormatter.SpecialDataClasses.SSN:

                        while (_items.Count < c.selectivity)
                        {
                            string val = getRandomSSN();

                            if (!relaxSelectivity)
                            {
                                if (!_items.Contains(val))
                                {
                                    _items.Add(val);
                                }
                            }
                            else
                            {
                                _items.Add(val);
                            }

                            // send update to console
                            selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                        }

                        break;

                    case CsvFormatter.SpecialDataClasses.State:

                        if (c.selectivity > states.Length)
                        {
                            sendToConsole(ConsoleColor.Yellow, "Selectivity value exceeds number of available states.  Relaxing selectivity requirement (duplicates may result).");
                            relaxSelectivity = true;
                        }

                        while (_items.Count < c.selectivity)
                        {
                            string val = getRandomState();

                            if (!relaxSelectivity)
                            {
                                if (!_items.Contains(val))
                                {
                                    _items.Add(val);
                                }
                            }
                            else
                            {
                                _items.Add(val);
                            }

                            // send update to console
                            selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                        }

                        break;

                    case CsvFormatter.SpecialDataClasses.StreetAddress:

                        while (_items.Count < c.selectivity)
                        {
                            string val = getRandomStreetAddress();

                            if (!relaxSelectivity)
                            {
                                if (!_items.Contains(val))
                                {
                                    _items.Add(val);
                                }
                            }
                            else
                            {
                                _items.Add(val);
                            }

                            // send update to console
                            selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                        }

                        break;

                    case CsvFormatter.SpecialDataClasses.ZipCode:

                        while (_items.Count < c.selectivity)
                        {
                            string val = getRandomZipCode();

                            if (!relaxSelectivity)
                            {
                                if (!_items.Contains(val))
                                {
                                    _items.Add(val);
                                }
                            }
                            else
                            {
                                _items.Add(val);
                            }

                            // send update to console
                            selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top);
                        }

                        break;

                    default:

                       // do nothing

                        break;
                }
            }


            // send update to console
            selectivityGenerationUpdate(_items.Count, c.selectivity, Left, Top, _items.Count);

            // convert into selectivity j-array
            selColumns[_idx] = _items.ToArray();
            currNextSelectIndex++;

            return _idx;
        }

        // send update during selectivity generation
        static void selectivityGenerationUpdate(long count, long total, int Left, int Top, int updateLevel = 100)
        {
            string update = @"PROGRESS >> {0} rows of selectivity data generated of {1} total rows. {2}";

            // send update to console
            if ((count % updateLevel) == 0)
            {
                // update console
                Console.CursorLeft = Left;
                Console.CursorTop = Top;
                sendToConsole(ConsoleColor.Yellow, string.Format(update.ToString(), count.ToString("000000"), total.ToString("000000"), " ".PadRight(30)));
            }
        }

        // get value list
        static int loadValueList(string valueListPath, string defaultValue, Int64 numberOfRows)
        {
            // we need to accommodate that we may have multiple list here
            List<string> _masterlist = new List<string>();
            string[] _valueFiles = valueListPath.Split(new char[] { ';' });
            string _cmd = "undefined";
            string[] _sizes = null;

            // we need to determine if we have a proportion defined
            if (defaultValue.Length > 4 && defaultValue.Substring(0,1) == "&")
            {
                _cmd = defaultValue.Substring(0, 4);

                // we have a command - but is it a proportion?
                if (_cmd == "&PRO")
                {
                    _sizes = defaultValue.Substring(5).Trim(new char[] { '[', ']' }).Split(new char[] { ',' });
                }
            }

            // if we have a command for &PRO and the sizes of the value lists and proportions don't match, something is wrong
            if (_cmd == "&PRO" && _sizes != null)
            {
                if (_valueFiles.Length != _sizes.Length)
                {
                    sendToConsole(ConsoleColor.Red, "For the &PRO command, the number of values lists provided must match the number of proportions provided.");
                    return -1;
                }
            }

            // in all cases, if you only provided 1 value list file, we'll load everything from that one file
            if (_valueFiles.Length == 1)
            {
                _sizes = new string[] { "1" };
            }

            // make sure sizes add up and are integers
            decimal accum = 0m;
            for (int s = 0; s < _sizes.Length; s++)
            {
                decimal x = 0m;
                bool _result = decimal.TryParse(_sizes[s].ToString(), out x);

                if (!_result)
                {
                    sendToConsole(ConsoleColor.Red, "One of the sizes provided by the proportion command is an incorrect value.");
                    return -1;
                }
                else
                {
                    accum += x;
                }
            }

            // final check here
            if (accum < 1.0m)
            {
                sendToConsole(ConsoleColor.Red, "Total sum of sizes provided by the proportion command do not add up to be 1.0");
                return -1;
            }
            
            // record current index
            int _idx = currNextSelectIndex;

            // console update format
            string update = @"PROGRESS >> {0} values loaded from '{1}'. {2}";

            try
            {
                for (int w = 0; w < _valueFiles.Length; w++)
                {
                    // create list to hold
                    List<string> _values = new List<string>();

                    sendToConsole(defaultColor, string.Format(@"Loading value list at location: '{0}'", _valueFiles[w].ToString()), true, true);

                    // Get position
                    int Left = Console.CursorLeft;
                    int Top = Console.CursorTop;
                    int count = 0;

                    StreamReader sr = new StreamReader(_valueFiles[w]);

                    while (!sr.EndOfStream)
                    {
                        string _currentValue = sr.ReadLine();
                        _values.Add(_currentValue.Trim());
                        count++;

                        // provide an update for every 100 rows
                        if ((count % 100) == 0)
                        {
                            Console.CursorLeft = Left;
                            Console.CursorTop = Top;
                            sendToConsole(ConsoleColor.Yellow, string.Format(update.ToString(), count.ToString("000000"), _valueFiles[w].ToString(), " ".PadRight(30)));
                        }
                    }

                    // final update
                    Console.CursorLeft = Left;
                    Console.CursorTop = Top;
                    sendToConsole(ConsoleColor.Yellow, string.Format(update.ToString(), count.ToString("000000"), valueListPath.ToString(), " ".PadRight(30)));

                    // write array into temporary array
                    string[] _temp = _values.ToArray();

                    // now build proportions proprotions
                    decimal _proportion = decimal.Parse(_sizes[w].ToString());
                    int rowCount = (int)(_proportion * numberOfRows);

                    // do we have enough proportional rows for the whole table?
                    if (rowCount < _temp.Length)
                    {
                        for (int p = 0; p < rowCount; p++)
                        {
                            _masterlist.Add(_temp[p]);
                        }
                    }
                    else
                    {
                        for (int t = 0; t < _temp.Length; t++)
                        {
                            _masterlist.Add(_temp[t]);
                        }
                    }
                }

                // assign
                selColumns[_idx] = _masterlist.ToArray();

                // increment index
                currNextSelectIndex++;

                // return location
                return _idx;
                
            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return -1;
            }

        }

        // technically just gets a new file name to write to
        static string initializeNewCsvDataFile()
        {

            string _filename = string.Empty;
            if (outputFileNameConvention != string.Empty)
            {
                _filename = string.Format(@"{0}\{1}_{2}_{3}.{4}", outputDirectory.ToString(), outputFileNameConvention.ToString(), _csv.tableName.ToString(), fileCount.ToString("000"), FILE_EXTENSION.ToString().Replace(".", ""));
            }
            else
            {
                _filename = string.Format(@"{0}\{1}_{2}.{3}", outputDirectory.ToString(), _csv.tableName.ToString(), fileCount.ToString("000"), FILE_EXTENSION.ToString().Replace(".", ""));
            }

            if (_csv.printColumnNames)
            {
                string _headers = string.Empty;
                int cnt = 0;
                foreach (CsvFormatter.column c in _csv.columns)
                {
                    if (cnt > 0)
                    {
                        _headers += ",";
                    }

                    _headers += c.columnName.ToString();
                    cnt++;
                }

                // add row terminator
                _headers += ROW_TERMINATOR;

                File.WriteAllText(_filename, _headers, encode);
            }
            else
            {
                File.Create(_filename).Close();
            }

            fileCount++;    // increment file count
            return _filename;
        }

        // load state data
        static bool loadStateData()
        {
            List<string> _states = new List<string>();

            try
            {
                sendToConsole(defaultColor, "Loading state data...");

                StreamReader sr = new StreamReader(_stateData);

                while (!sr.EndOfStream)
                {
                    string _currentState = sr.ReadLine();
                    _states.Add(_currentState.Trim());
                }

                states = _states.ToArray();
            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return false;
            }

            return true;
        }

        // load country data
        static bool loadCountryData()
        {
            List<string> _countries = new List<string>();

            try
            {
                sendToConsole(defaultColor, "Loading country data...");

                StreamReader sr = new StreamReader(_countryData);

                while (!sr.EndOfStream)
                {
                    string _currentState = sr.ReadLine();
                    _countries.Add(_currentState.Trim());
                }

                countries = _countries.ToArray();
            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return false;
            }

            return true;
        }

        // load city data
        static bool loadCityData()
        {
            List<string> _cities = new List<string>();

            try
            {
                sendToConsole(defaultColor, "Loading city data...");

                StreamReader sr = new StreamReader(_cityData);

                while (!sr.EndOfStream)
                {
                    string _currentCity = sr.ReadLine();
                    _cities.Add(_currentCity.Trim());
                }

                cities = _cities.ToArray();
            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return false;
            }

            return true;
        }

        // load street data
        static bool loadStreetData()
        {
            List<string> _streets = new List<string>();

            try
            {
                sendToConsole(defaultColor, "Loading street name data...");

                StreamReader sr = new StreamReader(_streetData);

                while (!sr.EndOfStream)
                {
                    string _currentStreet = sr.ReadLine();
                    _streets.Add(_currentStreet.Trim());
                }

                streetNames = _streets.ToArray();
            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return false;
            }

            return true;
        }

        // load firstname data
        static bool loadFirstNameData()
        {
            List<string> _firstnames = new List<string>();

            try
            {
                sendToConsole(defaultColor, "Loading first name data...");

                StreamReader sr = new StreamReader(_firstNameData);

                while (!sr.EndOfStream)
                {
                    string _currentFirstName = sr.ReadLine();
                    _firstnames.Add(_currentFirstName.Trim());
                }

                firstNames = _firstnames.ToArray();
            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return false;
            }

            return true;
        }

        // load lastname data
        static bool loadLastNameData()
        {
            List<string> _lastnames = new List<string>();

            try
            {
                sendToConsole(defaultColor, "Loading last name data...");

                StreamReader sr = new StreamReader(_lastNameData);

                while (!sr.EndOfStream)
                {
                    string _currentLastName = sr.ReadLine();
                    _lastnames.Add(_currentLastName.Trim());
                }

                lastNames = _lastnames.ToArray();
            }
            catch (Exception ex)
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

                    case "I":
                        // input format file
                        inputFormatFile = args[i].Substring(2);

                        if(!File.Exists(inputFormatFile))
                        {
                            sendToConsole(ConsoleColor.Red, "Input file not found.");
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

                    case "N":
                        // file naming format
                        outputFileNameConvention = args[i].Substring(2);

                        break;

                    case "F":
                        // cache flush value
                        string cfv = args[i].Substring(2);

                        // assign to var
                        if (!int.TryParse(cfv.ToString(), out cacheFlushValue))
                        {
                            sendToConsole(ConsoleColor.Red, "Incorrect cache flush value.");
                            return false;
                        }

                        break;

                    case "W":
                        // turn on wait flag for random # generation
                        WAIT_FLAG = true;

                        break;

                    case "D":
                        // use alternate date format
                        alternateDateFormat = true;

                        break;

                    case "V":
                        // relax selectivity format
                        relaxSelectivity = true;

                        break;

                    case "A":
                        // use ASCII encoding
                        encode = new ASCIIEncoding();

                        break;

                    case "U":
                        // change to UTF-8, 16, or 32 encoding
                        string _encode = args[i].Substring(2);
                        int _e = 8;

                        // assign to var
                        if (!int.TryParse(_encode.ToString(), out _e))
                        {
                            sendToConsole(ConsoleColor.Red, "Incorrect encoding value.");
                            return false;
                        }

                        if (_e == 16)
                        {
                            encode = new UnicodeEncoding(false, false);
                        }
                        else if (_e == 32)
                        {
                            encode = new UTF32Encoding(false, false);
                        }
                        else
                        {
                            encode = new UTF8Encoding(false);
                        }

                        break;

                    case "L":
                        // lower case strings
                        lowerString = true;

                        break;

                    case "X":
                        // disable uniqueness
                        disableUniqueness = true;

                        break;

                    case "C":
                        // hold termination
                        holdTermination = true;

                        break;

                    case "T":
                        FIELD_TERMINATOR = args[i].Substring(2);

                        break;

                    case "R":
                        // row terminator
                        ROW_TERMINATOR = args[i].Substring(2);

                        break;

                    case "E":
                        // file extension
                        FILE_EXTENSION = args[i].Substring(2);

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
            help.Append("csvdatagen - CSV File Data Generation Tool for testing.");
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
            help.Append("/I              Input format file that describes the data structure for the csv (columns & rows).");
            help.Append(Environment.NewLine);
            help.Append("/O              Output directory for CSV files.");
            help.Append(Environment.NewLine);
            help.Append("/N              Naming convention for CSV files (a ordinal number will be appended).");
            help.Append(Environment.NewLine);
            help.Append("/F              Cache flush value that sets how many internal random rows of data are generated before being flushed to the CSV file.   Default is 100,000.");
            help.Append(Environment.NewLine);
            help.Append("/D              Use alternate date format of YYYY-MM-DD as opposed to MM/DD/YYYY.");
            help.Append(Environment.NewLine);
            help.Append("/V              Relax selectivity requirement - selectivity will be on a best effort basis.");
            help.Append(Environment.NewLine);
            help.Append("/A              Use ASCII encoding (default is Unicode UTF-8).");
            help.Append(Environment.NewLine);
            help.Append("/U              Use UTF-8 (/U8), UTF-16 (/U16), or UTF-32 (/U32) encoding (default is Unicode UTF-8).");
            help.Append(Environment.NewLine);
            help.Append("/L              Use lower case for random strings.");
            help.Append(Environment.NewLine);
            help.Append("/X              Disable uniqueness handling for value list.");
            help.Append(Environment.NewLine);
            help.Append("/C              Holds 'Termination' at the end of generating csv data so summary stats and results can be read before the window closes (commonly used when automatically launching csvdatagen).");
            help.Append(Environment.NewLine);
            help.Append("/T              Specify the field terminator.  Default is a comma.");
            help.Append(Environment.NewLine);
            help.Append("/R              Specify the row terminator.  Default is CRLF.");
            help.Append(Environment.NewLine);
            help.Append("/E              Specify the file extension to use.  The default is .csv.");
            help.Append(Environment.NewLine);
            help.Append(Environment.NewLine);


            sendToConsole(defaultColor, help.ToString());

        }

        // Prompts to build a data model
        static bool defineModel()
        {
            // lead user through defining the model

            // create new model instance
            _csv = new CsvFormatter();

            // start with table name
            bool _tableValid = false;
            while(!_tableValid)
            {
                sendToConsole(defaultColor, "Enter the table name: ", false, false);
                _csv.tableName = Console.ReadLine();

                int _firstChar;
                if (_csv.tableName.Length > 0 && !int.TryParse(_csv.tableName.Substring(0,1), out _firstChar) && !checkForSpecialCharacters(_csv.tableName))
                {
                    _tableValid = true;
                }
                else
                {
                    sendToConsole(ConsoleColor.Red, "Table name is invalid.  Table names must begin with a character and may not contain any special characters.");
                }
            }

            // get the number of rows to generate
            Int64 _rows = 0;
            while (_rows < 1)
            {
                sendToConsole(defaultColor, "Enter the number of rows to generate: ", false, false);

                if (!Int64.TryParse(Console.ReadLine(), out _rows))
                {
                    sendToConsole(ConsoleColor.Red, string.Format(@"Invalid number of rows.  The value must be a number between {0} and {1}", "1", Int64.MaxValue.ToString()));
                }
                else
                {
                    if (_rows == 0)
                    {
                        sendToConsole(ConsoleColor.Red, string.Format(@"Invalid number of rows.  The value must be a number between {0} and {1}", "1", Int64.MaxValue.ToString()));
                    }
                    else
                    {
                        _csv.numberOfRows = _rows;
                    }
                }
            }

            // get the number of file to generate
            int _files = 0;
            while (_files < 1)
            {
                sendToConsole(defaultColor, "Enter the number of files to write to: ", false, false);
                
                if (!int.TryParse(Console.ReadLine(), out _files))
                {
                    sendToConsole(ConsoleColor.Red, string.Format(@"Invalid number of files.  The value must be a number between {0} and {1}", "1", "10000"));
                }
                else
                {
                    if (_files == 0)
                    {
                        sendToConsole(ConsoleColor.Red, string.Format(@"Invalid number of files.  The value must be a number between {0} and {1}", "1", "10000"));
                    }
                    else
                    {
                        _csv.numberOfFiles = _files;
                    }
                }
            }

            // get the number of columns to create
            int _cols = 0;
            while (_cols < 1)
            {
                sendToConsole(defaultColor, "Enter the number of columns to create: ", false, false);

                if (!int.TryParse(Console.ReadLine(), out _cols))
                {
                    sendToConsole(ConsoleColor.Red, string.Format(@"Invalid number of columns.  The value must be a number between {0} and {1}", "1", "256"));
                }
                else
                {
                    if (_cols < 1 || _cols > 256)
                    {
                        sendToConsole(ConsoleColor.Red, string.Format(@"Invalid number of columns.  The value must be a number between {0} and {1}", "1", "256"));
                    }
                    else
                    {
                        _csv.numberOfCols = _cols;
                    }
                }
            }

            // print header information?
            string _printheaders = "false";

            sendToConsole(defaultColor, string.Format(@"Print column names on the first line of each file [{0} - Enter to accept default]: ", _printheaders.ToString().ToUpper()), false, false);
            _printheaders = Console.ReadLine();

            if (_printheaders.ToLower() == "true" || _printheaders.ToLower() == "t")
            {
                _csv.printColumnNames = true;
            }
            else
            {
                _csv.printColumnNames = false;
            }

            // now we must create the columns
            // first we have to size the array in the csv formatter according to the # of columns entered
            _csv.columns = new CsvFormatter.column[_cols];

            // now we loop and get the column information
            for (int i = 0; i < _csv.columns.Length; i++)
            {
                // create a new column
                CsvFormatter.column c = new CsvFormatter.column();

                sendToConsole(ConsoleColor.Cyan, string.Format(@"{0}==> COLUMN {1}: ", "#".PadRight(80, '#'), (i + 1).ToString()));

                // get the name of the column
                bool _colNameValid = false;
                while (!_colNameValid)
                {
                    string _colName = string.Format(@"col{0}", (i + 1).ToString("000"));
                    sendToConsole(defaultColor, string.Format(@"Enter the name of column [{0} - Enter to accept default]: ", _colName.ToString()), false, false);
                    _colName = Console.ReadLine();

                    // set default
                    if (_colName == string.Empty && _colName.Length == 0)
                    {
                        _colName = string.Format(@"col{0}", (i + 1).ToString("000"));
                    }

                    int _firstChar;
                    if (!int.TryParse(_colName.Substring(0,1), out _firstChar) && !checkForSpecialCharacters(_colName))
                    {
                        _colNameValid = true;
                        c.columnName = _colName;
                    }
                    else
                    {
                        sendToConsole(ConsoleColor.Red, "Column name is invalid.  Column names must begin with a character.");
                    }
                }

                // get any default value
                sendToConsole(defaultColor, "Enter a default value for this column (this value will AUTOMATICALLY be used for every row) [PRESS ENTER FOR NO DEFAULT VALUE]: ", false, false);
                string _defaultValue = Console.ReadLine();
                string _cmd = string.Empty;

                if (_defaultValue.Length > 0)
                {
                    // for interactive mode, we need to see if the default value contains a special default or command = function - if so it'll be over 4 chars first so we don't index past the end
                    if (_defaultValue.Length > 4)
                    {
                        _cmd = _defaultValue.Substring(0, 4);

                        // this could be a command and only for &ASN do we need to continue to get a value list - safety checks for &GTR and &LSS
                        if (_cmd.ToUpper() == "&GTR")
                        {
                            bool _val = false;

                            while (!_val)
                            {
                                sendToConsole(defaultColor, "Enter the maximum integer value for this column: ", false, false);
                                string _maxValueGTR = Console.ReadLine();
                                Int64 _m;

                                _val = Int64.TryParse(_maxValueGTR.ToString(), out _m);

                                if (!_val)
                                {
                                    sendToConsole(ConsoleColor.Red, "You must enter a valid interger value.");
                                    _maxValueGTR = string.Empty;
                                }
                                else
                                {
                                    c.maxValue = _maxValueGTR;
                                }
                            }
                        }

                        if (_cmd.ToUpper() == "&LSS")
                        {
                            bool _val = false;

                            while (!_val)
                            {
                                sendToConsole(defaultColor, "Enter the minimum integer value for this column: ", false, false);
                                string _maxValueLSS = Console.ReadLine();
                                Int64 _m;

                                _val = Int64.TryParse(_maxValueLSS.ToString(), out _m);

                                if (!_val)
                                {
                                    sendToConsole(ConsoleColor.Red, "You must enter a valid interger value.");
                                    _maxValueLSS = string.Empty;
                                }
                                else
                                {
                                    c.minvalue = _maxValueLSS;
                                }
                            }
                        }

                        if (_cmd.ToUpper() == "&PRO")
                        {
                            string[] _sizes = _defaultValue.Substring(5).Trim(new char[] { '[', ']' }).Split(new char[] { ',' });

                            // just checking that we have integers here
                            // make sure sizes add up and are integers
                            decimal accum = 0m;
                            for (int s = 0; s < _sizes.Length; s++)
                            {
                                decimal x = 0m;
                                bool _result = decimal.TryParse(_sizes[s].ToString(), out x);

                                if (!_result)
                                {
                                    sendToConsole(ConsoleColor.Red, "One of the sizes provided by the proportion command is an incorrect value.");
                                    return false;
                                }
                                else
                                {
                                    accum += x;
                                }
                            }

                            // final check here
                            if (accum < 1.0m)
                            {
                                sendToConsole(ConsoleColor.Red, "Total sum of sizes provided by the proportion command do not add up to be 1.0");
                                return false;
                            }

                        }

                        // for all we do this
                        c.dataType = CsvFormatter.dataTypes.String;
                        c.defaultValue = _defaultValue;
                        _csv.columns[i] = c;

                        if (_cmd.ToUpper() != "&ASN" && _cmd.ToUpper() != "&PRO")
                        {
                            continue;
                        }
                    }
                    else
                    {
                        // not long enough to be a command so we know it is a normal default
                        c.dataType = CsvFormatter.dataTypes.String;
                        c.defaultValue = _defaultValue;
                        _csv.columns[i] = c;
                        continue;
                    }
                }

                // get any value list file
                string _valueListFile = "undefined";
                string[] _valueListFilesArray;

                while (_valueListFile == "undefined")
                {
                    sendToConsole(defaultColor, "Enter the full path for a file with a preset list of values [PRESS ENTER FOR NO VALUE LIST FILE]: ", false, false);
                    _valueListFile = Console.ReadLine();

                    if (_valueListFile.Length > 0)
                    {
                        // we must check for multiple files
                        _valueListFilesArray = _valueListFile.Split(new char[] { ';' });

                        for (int v = 0; v < _valueListFilesArray.Length; v++)
                        {
                            if (!File.Exists(_valueListFilesArray[v]))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"One or more of the provided files [{0}] could be found or does not exist.", _valueListFilesArray[v].ToString()));
                                _valueListFilesArray = null;
                                _valueListFile = "undefined";
                            }
                        }

                        if (_cmd.ToUpper() == "&ASN" && _valueListFilesArray.Length > 1)
                        {
                            sendToConsole(ConsoleColor.Red, "You can only provide one value list file for the &ASN command.");
                            _valueListFilesArray = null;
                            _valueListFile = "undefined";
                        }
                    }
                    else
                    {
                        // nothing was provided which is fine unless we require it for &ASN or &PRO
                        if (_cmd.ToUpper() == "&ASN")
                        {
                            sendToConsole(ConsoleColor.Red, "You must provide a value list file for the &ASN command in the special default.");
                            _valueListFile = "undefined";
                        }

                        if (_cmd.ToUpper() == "&PRO")
                        {
                            sendToConsole(ConsoleColor.Red, "You must provide one or more value list files for the &PRO command in the special default.");
                            _valueListFile = "undefined";
                        }
                    }
                }

                // if they provided a value we're done
                if (_valueListFile != string.Empty)
                {
                    c.dataType = CsvFormatter.dataTypes.String;
                    c.valueListFile = _valueListFile;
                    _csv.columns[i] = c;
                    continue;
                }

                // get the data type of the column
                int _dataType = 0;
                while (_dataType < 1 || _dataType > 4)
                {
                    // set current to accept default value
                    _dataType = (int)c.dataType;
                    sendToConsole(defaultColor, string.Format(@"Enter the data type value for the column (1=STRING; 2=DATE; 3=INTEGER; 4=DECIMAL) [{0} - Enter to accept default]: ", c.dataType.ToString().ToUpper()), false, false);
                    string _dataTypeValue = Console.ReadLine();

                    if (_dataTypeValue == string.Empty)
                    {
                        // default value
                        _dataTypeValue = ((int)c.dataType).ToString();
                    }

                    if (!int.TryParse(_dataTypeValue.ToString(), out _dataType))
                    {
                        sendToConsole(ConsoleColor.Red, "You must enter a data type selection between 1 and 4.");
                    }
                    else
                    {
                        if (_dataType < 1 || _dataType > 4)
                        {
                            sendToConsole(ConsoleColor.Red, "You must enter a data type selection between 1 and 4.");
                        }
                        else
                        {
                            c.dataType = (CsvFormatter.dataTypes)_dataType;
                        }
                    }
                }

                // get special data type class but only if STRING
                if (c.dataType == CsvFormatter.dataTypes.String)
                {
                    int _specialDataType = 0;
                    while (_specialDataType < 1 || _specialDataType > 11)
                    {
                        // set to allow breaking out and accepting default
                        _specialDataType = (int)c.specialDataClass;
                        int cnt = 1;

                        sendToConsole(defaultColor, "", false, true);
                        foreach (CsvFormatter.SpecialDataClasses s in Enum.GetValues(typeof(CsvFormatter.SpecialDataClasses)))
                        {
                            sendToConsole(defaultColor, string.Format(@"  {0} = {1}", cnt.ToString(), s.ToString()));
                            cnt++;
                        }
                        sendToConsole(defaultColor, "", false, true);

                        sendToConsole(defaultColor, string.Format(@"Select if this column is a special, well-known data type [{0} - Enter to accept default]: ", c.specialDataClass.ToString().ToUpper()), false, false);
                        string _specClass = Console.ReadLine();

                        // allow for accepting default
                        if (_specClass == string.Empty && _specClass.Length == 0)
                        {
                            _specClass = ((int)c.specialDataClass).ToString();
                        }

                        if (!int.TryParse(_specClass, out _specialDataType))
                        {
                            sendToConsole(ConsoleColor.Red, "You must enter a selection between 1 and 11.");
                        }
                        else
                        {
                            if (_specialDataType < 1 || _specialDataType > 11)
                            {
                                sendToConsole(ConsoleColor.Red, "You must enter a selection between 1 and 11.");
                            }
                            else
                            {
                                // all special data types require a value of STRING for their data type
                                if (_specialDataType > 1)
                                {
                                    // we have a special data type, so the column data type should be set to STRING
                                    c.dataType = CsvFormatter.dataTypes.String;
                                }
                                c.specialDataClass = (CsvFormatter.SpecialDataClasses)_specialDataType;
                            }
                        }
                    }
                }
                else
                {
                    c.specialDataClass = CsvFormatter.SpecialDataClasses.None;
                }

                // get min value - this depends on the data type
                if (c.specialDataClass == CsvFormatter.SpecialDataClasses.None)
                {
                    string _minValue = string.Empty;

                    while (_minValue == string.Empty || _minValue.Length == 0)
                    {
                        if (c.dataType == CsvFormatter.dataTypes.Integer)
                        {
                            sendToConsole(defaultColor, "Enter the minimum integer value for this column: ", false, false);
                            _minValue = Console.ReadLine();
                            Int64 _m;

                            if (!Int64.TryParse(_minValue.ToString(), out _m))
                            {
                                sendToConsole(ConsoleColor.Red, "You must enter a valid interger value.");
                                _minValue = string.Empty;
                            }
                            else
                            {
                                c.minvalue = _minValue;
                            }
                        }
                        else if (c.dataType == CsvFormatter.dataTypes.String)
                        {
                            sendToConsole(defaultColor, "Enter the minimum length for string data in this column: ", false, false);
                            _minValue = Console.ReadLine();
                            Int64 _m;

                            if (!Int64.TryParse(_minValue.ToString(), out _m))
                            {
                                sendToConsole(ConsoleColor.Red, "You must enter a valid interger value.");
                                _minValue = string.Empty;
                            }
                            else if (_m > 2500)
                            {
                                // for performance reasons will enforce a limit on string data
                                sendToConsole(ConsoleColor.Red, "String data cannot be longer than 2500 characters.");
                                _minValue = string.Empty;
                            }
                            else
                            {
                                c.minvalue = _minValue;
                            }
                        }
                        else if (c.dataType == CsvFormatter.dataTypes.Date)
                        {
                            sendToConsole(defaultColor, "Enter the minimum date value for this column: ", false, false);
                            _minValue = Console.ReadLine();
                            DateTime _m;

                            if (!DateTime.TryParse(_minValue.ToString(), out _m))
                            {
                                sendToConsole(ConsoleColor.Red, "You must enter a valid date.");
                                _minValue = string.Empty;
                            }
                            else if (_m.Day != 1)
                            {
                                sendToConsole(ConsoleColor.Red, "The minimum date must be the start of the month (i.e. 02/01/1976 or 08/01/1999).");
                                _minValue = string.Empty;
                            }
                            else
                            {
                                c.minvalue = _minValue;
                            }
                        }
                        else
                        {
                            sendToConsole(defaultColor, "Enter the minimum decimal value for this column: ", false, false);
                            _minValue = Console.ReadLine();
                            Decimal _m;

                            if (!Decimal.TryParse(_minValue.ToString(), out _m))
                            {
                                sendToConsole(ConsoleColor.Red, "You must enter a valid decimal value.");
                                _minValue = string.Empty;
                            }
                            else
                            {
                                c.minvalue = _minValue;
                            }
                        }
                    }
                }

                // get max value - this depends on the data type
                if (c.specialDataClass == CsvFormatter.SpecialDataClasses.None)
                {
                    string _maxValue = string.Empty;

                    while (_maxValue == string.Empty || _maxValue.Length == 0)
                    {
                        if (c.dataType == CsvFormatter.dataTypes.Integer)
                        {
                            sendToConsole(defaultColor, "Enter the maximum integer value for this column: ", false, false);
                            _maxValue = Console.ReadLine();
                            Int64 _m;

                            if (!Int64.TryParse(_maxValue.ToString(), out _m))
                            {
                                sendToConsole(ConsoleColor.Red, "You must enter a valid interger value.");
                                _maxValue = string.Empty;
                            }
                            else if (_m <= int.Parse(c.minvalue.ToString()))
                            {
                                sendToConsole(ConsoleColor.Red, "The maximum integer value cannot be less than or equal to the minimum value.");
                                _maxValue = string.Empty;
                            }
                            else
                            {
                                c.maxValue = _maxValue;
                            }
                        }
                        else if (c.dataType == CsvFormatter.dataTypes.String)
                        {
                            sendToConsole(defaultColor, "Enter the maximum length for string data in this column: ", false, false);
                            _maxValue = Console.ReadLine();
                            Int64 _m;

                            if (!Int64.TryParse(_maxValue.ToString(), out _m))
                            {
                                sendToConsole(ConsoleColor.Red, "You must enter a valid interger value.");
                                _maxValue = string.Empty;
                            }
                            else if (_m <= int.Parse(c.minvalue.ToString()) && _m != 32)  // if both min and max are set to 32, then we generate GUIDs
                            {
                                sendToConsole(ConsoleColor.Red, "The maximum length of the column cannot be less than or equal to the minimum length of the column.");
                                _maxValue = string.Empty;
                            }
                            else if (_m > 2500)
                            {
                                // for performance reasons will enforce a limit on string data
                                sendToConsole(ConsoleColor.Red, "String data cannot be longer than 2500 characters.");
                                _maxValue = string.Empty;
                            }
                            else
                            {
                                c.maxValue = _maxValue;
                            }
                        }
                        else if (c.dataType == CsvFormatter.dataTypes.Date)
                        {
                            sendToConsole(defaultColor, "Enter the maximum date value for this column: ", false, false);
                            _maxValue = Console.ReadLine();
                            DateTime _m;

                            if (!DateTime.TryParse(_maxValue.ToString(), out _m))
                            {
                                sendToConsole(ConsoleColor.Red, "You must enter a valid date.");
                                _maxValue = string.Empty;
                            }
                            else if (_m <= DateTime.Parse(c.minvalue.ToString()))
                            {
                                sendToConsole(ConsoleColor.Red, "The maximum date cannot be before or the same as the minimum date.");
                                _maxValue = string.Empty;
                            }
                            else if (_m.Day < 28 || _m.Day > 31)
                            {
                                sendToConsole(ConsoleColor.Red, "The maximum date must be the end of the month (i.e. 02/28/1976 or 08/31/1999). ");
                                _maxValue = string.Empty;
                            }
                            else
                            {
                                c.maxValue = _maxValue;
                            }
                        }
                        else
                        {
                            sendToConsole(defaultColor, "Enter the maximum decimal value for this column: ", false, false);
                            _maxValue = Console.ReadLine();
                            Decimal _m;

                            if (!Decimal.TryParse(_maxValue.ToString(), out _m))
                            {
                                sendToConsole(ConsoleColor.Red, "You must enter a valid decimal value.");
                                _maxValue = string.Empty;
                            }
                            else if (_m <= Decimal.Parse(c.minvalue))
                            {
                                sendToConsole(ConsoleColor.Red, "The maximum decimal value cannot be less than or equal to the minimum decimal value.");
                                _maxValue = string.Empty;
                            }
                            else
                            {
                                c.maxValue = _maxValue;
                            }
                        }
                    }
                }

                // is it monotonic 
                if (c.dataType == CsvFormatter.dataTypes.Integer && c.specialDataClass == CsvFormatter.SpecialDataClasses.None)
                {
                    string _monotonic = "false";

                    sendToConsole(defaultColor, string.Format(@"Use monotonically increasing values for this INTEGER column [{0} - Enter to accept default]: ", _monotonic.ToString().ToUpper()), false, false);
                    _monotonic = Console.ReadLine();

                    if (_monotonic.ToLower() == "true" || _monotonic.ToLower() == "t")
                    {
                        Int64 _minValue = Int64.Parse(c.minvalue.ToString());
                        Int64 _maxValue = Int64.Parse(c.maxValue.ToString());
                        Int64 _diff = ((_maxValue - _minValue) + 1); // needed because we want the values at both ends of the range
                        if (_diff < _csv.numberOfRows)
                        {
                            sendToConsole(ConsoleColor.Red, "You cannot use monotonically increasing values when the number of rows exceeds the range of min to max values.  Disabling monotonic value generation.");
                            c.monotonic = false;
                        }
                        else
                        {
                            c.monotonic = true;
                        }
                    }
                    else
                    {
                        c.monotonic = false;
                    }
                }


                // if monotonic, what is the seed?
                if (c.dataType == CsvFormatter.dataTypes.Integer && c.monotonic && c.specialDataClass == CsvFormatter.SpecialDataClasses.None)
                {
                    string _monotonicSeed = string.Empty;
                    Int64 _mSeed = 1;

                    Int64 _minSeed = 1;
                    bool _result = Int64.TryParse(c.minvalue.ToString(), out _minSeed);
                    if (_result && _minSeed >= 0) { c.monotonicSeed = _minSeed; }

                    while (_monotonicSeed == string.Empty)
                    {
                        sendToConsole(defaultColor, string.Format(@"Enter the seed value be for the monotonically increasing column [{0} - Enter to accept default]: ", c.monotonicSeed.ToString().ToUpper()), false, false);
                        _monotonicSeed = Console.ReadLine();

                        if (_monotonicSeed == string.Empty)
                        {
                            // nothing was provided, use default
                            _monotonicSeed = c.monotonicSeed.ToString();
                        }

                        if (!Int64.TryParse(_monotonicSeed.ToString(), out _mSeed))
                        {
                            sendToConsole(ConsoleColor.Red, "You must enter a valid integer value for the monotonic seed value.");
                            _monotonicSeed = string.Empty;
                        }
                        else
                        {
                            if (_mSeed < 1)
                            {
                                // need a valid positive value
                                sendToConsole(ConsoleColor.Red, "You must enter a valid integer value for the monotonic seed value.");
                                _monotonicSeed = string.Empty;
                            }
                            else
                            {
                                Int64 _minValue = Int64.Parse(c.minvalue.ToString());
                                if (_mSeed < _minValue)
                                {
                                    sendToConsole(ConsoleColor.Red, string.Format(@"You cannot use a monotonically increasing INTEGER value with a seed value of {0} that is below the minimum range value {1}.  Disabling monotonic value generation.", _mSeed.ToString(), _minValue.ToString()));
                                    c.monotonic = false;
                                    _mSeed = 1;
                                }
                                else
                                {
                                    c.monotonicSeed = _mSeed;
                                }
                            }
                        }
                    }
                }

                // if monotonic, is there a step to use?
                if (c.dataType == CsvFormatter.dataTypes.Integer && c.monotonic && c.specialDataClass == CsvFormatter.SpecialDataClasses.None)
                {
                    string _monotonicStep = string.Empty;
                    int _mStep = 1;

                    while (_monotonicStep == string.Empty)
                    {
                        sendToConsole(defaultColor, string.Format(@"Enter the step value be for the monotonically increasing column [{0} - Enter to accept default]: ", c.monotonicStep.ToString().ToUpper()), false, false);
                        _monotonicStep = Console.ReadLine();

                        if (_monotonicStep == string.Empty)
                        {
                            // accept default
                            _monotonicStep = c.monotonicStep.ToString();
                        }

                        if (!int.TryParse(_monotonicStep.ToString(), out _mStep))
                        {
                            sendToConsole(ConsoleColor.Red, "You must enter a valid integer value for the monotonic step value.");
                            _monotonicStep = string.Empty;
                        }
                        else
                        {
                            if (_mStep < 1)
                            {
                                // need a valid positive value
                                sendToConsole(ConsoleColor.Red, "You must enter a valid integer value for the monotonic step value.");
                            }
                            else
                            {
                                Int64 _maxValue = Int64.Parse(c.maxValue.ToString());
                                Int64 _diff = ((_maxValue - c.monotonicSeed) + 1);  // needed because we want the values at both ends of the range
                                Int64 _distincts = (_diff / _mStep);
                                if (_distincts < _csv.numberOfRows)
                                {
                                    sendToConsole(ConsoleColor.Red, string.Format(@"You cannot use a monotonically increasing INTEGER value with a step value of {0} since not enough distinct values will be available for the number of rows requested.  Disabling monotonic value generation.", _mStep.ToString()));
                                    c.monotonic = false;
                                    _mStep = 1;
                                }
                                else
                                {
                                    c.monotonicStep = _mStep;
                                }
                            }
                        }
                    }
                }

                // if decimal, what is the mantissa (the portion after the decimal)?
                if (c.dataType == CsvFormatter.dataTypes.Decimal && c.specialDataClass == CsvFormatter.SpecialDataClasses.None)
                {
                    string _mantissa = string.Empty;
                    byte _manT = 0;

                    while (_mantissa == string.Empty && _manT == 0)
                    {
                        sendToConsole(defaultColor, @"Enter the number of digits to the right of the decimal point that should be generated: ", false, false);
                        _mantissa = Console.ReadLine();
                        
                        if (!byte.TryParse(_mantissa.ToString(), out _manT))
                        {
                            sendToConsole(ConsoleColor.Red, "You must enter a valid value for the number of digits to the right of the decimal point [1-8].");
                            _mantissa = string.Empty;
                        }
                        else
                        {
                            if (_manT < 1 || _manT > 8)
                            {
                                // need a value within the range here
                                sendToConsole(ConsoleColor.Red, "You must enter a valid value for the number of digits to the right of the decimal point [1-8].");
                            }
                            else
                            {
                                c.mantissa = _manT;
                            }
                        }
                    }
                }

                // Do we specify a selectivity?  If the column is monotonic, then the selectivity is essentially equal to the overall cardinality
                if (!c.monotonic)
                {
                    string _selectivity = string.Empty;
                    int _selC = -1;

                    while (_selectivity == string.Empty)
                    {
                        sendToConsole(defaultColor, string.Format(@"Enter the selectivity of the column (-1 for random) [{0} - Enter to accept default]: ", c.selectivity.ToString().ToUpper()), false, false);
                        _selectivity = Console.ReadLine();

                        // set to default if no value provided
                        if (_selectivity == string.Empty && _selectivity.Length == 0)
                        {
                            _selectivity = c.selectivity.ToString();
                        }

                        if (!int.TryParse(_selectivity.ToString(), out _selC))
                        {
                            sendToConsole(ConsoleColor.Red, "You must enter a valid value for the selectivity of the column.  Enter -1 to use random selectivity.");
                            _selectivity = string.Empty;
                        }
                        else
                        {
                            if (_selC == 0)
                            {
                                sendToConsole(ConsoleColor.Red, "Selectivity cannot be set to 0.  Setting selectivity to random (-1).");
                                _selC = -1;
                            }

                            if (_selC > _csv.numberOfRows)
                            {
                                sendToConsole(ConsoleColor.Red, "Selectivity cannot be greater than the number of rows.  Setting selectivity to random (-1).");
                                _selC = -1;
                            }

                            Int64 _diff = 0;   // we need to check the # of selective values in the range defined

                            if (c.dataType == CsvFormatter.dataTypes.Date)
                            {
                                DateTime _minvalue = DateTime.Parse(c.minvalue.ToString());
                                DateTime _maxValue = DateTime.Parse(c.maxValue.ToString());
                                TimeSpan t = _maxValue.Subtract(_minvalue);
                                _diff = (Int64)t.TotalDays;
                            }
                            else
                            {
                                Int64 _minValue = Int64.Parse(c.minvalue.ToString());
                                Int64 _maxValue = Int64.Parse(c.maxValue.ToString());
                                _diff = ((_maxValue - _minValue) + 1); // needed because we want the values at both ends of the range
                            }

                            if (_selC > _diff)
                            {
                                sendToConsole(ConsoleColor.Red, "Selectivity cannot be greater than min / max value range.  Setting selectivity to random (-1).");
                                _selC = -1;
                            }
                            c.selectivity = _selC;
                        }
                    }
                }

                // assign to our array
                _csv.columns[i] = c;
            }

            return true;
        }

        // load model from JSON file
        static bool loadModelFormatFile()
        {
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
                return false;
            }

            return true;
        }

        // save model to disk
        static bool saveModelFormatFile()
        {
            string currentDirectory = Directory.GetCurrentDirectory().ToString();
            string fileName = string.Format(@"{0}_{1}_{2}.json", _csv.tableName.ToString(), DateTime.Now.ToString("yyyy_MM_dd"), DateTime.Now.ToString("hh_mm_ss"));
            string fullPath = string.Format(@"{0}\{1}", currentDirectory.ToString(), fileName.ToString());

            sendToConsole(defaultColor, string.Format(@"The model format file will be saved to '{0}'.  Press enter to continue or enter a new path and filename: ", fullPath.ToString()), false, false);
            string _path = Console.ReadLine();

            if (_path.Length > 0 && _path.Contains(@"\") && _path.Contains(".json"))
            {
                fullPath = _path;
            }

            // serialize Json
            string json = JsonConvert.SerializeObject(_csv);

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

        // write model format file to console window
        static void displayModelFormatFile()
        {
            sendToConsole(ConsoleColor.Green, string.Format(@"{0} MODEL DEFINITION {1}", "#".PadRight(30, '#'), "#".PadRight(30, '#')), true, true);

            if (inputFormatFile != string.Empty)
            {
                sendToConsole(defaultColor, (@"InputFile: ".PadRight(50) + inputFormatFile.ToString()));
            }
            sendToConsole(defaultColor, (@"TableName: ".PadRight(50) + _csv.tableName.ToString()));
            sendToConsole(defaultColor, (@"Version: ".PadRight(50) + _csv.version.ToString()));
            sendToConsole(defaultColor, (@"Number of Columns: ".PadRight(50) + _csv.numberOfCols.ToString()));
            sendToConsole(defaultColor, (@"Number of Rows: ".PadRight(50) + _csv.numberOfRows.ToString()));
            sendToConsole(defaultColor, (@"Number of Files: ".PadRight(50) + _csv.numberOfFiles.ToString()));
            sendToConsole(defaultColor, (@"Print Column Names: ".PadRight(50) + _csv.printColumnNames.ToString()));
            sendToConsole(defaultColor, "", false, true);
            sendToConsole(defaultColor, (@"Column Definitions:"));

            for (int i=0; i < _csv.columns.Length; i++)
            {
                sendToConsole(defaultColor, string.Format(@"  Column {0}{1}", i.ToString(), "-".PadRight(50, '-')));
                sendToConsole(defaultColor, (@"     Column Name: ".PadRight(40) + _csv.columns[i].columnName.ToString()));
                sendToConsole(defaultColor, (@"     Data Type: ".PadRight(40) + _csv.columns[i].dataType.ToString()));
                sendToConsole(defaultColor, (@"     Special Data Class: ".PadRight(40) + _csv.columns[i].specialDataClass.ToString()));
                sendToConsole(defaultColor, (@"     Default Value: ".PadRight(40) + _csv.columns[i].defaultValue.ToString()));
                sendToConsole(defaultColor, (@"     Value List: ".PadRight(40) + _csv.columns[i].valueListFile.ToString()));
                sendToConsole(defaultColor, (@"     Minimum Value: ".PadRight(40) + _csv.columns[i].minvalue.ToString()));
                sendToConsole(defaultColor, (@"     Maximum Value: ".PadRight(40) + _csv.columns[i].maxValue.ToString()));
                sendToConsole(defaultColor, (@"     Monotonically Increasing: ".PadRight(40) + _csv.columns[i].monotonic.ToString()));
                sendToConsole(defaultColor, (@"     Monotonic Step: ".PadRight(40) + _csv.columns[i].monotonicStep.ToString()));
                sendToConsole(defaultColor, (@"     Mantissa: ".PadRight(40) + _csv.columns[i].mantissa.ToString()));
                sendToConsole(defaultColor, (@"     Selectivity: ".PadRight(40) + _csv.columns[i].selectivity.ToString()));
                sendToConsole(defaultColor, (@"     Selectivity Column: ".PadRight(40) + _csv.columns[i].selColumn.ToString()));

                if (_csv.columns[i].monotonic || (_csv.columns[i].selectivity == _csv.numberOfRows))
                {
                    sendToConsole(defaultColor, (@"     Unique: ".PadRight(40) + "True"));
                }
                else if (_csv.columns[i].selColumn > -1 && _csv.columns[i].valueListFile != string.Empty)
                {
                    // we have a value list
                    if (_csv.numberOfRows <= selColumns[_csv.columns[i].selColumn].Length)
                    {
                        sendToConsole(defaultColor, (@"     Unique: ".PadRight(40) + "True"));
                    }
                    else
                    {
                        sendToConsole(defaultColor, (@"     Unique: ".PadRight(40) + "False"));
                    }
                }
                else
                {
                    sendToConsole(defaultColor, (@"     Unique: ".PadRight(40) + "False"));
                }
            }
        }

        // generates data files
        static bool generateCsvData()
        {
            // calculate # of rows to place per file
            rowsPerFile = (_csv.numberOfRows / _csv.numberOfFiles);

            // initialize array
            currentRow = new string[_csv.columns.Length];

            // initialize cache
            initializeCache();

            // initialize first file
            currCsvFile = initializeNewCsvDataFile();

            // position console for updates
            sendToConsole(defaultColor, "", true, true);

            // get position
            int left = Console.CursorLeft;
            int top = Console.CursorTop;

            // update statement
            string update = @"PROGRESS >> {0} rows generated of {1} total rows.  {2} rows written to {3} files.{4}";

            // initial update
            sendToConsole(ConsoleColor.Yellow, string.Format(update.ToString(), rowCount.ToString("000000"), _csv.numberOfRows.ToString("000000"), totalRowsWritten.ToString("000000"), fileCount.ToString("00"), " ".PadRight(30)));

            // loop through row count and do as follows
            for (Int64 i = 0; i < _csv.numberOfRows; i++)
            {
                // generate current row
                if (!generateCurrentRow())
                {
                    sendToConsole(ConsoleColor.Red, string.Format(@"Failed to generate new row at row number {0}", (i + 1).ToString()));
                    return false;
                }

                // copy current row to cache
                if (!moveCurrentRow())
                {
                    sendToConsole(ConsoleColor.Red, string.Format(@"Failed to move current row into cache at row number {0}", (i + 1).ToString()));
                    return false;
                }

                // threshold check (do we start a new file or do we flush the cache
                if (!thresholdCheck())
                {
                    sendToConsole(ConsoleColor.Red, string.Format(@"Failed cache flush or file rollover.  Please check previous errors for more information."));
                    return false;
                }

                // increment
                rowCount++;

                // update console
                Console.CursorLeft = left;
                Console.CursorTop = top;
                sendToConsole(ConsoleColor.Yellow, string.Format(update.ToString(), rowCount.ToString("000000"), _csv.numberOfRows.ToString("000000"), totalRowsWritten.ToString("000000"), fileCount.ToString("00"), " ".PadRight(30)));
            }

            // final flush
            flushCacheToFile();

            // update console
            Console.CursorLeft = left;
            Console.CursorTop = top;
            sendToConsole(ConsoleColor.Yellow, string.Format(update.ToString(), rowCount.ToString("000000"), _csv.numberOfRows.ToString("000000"), totalRowsWritten.ToString("000000"), fileCount.ToString("00"), " ".PadRight(30)));

            return true;
        }

        // generates current row
        static bool generateCurrentRow()
        {
            for (int i = 0; i < _csv.columns.Length; i++)
            {
                // get current column definition
                CsvFormatter.column c = _csv.columns[i];

                // in the default, PRO is a special situation that is built up front
                if (c.defaultValue != string.Empty && ((c.defaultValue.Length < 4) || (c.defaultValue.Substring(1,3).ToUpper() != "PRO")))
                {
                    if ((c.defaultValue.Substring(0, 1)) == "&" && ((c.defaultValue.Substring(1, 3).ToUpper() == "ASN") || (c.defaultValue.Substring(1, 3).ToUpper() == "EQL") || (c.defaultValue.Substring(1, 3).ToUpper() == "GTR") ||
                        (c.defaultValue.Substring(1, 3).ToUpper() == "LSS")))
                    {
                        // we have a special default = function
                        // here we actually need to defer because we don't know if the dependent value has been evaluated yet
                        currentRow[i] = "__DEFERRED__";
                    }
                    else
                    {
                        // we have a default value to set
                        if (c.defaultValue == "__DEFERRED__")
                        {
                            sendToConsole(ConsoleColor.Red, String.Format(@"Invalid default value specified for column '{0}'", _csv.columns[i].columnName.ToString()));
                        }
                        currentRow[i] = c.defaultValue;
                    }
                }
                else if (c.valueListFile != string.Empty)
                {
                    // we have a list of provided values that were loaded into the j-array
                    // we must get the location in the selectivty j-array 
                    int s = c.selColumn;

                    // now if the # of rows is less than the number of values provided in the valueList, we can ensure uniqueness
                    if (_csv.numberOfRows <= selColumns[s].Length && !disableUniqueness)
                    {
                        // we can provide unique values since we have fewer rows than values
                        int r = (int)rowCount;  // progress linearly into the j-array
                        currentRow[i] = selColumns[s][r];
                    }
                    else
                    {
                        // we have more rows so we will grab from the value list randomly - not unique but ensuring selectivity
                        // get a random index into the selectivity j-array
                        int r = int.Parse(getRandomIntegerAsString(0, (selColumns[s].Length)));
                        currentRow[i] = selColumns[s][r];
                    }
                }
                else if (c.selectivity != -1)
                {
                    if (_csv.numberOfRows == c.selectivity)
                    {
                        // since the selectivity is equal to the # of rows - we have a unique requirement
                        // because when selectivity == numberOfRows it implies that each row is different and hence unique
                        int s = c.selColumn;
                        int r = (int)rowCount;   // then into the selectivity j-array we need to progress linearly
                        currentRow[i] = selColumns[s][r];
                    }
                    else
                    {
                        // we have random cardinality with this selectivity
                        // get the location in the j-array much like the valueListFile
                        int s = c.selColumn;
                        // get a random index into the selectivity j-array
                        int r = int.Parse(getRandomIntegerAsString(0, (selColumns[s].Length)));
                        currentRow[i] = selColumns[s][r];
                    }
                }
                else if (c.monotonic)
                {
                    // we have a monotonic value
                    // either it is the first value which we assign the seed
                    // or it is already there and we add the step to it
                    if (currentRow[i] == null || currentRow[i] == string.Empty)
                    {
                        currentRow[i] = c.monotonicSeed.ToString();
                    }
                    else
                    {
                        Int64 j = Int64.Parse(currentRow[i]);
                        currentRow[i] = (j + c.monotonicStep).ToString();
                    }
                }
                else if (c.specialDataClass != CsvFormatter.SpecialDataClasses.None)
                {
                    // special class data - random
                    switch (c.specialDataClass)
                    {
                        case CsvFormatter.SpecialDataClasses.City:
                            currentRow[i] = getRandomCity();
                            break;

                        case CsvFormatter.SpecialDataClasses.Country:
                            currentRow[i] = getRandomCountry();
                            break;

                        case CsvFormatter.SpecialDataClasses.FirstName:
                            currentRow[i] = getRandomFirstName();
                            break;

                        case CsvFormatter.SpecialDataClasses.FullName:
                            currentRow[i] = getRandomFullName();
                            break;

                        case CsvFormatter.SpecialDataClasses.LastName:
                            currentRow[i] = getRandomLastName();
                            break;

                        case CsvFormatter.SpecialDataClasses.Phone:
                            currentRow[i] = getRandomPhone();
                            break;

                        case CsvFormatter.SpecialDataClasses.SSN:
                            currentRow[i] = getRandomSSN();
                            break;

                        case CsvFormatter.SpecialDataClasses.State:
                            currentRow[i] = getRandomState();
                            break;

                        case CsvFormatter.SpecialDataClasses.StreetAddress:
                            currentRow[i] = getRandomStreetAddress();
                            break;

                        case CsvFormatter.SpecialDataClasses.ZipCode:
                            currentRow[i] = getRandomZipCode();
                            break;

                        default:
                            currentRow[i] = "UNDEFINED";
                            break;
                    }
                }
                else
                {
                    // normal data type generation

                    Int64 n = 1;
                    Int64 x = 100;
                    DateTime s = DateTime.Now;
                    DateTime e = DateTime.Now;
                    byte m = 1;

                    if (c.dataType == CsvFormatter.dataTypes.Integer || c.dataType == CsvFormatter.dataTypes.String || c.dataType == CsvFormatter.dataTypes.Decimal)
                    {
                        n = Int64.Parse(c.minvalue);
                        x = Int64.Parse(c.maxValue);
                        m = c.mantissa;
                    }
                    else
                    {
                        // we have date
                        s = DateTime.Parse(c.minvalue);
                        e = DateTime.Parse(c.maxValue);
                    }

                    switch (c.dataType)
                    {
                        case CsvFormatter.dataTypes.Integer:
                            currentRow[i] = getRandomIntegerAsString(n, x);
                            break;

                        case CsvFormatter.dataTypes.String:
                            if (lowerString)
                            {
                                currentRow[i] = generateRandomString(n, x).ToLower();
                            }
                            else
                            {
                                currentRow[i] = generateRandomString(n, x);
                            }
                            break;

                        case CsvFormatter.dataTypes.Decimal:
                            currentRow[i] = getRandomDecimal(n, x, m);
                            break;

                        case CsvFormatter.dataTypes.Date:
                            currentRow[i] = generateRandomDateString(s, e);
                            break;

                        default:
                            currentRow[i] = "UNDEFINED";
                            break;

                    }
                }
            }

            // process deferred calculations (special defaults = functions)
            for (int i = 0; i < _csv.columns.Length; i++)
            {
                if (currentRow[i] == "__DEFERRED__")
                {
                    // we must get the default string from the column
                    string _default = _csv.columns[i].defaultValue.ToString();
                    string _cmd = _default.Substring(1, 3).ToString();

                    // we must extract the reference col id
                    int _colId = 0;

                    if (!int.TryParse(_default.Substring(5).Replace("]", ""), out _colId))
                    {
                        sendToConsole(ConsoleColor.Red, String.Format(@"Unable to extract dependent column id for special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                        continue;
                    }

                    switch (_cmd.ToUpper())
                    {
                        case "ASN":
                            // this means we must reference and "assign" or match a value with the same dependent value each time

                            // safety checks first
                            // is the reference outside the bounds of the array
                            if (_colId > (currentRow.Length - 1))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"The referenced column is outside the bounds of the current row for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            // is there a value in that referenced column
                            if (currentRow[_colId] == null)
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"The referenced column is NULL for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            // we must have a select column (a value list) defined
                            if (_csv.columns[i].selColumn == -1 && (_csv.columns[i].valueListFile == string.Empty || _csv.columns[i].valueListFile == ""))
                            {
                                // no value list set has been provided
                                sendToConsole(ConsoleColor.Red, String.Format(@"A value list has not been provided for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            // get length of value list to calculate the indexer
                            Int64 _totalValues = selColumns[_csv.columns[i].selColumn].Length;

                            // now we must get the referenced value
                            Int64 _baseValue = 0;

                            switch (_csv.columns[_colId].dataType)
                            {
                                case CsvFormatter.dataTypes.Integer:
                                    _baseValue = Int64.Parse(currentRow[_colId].ToString());

                                    break;

                                case CsvFormatter.dataTypes.String:
                                    char[] _chars = currentRow[_colId].ToString().ToCharArray();

                                    for (int j = 0; j < _chars.Length; j++)
                                    {
                                        _baseValue += (int)_chars[j];
                                    }

                                    break;

                                case CsvFormatter.dataTypes.Decimal:
                                    _baseValue = (int)Decimal.Parse(currentRow[_colId].ToString());

                                    break;

                                default:

                                    sendToConsole(ConsoleColor.Red, String.Format(@"The data type ({0}) of the reference column is not supported by the &ASN function for the special default in column '{1}'", _csv.columns[_colId].dataType.ToString(), _csv.columns[i].columnName.ToString()));
                                    continue;

                                    break;
                            }

                            // we have the value, now we must get the indexer
                            Int64 _indexer = Math.Abs(_baseValue % _totalValues); 

                            // now assign
                            currentRow[i] = selColumns[_csv.columns[i].selColumn][_indexer];

                            break;

                        case "EQL":
                            // this means assign the same number as the reference column

                            // safety checks first
                            // is the reference outside the bounds of the array
                            if (_colId > (currentRow.Length -1))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"The referenced column is outside the bounds of the current row for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            // is there a value in that referenced column
                            if (currentRow[_colId] == null)
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"The referenced column is NULL for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            // assign same value "EQL"
                            currentRow[i] = currentRow[_colId];
                            break;

                        case "GTR":
                            // this means we must have a greater value

                            // safety checks first
                            // is the reference outside the bounds of the array
                            if (_colId > (currentRow.Length - 1))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"The referenced column is outside the bounds of the current row for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            // is there a value in that referenced column
                            if (currentRow[_colId] == null)
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"The referenced column is NULL for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            // this number must be "greater" so we set the minimum here and validate this is a numeric type
                            Int64 _newMin = 0;

                            if (!Int64.TryParse(currentRow[_colId].ToString(), out _newMin))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"The referenced column is not a numeric type for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            // now we must verify the new number is not larger than the current maxValue
                            if (_newMin >= Int64.Parse(_csv.columns[i].maxValue))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"The referenced column value is larger than the defined max value for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            currentRow[i] = getRandomIntegerAsString(_newMin, Int64.Parse(_csv.columns[i].maxValue));
                            break;

                        case "LSS":
                            // this means we must have a lesser value

                            // safety checks first
                            // is the reference outside the bounds of the array
                            if (_colId > (currentRow.Length - 1))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"The referenced column is outside the bounds of the current row for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            // is there a value in that referenced column
                            if (currentRow[_colId] == null)
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"The referenced column is NULL for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            // this number must be "lesser" so we set the maximum here and validate this is a numeric type
                            Int64 _newMax = 0;

                            if (!Int64.TryParse(currentRow[_colId].ToString(), out _newMax))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"The referenced column is not a numeric type for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            // now we must verify the new number is not smaller than the current minValue
                            if (_newMax <= Int64.Parse(_csv.columns[i].minvalue))
                            {
                                sendToConsole(ConsoleColor.Red, String.Format(@"The referenced column value is smaller than the defined min value for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                                continue;
                            }

                            currentRow[i] = getRandomIntegerAsString(Int64.Parse(_csv.columns[i].minvalue), _newMax);
                            break;

                        default:

                            sendToConsole(ConsoleColor.Red, String.Format(@"An unknown function was provided for the special default in column '{0}'", _csv.columns[i].columnName.ToString()));
                            continue;

                            break;
                    }
                }
            }

            if (WAIT_FLAG)
            {
                Thread.Sleep(WAIT_TIME);
            }

            // one row done.

            return true;
        }

        // moves current row to cache
        static bool moveCurrentRow()
        {
            for (int i = 0; i < currentRow.Length; i++)
            {
                cache[cacheRows, i] = currentRow[i];
            }

            // increment cache counter
            cacheRows++;

            return true;
        }

        // checks for flush and file rolls
        static bool thresholdCheck()
        {
            // check for cache flush
            if (cacheRows == cacheFlushValue)
            {
                if (!flushCacheToFile())
                {
                    sendToConsole(ConsoleColor.Red, "Failed to flush cache to file.  Check for previous errors for more information.");
                    return false;
                }

                // clear the cache
                initializeCache();
            }

            return true;
        }

        // write cache to current file and rolls file if required
        static bool flushCacheToFile()
        {
            if (cacheRows == 0)
            {
                return true;
            }

            StreamWriter sr;

            // check to see if we need to create a new file
            if (currCsvFile == string.Empty)
            {
                currCsvFile = initializeNewCsvDataFile();
            }

            // open file
            try
            {
                sr = new StreamWriter(currCsvFile, true, encode);
            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return false;
            }

            // roll through cache and write file
            // process each row
            for (int i = 0; i < cacheRows; i++)
            {
                StringBuilder sb = new StringBuilder();

                // now loop through the columns
                for (int j = 0; j < cache.GetLength(1); j++)
                {
                    // build one line
                    if (j > 0)
                    {
                        sb.Append(FIELD_TERMINATOR);
                    }

                    sb.Append(cache[i, j].ToString());

                } // for - building csv line

                try
                {
                    // write to stream
                    sr.Write(sb.ToString());
                    // terminate row
                    sr.Write(ROW_TERMINATOR);

                    // increment
                    rowsCurrentlyWritten++;
                    totalRowsWritten++;
                }
                catch (Exception ex)
                {
                    sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                    return false;
                }

                // flush stream
                if ((rowsCurrentlyWritten % streamFlushValue) == 0)
                {
                    try
                    {
                        sr.Flush();
                    }
                    catch (Exception ex)
                    {
                        sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                        return false;
                    }
                }

                // do we need to roll the file?
                if (rowsCurrentlyWritten == rowsPerFile && totalRowsWritten < _csv.numberOfRows)
                {
                    try
                    {
                        // clean up current file
                        sr.Flush();
                        sr.Close();
                        sr.Dispose();

                        // get new file
                        currCsvFile = initializeNewCsvDataFile();

                        // re-open stream
                        sr = new StreamWriter(currCsvFile, true, Encoding.UTF8);
                        rowsCurrentlyWritten = 0;
                    }
                    catch (Exception ex)
                    {
                        sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                        return false;
                    }
                }

            } // for - rows

            // done so close file
            try
            {
                sr.Flush();
                sr.Close();
                sr.Dispose();
            }
            catch (Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return false;
            }

            return true;
        }

        // initialize the cache array
        static bool initializeCache()
        {
            // (re)initalize cache
            cache = null;
            cache = new string[cacheFlushValue, _csv.columns.Length];

            // reset cache rows
            cacheRows = 0;

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

        // check for special char
        static bool checkForSpecialCharacters(string s)
        {
            if (s.Contains(@"\"))
                return true;

            if (s.Contains(@"/"))
                return true;

            if (s.Contains(@":"))
                return true;

            if (s.Contains(@"*"))
                return true;

            if (s.Contains(@"?"))
                return true;

            if (s.Contains(@"<"))
                return true;

            if (s.Contains(@">"))
                return true;

            if (s.Contains(@"|"))
                return true;

            return false;
        }

        // write summary to console
        static void writeSummary()
        {
            // calculate time
            TimeSpan t = end.Subtract(start);

            // write results to console window
            sendToConsole(defaultColor, "", true, true);
            sendToConsole(ConsoleColor.Green, string.Format(@"RESULTS >> {0} rows written to {1} files in {2} hours {3} minutes {4} seconds.", rowCount.ToString(), fileCount.ToString(), t.Hours.ToString("00"), t.Minutes.ToString("00"), t.Seconds.ToString("00")));
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
