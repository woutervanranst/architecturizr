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

    public override string ToString() => $"{this.GetType().Name}-{Key}";
}

internal abstract class Node<T> : Node where T : Structurizr.StaticStructureElement
{
    protected Node(string key) : base(key)
    {
    }

    public T StructurizrObject { get; set; }
}

internal class Person : Node<Structurizr.Person>
{
    public Person(string key) : base(key) { }
}

internal class SoftwareSystem : Node<Structurizr.SoftwareSystem>
{
    public SoftwareSystem(string key) : base(key)
    {
    }

    public List<Container> Children { get; } = new List<Container>();
}

internal class Container : Node<Structurizr.Container>
{
    public Container(SoftwareSystem parent, string key) : base(key)
    {
        parent.Children.Add(this);
    }

    public string Technology { get; init; }

    public List<Component> Children { get; } = new List<Component>();
}

internal class Component : Node<Structurizr.Component>
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
        this.From = from;
        this.To = to;
    }

    public Node From { get; }
    public Node To { get; } //todo remove init from all others
}
