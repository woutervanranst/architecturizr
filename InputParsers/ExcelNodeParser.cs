using architecturizr.Models;
using ExcelToEnumerable;
using Microsoft.Extensions.Logging;

namespace architecturizr.InputParsers;

/// <summary>
/// Read the Excel Source file
/// https://stackoverflow.com/a/15793495
/// </summary>
internal class ExcelNodeParser : IINputParser<(string title, string description, IDictionary<string, Node>)>
{
    private readonly ILogger<ExcelNodeParser> logger;

    public ExcelNodeParser(ILogger<ExcelNodeParser> logger)
    {
        this.logger = logger;
    }

    public IEnumerable<(string title, string description, IDictionary<string, Node>)> Parse(FileInfo excel)
    {
        var exceptionList = new List<Exception>();

        // Parse General Tab
        using var fs1 = excel.OpenRead();
        var generalRows = fs1.ExcelToEnumerable<GeneralRow>(o => o
                .UsingSheet("General")
                .UsingHeaderNames(false) //map using column numbers, not names
                .OutputExceptionsTo(exceptionList))
            .ToArray();

        if (exceptionList.Any())
            throw new Exception();

        var title = generalRows.Single(r => r.Key == "Title").Value;
        var description = generalRows.Single(r => r.Key == "Description").Value;


        // Parse Nodes Tab
        using var fs2 = excel.OpenRead();
        var nodeRows = fs2.ExcelToEnumerable<NodeRow>(o => o
                .UsingSheet("Nodes")
                .OutputExceptionsTo(exceptionList)

                .IgnoreColumsWithoutMatchingProperties()

                .Property(r => r.Row).MapsToRowNumber())
            .ToArray();

        if (exceptionList.Any())
            throw new Exception();

        var nodes = ParseNodes(nodeRows);

        //Persons = nodes.Values.OfType<Person>().ToArray();
        //SoftwareSystems = nodes.Values.OfType<SoftwareSystem>().ToArray();
        //Containers = nodes.Values.OfType<Container>().ToArray();
        //Components = nodes.Values.OfType<Component>().ToArray();

        yield return (title, description, nodes);
    }

    //public string Title { get; init; }
    //public string Description { get; init; }

    //public IEnumerable<Node> Nodes { get; init; }

    

    // public IEnumerable<Person> Persons { get; init; }
    // public IEnumerable<SoftwareSystem> SoftwareSystems { get; init; }
    // public IEnumerable<Container> Containers { get; init; }
    // public IEnumerable<Component> Components { get; init; }

    // public IEnumerable<Edge> Edges { get; init; }

