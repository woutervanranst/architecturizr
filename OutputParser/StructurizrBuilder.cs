using System.Collections;
using System.Drawing;
using System.Globalization;
using architecturizr.Models;
using architecturizr.Utils;
using Microsoft.Extensions.Logging;
using Structurizr.Api;

namespace architecturizr.OutputParser;

internal class StructurizrBuilder
{
    public StructurizrBuilder(ILogger<StructurizrBuilder> logger, string title, string description, 
        IEnumerable<Process> processes, long workspaceId, string apiKey, string apiSecret)
    {
        client = new StructurizrClient(apiKey, apiSecret);
        workspace = new Structurizr.Workspace(title, description);

        this.logger = logger;
        this.processes = processes;
        this.workspaceId = workspaceId;
    }

    private readonly StructurizrClient client;
    private readonly Structurizr.Workspace workspace;
    private readonly ILogger logger;
    private readonly IEnumerable<Process> processes;
    private readonly long workspaceId;

    public void CreateWorkspace()
    {
        /* Original source
         * https://github.com/structurizr/dotnet-core-quickstart/blob/master/structurizr/Program.cs
         *
         * Inspiration: Microservice
         * https://structurizr.com/share/4241/diagrams#Containers
         * https://github.com/structurizr/dsl/tree/master/docs/cookbook/workspace-extension
         * https://structurizr.com/help/usage-recommendations
         *
         * Sequence Diagrams?
         * https://github.com/structurizr/java/pull/129/files#diff-b55fd8523c23d8ff04163446b3ffc28e4f93238660847d4394926df9398f7a53
         */
        
        // Add Nodes
        var nodes = GetNodesInUse(processes).Distinct().ToArray();
        AddNodes(workspace.Model, nodes);

        // Add Edges
        AddEdges(processes);

        // Add Views
        AddViews(workspace.Views, nodes, processes);

        // Add Processes
        AddProcesses(logger, workspace.Views, nodes, processes);

        // Add Styles
        AddStyles(workspace.Views.Configuration);

        var (response, json) = client.PutWorkspace(workspaceId, workspace);
    }
    
    private static IEnumerable<Node> GetNodesInUse(IEnumerable<Process> ps)
    {
        foreach (var n in ps
                     .SelectMany(p => p.Steps)
                     .SelectMany(s => new Node[] { s.From, s.To }))
        {
            yield return n;

            switch (n)
            {
                case Person:
                    // No Parent
                    break;
                case SoftwareSystem:
                    // No Parent
                    break;
                case Container cont:
                    yield return cont.Parent;
                    break;
                case Component comp:
                    yield return comp.Parent;
                    yield return comp.Parent.Parent;
                    break;
                default:
                    throw new Exception();
            }
        }
    }

    private static void AddNodes(Structurizr.Model model, IEnumerable<Node> nodes)
    {
        // This will add only the nodes in use
        // The separate foreach loops are important as they create the parents first
        foreach (var person in nodes.OfType<Person>())
            person.SetStructurizrObject(model.AddPerson(person.Name, person.Description));

        foreach (var ss in nodes.OfType<SoftwareSystem>())
            ss.SetStructurizrObject(model.AddSoftwareSystem(ss.Name, ss.Description));

        foreach (var cont in nodes.OfType<Container>())
            cont.SetStructurizrObject(cont.Parent.GetStructurizrObject().AddContainer(cont.Name, cont.Description, cont.Technology));

        foreach (var comp in nodes.OfType<Component>())
            comp.SetStructurizrObject(comp.Parent.GetStructurizrObject().AddComponent(comp.Name, comp.Description, comp.Technology));

        foreach (var node in nodes)
        {
            if (!string.IsNullOrWhiteSpace(node.Technology))
                node.GetStructurizrObject().AddTags(node.Technology);

            if (!string.IsNullOrWhiteSpace(node.Tags))
                node.GetStructurizrObject().AddTags(node.Tags.Split(',').Select(t => t.Trim()).ToArray());

            if (!string.IsNullOrWhiteSpace(node.Owner))
                node.GetStructurizrObject().AddTags("IVS");
        }
    }

