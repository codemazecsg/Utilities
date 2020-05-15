using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Schema;
using System.Diagnostics;

namespace ConsoleApp1
{
    class Program
    {
        static string fileName = string.Empty;
        static int readAhead = 1;
        static string[] lines;
        static string[] matches;
        static string[] exceptions;
        static string breakString;

        static void Main(string[] args)
        {
            // do we have enough args?
            if (args.Length < 5)
            {
                Console.WriteLine("You must provide all parameters.  If no exceptions are required, provide an empty set: \"\"");
                Console.WriteLine("usage: whereparser.exe <filename to parse> <# of readahead lines to parse> <quoted comma separated list of search strings> <quoted comma separated list of exception strings> <break string>");
                return;
            }

            // does the file exist?
            if (!File.Exists(args[0]))
            {
                Console.WriteLine("File not found.");
                return;
            }
            else
            {
                fileName = args[0].ToString();
            }

            // is the read ahead value valid?
            if (!int.TryParse(args[1].ToString(), out readAhead))
            {
                Console.WriteLine("Invalid read ahead value provided.");
                return;
            }

            // set match & exception criteria
            matches = args[2].Split(new char[] { ',' });
            exceptions = args[3].Split(new char[] { ',' });

            // load break string
            breakString = args[4].ToString();

            // size array
            lines = new string[readAhead];

            // now we open and read
            StreamReader streamReader = new StreamReader(fileName);
            string nextLine = string.Empty;
            short blockCnt = 0;
            short foundCnt = 0;
            bool flag = false;
            short lineCnt = 0;

            try
            {
                // let's read the file line by line
                while (!streamReader.EndOfStream)
                {

                    // reset the flag
                    flag = false;

                    // initialize / clean array
                    initLineArray();

                    // load array with the next X # of lines
                    // removed in favor of using a combination of a 'break' string for non-uniform blocks and still a cap as below with the size of the array (readAhead)
                    //
                    // 2020-05-15 
                    //
                    //for (int i = 0; i < lines.Length; i++)
                    //{
                    //    if (!streamReader.EndOfStream)
                    //    {
                    //        lines[i] = streamReader.ReadLine();

                    //        // debug
                    //        if (lines[i].ToLower().Contains(breakString))
                    //        {
                    //            break;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        // reached end of file
                    //        break;
                    //    }
                    //}

                    // build up the lines array
                    lineCnt = 0;
                    while (!streamReader.EndOfStream)
                    {
                        // are we at the limit?
                        if (lineCnt >= lines.Length)
                        {
                            nextLine = string.Empty;
                            break;
                        }

                        // get the right # of lines based on the "break" string
                        if (nextLine != string.Empty)
                        {
                            lines[lineCnt] = nextLine;
                            nextLine = string.Empty;
                        }
                        else
                        {
                            // get the next line
                            nextLine = streamReader.ReadLine();

                            // does the next line have the "break" string
                            if (nextLine.ToLower().Contains(breakString.ToLower()))
                            {
                                break;
                            }
                            else
                            {
                                lines[lineCnt] = nextLine;
                                nextLine = string.Empty;
                            }
                        }

                        lineCnt++;
                    }

                    // we have the array with a block of lines - now process each line
                    for (int i = 0; i < lines.Length; i++)
                    {
                        // we check to see if this line contains any of our search string(s)
                        for (int j = 0; j < matches.Length; j++)
                        {
                            if (lines[i].ToLower().Contains(matches[j].ToLower()))
                            {
                                // this line qualifies
                                flag = true;
                            }
                        }

                        // now check this line for exceptions
                        // we only need to do this if a match as found
                        if (flag)
                        {
                            for (int k = 0; k < exceptions.Length; k++)
                            {
                                if (lines[i].ToLower().Contains(exceptions[k].ToLower()))
                                {
                                    // this line no longer qualifies
                                    flag = false;
                                }
                            }
                        }

                        // now, if we found a line in the block that MATCHES, go ahead and write the whole block and process the next block
                        if (flag)
                        {
                            writeLineBlock();
                            foundCnt++;
                            break;
                        }
                    }

                    blockCnt++;
                    // otherwise a match that didn't have an exception was never found and this block is disgarded
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return;
            }

            Console.WriteLine(String.Format(@"{0} blocks of lines processed.  {1} blocks found with matching lines.", blockCnt.ToString(), foundCnt.ToString()));

        }

        // initialize array
        static void initLineArray()
        {
            // initialize so we're not null in case file is empty
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = "   ";
            }
        }

        // write out array
        static void writeLineBlock()
        {
            for (int i = 0; i < lines.Length; i++)
            {
                Console.WriteLine(lines[i].ToString());
            }
        }
    }
}
