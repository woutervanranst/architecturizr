using architecturizr.Models;
using architecturizr.Utils;
using Structurizr.Api;

namespace architecturizr.OutputParser;

internal class StructurizrBuilder
{
    public StructurizrBuilder(string title, string description, IEnumerable<Node> nodes, IEnumerable<Process> processes, long workspaceId, string apiKey, string apiSecret)
    {
        // Configure Workspace
        var workspace = new Structurizr.Workspace(title, description);
        var model = workspace.Model;

        model.ImpliedRelationshipsStrategy = new Structurizr.CreateImpliedRelationshipsUnlessSameRelationshipExistsStrategy(); //.CreateImpliedRelationshipsUnlessAnyRelationshipExistsStrategy(); // ! IMPORTANT, see https://github.com/structurizr/dotnet/issues/97


        // Add Nodes
        nodes = GetNodesInUse(processes).Distinct().ToArray();
        AddNodes(nodes, model);


        // Add Edges
        foreach (var p in processes)
        {
            foreach (var s in p.Steps)
            {
                var r = (Structurizr.Relationship)((dynamic)s).From.StructurizrObject.Uses(((dynamic)s.To).StructurizrObject, p.Name);

                if (s is AsyncStep)
                    r.AddTags("async");
                else if (s is SyncStep)
                    r.AddTags("sync");
                else
                    throw new Exception();
            }

        }
        //foreach (var steps in processes.SelectMany(p => p.Steps))
        //{
        //    ((dynamic)steps).From.StructurizrObject.Uses(((dynamic)steps.To).StructurizrObject, steps.Description);
        //}


        // Add Views
        var viewSet = workspace.Views;
        viewSet.Configuration.ViewSortOrder = Structurizr.ViewSortOrder.Type;

        foreach (var ss in nodes.OfType<SoftwareSystem>())
        {
            var v = viewSet.CreateSystemContextView(ss.StructurizrObject, ss.Key, $"Helicopter view of '{ss.Name}'");
            v.Title = $"[(1) System Context] {ss.Name}";

            v.AddAllElements();
            v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 200, 200, 200, false);
        }

        foreach (var ss in nodes.OfType<SoftwareSystem>())
        {
            var v = viewSet.CreateContainerView(ss.StructurizrObject, "c" + ss.Key, $"Interactions with the insides of '{ss.Name}'");
            v.Title = $"[(2) Container] {ss.Name}";

            v.AddAllElements();
            v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 200, 200, 200, false);
            // v.PaperSize = Structurizr.PaperSize.A0_Landscape;
        }

        foreach (var c in nodes.OfType<Container>())
        {
            if (c.Children.Count == 0) // if this Container does not have any children (Components), the diagram will not show anything useful
                continue;

            var v = viewSet.CreateComponentView(c.StructurizrObject, c.Key, $"What is inside/interacts with {c.Name}");

            v.Title = $"[(3) Component ALL] {c.Name}";

            v.AddAllElements();
            v.EnableAutomaticLayout( Structurizr.RankDirection.TopBottom, 200, 200, 200, false);
            // v.PaperSize = Structurizr.PaperSize.A0_Landscape;
        }

        foreach (var c in nodes.OfType<Component>())
        {
            var v = viewSet.CreateComponentView(c.Parent.StructurizrObject, "component-" + c.Key, $"What interacts with {c.Name}");
            v.Title = $"[(3) Component DIRECT] {c.Name}";

            v.Add(c.StructurizrObject);
            v.AddNearestNeighbours(c.StructurizrObject);

            v.EnableAutomaticLayout();
        }

        foreach (var p in processes)
        {
            //var c = ((SoftwareSystem)nodes.Where(n => n.Key == "ivs-be").Single()).StructurizrObject;
            var c = ((Container)nodes.Where(n => n.Key == "k8s").Single()).StructurizrObject;
            var v = viewSet.CreateDynamicView(c, $"process-{p.Name.ToKebabCase()}", p.Name);

            foreach (var s in p.Steps)
            {
                v.Add(((dynamic)s.From).StructurizrObject, s.Description, ((dynamic)s.To).StructurizrObject);
            }

            v.EnableAutomaticLayout();
        }


        /* Microservice
         * 
         *  https://structurizr.com/share/4241/diagrams#Containers
         *  https://github.com/structurizr/dsl/tree/master/docs/cookbook/workspace-extension
         *  https://structurizr.com/help/usage-recommendations
         */

        // Sequence Diagrams? https://github.com/structurizr/java/pull/129/files#diff-b55fd8523c23d8ff04163446b3ffc28e4f93238660847d4394926df9398f7a53

        // https://github.com/structurizr/dotnet-core-quickstart/blob/master/structurizr/Program.cs

        // https://structurizr.com/help/themes
        // viewSet.Configuration.Theme = "default";
        var styles = viewSet.Configuration.Styles;
        styles.Add(new Structurizr.ElementStyle(Structurizr.Tags.SoftwareSystem) { Background = "#1168bd", Color = "#ffffff" });
        styles.Add(new Structurizr.ElementStyle(Structurizr.Tags.Container) { Background = "#1168bd", Color = "#ffffff" });
        styles.Add(new Structurizr.ElementStyle(Structurizr.Tags.Person) { Background = "#08427b", Color = "#ffffff", Shape = Structurizr.Shape.Person });

        styles.Add(new Structurizr.ElementStyle(Tags.Python) { Icon = Icons.pythonPng });
        styles.Add(new Structurizr.ElementStyle(Tags.Scala) { Icon = Icons.scalaPng });

        styles.Add(new Structurizr.ElementStyle("IVS") { Background = "#e7285d" });

        styles.Add(new Structurizr.RelationshipStyle("sync") { Dashed = false });
        styles.Add(new Structurizr.RelationshipStyle("async") { Dashed = true });

        var structurizrClient = new StructurizrClient(apiKey, apiSecret);
        structurizrClient.PutWorkspace(workspaceId, workspace);
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
            person.StructurizrObject = model.AddPerson(person.Name, person.Description);

        foreach (var ss in nodes.OfType<SoftwareSystem>())
            ss.StructurizrObject = model.AddSoftwareSystem(ss.Name, ss.Description);

        foreach (var cont in nodes.OfType<Container>())
            cont.StructurizrObject = cont.Parent.StructurizrObject.AddContainer(cont.Name, cont.Description, cont.Technology);

        foreach (var comp in nodes.OfType<Component>())
        {
            var c = comp.StructurizrObject = comp.Parent.StructurizrObject.AddComponent(comp.Name, comp.Description, comp.Technology);

            c.AddTags(c.Technology);
            //if (!string.IsNullOrWhiteSpace(comp.Owner))
            //    c.AddTags("IVS");
        }

        foreach (var n in nodes)
        {
            if (!string.IsNullOrWhiteSpace(n.Owner))
                ((dynamic)n).StructurizrObject.AddTags("IVS");
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

}
