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
using LogAnalyzer.Messages.Enums;

namespace LogAnalyzer
{
    class Program
    {
        static readonly Regex NewMessageCatch = new Regex(@"^\[.*\]");

        public static bool HideTimeStamps = true;
        public static int HideMessagesIfCountLessThan = 0;
        public static MessageType LogLevel = MessageType.UNKNOWN;
        private static bool ContinueParsing = true;
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
                foreach (var @string in rawArchCounters)
                {
                    var archCounter = new ArchCounter();
                    if (archCounter.TryExtract(@string, ref archCounter))
                        Instance.ArchCounters.Add(archCounter);
                }

            if (rawPerformances != null)
                foreach (var @string in rawPerformances)
                {
                    var perfCounter = new PerformanceCounter();
                    if (perfCounter.TryExtract(@string, ref perfCounter))
                        Instance.Performances.Add(perfCounter);
                }

            timer.Stop();
            Console.WriteLine($"Parsed in {timer.ElapsedMilliseconds} ms!");

            timer.Restart();

            Instance.ErrorMessages.Sort((x, y) => y.Count.CompareTo(x.Count));
            Instance.DeviceConnectionMessages.Sort((x, y) => y.Count.CompareTo(x.Count));
            Instance.ArchCounters.Sort((x, y) => x.TimeStamp.CompareTo(y.TimeStamp));
            Instance.Performances.Sort((x, y) => x.TimeStamp.CompareTo(y.TimeStamp));

            timer.Stop();
            Console.WriteLine($"Sorted in {timer.ElapsedMilliseconds} ms!");

            var serializerSettings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
            };

            if (HideTimeStamps) serializerSettings.ContractResolver = new BaseMessage.ConsoleOutContractResolver();

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

            var s = $"==================== Errors ({Instance.ErrorMessages.Sum(item => item.Count)}) ====================";
            Console.WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (s.Length / 2)) + "}", s));

            foreach (var message in Instance.ErrorMessages)
                if (message.Count > HideMessagesIfCountLessThan)
                    Console.Write($"{JsonConvert.SerializeObject(message, Formatting.Indented, serializerSettings)}\n");

            s = $"==================== DevCons Messages ({Instance.DeviceConnectionMessages.Sum(item => item.Count)}) ====================";
            Console.WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (s.Length / 2)) + "}", s));

            foreach (var message in Instance.DeviceConnectionMessages)
                if (message.Count > HideMessagesIfCountLessThan)
                    Console.Write($"{JsonConvert.SerializeObject(message, Formatting.Indented, serializerSettings)}\n");

            s = $"==================== Debug Messages ({Instance.DebugMessages.Sum(item => item.Count)}) ====================";
            Console.WriteLine(String.Format("{0," + ((Console.WindowWidth / 2) + (s.Length / 2)) + "}", s));

            foreach (var message in Instance.DebugMessages)
                if (message.Count > HideMessagesIfCountLessThan)
                    Console.Write($"{JsonConvert.SerializeObject(message, Formatting.Indented, serializerSettings)}\n");
        }

        private static HashSet<string> ParseArgs(string[] args)
        {
            if (args.Length == 0) return ShowHelp() as HashSet<string>;

            HashSet<string> paths = new();

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i].Length == 2 && !args[i].Contains(":")) || args[i].StartsWith("--"))
                {
                    var argument = args[i][1..];
                    _ = argument.ToLower() switch
                    {
                        "s"          => (StartTime = DateTime.Parse(args[i + 1]), i++),
                        "-starttime" => (StartTime = DateTime.Parse(args[i + 1]), i++),
                        "e"          => (EndTime = DateTime.Parse(args[i + 1]), i++),
                        "-endtime"   => (EndTime = DateTime.Parse(args[i + 1]), i++),
                        "t"          => (HideTimeStamps = false),
                        "h"          => (HideMessagesIfCountLessThan = int.Parse(args[i + 1]), i++),
                        "-verbose"   => (SetVerboseLevel(args[i + 1].ToLower()), i++),
                        "?"          => (ShowHelp()),
                        _ => throw new NotImplementedException(message: $"Invalid input parameter: \"{args[i]}\""),
                    };
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

        private static object SetVerboseLevel(string lvl)
        {
            LogLevel = lvl switch
            {
                "debug" => MessageType.DEBUG,
                "exception" => MessageType.EXCEPTION,
                "error" => MessageType.ERROR,
                _ => throw new NotImplementedException(message: $"Invalid input parameter: \"--verbose\""),
            };
            return null;
        }

        private static object ShowHelp()
        {
            string[] ParamsDescription = new string[6]
            {
                "--starttime, -s",  // ParamsDescription[0]
                "--endtime, -e,",   // ParamsDescription[1]
                "-t",               // ParamsDescription[2]
                "-h",               // ParamsDescription[3]
                "--verbose",        // ParamsDescription[4]
                "-?",               // ParamsDescription[5]
            };

            Console.WriteLine(String.Format("{0,0}\n{1,89}\n\n  {2,-18}{3}\n  {4,-18}{5}\n  {6,-18}{7}\n  {8,-18}{9}\n  {10,-18}{11}\n  {12,-18}{13}\n",
                $" Usage: MacroscopRtspUrlGenerator <file|directory> [{ParamsDescription[4]} <DEBUG|EXCEPTION|ERROR>] " +
                $"[{ParamsDescription[2]}] [{ParamsDescription[3]} <int>]",
                $"[{ParamsDescription[0]} <DateTime>] [{ParamsDescription[1]} <DateTime>]",
                $"{ParamsDescription[0]}", "<DateTime>.",
                $"{ParamsDescription[1]}", "<DateTime>.",
                $"{ParamsDescription[2]}", "Specify to show timestamps and threads for messages.",
                $"{ParamsDescription[3]}", "<int>. Specify to hide messages that are less than repeated <int> times.",
                $"{ParamsDescription[4]}", "<DEBUG|EXCEPTION|ERROR>. Default value is UNKNOWN (everything messages including not recognized).",
                $"{ParamsDescription[5]}", "Show this message and exit."));

            Environment.Exit(0);
            return null;
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

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;

            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                ContinueParsing = false;
            }
        }

        private static bool ParseMessages(StreamReader reader, long position, HashSet<string> rawLogStrings, string fileName, ProgressBar progress)
        {
            string line;
            ContinueParsing = true;

            while ((line = reader.ReadLine()) != null && ContinueParsing)
            {
                if (position != reader.BaseStream.Position)
                {
                    progress.Report(reader.BaseStream.Position / (float)reader.BaseStream.Length);
                    position = reader.BaseStream.Position;
                }

                if (NewMessageCatch.Match(line).Success && rawLogStrings.Count > 0)
                {
                    ContinueParsing = CreateNewMessage(fileName, rawLogStrings);
                    rawLogStrings = new HashSet<string>();
                }
                rawLogStrings.Add(line);
            }

            if (rawLogStrings.Count > 0)
                ContinueParsing = CreateNewMessage(fileName, rawLogStrings);

            return ContinueParsing;
        }

        private static bool CreateNewMessage(string fileName, HashSet<string> messageStrings)
        {
#nullable enable
            object? _ = fileName switch
#nullable disable
            {
                "NetworkServer" => new NetworkServer(messageStrings.ToArray()),
                "AppConstruct" => new AppConstruct(messageStrings.ToArray()),
                // "Error" => new Error(messageStrings.ToArray()),
                // "ErrorArchive" => new Error(messageStrings.ToArray()),
                "ConfigStorage_Error" => new ConfigStorage_Error(messageStrings.ToArray()),
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
