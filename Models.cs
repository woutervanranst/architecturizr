namespace architecturizr;

internal abstract class Node
{
    protected Node(string key)
    {
        this.Key = key;
    }

    public string Key { get; init; }

    public string Name { get; init; }

    public string Description { get; init; }

    public Structurizr.StaticStructureElement StructurizrObject { get; set; }

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
        Parent = parent;
        parent.Children.Add(this);
    }

    public string Technology { get; init; }

    public SoftwareSystem Parent { get; }

    public List<Component> Children { get; } = new List<Component>();
}

internal class Component : Node
{
    public Component(Container parent, string key) : base(key)
    {
        Parent = parent;
        parent.Children.Add(this);
    }

    public Container Parent { get; }

    public string Technology { get; init; }
}

internal class Edge
{
    public Edge(Node from, Node to)
    {
        this.From = from;
        this.To = to;
    }

    public Node From { get; }
    public Node To { get; } //todo remove init from all others
}