    /// <summary>
    /// Parse every row in the source to exactly one node type
    /// </summary>
    /// <param name="rows"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private IDictionary<string, Node> ParseNodes(IEnumerable<NodeRow> rows)
    {
        // Checks:
        // 1. Use Dictionary to avoid duplicate keys
        // 2. Use null checks in the row type matcher to avoid matches to multiple row types

        var nodes = new Dictionary<string, Node>();

        foreach (var row in rows)
        {
            Node? n = null;

            ValidateTechnologyIconExists(row);

            if (row.IsPersonRow)
            {
                // Person
                n = new Person
                {
                    Key = row.PersonKey,
                    Name = row.Name ?? throw new ArgumentNullException(),
                    Description = row.Description,
                    Technology = row.Technology,
                    Tags = row.Tags
                };
            }
            if (row.IsSoftwareSystemRow)
            {
                if (n != null)
                    throw new InvalidOperationException($"Row #{row.Row} matches multiple types");
                
                // Software System
                n = new SoftwareSystem
                {
                    Key = row.SoftwareSystemKey,
                    Name = row.Name ?? throw new ArgumentNullException(),
                    Description = row.Description,
                    Technology = row.Technology,
                    Tags = row.Tags,
                    Owner = row.Owner
                };
            }
            if (row.IsContainerRow)
            {
                if (n != null)
                    throw new InvalidOperationException($"Row #{row.Row} matches multiple types");

                // Container
                var parent = nodes.Values.OfType<SoftwareSystem>().SingleOrDefault(n => n.Key == row.SoftwareSystemKey) ??
                             throw new InvalidOperationException($"Parent '{row.SoftwareSystemKey}' of row #{row.Row} is not defined.");

                n = new Container
                {
                    Key = row.ContainerKey,
                    Parent = parent,
                    Name = row.Name ?? throw new ArgumentNullException(),
                    Description = row.Description,
                    Technology = row.Technology,
                    Tags = row.Tags,
                    Owner = row.Owner
                };
            }
            if (row.IsComponentRow)
            {
                if (n != null)
                    throw new InvalidOperationException($"Row #{row.Row} matches multiple types");

                // Component
                var parent = nodes.Values.OfType<Container>().SingleOrDefault(n => n.Key == row.ContainerKey && n.Parent.Key == row.SoftwareSystemKey) ??
                        throw new InvalidOperationException($"Parent '{row.SoftwareSystemKey}|{row.ContainerKey}' of row #{row.Row} is not defined.");
                
                n = new Component
                {
                    Key = row.ComponentKey,
                    Parent = parent,
                    Name = row.Name ?? throw new ArgumentNullException(),
                    Description = row.Description,
                    Technology = row.Technology,
                    Tags = row.Tags,
                    Owner = row.Owner
                };
            }

            // Parse Views
            n.Views.AddRange(ParseViews((dynamic)n, row));


            if (n == null)
                throw new InvalidOperationException($"Row #{row.Row} did not match any Node type");
            else if (nodes.ContainsKey(n.Key))
                throw new InvalidOperationException($"Duplicate key '{n.Key}' in row #{row.Row}");
            else
                nodes.Add(n.Key, n);
        }

        return nodes;

        void ValidateTechnologyIconExists(NodeRow node)
        {
            if (node.Technology is null)
                return;
            
            if (Properties.Resources.ResourceManager.GetObject(node.Technology) is null)
                logger.LogWarning($"Technology '{node.Technology}' does not have an icon defined.");
        }
    }

    private static IEnumerable<string> ParseViews(Person o, NodeRow r)
    {
        if (!string.IsNullOrWhiteSpace(r.SystemContextView))
            throw new NotImplementedException();
        if (!string.IsNullOrWhiteSpace(r.ContainerView))
            throw new NotImplementedException();
        if (!string.IsNullOrWhiteSpace(r.ComponentView))
            throw new NotImplementedException();

        return Array.Empty<string>();
    }
    private static IEnumerable<string> ParseViews(SoftwareSystem o, NodeRow r)
    {
        if (!string.IsNullOrWhiteSpace(r.SystemContextView))
            yield return Views.SystemContextView;
        if (!string.IsNullOrWhiteSpace(r.ContainerView))
            yield return Views.ContainerView;
        if (!string.IsNullOrWhiteSpace(r.ComponentView))
            throw new InvalidOperationException($"{o.GetType().Name} '{o.Key}' cannot have a ComponentView");
    }
    private static IEnumerable<string> ParseViews(Container c, NodeRow r)
    {
        if (!string.IsNullOrWhiteSpace(r.SystemContextView))
            yield return Views.SystemContextView;
        if (!string.IsNullOrWhiteSpace(r.ContainerView))
            yield return Views.ContainerView;
        if (!string.IsNullOrWhiteSpace(r.ComponentView))
            yield return Views.ComponentView;

    }
    private static IEnumerable<string> ParseViews(Component c, NodeRow r)
    {
        if (!string.IsNullOrWhiteSpace(r.SystemContextView))
            yield return Views.SystemContextView;
        if (!string.IsNullOrWhiteSpace(r.ContainerView))
            yield return Views.ContainerView;
        if (!string.IsNullOrWhiteSpace(r.ComponentView))
            yield return Views.ComponentView;
    }

    private record GeneralRow
    {
        public string? Key { get; init; }
        public string? Value { get; init; }
    }

    private record NodeRow
    {
        public int Row { get; init; }
        public string? PersonKey { get; init; }
        public string? SoftwareSystemKey { get; init; }
        public string? ContainerKey { get; init; }
        public string? ComponentKey { get; init; }

        public string? Name { get; init; }
        public string? Technology { get; init; }
        public string? Tags { get; init; }
        public string? Owner { get; init; }
        public string? Deprecated { get; init; }
        public string? Description { get; init; }

