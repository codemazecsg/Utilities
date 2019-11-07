using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Outlook;
using System.IO;

namespace schedview
{
    class Program
    {
        static bool _printMatrix = false;
        static bool _debug = false;
        static bool displayHelp = false;
        static ConsoleColor defaultColor = Console.ForegroundColor;
        static bool iCal = false;
        static int startOfDay = 900;
        static int endOfDay = 1700;
        static DateTime startOfWeek;
        static DateTime refTime;
        static DateTime refStartOfWeek;
        static DateTime providedStartTime = new DateTime(1900, 1, 1, 0, 0, 0);
        static DateTime refEndOfWeek = new DateTime(1900, 1, 1, 0, 0, 0);
        static byte[,] freeBusyMatrix = new byte[5, 1440];       // here we are creating the array to 1440 minutes long by 5 days wide

        static void Main(string[] args)
        {
            // first process cmd lines
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

            // cannot start the day before midnight nor end the after midnight and start of day cannot be after the end of the day
            if (startOfDay < 0 || endOfDay > 2300 || (startOfDay > endOfDay))
            {
                sendToConsole(ConsoleColor.Red, "The Start of the day or end of the day provided are invalid.");
                return;
            }

            // normalize the start time to our reference time (beginning of the day for the start time or midnight 12:00:00 AM)
            refTime = new DateTime(providedStartTime.Year, providedStartTime.Month, providedStartTime.Day, 0, 0, 0);

            // calculate the start of this week (the beginning of the current week - we are allowed to print the current week but not previous)
            int daysBack = 0;
            switch (DateTime.Now.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    daysBack = 0;

                    break;

                case DayOfWeek.Tuesday:
                    daysBack = -1;

                    break;

                case DayOfWeek.Wednesday:
                    daysBack = -2;

                    break;

                case DayOfWeek.Thursday:
                    daysBack = -3;

                    break;

                case DayOfWeek.Friday:
                    daysBack = -4;

                    break;

                case DayOfWeek.Saturday:
                    daysBack = -5;

                    break;

                case DayOfWeek.Sunday:
                    daysBack = -6;

                    break;

                default:
                    daysBack = 0;

                    break;
            }

            startOfWeek = new DateTime(DateTime.Now.AddDays(daysBack).Year, DateTime.Now.AddDays(daysBack).Month, DateTime.Now.AddDays(daysBack).Day, 0, 0, 0);

            // we won't support historical dates for now
            if (refTime < startOfWeek)
            {
                sendToConsole(ConsoleColor.Red, "Start time is in the past.");
                return;
            }

            // now we want to get the start of the refTime week
            int refDaysBack = 0;
            switch (refTime.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    refDaysBack = 0;

                    break;

                case DayOfWeek.Tuesday:
                    refDaysBack = -1;

                    break;

                case DayOfWeek.Wednesday:
                    refDaysBack = -2;

                    break;

                case DayOfWeek.Thursday:
                    refDaysBack = -3;

                    break;

                case DayOfWeek.Friday:
                    refDaysBack = -4;

                    break;

                case DayOfWeek.Saturday:
                    refDaysBack = -5;

                    break;

                case DayOfWeek.Sunday:
                    refDaysBack = -6;

                    break;

                default:
                    refDaysBack = 0;

                    break;
            }

            // get refStartOfWeek
            refStartOfWeek = startOfWeek = new DateTime(refTime.AddDays(refDaysBack).Year, refTime.AddDays(refDaysBack).Month, refTime.AddDays(refDaysBack).Day, 0, 0, 0);

            // calculate refEndOfWeek
            refEndOfWeek = refStartOfWeek.AddDays(5);

            // we want to initialize the free/busy matrix to set everything to 0 (FREE) at first
            if (!initializeMatrix())
            {
                sendToConsole(ConsoleColor.Red, "Failed to initialize free/busy matrix.");
                return;
            }

            // branch to building free/busy time and exporting matrix to HTML or to exporting an iCal file
            if (iCal)
            {
                exportICal();
            }
            else
            {
                if (!fillMatrix())
                {
                    sendToConsole(ConsoleColor.Red, "Failed to fill matrix.");
                    return;
                }

                if (!exportMatrixHTML())
                {
                    sendToConsole(ConsoleColor.Red, "Failed to export free/busy matrix to HTML file.");
                    return;
                }
            }

            if (_debug)
            {
                sendToConsole(ConsoleColor.Yellow, "Press any key to terminate.");
                Console.ReadKey();
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
                        iCal = true;

                        break;

                    case "S":
                        string _time = args[i].Substring(2);

                        if (!DateTime.TryParse(_time, out providedStartTime))
                        {
                            sendToConsole(ConsoleColor.Red, "Invalid calendar start time");
                            return false;
                        }

                        break;

                    case "D":
                        _debug = true;

                        break;

                    case "P":
                        _printMatrix = true;

                        break;

                    case "B":
                        string _begin = args[i].Substring(2);

                        if (!int.TryParse(_begin, out startOfDay))
                        {
                            sendToConsole(ConsoleColor.Red, "Invalid start of day.");
                            return false;
                        }

                        break;

                    case "E":
                        string _end = args[i].Substring(2);

                        if (!int.TryParse(_end, out endOfDay))
                        {
                            sendToConsole(ConsoleColor.Red, "Invalid end of day.");
                            return false;
                        }

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
            help.Append("schedview - Weekly busy/free time scheduler exporting utility.");
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
            help.Append("/I              Exports calendar to the desktop in an iCalendar (.ics) file format (Default is HTML table).");
            help.Append(Environment.NewLine);
            help.Append("/S              Specifies the start date (in format MM/DD/YYYY) for exporting.  The whole week containing that date will be exported.  Only 1 week can be exported at a time.");
            help.Append(Environment.NewLine);
            help.Append("/D              Enable debug mode.");
            help.Append(Environment.NewLine);
            help.Append("/P              Print free/busy Matrix.");
            help.Append(Environment.NewLine);
            help.Append("/B              Time the work day begins in military time as an integer.  Default is 900 (9am).");
            help.Append(Environment.NewLine);
            help.Append("/E              Time the work day ends in military time as an integer.  Default is 1700 (5pm).");
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

        // initialize matrix
        static bool initializeMatrix()
        {
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 1440; j++)
                {
                    freeBusyMatrix[i, j] = 0;
                }
            }

            return true;
        }

