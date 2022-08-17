namespace architecturizr.Models;

internal class Process
{
    public string Name { get; set; }

    public List<Step> Steps { get; } = new();

    public override string ToString() => Name;
}

internal abstract class Step
{
    public Node From { get; init; }
    public Node To { get; init; }
    public string Description { get; init; }
}

internal class AsyncStep : Step
{
    public string Topic { get; init; }

    public override string ToString() => $"AsyncStep: {From.Name} -> {To.Name} on {Topic}";
}

internal class SyncStep : Step
{
    public override string ToString() => $"SyncStep: {From.Name} -> {To.Name}";
}