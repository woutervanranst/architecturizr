﻿using System;
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
    
    [GeneratedRegex(@"(?<from>[\w-]*) *-->\([0-9]\) ?(?<to>[\w-]*) ?: ?\[(?<topic>[\w-\.]*)\] ?(?<description>.*)?")]
    private static partial Regex AsyncStepRegex();

    [GeneratedRegex(@"(?<to>[\w-]*) ?<-- ?(?<from>[\w-\.]*) ?: ?\ ?(?<description>.*)?")]
    private static partial Regex SyncReturnRegex();

    [GeneratedRegex(@"==\s*(?<sectionName>.*?)\s*==")]
    private static partial Regex SectionRegex();

    public IEnumerable<Process> Parse(FileInfo fi)
    {
        var lines = File.ReadAllLines(fi.FullName);
        
        var p = new Process(fi);
        string title = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;
            
            if (TitleRegex().Match(line) is { Success: true } r0)
            {
                // Title
                p.Name = r0.Value;
                p.FullName = r0.Value;
                title = r0.Value;
            }
            else if (SectionRegex().Match(line) is { Success: true } r1)
            {
                // Flowchart section
                if (p.Steps.Count != 0)
                    yield return p;

                p = new Process(fi)
                {
                    Name = title,
                    FullName = $"{title} - {r1.Groups["sectionName"].Value}"
                };
            }
            else if (line.StartsWith("#") || line.StartsWith("'"))
            {
                // Comment line -- ignore
            }
            else if (line.StartsWith("note"))
            {
                // Note -- ignore
            }
            else if (line.StartsWith("participant "))
            {
                // Participant -- ignore
            }
            else if (line.StartsWith("alt") || 
                     line.StartsWith("else") || 
                     line.StartsWith("end") ||
                     line.StartsWith("opt"))
            {
                // various control statements -- ignore
            }
            else
            {
                // this weird construct since we can't use yield in a try/catch block
                try
                {
                    if (SyncStepRegex().Match(line) is { Success: true } r2)
                    {
                        // Sync step
                        var s = ParseSyncStep(r2);
                        p.Steps.Add(s);
                    }
                    else if (SyncReturnRegex().Match(line) is { Success: true } r3)
                    {
                        // A Sync Return Step -- ignore this for now, this makes the diagram confusing
                        //var s = ParseSyncStep(r3);
                        //p.Steps.Add(s);
                    }
                    else if (AsyncStepRegex().Match(line) is { Success: true } r4)
                    {
                        // Async step
                        var s = ParseAsyncStep(r4);
                        p.Steps.Add(s);
                    }
                    else
                        throw new InvalidOperationException($"'{fi.Name}' line {i}: '{line}' did not match any regular expression");
                }
                catch (Exception e)
                {
                    throw new Exception($"'{fi.Name}' line {i}: '{line}' cannot be parsed", e);
                }
            }
        }

        yield return p;
    }

    

    private SyncStep ParseSyncStep(Match m)
    {
        var from = m.Groups["from"].Value;
        var to = m.Groups["to"].Value;
        var description = m.Groups["description"].Value;

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

    private AsyncStep ParseAsyncStep(Match m)
    {
        AsyncStep s;
        var from = m.Groups["from"].Value;
        var to = m.Groups["to"].Value;
        var topic = m.Groups["topic"].Value;
        var description = m.Groups["description"].Value;

        try
        {
            s = new AsyncStep()
            {
                From = nodes[from],
                To = nodes[to],
                Topic = topic,
                Description = description,
            };
        }
        catch (KeyNotFoundException e)
        {
            throw new InvalidOperationException($"The Node '{e.GetKeyValue()}' is not defined");
        }

        return s;
    }
}