using architecturizr.Models;
using architecturizr.Utils;
using Microsoft.Extensions.Logging;
using Structurizr.Api;

namespace architecturizr.OutputParser;

internal class StructurizrBuilder
{
    private readonly ILogger logger;

    public StructurizrBuilder(ILogger<StructurizrBuilder> logger, string title, string description, IEnumerable<Node> nodes, IEnumerable<Process> processes, long workspaceId, string apiKey, string apiSecret)
    {
        this.logger = logger;

        // Configure Workspace
        var workspace = new Structurizr.Workspace(title, description);
        var model = workspace.Model;

        model.ImpliedRelationshipsStrategy = new Structurizr.CreateImpliedRelationshipsUnlessSameRelationshipExistsStrategy(); //.CreateImpliedRelationshipsUnlessAnyRelationshipExistsStrategy(); // ! IMPORTANT, see https://github.com/structurizr/dotnet/issues/97


        // Add Nodes
        nodes = GetNodesInUse(processes).Distinct().ToArray();
        AddNodes(nodes, model);

        // Add Edges
        AddEdges(processes);

        // Add Views
        var viewSet = workspace.Views;
        viewSet.Configuration.ViewSortOrder = Structurizr.ViewSortOrder.Type;

        foreach (var ss in nodes.OfType<SoftwareSystem>())
        {
            var v = viewSet.CreateSystemContextView(ss.GetStructurizrObject(), ss.Key, $"Helicopter view of '{ss.Name}'");
            v.Title = $"[(1) System Context] {ss.Name}";

            v.AddAllElements();
            v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 300, 300, 300, true);
        }

