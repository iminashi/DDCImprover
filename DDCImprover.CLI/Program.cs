using DDCImprover.Core;

using System;
using System.IO;

namespace DDCImprover.CLI
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if(args.Length == 1)
            {
                string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
                Configuration.LoadConfiguration(configFile);

                XMLProcessor processor = new XMLProcessor(args[0]);
                var status = processor.LoadXMLFile();
                if (status == ImproverStatus.LoadError)
                {
                    Console.WriteLine("Error during loading.");
                    return 1;
                }

                Console.Write("Processing file.");
                var progress = new Progress<ProgressValue>(_ => Console.Write("."));
                processor.ProcessFile(progress);

                status = processor.Status;
                if (status != ImproverStatus.Completed)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error during processing.");
                    return 1;
                }

                Console.WriteLine();
                Console.WriteLine("File processed.");
            }
            else
            {
                Console.WriteLine("Give an XML file name as an argument.");
            }

            return 0;
        }
    }
}
