using System;
using System.Text.RegularExpressions;
using architecturizr.Models;
using architecturizr.Utils;
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
    
    [GeneratedRegex(@"(?<from>[\w-]*) [-]?->\([0-9]\) (?<to>[\w-]*) ?: ?\[(?<topic>[\w-]*)\] ?(?<description>.*)")]
    private static partial Regex AsyncStepRegex();

    [GeneratedRegex(@"(?<to>[\w-]*) ?<-- ?(?<from>[\w-]*) ?: ?\ ?(?<description>.*)")]
    private static partial Regex SyncReturnRegex();

    // trade-universe-importer<--quote-provider-api:Return filtered instrument list with instruments that have quotes

    public Process Parse(FileInfo f)
    {
        var lines = File.ReadAllLines(f.FullName);
            //.Where(l => !string.IsNullOrWhiteSpace(l)) // remove empty lines from the file
            //.ToArray();

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

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            if (TitleRegex().Match(line) is { Success: true } r0)
            {
                // Title
                p.Name = r0.Value;
            }
            else if (line.StartsWith("#") || line.StartsWith("'"))
            {
                // Comment line -- ignore
            }
            else if (line.StartsWith("note"))
            {
                // Note -- ignore
            }
            else if (line.StartsWith(("participant ")))
            {
                // Participant -- ignore
            }
            else if (SyncStepRegex().Match(line) is { Success: true } r1)
            {
                // Sync step
                var s = ParseSyncStep(r1);
                p.Steps.Add(s);
            }
            else if (SyncReturnRegex().Match(line) is { Success: true } r3)
            {
                // A Sync Return Step
                var s = ParseSyncStep(r3);
                p.Steps.Add(s);
            }
            else if (AsyncStepRegex().Match(line) is { Success: true } r2)
            {
                // Async step
                var from = r2.Groups["from"].Value;
                var to = r2.Groups["to"].Value;
                var topic = r2.Groups["topic"].Value;
                var description = r2.Groups["description"].Value;

                try
                {
                    var s = new AsyncStep()
                    {
                        From = nodes[from],
                        To = nodes[to],
                        Topic = topic,
                        Description = description,
                    };

                    p.Steps.Add(s);
                }
                catch (KeyNotFoundException e)
                {
                    throw new InvalidOperationException($"The Node '{e.GetKeyValue()}' is not defined");
                }
            }
            else
                throw new Exception($"{f.Name}: Error on line {i}: '{line}' cannot be parsed");
        }

        return p;
    }

    private SyncStep ParseSyncStep(Match r1)
    {
        var from = r1.Groups["from"].Value;
        var to = r1.Groups["to"].Value;
        var description = r1.Groups["description"].Value;

        try
        {
            var s = new SyncStep()
            {
                From = nodes[from],
                To = nodes[to],
                Description = description
            };

            return s;
        }
        catch (KeyNotFoundException e)
        {
            throw new InvalidOperationException($"The Node '{e.GetKeyValue()}' is not defined");
        }
    }
}