        // build free busy time as a large matrix in memory
        static bool fillMatrix()
        {
            Microsoft.Office.Interop.Outlook.Application oApp = null;
            Microsoft.Office.Interop.Outlook.NameSpace mapiNamespace = null;
            Microsoft.Office.Interop.Outlook.MAPIFolder CalendarFolder = null;
            Microsoft.Office.Interop.Outlook.Items outlookCalendarItems = null;

            try
            {
                oApp = new Microsoft.Office.Interop.Outlook.Application();
                mapiNamespace = oApp.GetNamespace("MAPI"); ;
                CalendarFolder = mapiNamespace.GetDefaultFolder(Microsoft.Office.Interop.Outlook.OlDefaultFolders.olFolderCalendar);
                outlookCalendarItems = CalendarFolder.Items;
                outlookCalendarItems.IncludeRecurrences = true;

                outlookCalendarItems.Sort("[Start]");
                string _dateFilter = string.Format(@"[Start] >= '{0} {1}' AND [END] <= '{2} {3}'",
                    refStartOfWeek.ToShortDateString(),
                    refStartOfWeek.ToShortTimeString(),
                    refEndOfWeek.ToShortDateString(),
                    refEndOfWeek.ToShortTimeString());


                if (_debug)
                {
                    sendToConsole(ConsoleColor.Yellow, _dateFilter.ToString());
                }

                outlookCalendarItems = outlookCalendarItems.Restrict(_dateFilter);


                foreach (Microsoft.Office.Interop.Outlook.AppointmentItem item in outlookCalendarItems)
                {

                    if (_debug)
                    {
                        sendToConsole(ConsoleColor.Yellow, String.Format(@"{0}({1}) - {2}", item.Start.ToString(), item.Duration.ToString(), item.Subject.ToString()));
                    }

                    // we have to reflect this status in the matrix
                    if ((item.BusyStatus == OlBusyStatus.olBusy || item.BusyStatus == OlBusyStatus.olTentative || item.BusyStatus == OlBusyStatus.olOutOfOffice) && item.Start.Year != 4501)
                    {

                        // get day in matrix (row)
                        int _day = -1;
                        // get minute in day (column)
                        int _startBlock = -1;

                        _day = convertDayMatrix(item.Start);
                        _startBlock = convertTimeMatrix(item.Start);

                        // make sure valid, if not do nothing
                        if (_day > -1 && (_startBlock > -1 && _startBlock < 1440))
                        {
                            int _cnt = 0;
                            for (int i = 0; i < item.Duration; i++)
                            {
                                // handle multi-day
                                if (_cnt == 1439)
                                {
                                    // don't run off the week
                                    if (_day < 4)
                                    {
                                        _day++;
                                    }
                                    _cnt = 0;
                                }

                                int _nextBlock = _startBlock + _cnt;
                                if (item.BusyStatus == OlBusyStatus.olBusy)
                                {
                                    // set to busy but Out Of Office takes precedence
                                    if (freeBusyMatrix[_day, _nextBlock] != 3)
                                    {
                                        freeBusyMatrix[_day, _nextBlock] = 1;
                                    }
                                }
                                else if (item.BusyStatus == OlBusyStatus.olOutOfOffice)
                                {
                                    // out of office always takes precedence
                                    freeBusyMatrix[_day, _nextBlock] = 3;
                                }
                                else
                                {
                                    // set to tenative - but both busy and OOF take precedence if other meetings have already set them
                                    if (freeBusyMatrix[_day, _nextBlock] != 1 && freeBusyMatrix[_day, _nextBlock] != 3)
                                    {
                                        freeBusyMatrix[_day, _nextBlock] = 2;
                                    }
                                }

                                _cnt++;
                            }

                            if (item.Duration > 1440)
                            {

                            }
                        }

                    }

                }

                // print free/busy matrix
                if (_printMatrix)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Console.WriteLine("");
                        for (int j = 0; j < 1440; j++)
                        {
                            if (freeBusyMatrix[i, j] == 0)
                            {
                                Console.Write('-');
                            }
                            else if (freeBusyMatrix[i, j] == 1)
                            {
                                Console.Write('X');
                            }
                            else if (freeBusyMatrix[i, j] == 3)
                            {
                                Console.Write('O');
                            }
                            else
                            {
                                Console.Write('?');
                            }
                        }
                    }
                    Console.WriteLine("");
                }