    private static void AddEdges(IEnumerable<Process> processes)
    {
        // Add all DIRECT interactions:
        //      Interactions that are explicitly mentioned in the processes
        var rs = new List<Structurizr.Relationship>();
        
        var directInteractions = processes
            .SelectMany(p => p.Steps.Select(s => new { s.From, s.To, Step = s, Process = p }))
            .GroupBy(e => (e.From, e.To, StepType: e.Step.GetType()));

        foreach (var edge in directInteractions)
        {
            var from = edge.Key.From.GetStructurizrObject();
            dynamic to = edge.Key.To.GetStructurizrObject();
            var d = string.Join('\n', edge.Select(e => e.Process.Name).Distinct());
            var i = GetInteractionStyle(edge.Key.StepType);

            Structurizr.Relationship r = from.Uses(to, d, "", i);
            rs.Add(r);
        }

        // Add all IMPLIED interactions, based on the previous direct interactions:
        //      Interactions that are implied between the parents of each direct interaction (eg. if 'fe-core' -> 'grpc-api' then there is a relationship between 'Frontend' and 'Kubernetes Cluster', etc)
        var impliedInteractions = rs.SelectMany(GetImpliedRelationships)
            .GroupBy(r => (r.Source, r.Destination, r.InteractionStyle));

        foreach (var interaction in impliedInteractions)
        {
            var source = (Structurizr.StaticStructureElement)interaction.Key.Source;
            dynamic to = interaction.Key.Destination;

            //if (source.Name == "InvestSuite Backend at Client" &&
            //    to.Name == "3rd Parties")
            //{
            //}

            var d = string.Join('\n', interaction.SelectMany(i => i.Description.Split('\n')).Distinct());
            var style = interaction.Key.InteractionStyle;

            source.Uses(to, d, "", style);
        }


        static Structurizr.InteractionStyle GetInteractionStyle(Type t)
        {
            if (t == typeof(AsyncStep))
                return Structurizr.InteractionStyle.Asynchronous;
            else if (t == typeof(SyncStep))
                return Structurizr.InteractionStyle.Synchronous;
            else
                throw new Exception();
        }
        
        IEnumerable<(Structurizr.Element Source, Structurizr.Element Destination, string Description, Structurizr.InteractionStyle? InteractionStyle)> GetImpliedRelationships(Structurizr.Relationship relationship)
        {
            // NOTE: This is a fork of Structurizr.CreateImpliedRelationshipsUnlessSameRelationshipExistsStrategy
            
            var source = relationship.Source;
            var destination = relationship.Destination;

            while (source != null)
            {
                while (destination != null)
                {
                    if (ImpliedRelationshipIsAllowed(source, destination))
                        if (!source.HasEfferentRelationshipWith(destination, relationship.Description))
                            yield return (source, destination, relationship.Description, relationship.InteractionStyle);

                    destination = destination.Parent;
                }

                destination = relationship.Destination;
                source = source.Parent;
            }

            bool ImpliedRelationshipIsAllowed(Structurizr.Element source, Structurizr.Element destination)
            {
                if (source.Equals(destination))
                {
                    return false;
                }

                return !(IsChildOf(source, destination) || IsChildOf(destination, source));

                bool IsChildOf(Structurizr.Element e1, Structurizr.Element e2)
                {
                    if (e1 is Structurizr.Person || e2 is Structurizr.Person)
                    {
                        return false;
                    }

                    var parent = e2.Parent;
                    while (parent != null)
                    {
                        if (parent.Id.Equals(e1.Id))
                        {
                            return true;
                        }

                        parent = parent.Parent;
                    }

                    return false;
                }
            }
        }
    }

