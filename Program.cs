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
        static readonly Regex lineCatch = new Regex(@"[a-z0-9]{8}-.{4}-.{4}-.{4}-.{12}");
        static Dictionary<string, int> matches = new Dictionary<string, int>();
        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();

                foreach (string path in args)
                {
                    try
                    {
                        StreamReader file = new StreamReader(path);
                        string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            Match match = lineCatch.Match(line);
                            if (match.Success)
                                if (matches.ContainsKey(match.Value))
                                    matches[match.Value]++;
                                else
                                    matches.Add(match.Value, 1);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                timer.Stop();
                Console.WriteLine($"Channels found in {timer.ElapsedMilliseconds} ms!");
                
                foreach (KeyValuePair<string, int> keyValuePair in matches.OrderByDescending(key => key.Value))
                    Console.WriteLine($"{keyValuePair.Key} --- {keyValuePair.Value}");
            }
            else
                Console.WriteLine("Empty args!");
        }
    }
}
