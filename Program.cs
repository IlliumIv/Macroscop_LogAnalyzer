using LogAnalyzer.Messages;
using LogAnalyzer.Messages.DevCons;
using LogAnalyzer.Counters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogAnalyzer
{
    class Program
    {
        static readonly Regex NewMessageCatch = new Regex(@"^\[.*\]");
        public static (ArchCounters ArchCounters, List<LogMessage> ChannelParams) Instance = (new ArchCounters(), new List<LogMessage>());
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

            Instance.ChannelParams.Sort((x, y) => y.Count.CompareTo(x.Count));

            var serializerSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
            };

            if (HideTimeStamps) serializerSettings.ContractResolver = new LogMessage.ConsoleOutContractResolver();

            Console.WriteLine(JsonConvert.SerializeObject(Instance.ArchCounters, Formatting.Indented));

            foreach (var message in Instance.ChannelParams)
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
            // try
            // {
            var fileInfo = new FileInfo(path);
            var fileName = fileInfo.Name.Substring(0, fileInfo.Name.IndexOf('.'));

            Console.Write($"Processing {fileInfo.Name}... ");

            using (var progress = new ProgressBar())
            {
                HashSet<string> rawLogStrings = new HashSet<string>();

                using StreamReader reader = new StreamReader(path);
                var position = reader.BaseStream.Position;

                switch (fileName)
                {
                    case "!!!ArchCounters":
                        var data = File.ReadAllLines(fileInfo.FullName);
                        Instance.ArchCounters.Append(data);
                        break;
                    default:
                        ParseMessages(reader, position, rawLogStrings, fileName, progress);
                        break;
                }
            }

            Console.WriteLine($"Done.");
            // }
            // catch (Exception e)
            // {
            //     Console.WriteLine($"{e.Message}" +
            //         $"\n{e.StackTrace}" +
            //         $"");
            // }
        }

        private static void ParseMessages(StreamReader reader, long position, HashSet<string> rawLogStrings, string fileName, ProgressBar progress)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (position != reader.BaseStream.Position)
                {
                    progress.Report(reader.BaseStream.Position / (float)reader.BaseStream.Length);
                    position = reader.BaseStream.Position;
                }

                if (NewMessageCatch.Match(line).Success && rawLogStrings.Count > 0)
                {
                    CreateNewMessage(fileName, rawLogStrings);
                    rawLogStrings = new HashSet<string>();
                }
                rawLogStrings.Add(line);
            }

            if (rawLogStrings.Count > 0)
                CreateNewMessage(fileName, rawLogStrings);
        }

        private static void CreateNewMessage(string fileName, HashSet<string> messageStrings)
        {
            object _ = fileName switch
            {
                "AppConstruct" => new AppConstruct(messageStrings.ToArray()),
                "Error" => new Error(messageStrings.ToArray()),
                "DevConInfo" => new DevConInfo(messageStrings.ToArray()),
                "DevConError" => new DevConError(messageStrings.ToArray()),
                "DevConDebug" => new DevConDebug(messageStrings.ToArray()),
                _ => throw new NotImplementedException()
            };
        }
    }
}
