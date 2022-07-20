using System;
using System.Text.RegularExpressions;
using architecturizr.Models;

namespace architecturizr.InputParsers;

internal class SequencediagramDotOrgProcessParser
{
    public SequencediagramDotOrgProcessParser()
    {
    }

    public Process Parse(FileInfo f)
    {
        var lines = File.ReadAllLines(f.FullName)
            .Where(l => !string.IsNullOrWhiteSpace(l)) // remove empty lines from the file
            .ToArray();

        /*
         * multiline tryout
         * 
         * ! case insensitive
         * ! dotall
         * /(?<=title )(?<processName>([a-zA-Z0-9 ][^\r\n])*).*(?<=product: )(?<product>([a-zA-Z0-9 ][^\r\n])*).*(?<=source: )(?<source>([a-zA-Z0-9 ][^\r\n])*)/gis
         * 
         * stuck: how to not match a newline in a dotall regex
         */

        var p = new Process();

        var titleRegex = new Regex(@"(?<=title )(?<processName>.*)");
        var syncRegex = new Regex(@"(?<from>[\w-]*) ?-> ?(?<to>[\w-]*) ?: ?\ ?(?<description>.*)");
        var asyncRegex = new Regex(@"(?<from>[\w-]*) [-]?->\([0-9]\) (?<to>[\w-]*) ?: ?\[(?<topic>[\w-]*)\] ?(?<description>.*)");


        foreach (var line in lines)
        {
            var z = titleRegex.Match(line);

            if (titleRegex.Match(line) is var r0 && r0.Success)
            {
                p.Name = r0.Value;
            }
            else if (line.StartsWith("#"))
            {
                // Comment line
            }
            else if (syncRegex.Match(line) is var r1 && r1.Success)
            {
                var from = r1.Groups["from"].Value;
                var to = r1.Groups["to"].Value;
                var description = r1.Groups["description"].Value;

                var s = new SyncStep()
                {
                    From = from,
                    To = to,
                    Description = description
                };

                p.Steps.Add(s);
            }
            else if (asyncRegex.Match(line) is var r2 && r2.Success)
            {
                var from = r2.Groups["from"].Value;
                var to = r2.Groups["to"].Value;
                var topic = r2.Groups["topic"].Value;
                var description = r2.Groups["description"].Value;

                var s = new AsyncStep()
                {
                    From = from,
                    To = to,
                    Topic = topic,
                    Description = description,
                };

                p.Steps.Add(s);
            }
        }

        return p;
    }
}