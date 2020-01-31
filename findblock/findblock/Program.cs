using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text;

namespace findblock
{
    class Program
    {
        private Assembly assem = Assembly.GetExecutingAssembly();
        
        static void Main(string[] args)
        {
            // by default we just return the line the string is found in
            int ReadBack = 0;
            int ReadForward = 0;
            string LineNumber = String.Empty;
            int count = 0;
            bool showLineNumbers = false;
            bool showMatchCount = false;
            int matchCount = 0;
            string fileName = String.Empty;
            string[] searchString = null;
            string terminateString = String.Empty;
            string breakoutString = String.Empty;
            bool terminate = false;
            bool breakout = false;
            bool case_sensitive = true;
            bool colormatch = false;
            bool pointer = false;
            bool printFileName = false;
            TextReader tr;
            ConsoleColor cHighlighter = ConsoleColor.Yellow;

            // check for no args
            if (args.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Incorrect syntax.");
                Console.ResetColor();
                ShowHelp();

                return;
            }

            // assume we find nothing
            Environment.ExitCode = 0;

            // grab args and process
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower().StartsWith("-b") || args[i].ToLower().StartsWith("/b"))
                {
                    // we have our read back value
                    if (!int.TryParse(args[i].Substring(2), out ReadBack))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid Read Back Value.");
                        Console.ResetColor();
                        ShowHelp();

                        return;
                    }