        foreach (var ss in nodes.OfType<SoftwareSystem>())
        {
            var v = viewSet.CreateContainerView(ss.GetStructurizrObject(), "c" + ss.Key, $"Interactions with the insides of '{ss.Name}'");
            v.Title = $"[(2) Container] {ss.Name}";

            v.AddAllElements();
            v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 300, 300, 300, true);
            // v.PaperSize = Structurizr.PaperSize.A0_Landscape;
        }

        foreach (var c in nodes.OfType<Container>())
        {
            if (c.Children.Count == 0) // if this Container does not have any children (Components), the diagram will not show anything useful
                continue;

            var v = viewSet.CreateComponentView(c.GetStructurizrObject(), c.Key, $"What is inside/interacts with {c.Name}");

            v.Title = $"[(3) Component ALL] {c.Name}";

            v.AddAllElements();
            v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 300, 300, 300, false);
            // v.PaperSize = Structurizr.PaperSize.A0_Landscape;
        }

        foreach (var c in nodes.OfType<Component>())
        {
            var v = viewSet.CreateComponentView(c.Parent.GetStructurizrObject(), "component-" + c.Key, $"What interacts with {c.Name}");
            v.Title = $"[(3) Component DIRECT] {c.Name}";

            v.Add(c.GetStructurizrObject());
            v.AddNearestNeighbours(c.GetStructurizrObject());

            v.EnableAutomaticLayout();
        }

        foreach (var p in processes)
        {
            //var c = ((SoftwareSystem)nodes.Where(n => n.Key == "ivs-be").Single()).StructurizrObject;
            var c = ((Container)nodes.Where(n => n.Key == "k8s").Single()).GetStructurizrObject();
            var v = viewSet.CreateDynamicView(c, $"process-{p.Name.ToKebabCase()}", p.Name);
            v.Title = p.Name;

            foreach (var s in p.Steps)
            {
                string d;
                Structurizr.InteractionStyle interactionStyle;
                if (s is AsyncStep ss)
                {
                    d = $"{s.Description}\n[{ss.Topic}]";
                    interactionStyle = Structurizr.InteractionStyle.Asynchronous;
                }
                else if (s is SyncStep)
                {
                    d = s.Description;
                    interactionStyle = Structurizr.InteractionStyle.Synchronous;

                    if (string.IsNullOrWhiteSpace(s.Description))
                        logger.LogWarning($"Description is empty - may show erroneously on diagram");
                }
                else
                    throw new Exception();

                // NOTE: v.Add() Only adds the first relationship to the diagram.
                // Element.GetEfferentRelationshipWith only yields the first relationship.
                var r = v.Add(s.From.GetStructurizrObject(), d, s.To.GetStructurizrObject());
                r.Relationship.InteractionStyle = interactionStyle;

                //foreach (var r in s.From.GetStructurizrObject().Relationships)
                //   if (s.To.GetStructurizrObject() == r.Destination)
                //        v.Add(r, Structurizr.InteractionStyle.Asynchronous);

            }


            v.EnableAutomaticLayout(Structurizr.RankDirection.LeftRight, 200, 200, 200, false);
        }


        /* Microservice
         * 
         *  https://structurizr.com/share/4241/diagrams#Containers
         *  https://github.com/structurizr/dsl/tree/master/docs/cookbook/workspace-extension
         *  https://structurizr.com/help/usage-recommendations
         */

        // Sequence Diagrams? https://github.com/structurizr/java/pull/129/files#diff-b55fd8523c23d8ff04163446b3ffc28e4f93238660847d4394926df9398f7a53

        // https://github.com/structurizr/dotnet-core-quickstart/blob/master/structurizr/Program.cs

        // https://structurizr.com/help/themess
        // viewSet.Configuration.Theme = "default";
        var styles = viewSet.Configuration.Styles;
        styles.Add(new Structurizr.ElementStyle(Structurizr.Tags.SoftwareSystem) { Background = "#1168bd", Color = "#ffffff" });
        styles.Add(new Structurizr.ElementStyle(Structurizr.Tags.Container) { Background = "#1168bd", Color = "#ffffff" });
        styles.Add(new Structurizr.ElementStyle(Structurizr.Tags.Person) { Background = "#08427b", Color = "#ffffff", Shape = Structurizr.Shape.Person });

        styles.Add(new Structurizr.ElementStyle(Tags.Python) { Icon = Icons.pythonPng });
        styles.Add(new Structurizr.ElementStyle(Tags.Scala) { Icon = Icons.scalaPng });

        styles.Add(new Structurizr.ElementStyle("IVS") { Background = "#e7285d" });

        styles.Add(new Structurizr.RelationshipStyle(Structurizr.Tags.Synchronous) { Dashed = false });
        styles.Add(new Structurizr.RelationshipStyle(Structurizr.Tags.Asynchronous) { Dashed = true });

        var structurizrClient = new StructurizrClient(apiKey, apiSecret);
        structurizrClient.PutWorkspace(workspaceId, workspace);
    }



    

    private void AddEdges(IEnumerable<Process> processes)
    {
        // Group all interactions between two nodes and the interaction type
        var edges = processes
            .SelectMany(p => p.Steps.Select(s => new { s.From, s.To, Step = s, Process = p }))
            .GroupBy(e => (e.From, e.To, StepType: e.Step.GetType()));

        foreach (var edge in edges)
        {
            var from = edge.Key.From.GetStructurizrObject();
            dynamic to = edge.Key.To.GetStructurizrObject();
            var d = string.Join('\n', edge.Select(e => e.Process.Name).Distinct());
            var i = GetInteractionStyle(edge.Key.StepType);

            Structurizr.Relationship r = from.Uses(to, d, "", i);

        }
    }

    private static IEnumerable<Node> GetNodesInUse(IEnumerable<Process> ps)
    {
        foreach (var n in ps.SelectMany(p => p.Steps).SelectMany(s => new Node[] { s.From, s.To }))
        {
            yield return n;

            if (n is Person)
            {
                // No Parent
            }
            else if (n is SoftwareSystem)
            {
                // No Parent
            }
            else if (n is Container cont)
            {
                yield return cont.Parent;
            }
            else if (n is Component comp)
            {
                yield return comp.Parent;
                yield return comp.Parent.Parent;
            }
            else
                throw new Exception();
        }
    }

    private static void AddNodes(IEnumerable<Node> nodes, Structurizr.Model model)
    {
        // Only the nodes in use
        foreach (var person in nodes.OfType<Person>())
            person.SetStructurizrObject(model.AddPerson(person.Name, person.Description));

        foreach (var ss in nodes.OfType<SoftwareSystem>())
            ss.SetStructurizrObject(model.AddSoftwareSystem(ss.Name, ss.Description));

        foreach (var cont in nodes.OfType<Container>())
            cont.SetStructurizrObject(cont.Parent.GetStructurizrObject().AddContainer(cont.Name, cont.Description, cont.Technology));

        foreach (var comp in nodes.OfType<Component>())
        {
            var c = comp.Parent.GetStructurizrObject().AddComponent(comp.Name, comp.Description, comp.Technology);
            comp.SetStructurizrObject(c);

            c.AddTags(c.Technology);
            //if (!string.IsNullOrWhiteSpace(comp.Owner))
            //    c.AddTags("IVS");
        }

        foreach (var n in nodes)
        {
            if (!string.IsNullOrWhiteSpace(n.Owner))
                n.GetStructurizrObject().AddTags("IVS");
                //((dynamic)n).GetStructurizrObject().AddTags("IVS");
        }

        // THIS IS THE OLD CODE THAT ADDS ALL COMPONENTS REGARDLESS OF BEING USED
        //foreach (var softwareSystem in nodes.OfType<SoftwareSystem>())
        //{
        //    softwareSystem.StructurizrObject = model.AddSoftwareSystem(softwareSystem.Name, softwareSystem.Description);

        //    foreach (var container in softwareSystem.Children)
        //    {
        //        container.StructurizrObject = softwareSystem.StructurizrObject.AddContainer(container.Name, container.Description, container.Technology);

        //        foreach (var component in container.Children)
        //        {
        //            var c = container.StructurizrObject.AddComponent(component.Name, component.Description, component.Technology);

        //            c.AddTags(c.Technology);
        //            if (!string.IsNullOrWhiteSpace(component.Owner))
        //                c.AddTags("IVS");

        //            component.StructurizrObject = c;
        //        }
        //    }
        //}
    }

    private static Structurizr.InteractionStyle GetInteractionStyle(Step s)
    {
        if (s is AsyncStep)
            return Structurizr.InteractionStyle.Asynchronous;
        else if (s is SyncStep)
            return Structurizr.InteractionStyle.Synchronous;
        else
            throw new Exception();
    }
    private static Structurizr.InteractionStyle GetInteractionStyle(Type t)
    {
        if (t == typeof(AsyncStep))
            return Structurizr.InteractionStyle.Asynchronous;
        else if (t == typeof(SyncStep))
            return Structurizr.InteractionStyle.Synchronous;
        else
            throw new Exception();
    }
}
