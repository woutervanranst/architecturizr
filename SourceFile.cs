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


        // Parse Nodes Tab
        var nodes = fileName.ExcelToEnumerable<NodeRow>(
            x => x
                .UsingSheet("Nodes")
                .OutputExceptionsTo(exceptionList)
                //.UsingHeaderNames(false) //map using column numbers, not names
                .StartingFromRow(2) //data as of row 3
                .Property(x => x.Row).MapsToRowNumber()
            ).ToArray();

        if (exceptionList.Any())
            throw new Exception();

        var bla = Parse(nodes).ToArray();

        // Parse Persons

        // Parse Software Systems
        // SoftwareSystems = ParseSoftwareSystems(nodes).ToArray();
        SoftwareSystems = bla.OfType<SoftwareSystem>().ToArray();


    }

    public string Title { get; init; }

    public IEnumerable<SoftwareSystem> SoftwareSystems { get; init; }

    /// <summary>
    /// Parse every row in the source to exactly one node type
    /// </summary>
    /// <param name="nodeRows"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private IEnumerable<Node> Parse(IEnumerable<NodeRow> nodeRows)
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
                n = new Person(row.PersonKey);
            }
            if (row.IsSoftwareSystemRow)
            {
                if (n != null)
                    throw new InvalidOperationException($"Row #{row.Row} matches multiple types");

                // Software System
                n = new SoftwareSystem(row.SoftwareSystemKey)
                {
                    Description = row.Description
                };
            }
            if (row.IsContainerRow)
            {
                if (n != null)
                    throw new InvalidOperationException($"Row #{row.Row} matches multiple types");

                // Container
                var parent = (SoftwareSystem)nodes[row.SoftwareSystemKey];
                n = new Container(parent, row.ContainerKey)
                {
                    Description = row.Description,
                    Technology = row.Technology
                };
            }
            if (row.IsComponentRow)
            {
                if (n != null)
                    throw new InvalidOperationException($"Row #{row.Row} matches multiple types");

                // Component
                var parent = (Container)nodes[row.ContainerKey];
                n = new Component(parent, row.ComponentKey)
                {
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

        return nodes.Values;
    }


    //private IEnumerable<SoftwareSystemNode> ParseSoftwareSystems(IEnumerable<NodeRow> nodes)
    //{
    //    var sss = nodes
    //        .Where(n => !string.IsNullOrWhiteSpace(n.SoftwareSystemKey))
    //        .Select(n => n.SoftwareSystemKey).Distinct();

    //    foreach (var ss in sss)
    //    {
    //        var r = nodes.Where(n => n.SoftwareSystemKey == ss && !string.IsNullOrWhiteSpace(n.Description)).ToArray();

    //        if (r.Length == 0)
    //            throw new InvalidOperationException($"SoftwareSystem '{ss}' is not properly defined. Does not contain a Description");
    //        else if (r.Length > 1)
    //            throw new InvalidOperationException($"SoftwareSystem '{ss}' is not properly defined. Contains more than one Description.");

    //        yield return new SoftwareSystemNode(r.Single());
    //    }
    //}

    

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
        public string Technology { get; init; }
        public string Owner { get; init; }
        public string Deprecated { get; init; }
        public string Description { get; init; }

        internal bool IsPersonRow =>
            !string.IsNullOrWhiteSpace(PersonKey) &&
            string.IsNullOrWhiteSpace(SoftwareSystemKey) &&
            string.IsNullOrWhiteSpace(ContainerKey) &&
            string.IsNullOrWhiteSpace(ComponentKey);

        internal bool IsSoftwareSystemRow =>
            string.IsNullOrWhiteSpace(PersonKey) &&
            !string.IsNullOrWhiteSpace(SoftwareSystemKey) &&
            string.IsNullOrWhiteSpace(ContainerKey) &&
            string.IsNullOrWhiteSpace(ComponentKey);

        internal bool IsContainerRow =>
            string.IsNullOrWhiteSpace(PersonKey) &&
            !string.IsNullOrWhiteSpace(SoftwareSystemKey) &&
            !string.IsNullOrWhiteSpace(ContainerKey) &&
            string.IsNullOrWhiteSpace(ComponentKey);

        internal bool IsComponentRow =>
            string.IsNullOrWhiteSpace(PersonKey) &&
            !string.IsNullOrWhiteSpace(SoftwareSystemKey) &&
            !string.IsNullOrWhiteSpace(ContainerKey) &&
            !string.IsNullOrWhiteSpace(ComponentKey);

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

internal class Node
{
    protected Node(string key)
    {
        this.Key = key;
    }

    public string Key { get; init; }

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

    public string Description { get; init; }

    public List<Container> Children { get; } = new List<Container>();
}

internal class Container : Node
{
    public Container(SoftwareSystem parent, string key) : base(key)
    {
        parent.Children.Add(this);
    }

    public string Technology { get; init; }

    public string Description { get; init; }

    public List<Component> Children { get; } = new List<Component>();
}

internal class Component : Node
{
    public Component(Container parent, string key) : base(key)
    {
        parent.Children.Add(this);
    }

    public string Technology { get; init; }

    public string Description { get; init; }
}

