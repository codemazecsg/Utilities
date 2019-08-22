using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace recode
{
    class Program
    {
        static Encoding encode = Encoding.UTF8;

        static void Main(string[] args)
        {
            string _sourcePath = string.Empty;
            string _destinationPath = string.Empty;

            if (args.Length != 2)
            {
                Console.Write("You must provide the source directory and destination directory.");
                return;
            }
            else
            {
                _sourcePath = args[0];
                _destinationPath = args[1];
            }

            if (!Directory.Exists(_sourcePath))
            {
                Console.WriteLine("Source directory not found.");
                return;
            }

            if (!Directory.Exists(_destinationPath))
            {
                Console.WriteLine("Destination directory not found.");
                return;
            }


            foreach (string f in Directory.EnumerateFiles(_sourcePath))
            {
                string outputFile = String.Format(@"{0}\{1}", _destinationPath, f.Substring(f.LastIndexOf('\\')));

                Console.WriteLine(String.Format(@"{0} ==> {1}", f.ToString(), outputFile.ToString()));

                StreamReader rdr = new StreamReader(f);
                StreamWriter wr = new StreamWriter(outputFile, false, encode);

                try
                {
                    int cnt = 0;
                    while (!rdr.EndOfStream)
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
        }
    }
}
