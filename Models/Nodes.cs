﻿namespace architecturizr.Models;

internal abstract class Node
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; }
    public string? Technology { get; init; }
    public string? Tags { get; init; }
    public string? Owner { get; init; }
    public bool Deprecated { get; init; }

    public List<string> Views { get; } = new();

    public abstract Structurizr.StaticStructureElement GetStructurizrObject();

    public override string ToString() => $"{this.GetType().Name}-{Key}";
}


//internal abstract class Node
//{
//    protected Node(string key)
//    {
//        this.Key = key;
//    }

//    public string Key { get; init; }

//    public string Name { get; init; }

//    public string Description { get; init; }

//    public string Owner { get; init; }

//    public override string ToString() => $"{this.GetType().Name}-{Key}";
//}



//internal abstract class Node<T> : Node where T :Structurizr.StaticStructureElement
//{
//    protected Node(string key) : base(key)
//    {
//    }

//    public T GetStructurizrObject() => _structurizrObject;
//    public void SetStructurizrObject(T o) => _structurizrObject = o;
//    private T _structurizrObject;
//}

internal class Person : Node
{
    public override Structurizr.Person GetStructurizrObject() => structurizrObject;
    public Structurizr.Person SetStructurizrObject(Structurizr.Person o) => structurizrObject = o;
    private Structurizr.Person structurizrObject;
}

internal class SoftwareSystem : Node
{
    public override Structurizr.SoftwareSystem GetStructurizrObject() => structurizrObject;
    public Structurizr.SoftwareSystem SetStructurizrObject(Structurizr.SoftwareSystem o) => structurizrObject = o;
    private Structurizr.SoftwareSystem structurizrObject;

    public List<Container> Children { get; } = new List<Container>();
}

internal class Container : Node
{
    public override Structurizr.Container GetStructurizrObject() => structurizrObject;
    public Structurizr.Container SetStructurizrObject(Structurizr.Container o) => structurizrObject = o;
    private Structurizr.Container structurizrObject;

    public required SoftwareSystem Parent
    {
        get => parent;
        init
        {
            parent = value;
            parent.Children.Add(this);
        }
    }
    private readonly SoftwareSystem parent;


    public List<Component> Children { get; } = new List<Component>();
}

internal class Component : Node
{
    public override Structurizr.Component GetStructurizrObject() => structurizrObject;
    public Structurizr.Component SetStructurizrObject(Structurizr.Component o) => structurizrObject = o;
    private Structurizr.Component structurizrObject;

    public required Container Parent
    {
        get => parent;
        init
        {
            parent = value;
            parent.Children.Add(this);
        }
    }
    private readonly Container parent;

}