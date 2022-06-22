using System;
using architecturizr;
using Structurizr;
using Structurizr.Api;

namespace architecturizr
{
    internal class StructurizrBuilder
    {
        public StructurizrBuilder(SourceFile s, long workspaceId, string apiKey, string apiSecret)
        {
            var workspace = new Workspace(s.Title, s.Description);
            var model = workspace.Model;

            Structurizr.Person p = null;
            foreach (var person in s.Persons)
            {
                p = model.AddPerson(person.Name, person.Description);

            }

            //var user = model.AddPerson("User", "A user of my software system.");
            var softwareSystem = model.AddSoftwareSystem("Software System", "My software system.");
            p.Uses(softwareSystem, "Uses");

            ViewSet viewSet = workspace.Views;
            SystemContextView contextView = viewSet.CreateSystemContextView(softwareSystem, "SystemContext", "An example of a System Context diagram.");
            contextView.AddAllSoftwareSystems();
            contextView.AddAllPeople();
            contextView.AddAllElements();
            // contextView.AddNearestNeighbours(p);

            //var dv = viewSet.CreateDynamicView();
            //dv.

            // https://github.com/structurizr/dotnet-core-quickstart/blob/master/structurizr/Program.cs

            // https://structurizr.com/help/themes
            // viewSet.Configuration.Theme = "default";
            Styles styles = viewSet.Configuration.Styles;
            styles.Add(new ElementStyle(Tags.SoftwareSystem) { Background = "#1168bd", Color = "#ffffff" });
            styles.Add(new ElementStyle(Tags.Person) { Background = "#08427b", Color = "#ffffff", Shape = Shape.Person });

            var structurizrClient = new StructurizrClient(apiKey, apiSecret);
            structurizrClient.PutWorkspace(workspaceId, workspace);
        }
    }
}

