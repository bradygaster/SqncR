var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SqncR_Cli>("sqncr-cli");

builder.Build().Run();