    private static void AddViews(Structurizr.ViewSet viewSet, IEnumerable<Node> nodes, IEnumerable<Process> processes)
    {
        viewSet.Configuration.ViewSortOrder = Structurizr.ViewSortOrder.Type;

        // Add Landscape View
        var lsv = viewSet.CreateSystemLandscapeView("landscape", "Overview");
        lsv.AddDefaultElements();
        //lsv.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 300, 300, 300, true);

        // Add System Context Diagrams: https://structurizr.com/help/system-context-diagram
        foreach (var ss in nodes.Where(n => n.Views.Contains(Views.SystemContextView)).Cast<SoftwareSystem>())
        {
            var v = viewSet.CreateSystemContextView(ss.GetStructurizrObject(), $"sc-{ss.Key}" , $"Helicopter view of '{ss.Name}'");
            //v.Title = $"[(1) System Context] {ss.Name}";
            v.Title = $"Overview of {ss.Name}";

            //v.AddAllElements();
            v.AddDefaultElements();
            // v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 300, 300, 300, true);
        }

        // Add Container Diagrams: https://structurizr.com/help/container-diagram
        foreach (var ss in nodes.Where(n => n.Views.Contains(Views.ContainerView)).Cast<SoftwareSystem>())
        {
            var v = viewSet.CreateContainerView(ss.GetStructurizrObject(), $"cont-{ss.Key}", $"What is inside {ss.Name} and what do they interact with");
            //v.Title = $"[(2) Container] {ss.Name}";
            v.Title = $"Inside {ss.Name}";

            //v.AddAllElements();
            v.AddDefaultElements();
            // v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 300, 300, 300, true);
            // v.PaperSize = Structurizr.PaperSize.A0_Landscape;
        }

        // Add Component Diagrams: https://structurizr.com/help/component-diagram
        foreach (var cont in nodes.OfType<Container>()
                 .Where(n => n.Views.Contains(Views.ComponentView)))
        {
            var v = viewSet.CreateComponentView(cont.GetStructurizrObject(), $"comp1-{cont.Key}", $"What is inside {cont.Name} and what do they interact with");
            v.Title = $"Inside {cont.Name}";

            v.AddDefaultElements();
            //v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 300, 300, 300, true);
        }
        foreach (var comp in nodes.OfType<Component>()
                .Where(n => n.Views.Contains(Views.ComponentView))
                .Where(n => DirectRelationships(n, processes).Count() > 2))
        {
            var v = viewSet.CreateComponentView(comp.Parent.GetStructurizrObject(), $"comp2-{comp.Key}", $"What interacts with {comp.Name}");
            v.Title = $"What interacts with {comp.Name}";

            v.AddNearestNeighbours(comp.GetStructurizrObject());
            v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 300, 300, 300, true);

        }



        //foreach (var c in nodes.Where(n => n.Views.Contains(Views.ComponentView)))
        //{
        //    if (c is Container cont)
        //    {
        //        if (cont.Children.Count ==
        //            0) // if this Container does not have any children (Components), the diagram will not show anything useful
        //            continue;

        //        var v = viewSet.CreateComponentView(cont.GetStructurizrObject(), cont.Key,
        //            $"What is inside/interacts with {cont.Name}");

        //        v.Title = $"[(3) Component ALL] {cont.Name}";

        //        v.AddAllElements();
        //        v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 300, 300, 300, false);
        //    }
        //    else if (c is Component comp)
        //    {
        //        if (comp.Name == "grpc-api")
        //        {
        //        }

        //        var v = viewSet.CreateComponentView(comp.Parent.GetStructurizrObject(), "component-" + comp.Key,
        //            $"What interacts with {comp.Name}");
        //        v.Title = $"[(3) Component DIRECT] {comp.Name}";

        //        v.Add(comp.GetStructurizrObject());
        //        v.AddNearestNeighbours(comp.GetStructurizrObject());

        //        v.EnableAutomaticLayout();
        //    }
        //}

