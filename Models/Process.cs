namespace architecturizr.Models;

internal class Process
{
    public Process(FileInfo source)
    {
        Source = source;
    }

    public FileInfo Source { get; }
    public string Name { get; set; }
    public string FullName { get; set; }


    public List<Step> Steps { get; } = new();

    public override string ToString() => FullName;
}

internal abstract class Step
{
    public required Node From { get; init; }
    public required Node To { get; init; }
    public required string Description { get; init; }
}

internal class AsyncStep : Step
{
    public required string Topic { get; init; }

    public override string ToString() => $"AsyncStep: {From.Name} -> {To.Name} on {Topic}";
}

internal class SyncStep : Step
{
    public override string ToString() => $"SyncStep: {From.Name} -> {To.Name}";
}