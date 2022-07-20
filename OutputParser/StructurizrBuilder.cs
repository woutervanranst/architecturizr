using architecturizr.Models;
using Structurizr.Api;

namespace architecturizr.OutputParser;

internal class StructurizrBuilder
{
    public StructurizrBuilder(string title, string description, IEnumerable<Node> nodes, long workspaceId, string apiKey, string apiSecret)
    {
        var workspace = new Structurizr.Workspace(title, description);
        var model = workspace.Model;


        foreach (var person in nodes.OfType<Person>())
            person.StructurizrObject = model.AddPerson(person.Name, person.Description);

        foreach (var softwareSystem in nodes.OfType<SoftwareSystem>())
        {
            softwareSystem.StructurizrObject = model.AddSoftwareSystem(softwareSystem.Name, softwareSystem.Description);

            foreach (var container in softwareSystem.Children)
            {
                container.StructurizrObject = softwareSystem.StructurizrObject.AddContainer(container.Name, container.Description, container.Technology);

                foreach (var component in container.Children)
                {
                    var c = container.StructurizrObject.AddComponent(component.Name, component.Description, component.Technology);

                    c.AddTags(c.Technology);
                    if (!string.IsNullOrWhiteSpace(component.Owner))
                        c.AddTags("IVS");

                    component.StructurizrObject = c;
                }
            }
        }

        //foreach (var n in s.Nodes)
        //{
        //    Structurizr.StaticStructureElement e = ((dynamic)s.Nodes).StructurizrObject;
        //    n.te

        //}

        //foreach (var edge in s.Edges)
        //{
        //    ((dynamic)edge).From.StructurizrObject.Uses(((dynamic)edge.To).StructurizrObject, "Uses2");
        //}


        model.ImpliedRelationshipsStrategy = new Structurizr.CreateImpliedRelationshipsUnlessAnyRelationshipExistsStrategy(); // ! IMPORTANT, see https://github.com/structurizr/dotnet/issues/97


        var viewSet = workspace.Views;
        viewSet.Configuration.ViewSortOrder = Structurizr.ViewSortOrder.Type;

        foreach (var ss in nodes.OfType<SoftwareSystem>())
        {
            var v = viewSet.CreateSystemContextView(ss.StructurizrObject, ss.Key, "hahaha");

            v.Title = $"[(1) System Context] {ss.Name}";
            v.AddAllElements();
            v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 200, 200, 200, false);
        }

        foreach (var ss in nodes.OfType<SoftwareSystem>())
        {
            var v = viewSet.CreateContainerView(ss.StructurizrObject, "c" + ss.Key, "hahaha");

            v.Title = $"[(2) Container] {ss.Name}";
            v.AddAllElements();
            v.EnableAutomaticLayout(Structurizr.RankDirection.TopBottom, 200, 200, 200, false);
            // v.PaperSize = Structurizr.PaperSize.A0_Landscape;
        }

        foreach (var c in nodes.OfType<Container>())
        {
            if (c.Children.Count == 0) // if this Container does not have any children (Components), the diagram will not show anything useful
                continue;

            var v = viewSet.CreateComponentView(c.StructurizrObject, c.Key, "hehfdhjdfd");

            v.Title = $"[(3) Component 123] {c.Name}";
            v.Description = $"What is inside {c.Name}";

            v.AddAllElements();
            // v.EnableAutomaticLayout( Structurizr.RankDirection.TopBottom, 200, 200, 200, false);
            // v.PaperSize = Structurizr.PaperSize.A0_Landscape;
        }

        foreach (var c in nodes.OfType<Component>())
        {
            var v = viewSet.CreateComponentView(c.Parent.StructurizrObject, "component-" + c.Key, "hahad");

            v.Title = $"[(3) Component] {c.Name}";
            v.Description = $"What interacts with {c.Name}";
            v.Add(c.StructurizrObject);
            v.AddNearestNeighbours(c.StructurizrObject);

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

        var structurizrClient = new StructurizrClient(apiKey, apiSecret);
        structurizrClient.PutWorkspace(workspaceId, workspace);
    }


}
