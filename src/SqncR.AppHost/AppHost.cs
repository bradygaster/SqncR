var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SqncR_Cli>("sqncr-cli");
builder.AddProject<Projects.SqncR_McpServer>("sqncr-mcp");

builder.Build().Run();