        static IEnumerable<Node> DirectRelationships(Node n, IEnumerable<Process> processes)
        {
            // This is a copypaste from the structurizr code
            foreach (var p in processes)
            {
                foreach (var s in p.Steps)
                {
                    if (s.From == n || s.To == n)
                        yield return n;
                }
            }
        }
    }

    

    private static void AddProcesses(ILogger logger, Structurizr.ViewSet viewSet, IEnumerable<Node> nodes, IEnumerable<Process> processes)
    {
        foreach (var p in processes)
        {
            if (p.Steps.Count <= 1)
            {
                logger.LogInformation($"Process '{p.FullName}' has only one step. Skipping.");
                continue;
            }
            
            //var x = p.Steps.Select(s => s.From).Concat(p.Steps.Select(s => s.To)).GroupBy(n => n.Key);
            //var y = x.OrderBy(z => z.Count()).Last();
            //var c = y.First().GetStructurizrObject().Parent as Structurizr.Container;
            var c = ((Container)nodes.Single(n => n.Key == "k8s")).GetStructurizrObject();
            //var c = p.Steps.Select(s => s.From).Concat(p.Steps.Select(s => s.To)).OfType<Container>().First().GetStructurizrObject();
            var v = viewSet.CreateDynamicView(c, $"process-{p.FullName.ToKebabCase()}", p.FullName);
            v.Title = p.FullName;

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
                        logger.LogWarning(
                            $"Process '{p.FullName}': Description of step '{s}' is empty - may show erroneously on diagram");
                }
                else
                    throw new Exception();

                // NOTE: v.Add() Only adds the first relationship to the diagram.
                // Element.GetEfferentRelationshipWith only yields the first relationship.
                var r = v.Add(s.From.GetStructurizrObject(), d, s.To.GetStructurizrObject());
                r.Relationship.InteractionStyle = interactionStyle;
            }

            //if (p.Steps.Count <= 10)
            //    v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 300, 300, 300, false);
            //else
            //    logger.LogInformation($"Process '{p.FullName}' has {p.Steps.Count} steps. Disabling automatic layout.");
        }
    }

    private static void AddStyles(Structurizr.ViewConfiguration config)
    {
        // Add Icons
        // https://emojipedia.org/snake/
        // https://github.com/structurizr/themes
        // https://ezgif.com/svg-to-png
        var icons = Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
        foreach (DictionaryEntry icon in icons)
            config.Styles.Add(new Structurizr.ElementStyle(icon.Key.ToString()) { Icon = GetPngBase64((byte[])icon.Value) });

        // Add Colors
        config.Styles.Add(new Structurizr.ElementStyle("IVS") { Background = "#e7285d", Color = HexConverter(Color.White) });
        config.Styles.Add(new Structurizr.ElementStyle("Client") { Background = HexConverter(Color.Blue), Color = HexConverter(Color.White) });
        config.Styles.Add(new Structurizr.ElementStyle("Customer") { Background = HexConverter(Color.SteelBlue), Color = HexConverter(Color.White) });


        // Add Shapes
        config.Styles.Add(new Structurizr.ElementStyle("Database") { Shape = Structurizr.Shape.Cylinder });
        config.Styles.Add(new Structurizr.ElementStyle("Mobile App") { Shape = Structurizr.Shape.MobileDevicePortrait });
        config.Styles.Add(new Structurizr.ElementStyle(Structurizr.Tags.Person) { Shape = Structurizr.Shape.Person });

        // Add Relationship Styles
        config.Styles.Add(new Structurizr.RelationshipStyle(Structurizr.Tags.Relationship) { FontSize = 18, Width = 600 }); // See Relationships: https://structurizr.com/help/notation
        config.Styles.Add(new Structurizr.RelationshipStyle(Structurizr.Tags.Synchronous) { Dashed = false });
        config.Styles.Add(new Structurizr.RelationshipStyle(Structurizr.Tags.Asynchronous) { Dashed = true });


        static string GetPngBase64(byte[] imageBytes) => $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";

        static string HexConverter(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}
