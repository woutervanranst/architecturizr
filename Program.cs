﻿using System.Reflection;
using architecturizr;
using architecturizr.InputParsers;
using architecturizr.Models;
using architecturizr.OutputParser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


// Set up on Mac & read in Console App
//      https://makolyte.com/how-to-add-user-secrets-in-a-dotnetcore-console-app/
//      https://dotnetcoretutorials.com/2022/04/28/using-user-secrets-configuration-in-net/
// Add new secrets:
//      https://developercommunity.visualstudio.com/t/manage-user-secrets/179886#T-N195679


var services = new ServiceCollection()
    .AddLogging(builder =>
        {
            builder.AddConsole();
        })
    .AddSingleton<ExcelNodeParser>()
    .AddSingleton<PlantUmlParser>()
    .BuildServiceProvider();

// Parse Nodes
var nodeParser = services.GetRequiredService<ExcelNodeParser>();
var (title, description, nodes) = nodeParser.Parse(new FileInfo("/Users/wouter/Documents/GitLab/solution-architecture/microservice-dependencies/structurizr-c4/source2.xlsx"));

// Parse Processes
var processParser = services.GetRequiredService<PlantUmlParser>();
processParser.SetNodes(nodes);

var processesDirectory = new DirectoryInfo(@"/Users/wouter/Documents/GitLab/solution-architecture/microservice-dependencies/structurizr-c4/processes/");
var processes = processesDirectory.GetFiles().Select(fi => processParser.Parse(fi));

// var processes = new Process[]{ processParser.Parse(new FileInfo(@" / Users/wouter/Documents/GitLab/solution-architecture/microservice-dependencies/structurizr-c4/processes/s1.txt")) };

// Build Structurizr Diagram
var config = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
    .Build();

long workspaceId = 74785;
var apiKey = config["structurizr:apiKey"]; // see https://structurizr.com/workspace/74785/settings
var apiSecret = config["structurizr:apiSecret"];
var logger = services.GetRequiredService<ILogger<StructurizrBuilder>>();

var b = new StructurizrBuilder(logger, title, description, nodes.Values, processes, workspaceId, apiKey, apiSecret);
