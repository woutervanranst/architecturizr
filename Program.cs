using System.Net;
using architecturizr.InputParsers;
using architecturizr.Models;
using architecturizr.OutputParser;
using architecturizr.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Initialize Secrets
// On Windows > Right click project > Manage User Secrets
// Set up on Mac & read in Console App
//      https://makolyte.com/how-to-add-user-secrets-in-a-dotnetcore-console-app/
//      https://dotnetcoretutorials.com/2022/04/28/using-user-secrets-configuration-in-net/
//      Add new secrets:
//      https://developercommunity.visualstudio.com/t/manage-user-secrets/179886#T-N195679
//      Export: (see https://www.karltarvas.com/2019/10/28/visual-studio-mac-manage-user-secrets.html)
//      ~/.microsoft/usersecrets
var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

// Initialize Services
var services = new ServiceCollection()
    .AddLogging(builder =>
    {
        builder.AddConsole();
    })
    .AddSingleton<ExcelNodeParser>()
    .AddSingleton<PlantUmlParser>()
    .BuildServiceProvider();

// Get Nodes
var (title, description, nodes) = await GetNodesAsync(config["NodeDefinition:Url"], services);

// Parse GetProcesses
var processesDirectory = new DirectoryInfo(args[0]);
var processes = GetProcesses(services, nodes, processesDirectory);

// Build Structurizr Diagram
var workspaceId = long.Parse(config["Structurizr:WorkspaceId"]);
var apiKey = config["Structurizr:ApiKey"].Value(); // see https://structurizr.com/workspace/74785/settings
var apiSecret = config["Structurizr:ApiSecret"].Value();
var logger = services.GetRequiredService<ILogger<StructurizrBuilder>>();

var sb = new StructurizrBuilder(logger, title, description, nodes.Values, processes, workspaceId, apiKey, apiSecret);


static async Task<(string title, string description, IDictionary<string, Node> nodes)> GetNodesAsync(string nodeDefinitionsUrl, IServiceProvider serviceProvider)
{
    FileInfo? nodeDefinitionsFile = null;

    try
    {
        // Get Nodes Definition file
        using var client = new HttpClient();
        await using var s = await client.GetStreamAsync(nodeDefinitionsUrl);
        nodeDefinitionsFile = new FileInfo(Path.GetTempFileName());
        await using var fs = nodeDefinitionsFile.Create();
        await s.CopyToAsync(fs);
        fs.Close();
        s.Close();

        // Parse Nodes
        var nodeParser = serviceProvider.GetRequiredService<ExcelNodeParser>();
        var r = nodeParser.Parse(nodeDefinitionsFile);
        return r;
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
    {
        throw new Exception($"Cannot access the Nodes Definition File at {nodeDefinitionsUrl}");
    }
    finally
    {
        nodeDefinitionsFile?.Delete();
    }
}

static IEnumerable<Process> GetProcesses(IServiceProvider serviceProvider, IDictionary<string, Node> nodes, DirectoryInfo processesDirectory)
{
    var processParser = serviceProvider.GetRequiredService<PlantUmlParser>();
    processParser.SetNodes(nodes);

    var ps = processesDirectory.GetFiles("*.puml", SearchOption.AllDirectories)
        .Select(fi => processParser.Parse(fi))
        .ToArray();
    return ps;
}