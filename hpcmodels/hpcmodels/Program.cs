using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using System.Xml;
using System.Diagnostics;
using System.Threading.Tasks;

namespace hpcmodels
{

    /*
     * Author:  Jay Askew, Microsoft Corporation  2018 (c)
     * Date:    January 30, 2018 
     * 
     * This program runs one of (eventually) several different models to generate load for demos of real-world scenarios in a High Performance Computing (HPC) environment:
     * 
     * The models supported today are:
     * 
     *      - Loan generation:  (mode = g) Supports the amortization model by generating random loan structures for processing.  You must provide a min/max
     *          range for asset value for the loan document generation (base loan value is calculated from asset value).  Rates and Terms are provided during amortization 
     *          processing in separate resource files.  The model will generate the requested number of loan documents that consist of:  Asset Value, Loan Amount, zipcode, and loan start date.
     *          
     *      - Portfolio generation: (mode = y) Supports the portfolio yield model by generating random portfolio values for processing.  You must provide a min/max
     *          range for the number of securities in each portfolio and a list of equity symbols.  Pricing is determined randomly.  All other factors are supplied 
     *          when running the portfolio yield model.  The model will generate the requested number of portfolios with a random number of equities within range priced randomly with random share counts.
     *      
     *      - Amortization:  (mode = a) This model takes in a series of "loan documents" in json format to create amortization schedules.  Amortization schedules are created across a variety
     *          of rates and terms - both of which are provided in separate resource files.   You must provide an input directory (populated with json loan documents) and an output directory for
     *          model results.  Additionally, you must provide min/max values for a randomly selected appreciation/depreciation of asset value.  The model will track asset value and total equity 
     *          as an extension of the amortization schedule.
     *      
     *      - Portfolio Yield: (mode = p) This model takes in a series of "portfolios" which contain a list of equities with their associated price and share count - as well as total current values,
     *          and applies a per period yield to simulate market fluctuations and portfolio changes.  You must provide an input directory (populated with json portfolio documents) and an output 
     *          directory for model results.  You must also provide min/max values to randomly determine the per period portfolio yeild.  The model will produce a list of compounding periods by 
     *          date that show gains/losses in the portfolio.
     * 
     *  v1.0.0.1         jamesask        01/30/2018      -Base version.
     * 
     * 
     */

    class Program
    {
        // common vars
        static bool debug = false;
        static bool getHelp = false;
        static ConsoleColor defaultColor;
        static runMode mode = runMode.amortization;
        static outputFormat outputType = outputFormat.json;
        static string inputDirectory = string.Empty;
        static string outputDirectory = string.Empty;
        static int loops = 1;
        static int min = 0;
        static int max = int.MaxValue;
        static Random rnd;
        static frequency freq = frequency.monthly;
        static int seed = 0;

        // loan generation vars
        static Dictionary<int, loanStructures> dtLoans = new Dictionary<int, loanStructures>();
        static int zipcodeCount = 50;

        // amort vars
        static Dictionary<string, amortSchedule> dtAmortSchedule = new Dictionary<string, amortSchedule>();
        static List<decimal> rates = new List<decimal>();
        static List<int> terms = new List<int>();
        static string interestRateFile = string.Empty;
        static string termFile = string.Empty;

        // portfolio generation vars
        static Dictionary<int, portfolioStructure> dtPortfolios = new Dictionary<int, portfolioStructure>();
        static List<string> securities = new List<string>();
        static string securitiesFile = string.Empty;

        // portfolio vars
        static Dictionary<int, portfolioPerformance> dtPortYield = new Dictionary<int, portfolioPerformance>();

        // supported output formats
        enum outputFormat
        {
            csv = 1,
            xml = 2,
            json = 3
        }

        // output status level
        enum outputLevel
        {
            Error = 1,
            Warning = 2,
            Information = 3
        }

        // run mode enum
        enum runMode
        {
            loangeneration = 1,
            portfoliogeneration = 2,
            amortization = 3,
            portfolio = 4
        }

        // frequency / schedule for calculations
        enum frequency
        {
            daily = 1,
            weekly = 2,
            biweekly = 3,
            monthly = 4,
            bimonthly = 5,
            quarterly = 6,
            annually = 7
        }

        // portfolio performance
        struct portfolioPerformance
        {
            public Dictionary<DateTime, portfolioStructure> periods;

            public portfolioPerformance(
                Dictionary<DateTime, portfolioStructure> _periods
                )
            {
                periods = _periods;
            }
        }

        // portfolio definitions { SYM, PRICE }
        struct portfolioStructure
        {
            public decimal currentValue;
            public Dictionary<string, securityDetails> securities;

            public portfolioStructure(
                decimal _currentValue,
                Dictionary<string, securityDetails> _securities
                )
            {
                currentValue = _currentValue;
                securities = _securities;
            }
        }

        // represents quantity and price in one equity
        struct securityDetails
        {
            public int shares;
            public decimal sharePrice;
            public decimal totalValue;

            public securityDetails(
                int _shares,
                decimal _sharePrice,
                decimal _totalValue
                )
            {
                shares = _shares;
                sharePrice = _sharePrice;
                totalValue = _totalValue;
            }
        }

        // loan definitions
        struct loanStructures
        {
            public decimal assetValue;
            public decimal principalAmount;
            public string zipcode;
            public DateTime startDate;

            public loanStructures(
                decimal _assetValue,
                decimal _principalAmount,
                string _zipcode,
                DateTime _startDate
                )
            {
                assetValue = _assetValue;
                principalAmount = _principalAmount;
                zipcode = _zipcode;
                startDate = _startDate;
            }
        }