                    if (ReadBack > 12)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Read Back value is limited to 12.");
                        Console.ResetColor();
                        ReadBack = 12;
                    }
                    else
                    {
                        ReadBack++;
                    }
                }
                else if (args[i].ToLower().StartsWith("-f") || args[i].ToLower().StartsWith("/f"))
                {
                    // we have our read forward value
                    if (!int.TryParse(args[i].Substring(2), out ReadForward))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid Read Forward Value.");
                        Console.ResetColor();
                        ShowHelp();

                        return;
                    }

                }
                else if (args[i].ToLower().StartsWith("-i") || args[i].ToLower().StartsWith("/i"))
                {
                    fileName = args[i].Substring(2);

                    if (!File.Exists(fileName.ToString()))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("File not found.");
                        Console.ResetColor();
                        ShowHelp();

                        return;
                    }

                }
                else if (args[i].ToLower().StartsWith("-s") || args[i].ToLower().StartsWith("/s"))
                {
                    searchString = args[i].Substring(2).Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else if (args[i].ToLower().StartsWith("-?") || args[i].ToLower().StartsWith("/?"))
                {
                    ShowHelp();
                    return;
                }
                else if (args[i].ToLower().StartsWith("-n") || args[i].ToLower().StartsWith("/n"))
                {
                    showLineNumbers = true;
                }
                else if (args[i].ToLower().StartsWith("-x") || args[i].ToLower().StartsWith("/x"))
                {
                    case_sensitive = false;
                }
                else if (args[i].ToLower().StartsWith("-c") || args[i].ToLower().StartsWith("/c"))
                {
                    showMatchCount = true;
                }
                else if (args[i].ToLower().StartsWith("-h") || args[i].ToLower().StartsWith("/h"))
                {
                    colormatch = true;
                }
                else if (args[i].ToLower().StartsWith("-p") || args[i].ToLower().StartsWith("/p"))
                {
                    pointer = true;
                }
                else if (args[i].ToLower().StartsWith("-t") || args[i].ToLower().StartsWith("/t"))
                {
                    terminate = true;
                    terminateString = args[i].Substring(2);
                }
                else if (args[i].ToLower().StartsWith("-k") || args[i].ToLower().StartsWith("/k"))
                {
                    breakout = true;
                    breakoutString = args[i].Substring(2);
                }
                else if (args[i].ToLower().StartsWith("-e") || args[i].ToLower().StartsWith("/e"))
                {
                    printFileName = true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Unknown parameter.");
                    Console.ResetColor();
                    ShowHelp();

                    return;
                }
            } // for loop

            // Our highlighter color defaults to yellow - let's make sure we don't have a yellow background or a yellow foreground
            if (colormatch)
            {
                if (Console.BackgroundColor == ConsoleColor.Yellow || Console.BackgroundColor == ConsoleColor.DarkYellow || Console.ForegroundColor == ConsoleColor.Yellow
                        || Console.ForegroundColor == ConsoleColor.DarkYellow)
                {
                    // we have either a yellow background or yellow font - be sure we don't have a red background - or secondary choice
                    if (Console.BackgroundColor != ConsoleColor.Red)
                    {
                        cHighlighter = ConsoleColor.Red;
                    }
                    else
                    {
                        cHighlighter = ConsoleColor.White;
                    }
                }
            }

            // set our history queue
            Queue lineQueue = new Queue(ReadBack);  // we set the queue to the size of the read back request

            // open the file or stdin
            if (fileName == String.Empty)
            {
                tr = new StreamReader(Console.OpenStandardInput());
            }
            else
            {
                tr = new StreamReader(fileName.ToString());
            }            
            
            string line = String.Empty;

            while ((line = tr.ReadLine()) != null) // read the file line by line
            {
                // increment line counter for each line read
                count++;

                // first do we enqueue it? if 0, don't enqueue line
                if (ReadBack > 0)
                {
                    // we have a queue
                    // is it full?
                    if (lineQueue.Count == ReadBack)
                    {
                        // we must dequeue 1 item
                        lineQueue.Dequeue();
                    }

                    if (showLineNumbers) { LineNumber = count.ToString() + "  "; }
                    // now enqueue this item
                    lineQueue.Enqueue((string) LineNumber + line);
                }

                // check to see if we terminate now
                if (terminate)
                {
                    if (line.Contains(terminateString) || (!case_sensitive && (line.ToLower().Contains(terminateString.ToLower()))))
                    {
                        // jump out of while loop - we're done
                        return;
                    }
                }

                // Does the line have a match?
                foreach (string s in searchString)
                {
                    if (line.Contains(s) || (!case_sensitive && (line.ToLower().Contains(s.ToLower()))))
                    {
                        // increment match count
                        matchCount++;

                        if (printFileName && !showMatchCount)
                        {
                            Console.WriteLine();
                            Console.WriteLine(String.Format(@"=========>> {0}", fileName.ToString()));
                        }

                        // we have a match - return all queue if we are queueing
                        if (ReadBack > 0 && lineQueue.Count > 0)
                        {
                            // return all from queue
                            while (lineQueue.Count > 0)
                            {
                                // first grab the line
                                string qline = (string)lineQueue.Dequeue();

                                if (lineQueue.Count == 0) // we are at the end of the Queue
                                {
                                    if (colormatch)
                                    {
                                        int pos = qline.ToLower().IndexOf(s.ToLower());
                                        int srchLength = s.Length;

                                        if (pointer)
                                        {
                                            if (!showMatchCount) { Console.Write("--> "); }
                                        }

                                        if (!showMatchCount)
                                        {
                                            Console.Write(qline.Substring(0, pos));
                                            Console.ForegroundColor = cHighlighter;
                                            Console.Write(qline.Substring(pos, srchLength));
                                            Console.ResetColor();
                                            Console.WriteLine(qline.Substring(pos + srchLength));
                                        }
                                    }
                                    else
                                    {
                                        if (pointer)
                                        {
                                            if (!showMatchCount) { Console.Write("--> "); }
                                        }

                                        if (!showMatchCount) { Console.WriteLine(qline.ToString()); }
                                    }
                                }
                                else
                                {
                                    if (!showMatchCount) { Console.WriteLine(qline.ToString()); }
                                }
                            } // queue while
                        }
                        else
                        {
                            // we are not queueing

                            if (colormatch)
                            {
                                int pos = line.ToLower().IndexOf(s.ToLower());
                                int srchLength = s.Length;

                                if (pointer)
                                {
                                    if (!showMatchCount) { Console.Write("--> "); }
                                }

                                if (!showMatchCount)
                                {
                                    if (showLineNumbers) { LineNumber = count.ToString() + "  "; }
                                    Console.Write(LineNumber + line.Substring(0, pos));
                                    Console.ForegroundColor = cHighlighter;
                                    Console.Write(line.Substring(pos, srchLength));
                                    Console.ResetColor();
                                    Console.WriteLine(line.Substring(pos + srchLength));
                                }
                            }
                            else
                            {
                                if (pointer)
                                {
                                    if (!showMatchCount) { Console.Write("--> "); }
                                }

                                if (!showMatchCount)
                                {
                                    if (showLineNumbers) { LineNumber = count.ToString() + "  "; }
                                    Console.WriteLine(LineNumber + line.ToString());
                                }
                            }

                        }

                        // are we reading forward?
                        if (ReadForward > 0)
                        {
                            //advance the text reader and return all
                            for (int i = 0; i < ReadForward; i++)
                            {
                                string fline = tr.ReadLine();
                                count++;

                                // check to see if we have reached a point to jump
                                if (breakout)
                                {
                                    if (fline.Contains(breakoutString) || (!case_sensitive && (fline.ToLower().Contains(breakoutString.ToLower()))))
                                    {
                                        // jump out of this iteration
                                        break;
                                    }
                                }

                                // check to see if we terminate now
                                if (terminate)
                                {
                                    if (fline.Contains(terminateString) || (!case_sensitive && (fline.ToLower().Contains(terminateString.ToLower()))))
                                    {
                                        // jump out of while loop - we're done
                                        return;
                                    }
                                }

                                if (fline == null)
                                {
                                    return;
                                }

                                if (!showMatchCount)
                                {
                                    if (showLineNumbers) { LineNumber = count.ToString() + "  "; }
                                    Console.WriteLine(LineNumber + fline.ToString());
                                }
                            }
                        }
                    }
                }

            }

            // return console to original colors
            Console.ResetColor();

            // show count
            if (showMatchCount) { Console.WriteLine("Matches found: " + matchCount.ToString()); }

            // set exit 
            if (matchCount > 0) { Environment.ExitCode = 1; }
            
            // close file
            tr.Close();

        } // main

        // Display program help to console
        static void ShowHelp()
        {
            Assembly assem = Assembly.GetExecutingAssembly();
            AssemblyName assemName = assem.GetName();
            Version ver = assemName.Version;

            Console.WriteLine();
            Console.WriteLine("Written by Jay Askew (MSFT)");
            Console.WriteLine("Version: {0}", ver.ToString());
            Console.WriteLine();
            Console.WriteLine("Usage: findblock -i<input file> -s<search string> [-b<number of lines to read back> -f<number of lines to read forward> -t<terminate string> -x -h -c -p -n]");
            Console.WriteLine();
            Console.WriteLine("\t -b \t Number of lines to return before (Read Back) the matching line. Limit is 12 lines.");
            Console.WriteLine("\t -f \t Number of lines to return after (Read Forward) the matching line.");
            Console.WriteLine("\t -i \t Input file to search - must be a text file.");
            Console.WriteLine("\t -s \t String to search for.  The search string is case-sensitive by default without the -x parameter.");
            Console.WriteLine("\t -x \t Do case insensitive searching.");
            Console.WriteLine("\t -h \t Highlight match in search string.  This only highlights in the search string - not in the Read Back or Read Forward lines.");
            Console.WriteLine("\t -p \t Use a pointer to point to the matching lines (\"-->\").");
            Console.WriteLine("\t -n \t Show the line number from the orginal file.");
            Console.WriteLine("\t -c \t Returns just the number of matches but does not return the matching lines or any read back or read forward lines.");
            Console.WriteLine("\t -t \t Stops searching and exits once this provided \"terminate\" string is encountered.");
            Console.WriteLine("\t -k \t Stops outputting read forward lines once this \"break\" string is encountered and starts searching again from that point forward.");
            Console.WriteLine("\t -e \t Echos file name to console for lines containing a match.");
            Console.WriteLine("\t -? \t This screen.");
            Console.WriteLine();
            Console.WriteLine("\t Note: if -b and -f are *NOT* provided, then only the matching line is returned.");
            Console.WriteLine();
            Console.WriteLine("\t Note: The Read Forward lines are not searched, counted, or highlighted for matches but they are searched for terminating strings (-t) and breaking strings (-k).");
            Console.WriteLine();
            Console.WriteLine("\t Examples:  ");
            Console.WriteLine("\t \t \t To find \"mySearchString\" in \"myfile.txt\" and show two previous and 5 following lines, use: ");
            Console.WriteLine("\t \t \t findblock -b2 -f5 -ic:\\temp\\myfile.txt -s\"mySearchString\"");
            Console.WriteLine();
            Console.WriteLine("\t \t \t To search the same as the above and return the line numbers from the original file with pointers to the matching lines, use: ");
            Console.WriteLine("\t \t \t findblock -b2 -f5 -ic:\\temp\\myfile.txt -s\"mySearchString\" -p -n");
            Console.WriteLine();
            Console.WriteLine("\t \t \t To search the same as the above, stop searching, and exit when a certain string is encountered, use: ");
            Console.WriteLine("\t \t \t findblock -b2 -f5 -ic:\\temp\\myfile.txt -s\"mySearchString\" -t\"stopString\"");
            Console.WriteLine();
            Console.WriteLine("\t \t \t To search the same as the above and stop outputting read forward lines when a certain string is encountered and begin searching from that point, use: ");
            Console.WriteLine("\t \t \t findblock -b2 -f5 -ic:\\temp\\myfile.txt -s\"mySearchString\" -k\"breakString\"");
            Console.WriteLine();
            Console.WriteLine("\t \t \t To search the same as the above and and return only a count of the matches, use: ");
            Console.WriteLine("\t \t \t findblock -b2 -f5 -ic:\\temp\\myfile.txt -s\"mySearchString\" -c");
            Console.WriteLine();
            Console.WriteLine("\t \t \t To look for a specific string in redirected output - such as a directory listing, use: ");
            Console.WriteLine("\t \t \t dir c:\\windows\\*.exe | findblock -s\"notepad.exe\" -x");
            Console.WriteLine();
            Console.WriteLine();

            return;
        }

    }
}
