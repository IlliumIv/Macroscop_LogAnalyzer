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
using System.Threading;

namespace LogAnalyzer
{
    class Program
    {
        static readonly Regex NewMessageCatch = new Regex(@"^\[.*\]");

        public static bool HideTimeStamps = true;
        public static bool HideMessages = false;
        public static DateTime? StartTime { get; private set; }
        public static DateTime? EndTime { get; private set; }

        private static string[] rawArchCounters;
        private static string[] rawPerformances;

        public static long GlobalMessageID;

        static void Main(string[] args)
        {
            var directoryPaths = ParseArgs(args);

            Stopwatch timer = new Stopwatch();
            timer.Start();

            foreach (string path in directoryPaths)
                Parse(path);

            if (rawArchCounters != null)
                foreach (var s in rawArchCounters)
                {
                    var archCounter = new ArchCounter();
                    if (archCounter.TryExtract(s, ref archCounter))
                        Instance.ArchCounters.Add(archCounter);
                }

            if (rawPerformances != null)
                foreach (var s in rawPerformances)
                {
                    var perfCounter = new PerformanceCounter();
                    if (perfCounter.TryExtract(s, ref perfCounter))
                        Instance.Performances.Add(perfCounter);
                }

            timer.Stop();
            Console.WriteLine($"Parsed in {timer.ElapsedMilliseconds} ms!");

            timer.Restart();

            Instance.ChannelParams.Sort((x, y) => y.Count.CompareTo(x.Count));
            Instance.DeviceConnectionMessages.Sort((x, y) => y.Count.CompareTo(x.Count));
            Instance.ArchCounters.Sort((x, y) => x.TimeStamp.CompareTo(y.TimeStamp));
            Instance.Performances.Sort((x, y) => x.TimeStamp.CompareTo(y.TimeStamp));

            timer.Stop();
            Console.WriteLine($"Sorted in {timer.ElapsedMilliseconds} ms!");

            var serializerSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
            };

            if (HideTimeStamps) serializerSettings.ContractResolver = new LogMessage.ConsoleOutContractResolver();

            if (Instance.ArchCounters.Count > 0) Console.Write($"ArchCounters.Count: {Instance.ArchCounters.Count}\n" +
                "ArchCounters.MinTime: {0}\n" +
                "ArchCounters.MaxTime: {1}\n" +
                "ArchCounters.Shrs: {2}\n" +
                "\n",
                Instance.ArchCounters.Aggregate((curMin, x) => (curMin == null || x.TimeStamp < curMin.TimeStamp ? x : curMin)).TimeStamp,
                Instance.ArchCounters.Aggregate((curMax, x) => (curMax == null || x.TimeStamp > curMax.TimeStamp ? x : curMax)).TimeStamp,
                Instance.ArchCounters.Where(item => item.Shr > 0).Count()
                );

            if (Instance.Performances.Count > 0) Console.Write($"Performances.Count: {Instance.Performances.Count}\n" +
                "Performances.MinTime: {3}\n" +
                "Performances.MaxTime: {4}\n" +
                "Performances.MinPrivateCpuUsage: {0:F}\n" +
                "Performances.MaxPrivateCpuUsage: {1:F}\n" +
                "Performances.AvgPrivateCpuUsage: {2:F}\n" +
                "Performances.MinPrivateMemoryUsage: {5}\n" +
                "Performances.MaxPrivateMemoryUsage: {6}\n" +
                "Performances.AvgPrivateMemoryUsage: {7:F0}\n" +
                "",
                Instance.Performances.Aggregate((curMin, x) => (curMin == null || x.PrivateCpuUsage < curMin.PrivateCpuUsage ? x : curMin)).PrivateCpuUsage,
                Instance.Performances.Aggregate((curMax, x) => (curMax == null || x.PrivateCpuUsage > curMax.PrivateCpuUsage ? x : curMax)).PrivateCpuUsage,
                Instance.Performances.Average(item => item.PrivateCpuUsage),
                Instance.Performances.Aggregate((curMin, x) => (curMin == null || x.TimeStamp < curMin.TimeStamp ? x : curMin)).TimeStamp,
                Instance.Performances.Aggregate((curMax, x) => (curMax == null || x.TimeStamp > curMax.TimeStamp ? x : curMax)).TimeStamp,
                Instance.Performances.Aggregate((curMin, x) => (curMin == null || x.PrivateMemoryUsage < curMin.PrivateMemoryUsage ? x : curMin)).PrivateMemoryUsage,
                Instance.Performances.Aggregate((curMax, x) => (curMax == null || x.PrivateMemoryUsage > curMax.PrivateMemoryUsage ? x : curMax)).PrivateMemoryUsage,
                Instance.Performances.Average(item => item.PrivateMemoryUsage)
                );

