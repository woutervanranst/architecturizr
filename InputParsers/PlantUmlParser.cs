using System;
using System.Text.RegularExpressions;
using architecturizr.Models;
using Microsoft.Extensions.Logging;

namespace architecturizr.InputParsers;

internal partial class PlantUmlParser : IINputParser<Process>
{
    public PlantUmlParser(ILogger<PlantUmlParser> logger)
    {
        this.logger = logger;
    }

    private IDictionary<string, Node> nodes;
    private readonly ILogger<PlantUmlParser> logger;

    public void SetNodes(IDictionary<string, Node> nodes)
    {
        if (this.nodes is not null)
            throw new InvalidOperationException("Nodes can only be set once");

        this.nodes = nodes;
    }

    [GeneratedRegex("(?<=title )(?<processName>.*)")]
    private static partial Regex TitleRegex();
    [GeneratedRegex(@"(?<from>[\w-]*) ?-> ?(?<to>[\w-]*) ?: ?\ ?(?<description>.*)")]
    private static partial Regex SyncStepRegex();
    [GeneratedRegex(@"(?<from>[\w-]*) [-]?->\([0-9]\) (?<to>[\w-]*) ?: ?\[(?<topic>[\w-]*)\] ?(?<description>.*)"]
    private static partial Regex AsyncStepRegex();

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

        foreach (var line in lines)
        {
            if (TitleRegex().Match(line) is var r0 && r0.Success)
            {
                // Title
                p.Name = r0.Value;
            }
            else if (line.StartsWith("#"))
            {
                // Comment line
            }
            else if (line.StartsWith("note"))
            {
                // Note -- ignore
            }
            else if (SyncStepRegex().Match(line) is var r1 && r1.Success)
            {
                // Sync step
                var from = r1.Groups["from"].Value;
                var to = r1.Groups["to"].Value;
                var description = r1.Groups["description"].Value;

                var s = new SyncStep()
                {
                    From = nodes[from],
                    To = nodes[to],
                    Description = description
                };

                p.Steps.Add(s);
            }
            else if (AsyncStepRegex().Match(line) is var r2 && r2.Success)
            {
                // Async step
                var from = r2.Groups["from"].Value;
                var to = r2.Groups["to"].Value;
                var topic = r2.Groups["topic"].Value;
                var description = r2.Groups["description"].Value;

                var s = new AsyncStep()
                {
                    From = nodes[from],
                    To = nodes[to],
                    Topic = topic,
                    Description = description,
                };

                p.Steps.Add(s);
            }
            else
                throw new Exception($"{f.Name}: Line '{line}' cannot be parsed");
        }

        return p;
    }
}