        public string? SystemContextView { get; init; }
        public string? ContainerView { get; init; }
        public string? ComponentView { get; init; }

        internal bool IsPersonRow =>
            !string.IsNullOrWhiteSpace(PersonKey) &&
            string.IsNullOrWhiteSpace(SoftwareSystemKey) &&
            string.IsNullOrWhiteSpace(ContainerKey) &&
            string.IsNullOrWhiteSpace(ComponentKey) &&
            !string.IsNullOrWhiteSpace(Name);

        internal bool IsSoftwareSystemRow =>
            string.IsNullOrWhiteSpace(PersonKey) &&
            !string.IsNullOrWhiteSpace(SoftwareSystemKey) &&
            string.IsNullOrWhiteSpace(ContainerKey) &&
            string.IsNullOrWhiteSpace(ComponentKey) &&
            !string.IsNullOrWhiteSpace(Name);

        internal bool IsContainerRow =>
            string.IsNullOrWhiteSpace(PersonKey) &&
            !string.IsNullOrWhiteSpace(SoftwareSystemKey) &&
            !string.IsNullOrWhiteSpace(ContainerKey) &&
            string.IsNullOrWhiteSpace(ComponentKey) &&
            !string.IsNullOrWhiteSpace(Name);

        internal bool IsComponentRow =>
            string.IsNullOrWhiteSpace(PersonKey) &&
            !string.IsNullOrWhiteSpace(SoftwareSystemKey) &&
            !string.IsNullOrWhiteSpace(ContainerKey) &&
            !string.IsNullOrWhiteSpace(ComponentKey) &&
            !string.IsNullOrWhiteSpace(Name);

        //internal bool IsValidPersonRow =>
        //    !string.IsNullOrWhiteSpace(PersonKey) &&
        //    string.IsNullOrWhiteSpace(SoftwareSystemKey) &&
        //    string.IsNullOrWhiteSpace(ContainerKey) &&
        //    string.IsNullOrWhiteSpace(ComponentKey) &&
        //    string.IsNullOrWhiteSpace(Technology) &&
        //    string.IsNullOrWhiteSpace(Owner) &&
        //    string.IsNullOrWhiteSpace(Deprecated) &&
        //    string.IsNullOrWhiteSpace(Description);

        //internal bool IsValidSoftwareSystemRow =>
        //    string.IsNullOrWhiteSpace(PersonKey) &&
        //    !string.IsNullOrWhiteSpace(SoftwareSystemKey) &&
        //    string.IsNullOrWhiteSpace(ContainerKey) &&
        //    string.IsNullOrWhiteSpace(ComponentKey) &&
        //    string.IsNullOrWhiteSpace(Technology) &&
        //    string.IsNullOrWhiteSpace(Owner) &&
        //    string.IsNullOrWhiteSpace(Deprecated) &&
        //    string.IsNullOrWhiteSpace(Description);

        //internal bool IsValidContainerRow =>
        //    string.IsNullOrWhiteSpace(PersonKey) &&
        //    !string.IsNullOrWhiteSpace(SoftwareSystemKey) &&
        //    !string.IsNullOrWhiteSpace(ContainerKey) &&
        //    string.IsNullOrWhiteSpace(ComponentKey) &&
        //    string.IsNullOrWhiteSpace(Technology) &&
        //    string.IsNullOrWhiteSpace(Owner) &&
        //    string.IsNullOrWhiteSpace(Deprecated) &&
        //    string.IsNullOrWhiteSpace(Description);

        //internal bool IsValidComponentRow =>
        //    string.IsNullOrWhiteSpace(PersonKey) &&
        //    !string.IsNullOrWhiteSpace(SoftwareSystemKey) &&
        //    !string.IsNullOrWhiteSpace(ContainerKey) &&
        //    !string.IsNullOrWhiteSpace(ComponentKey) &&
        //    string.IsNullOrWhiteSpace(Technology) &&
        //    string.IsNullOrWhiteSpace(Owner) &&
        //    string.IsNullOrWhiteSpace(Deprecated) &&
        //    string.IsNullOrWhiteSpace(Description);
    }
}