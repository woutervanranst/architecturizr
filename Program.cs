using System.Reflection;
using architecturizr;
using Microsoft.Extensions.Configuration;


// Set up on Mac & read in Console App
//      https://makolyte.com/how-to-add-user-secrets-in-a-dotnetcore-console-app/
//      https://dotnetcoretutorials.com/2022/04/28/using-user-secrets-configuration-in-net/
// Add new secrets:
//      https://developercommunity.visualstudio.com/t/manage-user-secrets/179886#T-N195679

var s = new SourceFile("/Users/wouter/Documents/GitLab/solution-architecture/c4/source2.xlsx");


var config = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
    .Build();

long workspaceId = 74785;
var apiKey = config["structurizr:apiKey"]; // see https://structurizr.com/workspace/74785/settings
var apiSecret = config["structurizr:apiSecret"];

var b = new StructurizrBuilder(s, workspaceId, apiKey, apiSecret);
