namespace architecturizr.Models;

internal class Process
{
    public string Name { get; set; }

    public List<Step> Steps { get; } = new();


}

internal abstract class Step
{
    public string From { get; init; }
    public string To { get; init; }
    public string Description { get; init; }
}

internal class AsyncStep : Step
{
    public string Topic { get; init; }

}

internal class SyncStep : Step
{
}