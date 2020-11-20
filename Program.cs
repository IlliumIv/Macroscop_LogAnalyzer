using FindChannels.LogMessages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FindChannels
{
    class Program
    {
        static readonly Regex NewMessageCatch = new Regex(@"^\[.*\]");
        public static Dictionary<string, int> Matches;
        public static Dictionary<string, string> ChannelParams = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();
                Matches = new Dictionary<string, int>();

                foreach (string path in args)
                    LogParser(path);

                timer.Stop();
                Console.WriteLine($"Channels found in {timer.ElapsedMilliseconds} ms!");
                
                foreach (KeyValuePair<string, int> keyValuePair in Matches.OrderByDescending(key => key.Value))
                    Console.WriteLine($"{keyValuePair.Key} --- {keyValuePair.Value}\n\t{ChannelParams[keyValuePair.Key]}\n");
            }
            else
                Console.WriteLine("Empty args!");
        }

        static void LogParser(string path)
        {
            try
            {
                using StreamReader logFile = new StreamReader(path);
                string line;
                List<string> rawLogStrings = new List<string>();
                LogMessage logMessage;

                while ((line = logFile.ReadLine()) != null)
                {
                    if (NewMessageCatch.Match(line).Success && rawLogStrings.Count > 0)
                    {
                        logMessage = new LogMessage(rawLogStrings);
                        rawLogStrings = new List<string>();
                    }

                    rawLogStrings.Add(line);
                }

                if (rawLogStrings.Count > 0)
                    logMessage = new LogMessage(rawLogStrings);

            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}\n{e.StackTrace}");
            }
        }
    }
}