        // structure for tracking amortization schedule
        struct amortSchedule
        {
            public decimal baseValue;
            public decimal assetValue;
            public string zipCode;
            public decimal loanRate;
            public decimal appreciationRate;
            public int term;
            public frequency freq;
            public Dictionary<int, amortDetails> schedule;

            public amortSchedule(
                decimal _baseValue,
                decimal _assetValue,
                string _zipCode,
                decimal _loanRate,
                decimal _appreciationRate,
                frequency _freq,
                int _term
                )
            {
                baseValue = _baseValue;
                assetValue = _assetValue;
                zipCode = _zipCode;
                loanRate = _loanRate;
                appreciationRate = _appreciationRate;
                freq = _freq;
                term = _term;
                schedule = new Dictionary<int, amortDetails>();
            }

        }

        // schedule lineitems
        struct amortDetails
        {
            public DateTime pmtDate;
            public decimal currentBalance;
            public decimal currentPayment;
            public decimal currentPrincipal;
            public decimal currentInterest;
            public decimal currentAssetValue;
            public decimal currentEquity;

            public amortDetails(
                DateTime _pmtDate,
                decimal _currentBalance,
                decimal _currentPayment,
                decimal _currentPrincipal,
                decimal _currentInterest,
                decimal _currentAssetValue,
                decimal _currentEquity
                )
            {
                pmtDate = _pmtDate;
                currentBalance = _currentBalance;
                currentPayment = _currentPayment;
                currentPrincipal = _currentPrincipal;
                currentInterest = _currentInterest;
                currentAssetValue = _currentAssetValue;
                currentEquity = _currentEquity;
            }
        }

        // main entry point
        static void Main(string[] args)
        {
            // grab default color
            defaultColor = Console.ForegroundColor;

            // first, get user passed params
            if (!getCmdLineParams())
            {
                // failed - terminate
                return;
            }

            // are we just showing the help context?
            if (getHelp)
            {
                // done
                return;
            }

            // parameter safety checks
            // output directory?
            if (outputDirectory == string.Empty)
            {
                writeConsole(outputLevel.Error, "You must specify an output directory");
                return;
            }

            // input directory?
            if ((mode == runMode.amortization || mode == runMode.portfolio) && inputDirectory == string.Empty)
            {
                writeConsole(outputLevel.Error, "You must specify an input directory when running amortization or portfolio yield calculations.");
                return;
            }

            // initialize random number generator
            rnd = new Random((DateTime.Now.Millisecond * Thread.CurrentThread.ManagedThreadId));

            // what model are we running?
            switch (mode)
            {
                case runMode.amortization:
                    generateAmortTables();
                    break;

                case runMode.loangeneration:
                    generateLoanDocs();
                    break;

                case runMode.portfolio:
                    calculatePortfolioYield();
                    break;

                case runMode.portfoliogeneration:
                    generatePortfolios();
                    break;

                default:

                    // technically this shouldn't be able to happen
                    writeConsole(outputLevel.Error, "An invalid model was specified.");
                    break;
            }

            // write output
            writeDataOutput();

            if (debug)
            {
                Console.WriteLine("Debug stop...");
                Debugger.Break();
            }
               
        }

