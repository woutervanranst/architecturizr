using ExcelToEnumerable;

namespace architecturizr;

/// <summary>
/// Read the Excel Source file
/// https://stackoverflow.com/a/15793495
/// </summary>
internal class SourceFile
{
    public SourceFile(string fileName)
    {
        var exceptionList = new List<Exception>();

        // Parse General Tab
        var generalRows = fileName.ExcelToEnumerable<GeneralRow>(
            x => x
                .UsingSheet("General")
                .UsingHeaderNames(false) //map using column numbers, not names
                .OutputExceptionsTo(exceptionList)
            );

        if (exceptionList.Any())
            throw new Exception();

        Title = generalRows.Single(r => r.Key == "Title").Value;
        Description = generalRows.Single(r => r.Key == "Description").Value;


        // Parse Nodes Tab
        var nodeRows = fileName.ExcelToEnumerable<NodeRow>(
            x => x
                .UsingSheet("Nodes")
                .OutputExceptionsTo(exceptionList)
                .Property(x => x.Row).MapsToRowNumber()
            ).ToArray();

        if (exceptionList.Any())
            throw new Exception();

        var nodes = ParseNodes(nodeRows);

        Persons = nodes.Values.OfType<Person>().ToArray();
        SoftwareSystems = nodes.Values.OfType<SoftwareSystem>().ToArray();
        Containers = nodes.Values.OfType<Container>().ToArray();
        Components = nodes.Values.OfType<Component>().ToArray();


        // Parse Edges Tab
        var edgeRows = fileName.ExcelToEnumerable<EdgeRow>(
            x => x.
                UsingSheet("Edges")
                .HeaderOnRow(2)
                .StartingFromRow(3)
                .OutputExceptionsTo(exceptionList)
                .Property(x => x.Row).MapsToRowNumber()
            );

        if (exceptionList.Any())
            throw new Exception();

        var edges = ParseEdges(nodes, edgeRows);
    }

    public string Title { get; init; }
    public string Description { get; init; }

    public IEnumerable<Person> Persons { get; init; }
    public IEnumerable<SoftwareSystem> SoftwareSystems { get; init; }
    public IEnumerable<Container> Containers { get; init; }
    public IEnumerable<Component> Components { get; init; }

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
                    Description = row.Description
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
                    Technology = row.Technology
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
                    Technology = row.Technology
                };
            }

            if (n == null)
                throw new InvalidOperationException($"Row #{row.Row} did not match any Node type");
            else if (nodes.ContainsKey(n.Key))
                throw new InvalidOperationException($"Duplicate key '{n.Key}' in row #{row.Row}");
            else
                nodes.Add(n.Key, n);
        }

        return nodes;
    }

    private IEnumerable<Edge> ParseEdges(IDictionary<string, Node> nodes, IEnumerable<EdgeRow> edgeRows)
    {
        var edges = new List<Edge>();

        foreach (var row in edgeRows)
        {
            var fromNode = nodes[row.From];
            var toNode = nodes[row.To];

            edges.Add(new Edge(fromNode, toNode));
        }

        return edges;
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

    private class EdgeRow
    {
        public int Row { get; init; }
        public string From { get; init; }
        public string To { get; init; }
        public string A { get; init; }
        public string B { get; init; }
        public string E { get; init; }
        public string F { get; init; }
    }
}

internal class Node
{
    protected Node(string key)
    {
        this.Key = key;
    }

    public string Key { get; init; }

    public string Name { get; init; }

    public string Description { get; init; }

    public override string ToString() => $"{this.GetType().Name}-{Key}";
}

internal class Person : Node
{
    public Person(string key) : base(key) { }
}

internal class SoftwareSystem : Node
{
    public SoftwareSystem(string key) : base(key)
    {
    }

    public List<Container> Children { get; } = new List<Container>();
}

internal class Container : Node
{
    public Container(SoftwareSystem parent, string key) : base(key)
    {
        parent.Children.Add(this);
    }

    public string Technology { get; init; }

    public List<Component> Children { get; } = new List<Component>();
}

internal class Component : Node
{
    public Component(Container parent, string key) : base(key)
    {
        parent.Children.Add(this);
    }

    public string Technology { get; init; }
}

internal class Edge
{
    public Edge(Node from, Node to)
    {
        this.from = from;
        this.to = to;
    }

    private readonly Node from;
    private readonly Node to;
}

