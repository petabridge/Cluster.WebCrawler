using System.Net.Sockets;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var azure = builder.AddAzureStorage("azure")
    .RunAsEmulator();

var azureTables = azure.AddTables("azure-tables");

var web = builder.AddProject<WebCrawler_Web>("webcrawler-web")
    .WithEndpoint(name: "http", port: 8080, targetPort:8080, isExternal: true, isProxied:false, protocol:ProtocolType.Tcp, scheme:"http")
    .WithEndpoint(name: "remote", env: "CLUSTER__PORT")
    .WithEndpoint(name: "management", env: "CLUSTER__MANAGEMENTPORT")
    .WithEndpoint(name: "readiness", env: "CLUSTER__READINESSPORT")
    .WithEndpoint(name: "pbm", env: "CLUSTER__PBMPORT")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
    .WithEnvironment("CLUSTER__STARTUPMETHOD", "AzureDiscovery")
    .WithEnvironment("CLUSTER__DISCOVERY__SERVICENAME", "AzureService")
    .WithReference(azureTables)
    .WaitFor(azure);

builder.AddProject<WebCrawler_CrawlService>("webcrawler-crawlservice")
    .WithReplicas(3)
    .WithEndpoint(name: "remote", env: "CLUSTER__PORT")
    .WithEndpoint(name: "management", env: "CLUSTER__MANAGEMENTPORT")
    .WithEndpoint(name: "readiness", env: "CLUSTER__READINESSPORT")
    .WithEndpoint(name: "pbm", env: "CLUSTER__PBMPORT")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("CLUSTER__STARTUPMETHOD", "AzureDiscovery")
    .WithEnvironment("CLUSTER__DISCOVERY__SERVICENAME", "AzureService")
    .WithReference(azureTables)
    .WaitFor(azure);

builder.AddProject<WebCrawler_TrackerService>("webcrawler-trackerservice")
    .WithReplicas(3)
    .WithEndpoint(name: "remote", env: "CLUSTER__PORT")
    .WithEndpoint(name: "management", env: "CLUSTER__MANAGEMENTPORT")
    .WithEndpoint(name: "readiness", env: "CLUSTER__READINESSPORT")
    .WithEndpoint(name: "pbm", env: "CLUSTER__PBMPORT")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithEnvironment("CLUSTER__STARTUPMETHOD", "AzureDiscovery")
    .WithEnvironment("CLUSTER__DISCOVERY__SERVICENAME", "AzureService")
    .WithReference(azureTables)
    .WaitFor(azure);

builder.Build().Run();