        // get command line args
        static bool getCmdLineParams()
        {
            // get cmd line args
            string[] args = Environment.GetCommandLineArgs();

            // we walk through each parameter
            for (int i = 1; i < args.Length; i++)
            {

                // test validity
                if (args[i].Substring(0, 1) != "/" && args[i].Substring(0, 1) != "-")
                {
                    writeConsole(outputLevel.Error, String.Format("Invalid argument provided in ordinal position {0} = {1}", i.ToString(), args[i].ToString()));
                    return false;
                }

                // process arguments
                try
                {
                    switch (args[i].Substring(1, 1).ToUpper())
                    {
                        case "?":
                            // get help
                            getHelp = true;
                            showHelp();
                            break;

                        case "H":
                            // get help
                            getHelp = true;
                            showHelp();
                            break;

                        case "M":
                            // set model
                            string model = args[i].Substring(2).ToString();

                            if (model.ToUpper() == "G")
                            {
                                mode = runMode.loangeneration;
                            }
                            else if (model.ToUpper() == "Y")
                            {
                                mode = runMode.portfoliogeneration;
                            }
                            else if (model.ToUpper() == "A")
                            {
                                mode = runMode.amortization;
                            }
                            else
                            {
                                mode = runMode.portfolio;
                            }
                            break;

                        case "W":
                            // get output type
                            string output = args[i].Substring(2).ToString();

                            if (output.ToUpper() == "CSV")
                            {
                                outputType = outputFormat.csv;
                            }
                            else if (output.ToUpper() == "XML")
                            {
                                outputType = outputFormat.xml;
                            }
                            else
                            {
                                outputType = outputFormat.json;
                            }
                            break;
                        case "I":
                            // input directory
                            inputDirectory = args[i].Substring(2).ToString();

                            if (!Directory.Exists(inputDirectory))
                            {
                                writeConsole(outputLevel.Error, String.Format("An invalid directory has been provided for the input directory: {0}", inputDirectory.ToString()));
                                return false;
                            }
                            
                            break;

                        case "O":
                            // input directory
                            outputDirectory = args[i].Substring(2).ToString();

                            if (!Directory.Exists(outputDirectory))
                            {
                                writeConsole(outputLevel.Error, String.Format("An invalid directory has been provided for the output directory: {0}", outputDirectory.ToString()));
                                return false;
                            }

                            break;

                        case "S":
                            // securities file
                            securitiesFile = args[i].Substring(2).ToString();

                            if (!File.Exists(securitiesFile))
                            {
                                writeConsole(outputLevel.Error, String.Format("An invalid securities file has been provided: {0}", securitiesFile.ToString()));
                                return false;
                            }

                            break;

                        case "R":
                            // rate file
                            interestRateFile = args[i].Substring(2).ToString();

                            if (!File.Exists(interestRateFile))
                            {
                                writeConsole(outputLevel.Error, String.Format("An invalid interest rate file has been provided: {0}", interestRateFile.ToString()));
                                return false;
                            }

                            break;

                        case "T":
                            // term file
                            termFile = args[i].Substring(2).ToString();

                            if (!File.Exists(termFile))
                            {
                                writeConsole(outputLevel.Error, String.Format("An invalid term file has been provided: {0}", termFile.ToString()));
                                return false;
                            }

                            break;

                        case "V":
                            // seed value
                            string v = args[i].Substring(2).ToString();
                            if (!int.TryParse(v.ToString(), out seed))
                            {
                                writeConsole(outputLevel.Error, String.Format("An invalid seed value was provided: {0}", v.ToString()));
                                return false;
                            }

                            break;

                        case "L":
                            // set loop count
                            string l = args[i].Substring(2).ToString();
                            if (!int.TryParse(l.ToString(), out loops))
                            {
                                writeConsole(outputLevel.Error, String.Format("An invalid loop value was provided: {0}", l.ToString()));
                                return false;
                            }

                            break;

                        case "N":
                            // set minimum value
                            string n = args[i].Substring(2).ToString();
                            if (!int.TryParse(n.ToString(), out min))
                            {
                                writeConsole(outputLevel.Error, String.Format("An invalid minimum value was provided: {0}", n.ToString()));
                                return false;
                            }

                            break;

                        case "X":
                            // set maximum value
                            string x = args[i].Substring(2).ToString();
                            if (!int.TryParse(x.ToString(), out max))
                            {
                                writeConsole(outputLevel.Error, String.Format("An invalid maximum value was provided: {0}", x.ToString()));
                                return false;
                            }

                            break;

                        case "F":
                            // frequency
                            string f = args[i].Substring(2).ToString();

                            if (f.ToUpper() == "DAILY")
                            {
                                freq = frequency.daily;   
                            }
                            else if (f.ToUpper() == "ANNUALLY")
                            {
                                freq = frequency.annually;
                            }
                            else if (f.ToUpper() == "QUARTERLY")
                            {
                                freq = frequency.quarterly;
                            }
                            else if (f.ToUpper() == "BIMONTHLY")
                            {
                                freq = frequency.bimonthly;
                            }
                            else if (f.ToUpper() == "WEEKLY")
                            {
                                freq = frequency.weekly;
                            }
                            else if (f.ToUpper() == "BIWEEKLY")
                            {
                                freq = frequency.biweekly;
                            }
                            else
                            {
                                freq = frequency.monthly;
                            }

                            break;

                        default:

                            writeConsole(outputLevel.Error, String.Format("An invalid parameter was provided: {0}", args[i].ToString()));
                            return false;
                    }
                }
                catch (Exception ex)
                {
                    writeConsole(outputLevel.Error, ex.Message.ToString());
                    return false;
                }
            }

            return true;
        }

