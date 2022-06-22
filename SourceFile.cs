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

    private IEnumerable<Node> Parse(IEnumerable<NodeRow> nodeRows)
    {
        var nodes = new Dictionary<string, Node>();

        foreach (var row in nodeRows)
        {
            if (row.IsPersonRow)
            {
                // Person
                var p = new Person(row.PersonKey);

                nodes.Add(p.Key, p);
            }
            else if (row.IsSoftwareSystemRow)
            {
                // Software System
                var ss = new SoftwareSystem(row.SoftwareSystemKey)
                {
                    Description = row.Description
                };

                nodes.Add(ss.Key, ss);
            }
            else if (row.IsContainerRow)
            {
                // Container
                var parent = (SoftwareSystem)nodes[row.SoftwareSystemKey];
                var c = new Container(parent, row.ContainerKey)
                {
                    Description = row.Description,
                    Technology = row.Technology
                };

                nodes.Add(c.Key, c);
            }
            else if (row.IsComponentRow)
            {
                // Component
                var parent = (Container)nodes[row.ContainerKey];
                var c = new Component(parent, row.ComponentKey)
                {
                    Description = row.Description,
                    Technology = row.Technology
                };

                nodes.Add(c.Key, c);
            }
            else
            {
                throw new InvalidOperationException($"Row #{row.Row} did not match any Node type");
            }
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
}

internal class Container : Node
{
    public Container(SoftwareSystem parent, string key) : base(key)
    {
    }

    public string Technology { get; init; }

    public string Description { get; init; }
}

internal class Component : Node
{
    public Component(Container parent, string key) : base(key)
    {
    }

    public string Technology { get; init; }

    public string Description { get; init; }
}

