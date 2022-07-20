namespace architecturizr.Models;

internal abstract class Node
{
    protected Node(string key)
    {
        this.Key = key;
    }

    public string Key { get; init; }

    public string Name { get; init; }

    public string Description { get; init; }

    public string Owner { get; init; }

    // public virtual Structurizr.StaticStructureElement StructurizrObject { get; set; }

    public override string ToString() => $"{this.GetType().Name}-{Key}";
}

internal class Person : Node
{
    public Person(string key) : base(key) { }

    public Structurizr.Person StructurizrObject { get; set; }
}

internal class SoftwareSystem : Node
{
    public SoftwareSystem(string key) : base(key)
    {
    }

    public List<Container> Children { get; } = new List<Container>();

    public Structurizr.SoftwareSystem StructurizrObject { get; set; }
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

    public Structurizr.Container StructurizrObject { get; set; }
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

    public Structurizr.Component StructurizrObject { get; set; }
}