            var errors = Instance.ChannelParams.Where(item => item.MessageType == Messages.Enums.MessageType.ERROR
                                                               || item.MessageType == Messages.Enums.MessageType.EXCEPTION
                                                               || (item.MessageType == Messages.Enums.MessageType.UNKNOWN && item.Count > 1));

            Console.WriteLine($"==============================================\nErrors " +
                $"({errors.Sum(item => item.Count)})");

            if (!HideMessages)
                foreach (var message in errors)
                {
                    Console.Write($"{JsonConvert.SerializeObject(message, Formatting.Indented, serializerSettings)}\n");
                }

            var devCons = Instance.DeviceConnectionMessages.Where(item => item.MessageType == Messages.Enums.MessageType.ERROR
                                                               || item.MessageType == Messages.Enums.MessageType.EXCEPTION
                                                               || (item.MessageType == Messages.Enums.MessageType.UNKNOWN && item.Count > 1));

            Console.WriteLine();
            Console.WriteLine($"==============================================\nDevCons Errors " +
                $"({devCons.Sum(item => item.Count)})");

            if (!HideMessages)
                foreach (var message in devCons)
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

                        case "h":
                            HideMessages = true;
                            break;

                        case "?":
                            ShowHelp();
                            break;
                    }
                }
                else
                {
                    var attrs = File.GetAttributes(args[i]);
                    if (attrs.HasFlag(FileAttributes.Directory)) paths = paths.Concat(Directory.GetFiles(args[i])).ToHashSet();
                    else paths.Add(args[i]);
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
            var fileInfo = new FileInfo(path);
            var fileName = fileInfo.Name.Substring(0, fileInfo.Name.IndexOf('.'));
            string message = "Done.";

            Console.Write($"Processing {fileInfo.Name}... ");

            using (var progress = new ProgressBar())
            {
                HashSet<string> rawLogStrings = new HashSet<string>();

                using StreamReader reader = new StreamReader(path);
                var position = reader.BaseStream.Position;

                switch (fileName)
                {
                    case "!!!ArchCounters":
                        if (rawArchCounters == null) rawArchCounters = File.ReadAllLines(fileInfo.FullName);
                        else rawArchCounters = rawArchCounters.Concat(File.ReadAllLines(fileInfo.FullName)).ToArray();
                        break;
                    case "Performance":
                        if (rawPerformances == null) rawPerformances = File.ReadAllLines(fileInfo.FullName).Skip(1).ToArray();
                        else rawPerformances = rawPerformances.Concat(File.ReadAllLines(fileInfo.FullName).Skip(1)).ToArray();
                        break;
                    default:
                        if (!ParseMessages(reader, position, rawLogStrings, fileName, progress)) message = new NotImplementedException().Message;
                        break;
                }
            }

            Console.WriteLine(message);
        }

        private static bool ParseMessages(StreamReader reader, long position, HashSet<string> rawLogStrings, string fileName, ProgressBar progress)
        {
            string line;
            bool success = true;

            while ((line = reader.ReadLine()) != null && success)
            {
                if (position != reader.BaseStream.Position)
                {
                    progress.Report(reader.BaseStream.Position / (float)reader.BaseStream.Length);
                    position = reader.BaseStream.Position;
                }

                if (NewMessageCatch.Match(line).Success && rawLogStrings.Count > 0)
                {
                    success = CreateNewMessage(fileName, rawLogStrings);
                    rawLogStrings = new HashSet<string>();
                }
                rawLogStrings.Add(line);
            }

            if (rawLogStrings.Count > 0)
                success = CreateNewMessage(fileName, rawLogStrings);

            return success;
        }

        private static bool CreateNewMessage(string fileName, HashSet<string> messageStrings)
        {
#nullable enable
            object? _ = fileName switch
#nullable disable
            {
                "AppConstruct" => new AppConstruct(messageStrings.ToArray()),
                "Error" => new Error(messageStrings.ToArray()),
                "DevConInfo" => new DevConInfo(messageStrings.ToArray()),
                "DevConError" => new DevConError(messageStrings.ToArray()),
                "DevConDebug" => new DevConDebug(messageStrings.ToArray()),
                // _ => throw new NotImplementedException($"Could not parse {fileName}")
                _ => null
            };

            return _ != null;
        }
    }
}
