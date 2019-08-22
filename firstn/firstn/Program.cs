using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace firstn
{
    class Program
    {
        static int numberOfLines = 0;
        static string inputFile = string.Empty;
        static int cnt = 0;

        static void Main(string[] args)
        {
            // see if we asked for help

            if (args.Length == 1 && args[0] == "?")
            {
                showUsage();
                return;
            }

            // check for proper # of arguments
            if (args.Length < 2)
            {
                Console.WriteLine("You must provide exactly two arguments in the following format:");
                showUsage();
                return;
            }

            // check for proper arguments
            if (!int.TryParse(args[0].ToString(), out numberOfLines))
            {
                Console.WriteLine("The first argument must be an integer representing the number of lines.");
                showUsage();
                return;
            }

            if (!File.Exists(args[1].ToString()))
            {
                Console.WriteLine("The second argument must be a valid input file.");
                showUsage();
                return;
            }
            else
            {
                inputFile = args[1].ToString();
            }

            // ok we've made it, so now we extract out the first X # of lines

            // create output file
            string outputFile = String.Format(@"{0}_firstn{1}", inputFile.Substring(0, inputFile.IndexOf('.')), inputFile.Substring(inputFile.IndexOf('.')));

            StreamReader rdr = new StreamReader(inputFile);
            StreamWriter wr = new StreamWriter(outputFile);

            try
            {
                while (!rdr.EndOfStream && cnt < numberOfLines)
                {
                    string _line = rdr.ReadLine();
                    wr.WriteLine(_line);
                    cnt++;

                    // flush every 100 lines
                    if ((cnt % 100) == 0)
                    {
                        wr.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            finally
            {
                rdr.Close();
                wr.Close();
            }
        }

        static void showUsage()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: firstn <number of lines> <input file>");
            Console.WriteLine();
            Console.WriteLine("firstn will write the output to a file in the same location and append _firstn to the filename");
            Console.WriteLine();
        }
    }
}
