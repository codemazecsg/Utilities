using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stressIO
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length == 0 || args == null || args[0] == "?" || args.Length < 6)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("stressIO  <filename> <file size in KB> <seconds between writes> <block size in bytes> <outstanding I/Os> <WriteThrough>");
                return;
            }

            long fileSizeKB = 1024;
            int pause = 0;
            int szWrite = 8192;
            int outStanding = 0;
            double avgDuration = 0;
            long iops = 0;
            DateTime start;
            DateTime end;
            Boolean WriteThrough = false;
            string fileName = String.Empty;

            try
            {
                fileSizeKB = Int32.Parse(args[1].ToString());
                pause = Int32.Parse(args[2].ToString());
                szWrite = Int32.Parse(args[3].ToString());
                fileName = args[0].ToString();
                outStanding = Int32.Parse(args[4].ToString());
                WriteThrough = Boolean.Parse(args[5].ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return;
            }

            if (fileName == String.Empty)
            {
                Console.WriteLine("You must provide a filename");
                return;
            }

            if (outStanding < 0)
            {
                Console.WriteLine("Outstanding I/Os must be 0 or greater.");
                return;
            }

            if (fileSizeKB <= 0)
            {
                Console.WriteLine("Iterations must be greater than 0.");
                return;
            }

            if (pause < 0)
            {
                Console.WriteLine("The # of seconds between each write must be 0 or greater.");
                return;
            }

            if (szWrite % 1024 != 0)
            {
                Console.WriteLine("The write size must be a multiple of 1024");
                return;
            }

            if (fileSizeKB % 1024 != 0)
            {
                Console.WriteLine("The file size must be a multiple of 1024 in KB");
                return;
            }

            if (szWrite < 8192 || szWrite > 16777216)
            {
                Console.WriteLine("The write size must be between 8192 and 16777216");
                return;
            }

            if (szWrite > (fileSizeKB * 1024))
            {
                Console.Write("The file size must be larger than the write size");
                return;
            }

            // source buffer
            byte[] srcBuffer = new byte[szWrite];

            // initialize array
            Console.WriteLine("Initializing...");
            for (int i = 0; i < (szWrite); i++)
            {
                srcBuffer[i] = ASCIIEncoding.ASCII.GetBytes("0")[0];
            }

            // remove old file
            if (File.Exists(fileName))
            {
                try
                {
                    File.Delete(fileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message.ToString());
                    return;
                }
            }
            Console.WriteLine("Done...");

            // start file stream
            FileStream fs;
            if (WriteThrough)
            {
                fs = new FileStream(fileName.ToString(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, szWrite, FileOptions.WriteThrough);
            }
            else
            {
                fs = new FileStream(fileName.ToString(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, szWrite, FileOptions.None);
            }
            

            start = DateTime.Now;

            try
            {
                int currQueue = 0;
                for (long i = 0; i < ((fileSizeKB * 1024) / szWrite); i++)
                {
                    DateTime t = DateTime.Now;
                    fs.Write(srcBuffer, 0, szWrite);
                    fs.Flush();
                    currQueue++;

                    if (currQueue >= outStanding)
                    {
                        fs.Flush();
                        currQueue = 0;
                    }

                    DateTime e = DateTime.Now;
                    avgDuration += e.Subtract(t).TotalSeconds;
                    iops++;
                    Console.WriteLine(szWrite.ToString() + " bytes written to " + fileName.ToString() + " in " + e.Subtract(t).TotalSeconds.ToString() + " seconds.");

                    if (pause != 0)
                    {
                        Console.WriteLine("pausing " + pause.ToString() + " seconds.");
                        Thread.Sleep(pause);   
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return;
            }
            finally
            {
                fs.Flush();
                fs.Close();
            }

            end = DateTime.Now;

            Console.WriteLine("Total duration: " + end.Subtract(start).TotalSeconds.ToString() + " seconds.");
            Console.WriteLine(iops.ToString() + " logical IOPs total of " + szWrite.ToString() + " bytes averaging " + (avgDuration/iops).ToString() + " seconds in duration.");

        }
    }
}
