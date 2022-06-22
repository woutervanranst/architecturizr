using System.Reflection;
using architecturizr;
using Microsoft.Extensions.Configuration;


// Set up on Mac & read in Console App
//      https://makolyte.com/how-to-add-user-secrets-in-a-dotnetcore-console-app/
//      https://dotnetcoretutorials.com/2022/04/28/using-user-secrets-configuration-in-net/

var config = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
    .Build();


var workspaceId = "74785";
var apiKey = config["structurizr:apiKey"]; // see https://structurizr.com/workspace/74785/settings
var apiSecret = config["structurizr:apiSecret"];

var s = new SourceFile("/Users/wouter/Documents/GitLab/solution-architecture/c4/source2.xlsx");
