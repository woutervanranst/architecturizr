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

    public (string title, string description, IDictionary<string, Node>) Parse(FileInfo excel)
    {
        var exceptionList = new List<Exception>();

        // Parse General Tab
        using var fs1 = excel.OpenRead();
        var generalRows = fs1.ExcelToEnumerable<GeneralRow>(
            x => x
                .UsingSheet("General")
                .UsingHeaderNames(false) //map using column numbers, not names
                .OutputExceptionsTo(exceptionList)
            ).ToArray();

        if (exceptionList.Any())
            throw new Exception();

        var title = generalRows.Single(r => r.Key == "Title").Value;
        var description = generalRows.Single(r => r.Key == "Description").Value;


        // Parse Nodes Tab
        using var fs2 = excel.OpenRead();
        var nodeRows = fs2.ExcelToEnumerable<NodeRow>(
            x => x
                .UsingSheet("Nodes")
                .OutputExceptionsTo(exceptionList)

                .IgnoreColumsWithoutMatchingProperties()

                .Property(x => x.Row).MapsToRowNumber()
            ).ToArray();

        if (exceptionList.Any())
            throw new Exception();

        var nodes = ParseNodes(nodeRows);

        //Persons = nodes.Values.OfType<Person>().ToArray();
        //SoftwareSystems = nodes.Values.OfType<SoftwareSystem>().ToArray();
        //Containers = nodes.Values.OfType<Container>().ToArray();
        //Components = nodes.Values.OfType<Component>().ToArray();




        return (title, description, nodes);
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
    /// <param name="nodeRows"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private IDictionary<string, Node> ParseNodes(IEnumerable<NodeRow> nodeRows)
    {
        // Checks:
        // 1. Use Dictionary to avoid duplicate keys
        // 2. Use null checks in the row type matcher to avoid matches to multiple row types

        var nodes = new Dictionary<string, Node>();

        foreach (var row in nodeRows)
        {
            Node? n = null;

            if (row.IsPersonRow)
            {
                // Person
                n = new Person(row.PersonKey)
                {
                    Name = row.Name,
                    Description = row.Description
                };
            }
            if (row.IsSoftwareSystemRow)
            {
                if (n != null)
                    throw new InvalidOperationException($"Row #{row.Row} matches multiple types");
                
                // Software System
                n = new SoftwareSystem(row.SoftwareSystemKey)
                {
                    Name = row.Name,
                    Description = row.Description,
                    Owner = row.Owner
                };
            }
            if (row.IsContainerRow)
            {
                if (n != null)
                    throw new InvalidOperationException($"Row #{row.Row} matches multiple types");

                // Container
                var parent = nodes.ContainsKey(row.SoftwareSystemKey) ?
                    (SoftwareSystem)nodes[row.SoftwareSystemKey] :
                    throw new InvalidOperationException($"Parent '{row.SoftwareSystemKey}' of row #{row.Row} is not defined.");

                n = new Container(parent, row.ContainerKey)
                {
                    Name = row.Name,
                    Description = row.Description,
                    Technology = row.Technology,
                    Owner = row.Owner
                };
            }
            if (row.IsComponentRow)
            {
                if (n != null)
                    throw new InvalidOperationException($"Row #{row.Row} matches multiple types");

                // Component
                var parent = nodes.ContainsKey(row.ContainerKey) ?
                    (Container)nodes[row.ContainerKey] :
                    throw new InvalidOperationException($"Parent '{row.ContainerKey}' of row #{row.Row} is not defined.");

                n = new Component(parent, row.ComponentKey)
                {
                    Name = row.Name,
                    Description = row.Description,
                    Technology = row.Technology,
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
    }

    private IEnumerable<string> ParseViews(Person o, NodeRow r)
    {
        if (!string.IsNullOrWhiteSpace(r.SystemContextView))
            throw new NotImplementedException();
        if (!string.IsNullOrWhiteSpace(r.ContainerView))
            throw new NotImplementedException();
        if (!string.IsNullOrWhiteSpace(r.ComponentView))
            throw new NotImplementedException();

        return Array.Empty<string>();
    }
    private IEnumerable<string> ParseViews(SoftwareSystem o, NodeRow r)
    {
        if (!string.IsNullOrWhiteSpace(r.SystemContextView))
            yield return Views.SystemContextView;
        if (!string.IsNullOrWhiteSpace(r.ContainerView))
            yield return Views.ContainerView;
            //throw new InvalidOperationException($"{o.GetType().Name} '{o.Key}' cannot have a ContainerView");
        if (!string.IsNullOrWhiteSpace(r.ComponentView))
            throw new InvalidOperationException($"{o.GetType().Name} '{o.Key}' cannot have a ComponentView");
    }
    private IEnumerable<string> ParseViews(Container c, NodeRow r)
    {
        if (!string.IsNullOrWhiteSpace(r.SystemContextView))
            yield return Views.SystemContextView;
        if (!string.IsNullOrWhiteSpace(r.ContainerView))
            yield return Views.ContainerView;
        if (!string.IsNullOrWhiteSpace(r.ComponentView)) //yes
            yield return Views.ComponentView;

    }
    private IEnumerable<string> ParseViews(Component c, NodeRow r)
    {
        if (!string.IsNullOrWhiteSpace(r.SystemContextView))
            yield return Views.SystemContextView;
        if (!string.IsNullOrWhiteSpace(r.ContainerView))
            yield return Views.ContainerView;
        if (!string.IsNullOrWhiteSpace(r.ComponentView)) //yes
            yield return Views.ComponentView;
    }

    private class GeneralRow
    {
        public string Key { get; init; }
        public string Value { get; init; }
    }

    private class NodeRow
    {
        public int Row { get; init; }
        public string PersonKey { get; init; }
        public string SoftwareSystemKey { get; init; }
        public string ContainerKey { get; init; }
        public string ComponentKey { get; init; }

        public string Name { get; init; }
        public string Technology { get; init; }
        public string Owner { get; init; }
        public string Deprecated { get; init; }
        public string Description { get; init; }

        public string SystemContextView { get; init; }
        public string ContainerView { get; init; }
        public string ComponentView { get; init; }

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