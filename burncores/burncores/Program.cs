using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace burncores
{

    /*
     * Author:  Jay Askew, Microsoft Corporation  2018 (c)
     * Date:    January 8, 2018 
     * 
     * This program stresses one or more cores on a system by calculating the value of PI to 'x' decimal places.  The code supports the following features:
     * 
     *      - Setting the precision / # of decimal places to calculate (used to lengthen run time)
     *      - Measuring baseline CPU before and during calculation execution
     *      - Specifying (affinitizing) which CPU each thread runs on (if sampling is enabled - then threads must be affinitized)
     *      - Toggling whether or not to collect and display clock speed and elapsed time 
     *      - Provide an output path to write the cpu stats to (after run)
     *      - Configure sampling value for measuring clock speed (to reduce overhead)
     * 
     */
    class burncores
    {

        // gen config/execution vars
        static bool getHelp = false;                                        // help flag toggle
        static bool display = true;                                         // toggle showing cpu perf stats on console
        static ConsoleColor defaultColor;                                   // save the default console foreground color for writeConsole
        static decimal mainThreadPause = .5m;                               // pause time in seconds for the main thread during monitoring

        // perf vars
        static int procCount = 0;                                           // dynamically determine the number of processors in system
        static cpuPerf[] cpuStats;                                          // array of cpuPerf structures to collect cpu perf stats
        static int threadsComplete = 0;                                     // used to signal the main thread all worker threads are complete
        static int threadsInit = 0;                                         // wait until all threads of initialized
        static object threadLock = new object();                            // synchronize access to threadsComplete and threadsInit vars

        // calculation vars
        static int piPrecision = 1000000;                                   // number of decimal places of PI to calculate
        static int calcCycles = 100;                                        // number of cycles
        static int sampling = 10;                                           // used to determine frequency for measuring clock speed - disabled when set to 0

        // threading vars
        static int threadCount = 0;                                         // thread counter
        static int numOfThreads = 0;                                        // run a specific number of threads in a test on CPUs starting with CPU ID 0 when sampling or any cpu without sampling
        static bool useWindowsScheduler = true;                             // determines whether or not to let the Windows Scheduled select the CPUs to run tests on
        static long affinityMask = 0;                                       // affinity mask / bitmask used to assign each thread to specific processors / when sampling is off and affinityMask is 0 Windows Scheduler decides


        // PInvoke to get OS ThreadID
        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GetCurrentProcessorNumber();

        // basic structure for keeping cpu stats
        struct cpuPerf
        {
            public int? threadId;
            public int? cpuId;
            public DateTime threadStartTime;
            public DateTime threadEndTime;

            public cpuPerf(
                int _threadId,
                int _cpuId
                )
            {
                threadId = _threadId;
                cpuId = _cpuId;
                threadStartTime = new DateTime(1900, 1, 1, 12, 0, 0);
                threadEndTime = new DateTime(1900, 1, 1, 12, 0, 0);
            }
        }

        // output status level
        enum outputLevel
        {
            Error = 1,
            Warning = 2,
            Information = 3
        }

        // main entry point
        static void Main(string[] args)
        {
            // save the default console color
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

            // now we must check for conflicting arguments 
            if (affinityMask > 0 && numOfThreads > 0)
            {
                writeConsole(outputLevel.Error, "You cannot specify the 'Affinity Mask' (M) and 'Number of Threads' (T) parameters together in an execution.  You must choose only one of these parameters.");
                return;
            }

            // lastly we must check that we were given a scheduling option
            if ((affinityMask == 0 && numOfThreads == 0))
            {
                writeConsole(outputLevel.Error, "You must provide one of the following parameters: 'Affinity Mask' (M) or 'Number of Threads' (T)");
                return;
            }
            
            // at this point we should have all the right parameters - let's detemine how to execute the run

            // set the proc count
            procCount = Environment.ProcessorCount;

            // we are only going to support up to 64 procs right now
            if (procCount > 64)
            {
                procCount = 64;
            }

            // by default the Affinity Mask is 0 and we let Windows schedule the threads on the next avail proc
            // first, we must check and set affinity mask
            if (numOfThreads > 0 && sampling > 0)
            {
                // we must calculate the bitmask with 2 to the order of core count and subtract 1 from the result - sampling is ON so we affinitize
                affinityMask = (((long)Math.Pow(2, numOfThreads)) - 1);
                useWindowsScheduler = false;
            }
            else if (affinityMask > 0)
            {
                // a specific affinity mask was provided to us - so we always affinitize
                useWindowsScheduler = false;
            }
            else
            {
                // otherwise neither of the above conditions are true - we were not provided an affinity mask and the tests are not using sampling - so use windows scheduling
                useWindowsScheduler = true;
            }

            // build perf array
            if (useWindowsScheduler)
            {
                cpuStats = new cpuPerf[numOfThreads];
            }
            else
            {
                cpuStats = new cpuPerf[procCount];
            }

            // at this point, the affinity mask is set and we know whether or not to affinitize
            // spin up the threads
            if (useWindowsScheduler)
            {
                // just launch the threads and let Windows decide where to place the threads - we don't care about the affinity mask here - just the thread count
                for (int i = 0; i < numOfThreads; i++)
                {
                    try
                    {
                        // spin up one thread
                        long[] options = new long[] { -1, i };
                        Thread t = new Thread(() => burncores.calculatePi(options));
                        t.Start();
                        threadCount++;
                    }
                    catch (Exception ex)
                    {
                        writeConsole(outputLevel.Error, ex.Message.ToString());
                    }
                }
            }
            else
            {
                // launch the threads according to the calculated affinity mask or the provided affinity mask
                for (int i = 0; i < procCount; i++)
                {
                    // we calculate the proc flag
                    long flag = (long)Math.Pow(2, i);

                    if ((affinityMask & flag) == flag)
                    {
                        try
                        {
                            // bit is "on" - we launch a thread
                            long[] options = new long[] { flag, i };
                            Thread t = new Thread(() => burncores.calculatePi(options));
                            t.Start();
                            threadCount++;
                        }
                        catch(Exception ex)
                        {
                            writeConsole(outputLevel.Error, ex.Message.ToString());
                        }
                    }
                }

            }

            // wait until ready
            while (threadsInit < threadCount)
            {
                Thread.Sleep(500);
            }

            // save console location
            int Left = Console.CursorLeft;
            int Top = Console.CursorTop;
            int loops = 0;

            // init clockSpeed;
            uint numLogicalProcs = 0;

            // figure out ratio of logical:physical
            var cpuSrch = new ManagementObjectSearcher("select DeviceId, NumberOfLogicalProcessors from Win32_Processor");
            int cpuCnt = 0;

            // find # of physical CPUs / sockets
            foreach (var cpu in cpuSrch.Get())
            {
                numLogicalProcs = (uint)cpu["NumberofLogicalProcessors"];
                cpuCnt++;
            }

            // now size array
            uint[] cpuSockets = new uint[cpuCnt];

            // wait until we are complete
            while (threadsComplete < threadCount)
            {
                // increment loop ctr
                loops++;

                // get sampling
                if (sampling > 0)
                {
                    if ((loops % sampling) == 0)
                    {
                        // sample clock speed
                        var searcher = new ManagementObjectSearcher("select CurrentClockSpeed from Win32_Processor");

                        int clockCount = 0;
                        foreach (var item in searcher.Get())
                        {
                            cpuSockets[clockCount] = (uint)item["CurrentClockSpeed"];
                            clockCount++;
                        }
                    }
                }

                // monitor if display 
                if (display)
                {
                    Console.CursorLeft = Left;
                    Console.CursorTop = Top;
                    writeConsole(outputLevel.Information, "");
                    writeConsole(outputLevel.Information, "THREAD #".PadRight(10) + "CPU ID".PadRight(10) + "OS THREAD ID".PadRight(15) + "CLOCK SPEED (MHZ)".PadRight(20) + "ELAPSED TIME (s)");


                    // count threads
                    int cnt = 0;
                    for (int j = 0; j < cpuStats.Length; j++)
                    {
                        // format display
                        string strThreadNo = "--";
                        string strCpuId = "--";
                        string strThreadId = "--";
                        string strTotalSeconds = "--";
                        string strClockSpeed = "--";

                        if (cpuStats[j].cpuId != null && cpuStats[j].threadId != null)
                        {
                            // count threads
                            cnt++;

                            strThreadNo = cnt.ToString();
                            strCpuId = cpuStats[j].cpuId.ToString();
                            strThreadId = cpuStats[j].threadId.ToString();
                            strTotalSeconds = (DateTime.Now.Subtract(cpuStats[j].threadStartTime).TotalSeconds).ToString();

                            // figure out which socket we're on
                            int cpuIndex = ((int)cpuStats[j].cpuId / (int)numLogicalProcs);
                            if (cpuSockets[cpuIndex] > 0)
                            {
                                strClockSpeed = String.Format("{0}[{1}]", cpuSockets[cpuIndex].ToString(), cpuIndex.ToString());
                            }
                        }
                        writeConsole(outputLevel.Information, strThreadNo.ToString().PadRight(10) + strCpuId.ToString().PadRight(10) + strThreadId.ToString().PadRight(15) + strClockSpeed.ToString().PadRight(20) + strTotalSeconds.ToString());

                    }
                }

                // let's wait a bit here
                Thread.Sleep((int)(mainThreadPause * 1000));

            }

            // final update
            if (display)
            {
                int finalLeft = Console.CursorLeft;
                int finalTop = Console.CursorTop;

                Console.CursorTop = Top + 2;

                for (int f = 0; f < cpuStats.Length; f++)
                {
                    if (cpuStats[f].cpuId != null && cpuStats[f].threadId != null)
                    {
                        string strTotalSeconds = (DateTime.Now.Subtract(cpuStats[f].threadStartTime).TotalSeconds).ToString();
                        Console.CursorLeft = 55;
                        writeConsole(outputLevel.Information, strTotalSeconds.ToString() + "*");
                    }
                    else
                    {
                        Console.CursorLeft = 55;
                        writeConsole(outputLevel.Information, "--");
                    }
                }

                Console.CursorLeft = finalLeft;
                Console.CursorTop = finalTop;

                writeConsole(outputLevel.Information, "");
                writeConsole(outputLevel.Information, "Final elapsed time updated.");
            }

        }

        // set parameters
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

                        case "P":
                            // set precision
                            if (!int.TryParse(args[i].Substring(2).ToString(), out piPrecision))
                            {
                                writeConsole(outputLevel.Error, "An invalid value was provided for the calculation precision parameter.");
                                return false;
                            }

                            if (piPrecision < 1 || piPrecision > 1000000)
                            {
                                writeConsole(outputLevel.Error, "The precision must be between 1 and 1000000.");
                                return false;
                            }
                            break;

                        case "C":
                            // set cycle count
                            if (!int.TryParse(args[i].Substring(2).ToString(), out calcCycles))
                            {
                                writeConsole(outputLevel.Error, "An invalid value was provided for the calculation cycles parameter.");
                                return false;
                            }

                            break;

                        case "M":
                            // set affinity mask
                            if (!long.TryParse(args[i].Substring(2).ToString(), out affinityMask))
                            {
                                writeConsole(outputLevel.Error, "An invalid value was provided for the Affinity Mask parameter.");
                                return false;
                            }
                            break;

                        case "T":
                            // number of threads
                            if (!int.TryParse(args[i].Substring(2).ToString(), out numOfThreads))
                            {
                                writeConsole(outputLevel.Error, "An invalid value was provided for the number of threads parameter.");
                                return false;
                            }
                            if (numOfThreads > 64)
                            {
                                writeConsole(outputLevel.Error, "Currently only 64 and less threads are supported.");
                            }
                            break;

                        case "D":
                            // set display on/off
                            string displayOn = args[i].Substring(2).ToString();

                            if (displayOn.ToUpper() == "T")
                            {
                                display = true;
                            }
                            else
                            {
                                display = false;
                            }
                            break;

                        case "L":
                            // set sampling rate
                            if (!int.TryParse(args[i].Substring(2).ToString(), out sampling))
                            {
                                writeConsole(outputLevel.Error, "An invalid value was provided for the sampling parameter.");
                                return false;
                            }
                            break;

                        default:

                            writeConsole(outputLevel.Error, String.Format("An invalid parameter was provided: {0}", args[i].ToString()));
                            break;

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

        // display help
        static void showHelp()
        {
            // show command line startup parameters
            StringBuilder helpMsg = new StringBuilder();

            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("burncores - measures and stress tests cpu cores.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("Written by: Jay Askew");
            helpMsg.Append("Microsoft 2018(c)");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("Version: ");
            helpMsg.Append(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/? or /H        This help screen.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/C              Cycles - number of times to perform pi calculation.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/M              Affinity Mask - bitmask used to specify which CPUs to test.  Cannot be used with /s or /a parameters.  When set to 0 (using /t) and sampling is disabled, Windows selects CPUs.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/T              Number of Threads - number of threads to execute (max 64).  Cannot be used with Affinity Mask parameter.  When sampling is enabled, threads will be assigned starting with CPU ID 0.  When sampling is disabled, Windows selects CPUs.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/D              Display - toggles whether or not stats are shown on the display.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append("/L              Sampling Rate - collects processor clock speed on every nth update.  Disabled by setting to 0.");
            helpMsg.Append(Environment.NewLine);
            helpMsg.Append(Environment.NewLine);
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

        // main calculation 
        static void calculatePi(long[] options)
        {
            long procMask = options[0];
            long idx = options[1];

            uint osThreadId = 0;

            try
            {

                // to get the OS thread ID we must use a Win32 API call via Pinvoke
                osThreadId = GetCurrentThreadId();

                // first we need to determine if we need to set affinity mask
                if (procMask != -1)
                {
                    // we must find it in the process thread collection to set affinity
                    ProcessThread currThread = (from ProcessThread entry in Process.GetCurrentProcess().Threads
                                                where entry.Id == osThreadId
                                                select entry).First();

                    // set affinity
                    currThread.ProcessorAffinity = (IntPtr)procMask;
                }

                // update perf stats array with logical CPU #
                int cpuId = GetCurrentProcessorNumber();

                // init perf stats record in array
                cpuStats[idx] = new cpuPerf((int)osThreadId, cpuId);
                
                // register init
                lock(threadLock)
                {
                    threadsInit++;
                }

                cpuStats[idx].threadStartTime = DateTime.Now;

                // init - we won't actually store b/c only a string can store that much data; a decimal is only 128 bits
                decimal pi = 3.0m;

                // now just perform the calculation and burn CPU cycles
                for (int t = 1; t < calcCycles; t++)
                {
                    // update perf stats array with logical CPU #
                    cpuId = GetCurrentProcessorNumber();
                    cpuStats[idx].cpuId = cpuId;

                    for (int i = 1; i <= piPrecision; i++)
                    {
                        // we are calculating using the Nilakantha series
                        decimal x = (i * 2);
                        decimal op = (4 / (x * (x + 1) * (x + 2)));

                        if ((i % 2) == 0)
                        {
                            // we add
                            pi -= op;
                        }
                        else
                        {
                            pi += op;
                        }
                    }
                }
                // update completion time
                cpuStats[idx].threadEndTime = DateTime.Now;

                // we're done
                lock(threadLock)
                {
                    threadsComplete++;
                }

            }
            catch (Exception ex)
            {
                writeConsole(outputLevel.Error, ex.Message.ToString());

                // we crashed but need to register 
                lock (threadLock)
                {
                    threadsComplete++;
                }
            }
        }
    }
}
