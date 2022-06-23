using Structurizr.Api;

namespace architecturizr
{
    internal class StructurizrBuilder
    {
        public StructurizrBuilder(SourceFile s, long workspaceId, string apiKey, string apiSecret)
        {
            var workspace = new Structurizr.Workspace(s.Title, s.Description);
            var model = workspace.Model;
            

            foreach (var person in s.Nodes.OfType<Person>())
                person.StructurizrObject = model.AddPerson(person.Name, person.Description);

            foreach (var softwareSystem in s.Nodes.OfType<SoftwareSystem>())
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

            foreach (var edge in s.Edges)
            {
                ((dynamic)edge).From.StructurizrObject.Uses(((dynamic)edge.To).StructurizrObject, "Uses2");
            }


            model.AddImplicitRelationships(); // ! IMPORTANT, see https://github.com/structurizr/dotnet/issues/97


            var viewSet = workspace.Views;

            foreach (var ss in s.Nodes.OfType<SoftwareSystem>())
            {
                var v = viewSet.CreateSystemContextView(ss.StructurizrObject, ss.Key, "hahaha");

                v.AddAllElements();
                v.EnableAutomaticLayout();
            }

            foreach (var ss in s.Nodes.OfType<SoftwareSystem>())
            {
                var v = viewSet.CreateContainerView(ss.StructurizrObject, "c" + ss.Key, "hahaha");

                v.AddAllElements();
                v.EnableAutomaticLayout();
            }

            foreach (var c in s.Nodes.OfType<Container>())
            {
                var v = viewSet.CreateComponentView(c.StructurizrObject, c.Key, "hehfdhjdfd");

                v.AddAllElements();
                v.EnableAutomaticLayout();
            }

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
}