                return true;

            }
            catch (System.Exception ex)
            {
                sendToConsole(ConsoleColor.Red, ex.Message.ToString());
                return false;
            }

        }

        // convert a datetime into a minute integer
        static int convertTimeMatrix(DateTime d)
        {
            return ((d.Hour * 60) + d.Minute);
        }

        // convert a datetime into a day integer
        static int convertDayMatrix(DateTime d)
        {
            switch(d.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    return 0;

                    break;

                case DayOfWeek.Tuesday:
                    return 1;

                    break;

                case DayOfWeek.Wednesday:
                    return 2;

                    break;

                case DayOfWeek.Thursday:
                    return 3;

                    break;

                case DayOfWeek.Friday:
                    return 4;

                    break;

                default:
                    return -1;

                    break;
            }
        }

        // export the free/busy matrix to an HTML file
        static bool exportMatrixHTML()
        {
            StringBuilder _html = new StringBuilder();

            _html.Append("<html>");
            _html.Append("<body>");
            _html.Append("<br>");
            _html.Append("<div style='font-family: Segoe UI; font-size: 13px; font-weight: bold; text-decoration: underline;'>");
            _html.Append(string.Format(@"Free/Busy time for the week of {0}", refStartOfWeek.ToLongDateString()));
            _html.Append("</div>");
            _html.Append("<br>");
            _html.Append("<table style='font-family: Segoe UI; font-size: 10px; font-weight: bold;'>");

            _html.Append("<tr>");
            _html.Append("<td style='text-align: right; font-family: Segoe UI; font-size: 11px; font-weight: bold;'> Block Start Time &#x2192; </td>");

            for (int w = startOfDay; w < endOfDay; w += 15)
            {
                _html.Append("<td style='font-family: Segoe UI; font-size: 10px; font-weight: bold; transform: rotate(-90.0deg); transform-origin: middle top; padding-bottom: 10px;'>");

                if ((w % 100) == 60)
                {
                    w = (w + 40);
                }

                if (w < endOfDay)
                {
                    _html.Append(w.ToString("0000"));
                }

                _html.Append("</td>");
            }

            _html.Append("</tr>");

            for (int i = 0; i < 5; i++)
            {
                _html.Append("<tr>");
                _html.Append("<td style='font-family: Segoe UI; font-size: 10px; text-align: right; font-weight: bold; padding-right: 10px;'>");
                _html.Append(refStartOfWeek.AddDays(i).DayOfWeek.ToString());
                _html.Append(", ");
                _html.Append(refStartOfWeek.AddDays(i).ToShortDateString());
                _html.Append("</td>");
                for (int j = enCodedMatrixReference(startOfDay); j < enCodedMatrixReference(endOfDay); j += 15)
                {
                    if (freeBusyMatrix[i, j] == 0)
                    {
                        _html.Append("<td style='background-color: white; border-style: solid; border-width: 1px; border-color: black;'>&nbsp;</td>");
                    }
                    else if (freeBusyMatrix[i, j] == 1)
                    {
                        _html.Append("<td style='background-color: darkred; border-style: solid; border-width: 1px; border-color: darkred;'>&nbsp;</td>");
                    }
                    else if (freeBusyMatrix[i, j] == 3)
                    {
                        _html.Append("<td style='background-color: purple; border-style: solid; border-width: 1px; border-color: purple;'>&nbsp;</td>");
                    }
                    else
                    {
                        _html.Append("<td style='background-color: orange; border-style: solid; border-width: 1px; border-color: orange;'>&nbsp;</td>");
                    }
                }
                _html.Append("</tr>");
            }

            _html.Append("</table>");

            _html.Append("<br>");
            _html.Append("<table>");
            _html.Append("<tr>");
            _html.Append("<td style='background-color: orange; border-style: solid; border-width: 1px; border-color: black; color: white; font-family: Segoe UI; font-size: 10px; text-align: right; font-weight: bold; margin: 0px; padding: 5px;'>Tentative - please ask</td>");
            _html.Append("<td style='background-color: darkred; border-style: solid; border-width: 1px; border-color: black; color: white; font-family: Segoe UI; font-size: 10px; text-align: right; font-weight: bold; margin: 0px; padding: 5px;'>Busy - not available</td>");
            _html.Append("<td style='background-color: purple; border-style: solid; border-width: 1px; border-color: black; color: white; font-family: Segoe UI; font-size: 10px; text-align: right; font-weight: bold; margin: 0px; padding: 5px;'>Out Of Office</td>");
            _html.Append("<td style='background-color: white; border-style: solid; border-width: 1px; border-color: black; color: black; font-family: Segoe UI; font-size: 10px; text-align: right; font-weight: bold; margin: 0px; padding: 5px;'>Available</td>");
            _html.Append("</tr>");
            _html.Append("</table>");
            _html.Append("</body>");
            _html.Append("</html>");

            // now we save the file
            File.WriteAllText(getDesktopFilePath(false), _html.ToString());

            return true;
        }

        // Takes time reference and returns array
        static int enCodedMatrixReference(int T)
        {
            int _hour = (T / 100);
            int _minutes = (T % 100);

            return ((_hour * 60) + _minutes);
        }

        // export current calendar to ics formatted file
        static void exportICal()
        {
            Application outlook;
            NameSpace OutlookNS;

            outlook = new Application();
            OutlookNS = outlook.GetNamespace("MAPI");

            MAPIFolder f = OutlookNS.GetDefaultFolder(OlDefaultFolders.olFolderCalendar);

            CalendarSharing cs = f.GetCalendarExporter();
            cs.CalendarDetail = OlCalendarDetail.olFreeBusyOnly;
            cs.StartDate = refStartOfWeek;
            cs.EndDate = refEndOfWeek;
            cs.RestrictToWorkingHours = true;

            string _saveFileLoc = getDesktopFilePath(true);

            sendToConsole(defaultColor, string.Format(@"iCalendar file saved to: {0}", _saveFileLoc));
            cs.SaveAsICal(_saveFileLoc);
        }

        // gets and returns a file fully formatted file name and path to the desktop
        static string getDesktopFilePath(bool iCal = false)
        {
            string _filename = string.Format(@"{0}_{1}_{2}{3}{4}", Environment.GetEnvironmentVariable("username").ToString(), "availability", refStartOfWeek.Year.ToString(), refStartOfWeek.Month.ToString("00"), refStartOfWeek.Day.ToString("00"));
            string _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string _ext = "html";

            if (iCal)
            {
                _ext = "ics";
            }

            return string.Format(@"{0}\{1}.{2}", _desktopPath, _filename, _ext);
        }
    }
}
