using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FindChannels.LogMessages
{
    class LogMessage
    {
        DateTime DateTime { get; }
        string ChannelId { get; }

        private readonly string dateTamePattern = "yyyy-MM-dd HH:mm:ss,fff";
        static readonly Regex ChannelIdCatch = new Regex(@"[a-z0-9]{8}-.{4}-.{4}-.{4}-.{12}");
        static readonly Regex AdditionalInfoCatch = new Regex(@"[^,]*= ");

        public LogMessage(List<string> rawLogStrings)
        {
            var strings = rawLogStrings.ToArray();

            string jsonRaw = "{";
            string cropped = strings[0].Substring(1); // Cut off symbol "["
            cropped = cropped.Substring(0, cropped.Length - 1); // Cut off symbol "]"

            string dateTime = cropped.Substring(0, 23);
            jsonRaw += $"\n\t\"DateTime\" : \"{dateTime}\"";

            cropped = cropped.Substring(31); // Cut off date, time and " Thread="

            MatchCollection addInfoMatches = AdditionalInfoCatch.Matches(cropped);
            if (addInfoMatches.Count > 0)
            {
                List<string> addInfoStringCollection = new List<string>();
                int i;

                for (i = 0; i < (addInfoMatches.Count - 2); i++)
                {
                    int start = cropped.IndexOf(addInfoMatches[i].Value);
                    int length = cropped.Length - start - (cropped.Length - cropped.IndexOf(addInfoMatches[i + 1].Value)) - 1;
                    var st = cropped.Substring(start, length);
                    addInfoStringCollection.Add(st.Trim());
                }
                addInfoStringCollection.Add(cropped.Substring(cropped.IndexOf(addInfoMatches[i + 1].Value)).Trim());

                Regex paramValueSeparatorCatch = new Regex(@" = ");
                Regex valueCollectionSeparatorCatch = new Regex(@", ");

                foreach (string s in addInfoStringCollection)
                {
                    string valueString = s;
                    string paramString = "";
                    Match paramValueSeparatorMatch = paramValueSeparatorCatch.Match(valueString);

                    if (paramValueSeparatorMatch.Success)
                    {
                        int length = valueString.Length - (valueString.Length - valueString.IndexOf(paramValueSeparatorMatch.Value));
                        paramString = $"\"{s.Substring(0, length)}\"";
                        valueString = s.Substring(s.IndexOf(paramValueSeparatorMatch.Value) + paramValueSeparatorMatch.Value.Length);
                    }

                    MatchCollection valueCollectionSeparatorMatches = valueCollectionSeparatorCatch.Matches(valueString);
                    if (valueCollectionSeparatorMatches.Count > 0)
                    {

                        #region Not Working
                        List<string> valueStringCollection = new List<string>();
                        int j;

                        for (j = 0; j < (valueCollectionSeparatorMatches.Count - 1); j++)
                        {
                            int start = valueString.IndexOf(valueCollectionSeparatorMatches[j].Value);
                            // int length = valueString.Length - start - (valueString.Length - valueString.IndexOf(valueCollectionSeparatorMatches[j + 1].Value));
                            int length = (valueString.Length - valueString.IndexOf(valueCollectionSeparatorMatches[j + 1].Value)) - start;
                            var st = valueString.Substring(start, length);
                            // Console.WriteLine($"{st}");
                            valueStringCollection.Add(st.Trim());
                        }

                        // valueStringCollection.Add(valueString.Substring(valueString.IndexOf(valueCollectionSeparatorMatches[j + 1].Value)).Trim());


                        valueString = $"[";
                        valueString += $"Not implemented yet";

                        // foreach (string st in valueStringCollection)
                        //     valueString += $"\n\t\t\"{st}\",";
                        // valueString = valueString.Substring(0, valueString.Length - 1);
                        valueString += $"]";
                        #endregion

                    }
                    else
                        valueString = $"\"{valueString}\"";


                    jsonRaw += $"\n\t{paramString} : {valueString}";
                }
            }
            else
                jsonRaw += $"\n\t\"Thread\" : \"{cropped}\"";

            jsonRaw += "\n}";

            Match match = ChannelIdCatch.Match(jsonRaw);
            if (match.Success)
                ChannelId = match.Value;

            if (!(ChannelId is null))
                if (Program.Matches.ContainsKey(ChannelId))
                    Program.Matches[ChannelId]++;
                else
                {
                    Program.Matches.Add(ChannelId, 1);
                    Program.ChannelParams.Add(ChannelId, jsonRaw);
                }
            // Console.WriteLine(jsonRaw);
        }
    }
}