        // show help
        static void showHelp()
        {
            // show command line startup parameters
            StringBuilder helpMsg = new StringBuilder();

            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("hpcmodels - calculation subroutines for running in parallel in a High Performance Computing (HPC) environment.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("Written by: Jay Askew  ");
            helpMsg.Append("Microsoft 2018(c)");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("Version: ");
            helpMsg.Append(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("All Parameters:");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/? or /H        This help screen.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/M              Model to execute.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - 'G' Loan Document Generation.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - 'Y' Portfolio Generation.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - 'A' Amortization Calculation.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - 'P' Portfolio Calculation (default).");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/W              Output format.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - csv");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - xml");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - json (default).");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/I              Input directory for amortization and portfolio calculation models.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/O              Output directory for all models.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/S              Securities file path for portfolio generation.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/R              Rates file path for amortization calculation model.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/T              Term file path for amortization calculation model.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/V              Seed value for loan ID (key) and portfolio ID (key) in loan and portfolio generation.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/L              Loop count used for portfolio and loan document generation count and cycle count in portfolio yield calculations.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/N              Minimum value used for number of securities in each portfolio, asset value in loan documents, asset value appreciation rate, and per run per equity yield.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/X              Maximum value used for number of securities in each portfolio, asset value in loan documents, asset value appreciation rate, and per run per equity yield.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/F              Period frequency used to describe calculation intervals.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - Daily");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - Annually");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - Quarterly");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - Bimonthly");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - Weekly");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - Biweekly");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("                 - Monthly (default)");
            helpMsg.Append(Environment.NewLine);

            writeConsole(outputLevel.Information, helpMsg.ToString());
        }

        // send message to console
        static void writeConsole(outputLevel level, string displayMessage, bool carriageReturn = true)
        {
            // set foreground color
            switch (level)
            {
                case outputLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case outputLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                default:
                    Console.ForegroundColor = defaultColor;
                    break;
            }

            // do we need a CR?
            if (carriageReturn)
            {
                Console.WriteLine(displayMessage.ToString());
            }
            else
            {
                Console.Write(displayMessage.ToString());
            }

            // set back to default foreground color
            Console.ForegroundColor = defaultColor;
        }

        // generate portfolios
        static void generatePortfolios()
        {
            if (min == max || min > max || min < 0)
            {
                writeConsole(outputLevel.Error, "You have provided an incorrect value for either min or max for number of securities.");
                return;
            }

            if (securitiesFile == string.Empty || !File.Exists(securitiesFile))
            {
                writeConsole(outputLevel.Error, "You must specify a securities file for portfolio generation.");
                return;
            }

            // load securities first
            using (StreamReader sr = new StreamReader(securitiesFile.ToString()))
            {
                while (!sr.EndOfStream)
                {
                    try
                    {
                        securities.Add(sr.ReadLine());
                    }
                    catch (Exception ex)
                    {
                        writeConsole(outputLevel.Warning, "Bad security in securities file.  Skipping...");
                        writeConsole(outputLevel.Warning, ex.Message.ToString());
                        continue;
                    }
                }
            }

            // generate # of portfolios specified in loops param
            for (int i = 0; i < loops; i++)
            {
                // how many securities should this portfolio have
                int noSecurities = generateRandomInt(min, max);

                // new portfolio
                portfolioStructure p = new portfolioStructure(0.00m, new Dictionary<string, securityDetails>());

                // total value
                decimal total = 0.00m;

                // now pick each one
                for (int j = 0; j < noSecurities; j++)
                {
                    // pick a security
                    int index = generateRandomInt(0, (securities.Count - 1));

                    // grab a security
                    string symbol = securities[index].ToString();

                    // check for symbol
                    if (p.securities.ContainsKey(symbol))
                    {
                        // move on - this one is already on the portfolio
                        continue;
                    }

                    // share count
                    int shares = generateRandomInt(0, 10000);

                    // price
                    decimal price = generateRandomDecimal(1, 100);
                    decimal totalValue = (shares * price);
                    total += totalValue;

                    p.securities.Add(symbol, new securityDetails(shares, price, totalValue));

                }

                // assign the total and add to the collection
                p.currentValue = total;
                dtPortfolios.Add((seed + i), p);

            }

        }

        // generate loan docs
        static void generateLoanDocs()
        {
            if (min == max || min > max || min < 0)
            {
                writeConsole(outputLevel.Error, "You have provided an incorrect value for either min or max for asset value.");
                return;
            }

            // generate a list of zip codes
            // set zipCodes count to something appropriate for the loop size

            zipcodeCount = (int)Math.Sqrt(loops);
            List<string> zipCodes = new List<string>();
            for (int z = 0; z < zipcodeCount; z++)
            {
                zipCodes.Add(generateRandomInt(10000, 99999).ToString());
            }

            for (int i = 0; i < loops; i++)
            {
                decimal assetValue = generateRandomDecimal(min, max);
                decimal upFrontCapitalPerc = generateRandomDecimal(0, 20);
                decimal principalAmount = (assetValue - (assetValue * (upFrontCapitalPerc / 100)));

                // for now we'll get even distribution
                int index = (i % zipCodes.Count);
                string zipcode = zipCodes[index];

                // get a random startDate
                int minYear = (DateTime.Now.Year - 20);
                int maxYear = DateTime.Now.Year;
                DateTime startDate = new DateTime(generateRandomInt(minYear, maxYear), generateRandomInt(1, 12), 1);

                // create new loan
                loanStructures loan = new loanStructures(assetValue, principalAmount, zipcode, startDate);

                // add
                dtLoans.Add((seed + i), loan);
            }
        }

        // calculate amort
        static void generateAmortTables()
        {
            if (min == max || min > max)
            {
                writeConsole(outputLevel.Error, "You have provided an incorrect value for either min or max for appreciation rate.");
                return;
            }

            if (interestRateFile == string.Empty || !File.Exists(interestRateFile))
            {
                writeConsole(outputLevel.Error, "An invalid rate file was provided.");
                return;
            }

            if (termFile == string.Empty || !File.Exists(termFile))
            {
                writeConsole(outputLevel.Error, "An invalid term file was provided.");
                return;
            }

            // read in terms into List
            using (StreamReader srTerms = new StreamReader(termFile.ToString()))
            {
                while (!srTerms.EndOfStream)
                {
                    try
                    {
                        terms.Add(int.Parse(srTerms.ReadLine().ToString()));
                    }
                    catch (Exception ex)
                    {
                        writeConsole(outputLevel.Warning, "Bad term in terms file.  Skipping...");
                        writeConsole(outputLevel.Warning, ex.Message.ToString());
                        continue;
                    }
                }
            }

            // read in rates into List
            using (StreamReader srRates = new StreamReader(interestRateFile.ToString()))
            {
                while (!srRates.EndOfStream)
                {
                    try
                    {
                        rates.Add(decimal.Parse(srRates.ReadLine().ToString()));
                    }
                    catch (Exception ex)
                    {
                        writeConsole(outputLevel.Warning, "Bad rate in rates file.  Skipping...");
                        writeConsole(outputLevel.Warning, ex.Message.ToString());
                        continue;
                    }
                }
            }

            // read in loan documents into dtLoans
            int cnt = 0;
            foreach (string f in Directory.EnumerateFiles(inputDirectory, "*.json"))
            {
                cnt++;
                using (StreamReader r = new StreamReader(f))
                {
                    string json = r.ReadToEnd();
                    KeyValuePair<int, loanStructures> l = JsonConvert.DeserializeObject <KeyValuePair<int, loanStructures>>(json);
                    dtLoans.Add(l.Key, l.Value);
                }
            }

            // check we have loans
            if (dtLoans.Count == 0)
            {
                writeConsole(outputLevel.Error, "No loan documents found.  Loan documents must be in *.json format");
                return;
            }

            // generate amort table per doc, per rate, per term
            foreach (KeyValuePair<int, loanStructures> l in dtLoans)
            {
                // reset variation count
                int c = 0;

                // calculate appreciation rate
                decimal appRate = generateRandomDecimal(min, max);

                foreach (int t in terms)
                {
                    foreach (decimal r in rates)
                    {
                        // count loan variation
                        c++;

                        // create new schedule
                        amortSchedule s = new amortSchedule(l.Value.principalAmount, l.Value.assetValue, l.Value.zipcode, r, appRate, freq, t);

                        decimal _currentBalance = l.Value.principalAmount;
                        decimal _assetValue = l.Value.assetValue;
                        DateTime _currentDate = l.Value.startDate;
                        decimal _currentEquity = (_assetValue - _currentBalance);

                        // adjust rate
                        decimal apr = (r / 100);

                        // we need to calculate the monthly payment first
                        decimal monthlyPayment = ((apr / getPeriod()) / (decimal)(1 - (Math.Pow((double)(1 + (apr / getPeriod())), -(t))))) * _currentBalance;
                        monthlyPayment = Math.Round(monthlyPayment, 2);

                        // now calculate payments
                        for (int i = 0; i < t; i++)
                        {
                            // PERFORM calculation
                            decimal monthlyInterest = (_currentBalance * (apr / getPeriod()));
                            decimal monthlyPrincipal = monthlyPayment - monthlyInterest;
                            _currentBalance = _currentBalance - monthlyPrincipal;
                            
                            // get payment
                            if (i > 0)
                            {
                                _currentDate = getNextDate(_currentDate);
                            }

                            // check if this is the last payment and balance is 0
                            if ((i == (t - 1)) && _currentBalance != 0)
                            {
                                monthlyPayment += _currentBalance;
                                _currentBalance = 0.00m;
                            }

                            // calculate appreciation
                            _assetValue = (_assetValue + (_assetValue * (appRate / 100)));

                            // calculate equity
                            _currentEquity = (_assetValue - _currentBalance);

                            // add to schedule
                            amortDetails d = new amortDetails(_currentDate, _currentBalance, monthlyPayment, monthlyPrincipal, monthlyInterest, _assetValue, _currentEquity);
                            s.schedule.Add(i, d);

                        }

                        // add to schedule collection
                        dtAmortSchedule.Add(String.Format("{0}-{1}", l.Key.ToString(), c.ToString()), s);
                    }
                }
            }
        }

        // calculate yeilds
        static void calculatePortfolioYield()
        {
            if (min == max || min > max)
            {
                writeConsole(outputLevel.Error, "You have provided an incorrect value for either min or max for the per run per equity yield.");
                return;
            }

            // get portfolios
            int cnt = 0;
            foreach (string f in Directory.EnumerateFiles(inputDirectory, "*.json"))
            {
                cnt++;
                using (StreamReader r = new StreamReader(f))
                {
                    string json = r.ReadToEnd();
                    KeyValuePair<int, portfolioStructure> p = JsonConvert.DeserializeObject<KeyValuePair<int, portfolioStructure>>(json);
                    dtPortfolios.Add(p.Key, p.Value);
                }
            }

            // verify we got portfolios
            if (dtPortfolios.Count == 0)
            {
                writeConsole(outputLevel.Error, "No portfolios found.  Portfolios must be in *.json format.");
                return;
            }

            // processes yields
            foreach (KeyValuePair<int, portfolioStructure> p in dtPortfolios)
            {
                // we start with the initial portfolio
                Dictionary<DateTime, portfolioStructure> periods = new Dictionary<DateTime, portfolioStructure>();

                // add first/current
                DateTime _date = DateTime.Now;
                periods.Add(_date, p.Value);

                // number of periods to calculate is in loops
                for (int i = 0; i < loops; i++)
                {
                    // get current value
                    decimal _currentValue = 0.00m;
                    Dictionary<string, securityDetails> securities = new Dictionary<string, securityDetails>();

                    // eval each security
                    foreach (KeyValuePair<string, securityDetails> s in p.Value.securities)
                    {
                        int _shares = s.Value.shares;
                        decimal _price = s.Value.sharePrice;

                        // get period yield
                        decimal _yield = (generateRandomDecimal(min, max) / 100);

                        // adjust
                        _price = (_price + (_price * _yield));

                        // calculate total value
                        decimal _totalValue = (_price * _shares);

                        // add yield
                        securityDetails sd = new securityDetails(_shares, _price, _totalValue);

                        // adjust current value
                        _currentValue += _totalValue;

                        // add security details to temp list
                        securities.Add(s.Key, sd);

                    }

                    // get date
                    _date = getNextDate(_date);

                    // add to portfolio period list
                    periods.Add(_date, new portfolioStructure(_currentValue, securities));
                }

                dtPortYield.Add(p.Key, new portfolioPerformance(periods));
            }
        }

        // returns next date based on frequency parameter
        static DateTime getNextDate(DateTime currentDate)
        {
            if (freq == frequency.annually)
            {
                return currentDate.AddYears(1);
            }
            else if (freq == frequency.bimonthly)
            {
                return currentDate.AddMonths(2);
            }
            else if (freq == frequency.biweekly)
            {
                return currentDate.AddDays(14);
            }
            else if (freq == frequency.daily)
            {
                return currentDate.AddDays(1);
            }
            else if (freq == frequency.monthly)
            {
                return currentDate.AddMonths(1);
            }
            else if (freq == frequency.quarterly)
            {
                return currentDate.AddMonths(3);
            }
            else if (freq == frequency.weekly)
            {
                return currentDate.AddDays(7);
            }
            else
            {
                return currentDate;
            }

        }

        // gets period based on frequency parameter
        static int getPeriod()
        {
            if (freq == frequency.annually)
            {
                return 1;
            }
            else if (freq == frequency.bimonthly)
            {
                return 6;
            }
            else if (freq == frequency.biweekly)
            {
                return 104;
            }
            else if (freq == frequency.daily)
            {
                return 365;
            }
            else if (freq == frequency.monthly)
            {
                return 12;
            }
            else if (freq == frequency.quarterly)
            {
                return 4;
            }
            else if (freq == frequency.weekly)
            {
                return 52;
            }
            else
            {
                writeConsole(outputLevel.Warning, "Invalid frequency detected.");
                return 12;
            }
        }

        // generate random decimal
        static decimal generateRandomDecimal(int min, int max)
        {
            int mantissa = generateRandomInt(0, 99);
            int magnitude = generateRandomInt(min, max);
            bool resetSign = false;

            // if the whole part of our # is at the upper boundary
            if (magnitude == max)
            {
                magnitude -= 1;
            }

            // if the floor is negative and the random min is equal to the floor - adjust
            if (min < 0 && magnitude == min)
            {
                magnitude += 1;

                // special case where min == -1 and max == 0
                // in this case we adjust b/c the +1 will bring us to 0 and since we concatenate that takes us out of range - so we have to reset the sign
                if (magnitude == 0)
                {
                    resetSign = true;
                }
            }

            string d = String.Format("{0}.{1}", magnitude.ToString(), mantissa.ToString());

            decimal rand = decimal.Parse(d.ToString());

            if (resetSign)
            {
                rand *= -1;
            }

            return rand;
        }

        // generate random integer
        static int generateRandomInt(int min, int max)
        {
            return rnd.Next(min, max);
        }

        // persist output
        static void writeDataOutput()
        {
            if (outputType == outputFormat.json)
            {
                // write JSON
                writeJsonOutput();
            }
            else if (outputType == outputFormat.xml)
            {
                // write XML
                writeXmlOutput();
            }
            else
            {
                // write CSV
                writeCsvOutput();
            }
        }

        // persist JSON
        static void writeJsonOutput()
        {
            // portfolio generation
            if (mode == runMode.portfoliogeneration)
            {
                foreach(KeyValuePair<int, portfolioStructure> k in dtPortfolios)
                {
                    string fullFileName = String.Format(@"{0}\portfolio_{1}_{2}_{3}.json", outputDirectory.ToString(), k.Key.ToString(), k.Value.securities.Count.ToString(), Guid.NewGuid().ToString());

                    using (StreamWriter wr = File.CreateText(fullFileName))
                    {
                        JsonSerializer s = new JsonSerializer();
                        s.Formatting = Newtonsoft.Json.Formatting.Indented;
                        s.Serialize(wr, k);
                    }
                }
            }
            else if (mode == runMode.loangeneration)
            {
                foreach(KeyValuePair<int, loanStructures> l in dtLoans)
                {
                    string fullFileName = String.Format(@"{0}\loandoc_{1}_{2}_{3}.json", outputDirectory.ToString(), l.Key.ToString(), l.Value.zipcode.ToString(), Guid.NewGuid().ToString());

                    using (StreamWriter wr = File.CreateText(fullFileName))
                    {
                        JsonSerializer s = new JsonSerializer();
                        s.Formatting = Newtonsoft.Json.Formatting.Indented;
                        s.Serialize(wr, l);
                    }
                }
            }
            else if(mode == runMode.amortization)
            {
                foreach(KeyValuePair<string, amortSchedule> a in dtAmortSchedule)
                {
                    string fullFileName = String.Format(@"{0}\amortschedule_{1}_{2}_{3}_{4}.json", outputDirectory.ToString(), a.Key.ToString(), a.Value.term.ToString(), a.Value.loanRate.ToString(), Guid.NewGuid().ToString());

                    using (StreamWriter wr = File.CreateText(fullFileName))
                    {
                        JsonSerializer s = new JsonSerializer();
                        s.Formatting = Newtonsoft.Json.Formatting.Indented;
                        s.Serialize(wr, a);
                    }
                }
            }
            else if(mode == runMode.portfolio)
            {
                foreach (KeyValuePair<int, portfolioPerformance> p in dtPortYield)
                {
                    string fullFileName = String.Format(@"{0}\yield_{1}_{2}.json", outputDirectory.ToString(), p.Key.ToString(), Guid.NewGuid().ToString());

                    using (StreamWriter wr = File.CreateText(fullFileName))
                    {
                        JsonSerializer s = new JsonSerializer();
                        s.Formatting = Newtonsoft.Json.Formatting.Indented;
                        s.Serialize(wr, p);
                    }
                }
            }
            else
            {
                writeConsole(outputLevel.Error, "Model not supported today.");
            }
        }

        // persist XML
        static void writeXmlOutput()
        {
            // portfolio generation
            if (mode == runMode.portfoliogeneration)
            {
                foreach (KeyValuePair<int, portfolioStructure> k in dtPortfolios)
                {
                    string fullFileName = String.Format(@"{0}\portfolio_{1}_{2}_{3}.xml", outputDirectory.ToString(), k.Key.ToString(), k.Value.securities.Count.ToString(), Guid.NewGuid().ToString());

                    XmlWriterSettings xmlSettings = new XmlWriterSettings();
                    xmlSettings.Indent = true;

                    StringBuilder xml = new StringBuilder();

                    using (XmlWriter writer = XmlWriter.Create(xml, xmlSettings))
                    {
                        writer.WriteStartElement("portfolio");
                        writer.WriteStartElement("key");
                        writer.WriteValue(k.Key);
                        writer.WriteEndElement();
                        writer.WriteStartElement("value");
                        writer.WriteStartElement("currentvalue");
                        writer.WriteValue(k.Value.currentValue);
                        writer.WriteEndElement();
                        writer.WriteStartElement("securities");

                        foreach (KeyValuePair<string, securityDetails> l in k.Value.securities)
                        {
                            writer.WriteStartElement("security");
                            writer.WriteStartAttribute("symbol");
                            writer.WriteValue(l.Key);
                            writer.WriteEndAttribute();
                            writer.WriteStartAttribute("totalvalue");
                            writer.WriteValue(l.Value.totalValue.ToString());
                            writer.WriteEndAttribute();
                            writer.WriteStartElement("shares");
                            writer.WriteValue(l.Value.shares);
                            writer.WriteEndElement();
                            writer.WriteStartElement("price");
                            writer.WriteValue(l.Value.sharePrice);
                            writer.WriteEndElement();
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }

                    File.WriteAllText(fullFileName, xml.ToString());
                }

            }
            else if (mode == runMode.loangeneration)
            {
                foreach (KeyValuePair<int, loanStructures> l in dtLoans)
                {
                    string fullFileName = String.Format(@"{0}\loandoc{1}_{2}_{3}.xml", outputDirectory.ToString(), l.Key.ToString(), l.Value.zipcode.ToString(), Guid.NewGuid().ToString());

                    XmlWriterSettings xmlSettings = new XmlWriterSettings();
                    xmlSettings.Indent = true;

                    StringBuilder xml = new StringBuilder();

                    using (XmlWriter writer = XmlWriter.Create(xml, xmlSettings))
                    {
                        writer.WriteStartElement("loandoc");
                        writer.WriteStartElement("key");
                        writer.WriteValue(l.Key);
                        writer.WriteEndElement();
                        writer.WriteStartElement("assetvalue");
                        writer.WriteValue(l.Value.assetValue.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("princpalamount");
                        writer.WriteValue(l.Value.principalAmount.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("zipcode");
                        writer.WriteValue(l.Value.zipcode);
                        writer.WriteEndElement();
                        writer.WriteStartElement("startdate");
                        writer.WriteValue(l.Value.startDate);
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }

                    File.WriteAllText(fullFileName, xml.ToString());

                }

            }
            else if (mode == runMode.amortization)
            {
                foreach (KeyValuePair<string, amortSchedule> s in dtAmortSchedule)
                {
                    string fullFileName = String.Format(@"{0}\amortschedule_{1}_{2}_{3}_{4}.xml", outputDirectory.ToString(), s.Key.ToString(), s.Value.term.ToString(), s.Value.loanRate.ToString(), Guid.NewGuid().ToString());

                    XmlWriterSettings xmlSettings = new XmlWriterSettings();
                    xmlSettings.Indent = true;

                    StringBuilder xml = new StringBuilder();

                    using (XmlWriter writer = XmlWriter.Create(xml, xmlSettings))
                    {
                        writer.WriteStartElement("loan");
                        writer.WriteStartAttribute("key");
                        writer.WriteValue(s.Key.ToString());
                        writer.WriteEndAttribute();
                        writer.WriteStartElement("terms");
                        writer.WriteStartElement("basevalue");
                        writer.WriteValue(s.Value.baseValue.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("assetvalue");
                        writer.WriteValue(s.Value.assetValue.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("zipcode");
                        writer.WriteValue(s.Value.zipCode.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("loanrate");
                        writer.WriteValue(s.Value.loanRate.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("appreciationrate");
                        writer.WriteValue(s.Value.appreciationRate.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("term");
                        writer.WriteValue(s.Value.term.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("freq");
                        writer.WriteValue(s.Value.freq.ToString());
                        writer.WriteEndElement();
                        writer.WriteStartElement("schedule");

                        foreach (KeyValuePair<int, amortDetails> d in s.Value.schedule)
                        {
                            writer.WriteStartElement("payment");
                            writer.WriteStartAttribute("paymentno");
                            writer.WriteValue(d.Key.ToString());
                            writer.WriteEndAttribute();
                            writer.WriteStartElement("pmtDate");
                            writer.WriteValue(d.Value.pmtDate.ToString());
                            writer.WriteEndElement();
                            writer.WriteStartElement("currentbalance");
                            writer.WriteValue(d.Value.currentBalance.ToString());
                            writer.WriteEndElement();
                            writer.WriteStartElement("currentpayment");
                            writer.WriteValue(d.Value.currentPayment.ToString());
                            writer.WriteEndElement();
                            writer.WriteStartElement("currentprincipal");
                            writer.WriteValue(d.Value.currentPrincipal.ToString());
                            writer.WriteEndElement();
                            writer.WriteStartElement("currentinterest");
                            writer.WriteValue(d.Value.currentInterest.ToString());
                            writer.WriteEndElement();
                            writer.WriteStartElement("currentassetvalue");
                            writer.WriteValue(d.Value.currentAssetValue.ToString());
                            writer.WriteEndElement();
                            writer.WriteStartElement("currentequity");
                            writer.WriteValue(d.Value.currentEquity.ToString());
                            writer.WriteEndElement();
                            writer.WriteEndElement(); //payment
                        }

                        writer.WriteEndElement(); // schedule
                        writer.WriteEndElement(); // terms
                        writer.WriteEndElement(); // loan
                    }

                    File.WriteAllText(fullFileName, xml.ToString());
                }

            }
            else if (mode == runMode.portfolio)
            {
                foreach (KeyValuePair<int, portfolioPerformance> y in dtPortYield)
                {
                    string fullFileName = String.Format(@"{0}\yield_{1}_{2}.xml", outputDirectory.ToString(), y.Key.ToString(), Guid.NewGuid().ToString());

                    XmlWriterSettings xmlSettings = new XmlWriterSettings();
                    xmlSettings.Indent = true;

                    StringBuilder xml = new StringBuilder();

                    using (XmlWriter writer = XmlWriter.Create(xml, xmlSettings))
                    {
                        writer.WriteStartElement("portfolioyield");
                        writer.WriteStartAttribute("key");
                        writer.WriteValue(y.Key.ToString());
                        writer.WriteEndAttribute();
                        writer.WriteStartElement("periods");

                        foreach (KeyValuePair<DateTime, portfolioStructure> p in y.Value.periods)
                        {
                            writer.WriteStartElement("period");
                            writer.WriteStartAttribute("date");
                            writer.WriteValue(p.Key.ToString());
                            writer.WriteEndAttribute();
                            writer.WriteStartElement("currentvalue");
                            writer.WriteValue(p.Value.currentValue.ToString());
                            writer.WriteEndElement();
                            writer.WriteStartElement("securities");

                            foreach (KeyValuePair<string, securityDetails> s in p.Value.securities)
                            {
                                writer.WriteStartElement("security");
                                writer.WriteStartAttribute("symbol");
                                writer.WriteValue(s.Key.ToString());
                                writer.WriteEndAttribute();
                                writer.WriteStartAttribute("totalvalue");
                                writer.WriteValue(s.Value.totalValue.ToString());
                                writer.WriteEndAttribute();
                                writer.WriteStartElement("shares");
                                writer.WriteValue(s.Value.shares.ToString());
                                writer.WriteEndElement();
                                writer.WriteStartElement("shareprice");
                                writer.WriteValue(s.Value.sharePrice.ToString());
                                writer.WriteEndElement();
                                writer.WriteEndElement();
                            }

                            writer.WriteEndElement();
                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }

                    File.WriteAllText(fullFileName, xml.ToString());
                }

            }
            else
            {
                writeConsole(outputLevel.Error, "Model not supported today.");
            }
        }

        // persist CSV
        static void writeCsvOutput()
        {
            // portfolio generation
            if (mode == runMode.portfoliogeneration)
            {
                foreach (KeyValuePair<int, portfolioStructure> k in dtPortfolios)
                {
                    string fullFileName = String.Format(@"{0}\portfolio_{1}_{2}_{3}.csv", outputDirectory.ToString(), k.Key.ToString(), k.Value.securities.Count.ToString(), Guid.NewGuid().ToString());

                    foreach(KeyValuePair<string, securityDetails> l in k.Value.securities)
                    {
                        string contents = String.Format("{0},{1},{2},{3},{4},{5}\n", k.Key, k.Value.currentValue.ToString(), l.Key, l.Value.totalValue.ToString(), l.Value.shares, l.Value.sharePrice);
                        File.AppendAllText(fullFileName, contents);
                    }
                }
            }
            else if (mode == runMode.loangeneration)
            {
                string fullFileName = String.Format(@"{0}\loandoc_{1}.csv", outputDirectory.ToString(), Guid.NewGuid().ToString());

                foreach (KeyValuePair<int, loanStructures> l in dtLoans)
                {
                    string contents = String.Format("{0},{1},{2},{3},{4}\n", l.Key.ToString(), l.Value.assetValue.ToString(), l.Value.principalAmount.ToString(), l.Value.zipcode.ToString(), l.Value.startDate.ToString());
                    File.AppendAllText(fullFileName, contents);
                }
            }
            else if (mode == runMode.amortization)
            {
                foreach (KeyValuePair<string, amortSchedule> a in dtAmortSchedule)
                {
                    string fullFileName = String.Format(@"{0}\amortschedule_{1}_{2}_{3}_{4}.csv", outputDirectory.ToString(), a.Key.ToString(), a.Value.term.ToString(), a.Value.loanRate.ToString(), Guid.NewGuid().ToString());
                    StringBuilder doc = new StringBuilder();

                    foreach (KeyValuePair<int, amortDetails> d in a.Value.schedule)
                    {
                        string contents = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15}\n",
                            a.Key.ToString(), a.Value.baseValue.ToString(), a.Value.assetValue.ToString(), a.Value.zipCode.ToString(),
                            a.Value.loanRate.ToString(), a.Value.appreciationRate.ToString(), a.Value.term.ToString(), a.Value.freq.ToString(),
                            d.Key.ToString(), d.Value.pmtDate.ToString(), d.Value.currentBalance.ToString(), d.Value.currentPayment.ToString(),
                            d.Value.currentPrincipal.ToString(), d.Value.currentInterest.ToString(), d.Value.currentAssetValue.ToString(), d.Value.currentEquity.ToString());

                        doc.Append(contents);
                    }

                    File.WriteAllText(fullFileName, doc.ToString());
                }
            }
            else if (mode == runMode.portfolio)
            {
                // go through each portfolio performance period struct
                foreach (KeyValuePair<int, portfolioPerformance> p in dtPortYield)
                {
                    string fullFileName = String.Format(@"{0}\yield_{1}_{2}.csv", outputDirectory.ToString(), p.Key.ToString(), Guid.NewGuid().ToString());
                    StringBuilder doc = new StringBuilder();

                    // go through each period 
                    foreach (KeyValuePair<DateTime, portfolioStructure> s in p.Value.periods)
                    {
                        // go through each security list
                        foreach (KeyValuePair<string, securityDetails> d in s.Value.securities)
                        {
                            string contents = String.Format("{0},{1},{2},{3},{4},{5},{6}\n",
                                p.Key.ToString(), s.Key.ToString(), s.Value.currentValue.ToString(), d.Key.ToString(),
                                d.Value.totalValue.ToString(), d.Value.shares.ToString(), d.Value.sharePrice.ToString());

                            doc.Append(contents);
                        }
                    }

                    File.WriteAllText(fullFileName, doc.ToString());
                }
            }
            else
            {
                writeConsole(outputLevel.Error, "Model not supported today.");
            }
        }
        
    }

}
