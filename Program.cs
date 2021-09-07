using FindChannels.LogMessages;
using FindChannels.LogMessages.DevCons;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public static List<LogMessage> ChannelParams = new List<LogMessage>();
        public static bool HideTimeStamps = true;
        public static DateTime? StartTime { get; private set; }
        public static DateTime? EndTime { get; private set; }

        static void Main(string[] args)
        {
            var directoryPaths = ParseArgs(args);

            Stopwatch timer = new Stopwatch();
            timer.Start();

            foreach (string path in directoryPaths)
                Parse(path);

            timer.Stop();

            Console.WriteLine($"Parsed in {timer.ElapsedMilliseconds} ms!");

            ChannelParams.Sort((x, y) => y.Count.CompareTo(x.Count));

            var serializerSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
            };

            if (HideTimeStamps) serializerSettings.ContractResolver = new LogMessage.ConsoleOutContractResolver();

            foreach (var message in ChannelParams)
            {
                Console.Write($"{JsonConvert.SerializeObject(message, Formatting.Indented, serializerSettings)}\n");
            }
        }

        private static HashSet<string> ParseArgs(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return null;
            }

            List<string> argsList = new List<string>();
            HashSet<string> paths = new HashSet<string>();

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i].Length == 2 && !args[i].Contains(":")) || args[i].StartsWith("--"))
                {
                    var argument = args[i][1..];
                    switch (argument.ToLower())
                    {
                        case "s":
                        case "-starttime":
                            StartTime = DateTime.Parse(args[i + 1]);
                            i++;
                            break;

                        case "e":
                        case "-endtime":
                            EndTime = DateTime.Parse(args[i + 1]);
                            i++;
                            break;

                        case "t":
                            HideTimeStamps = false;
                            break;

                        case "?":
                            ShowHelp();
                            break;
                    }
                }
                else
                {
                    paths.Add(args[i]);
                }
            }

            return paths;
        }

        private static void ShowHelp()
        {
            Console.WriteLine($"Help");
            Environment.Exit(0);
        }

        static void Parse(string path)
        {
            try
            {
                using StreamReader logFile = new StreamReader(path);
                string line;
                HashSet<string> rawLogStrings = new HashSet<string>();

                var fileInfo = new FileInfo(path);
                var fileName = fileInfo.Name.Substring(0, fileInfo.Name.IndexOf('.'));

                var pos = logFile.BaseStream.Position;

                Console.Write($"Processing {fileInfo.Name}... ");

                using (var progress = new ProgressBar())
                {
                    while ((line = logFile.ReadLine()) != null)
                    {
                        if (pos != logFile.BaseStream.Position)
                        {
                            
                            progress.Report(((float)logFile.BaseStream.Position / (float)logFile.BaseStream.Length));
                            pos = logFile.BaseStream.Position;
                        }

                        if (NewMessageCatch.Match(line).Success && rawLogStrings.Count > 0)
                        {
                            CreateNewMessage(fileName, rawLogStrings);
                            rawLogStrings = new HashSet<string>();
                        }

                        rawLogStrings.Add(line);
                    }
                }

                Console.WriteLine($"Done.");

                if (rawLogStrings.Count > 0)
                    CreateNewMessage(fileName, rawLogStrings);

            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}" +
                    $"\n{e.StackTrace}" +
                    $"");
            }
        }

        private static void CreateNewMessage(string fileName, HashSet<string> messageStrings)
        {
            object _ = fileName switch
            {
                "Error" => new Error(messageStrings.ToArray()),
                "DevConInfo" => new DevConInfo(messageStrings.ToArray()),
                "DevConError" => new DevConError(messageStrings.ToArray()),
                "DevConDebug" => new DevConDebug(messageStrings.ToArray()),
                _ => throw new NotImplementedException()
            };
        }
    }
}
