﻿using Structurizr.Api;

namespace architecturizr
{
    internal class StructurizrBuilder
    {
        public StructurizrBuilder(SourceFile s, long workspaceId, string apiKey, string apiSecret)
        {
            var workspace = new Structurizr.Workspace(s.Title, s.Description);
            var model = workspace.Model;
            

            foreach (var p in s.Nodes.OfType<Person>())
                p.StructurizrObject = model.AddPerson(p.Name, p.Description);

            foreach (var ss in s.Nodes.OfType<SoftwareSystem>())
            {
                ss.StructurizrObject = model.AddSoftwareSystem(ss.Name, ss.Description);

                foreach (var cont in ss.Children)
                {
                    cont.StructurizrObject = ((Structurizr.SoftwareSystem)ss.StructurizrObject).AddContainer(cont.Name, cont.Description, cont.Technology);

                    foreach (var comp in cont.Children)
                    {
                        comp.StructurizrObject = ((Structurizr.Container)cont.StructurizrObject).AddComponent(comp.Name, comp.Description, comp.Technology);
                    }
                }
            }

            foreach (var edge in s.Edges)
            {
                //if (edge.To.StructurizrObject is Structurizr.Container)
                //    edge.From.StructurizrObject.Uses((Structurizr.Container)edge.To.StructurizrObject, "hheh");

                edge.From.StructurizrObject.Uses((dynamic)edge.To.StructurizrObject, "Uses2");
            }


            model.AddImplicitRelationships(); // ! IMPORTANT, see https://github.com/structurizr/dotnet/issues/97


            var viewSet = workspace.Views;

            foreach (var ss in s.Nodes.OfType<SoftwareSystem>())
            {
                var contextView = viewSet.CreateSystemContextView((Structurizr.SoftwareSystem)ss.StructurizrObject, ss.Key, "hahaha");

                contextView.AddAllElements();
                contextView.EnableAutomaticLayout();
            }

            // foreach (var c in s.Nodes.OfType<Container>())
            // {
            //     var containerView = viewSet.CreateContainerView(c.pa ((Structurizr.SoftwareSystem)c.StructurizrObject).Parent, c.Key, "haha");
            //}

            // https://github.com/structurizr/dotnet-core-quickstart/blob/master/structurizr/Program.cs

            // https://structurizr.com/help/themes
            // viewSet.Configuration.Theme = "default";
            var styles = viewSet.Configuration.Styles;
            styles.Add(new Structurizr.ElementStyle(Structurizr.Tags.SoftwareSystem) { Background = "#1168bd", Color = "#ffffff" });
            styles.Add(new Structurizr.ElementStyle(Structurizr.Tags.Container) { Background = "#1168bd", Color = "#ffffff" });
            styles.Add(new Structurizr.ElementStyle(Structurizr.Tags.Person) { Background = "#08427b", Color = "#ffffff", Shape = Structurizr.Shape.Person });



            var structurizrClient = new StructurizrClient(apiKey, apiSecret);
            structurizrClient.PutWorkspace(workspaceId, workspace);
        }
    }
}

