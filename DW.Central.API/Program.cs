using DW.Central.API.Services.Azure;
using DW.Central.API.Services.Dataverse;
using DW.Central.API.Services.Internal;
using DW.Central.API.Services.MicrosoftEntra;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<TokenService>(); // Register TokenService
        services.AddSingleton<CheckFlows>();    // Register CheckFlows if it's a service
        services.AddSingleton<KeyvaultService>();
        services.AddSingleton<StringServices>();
        services.AddSingleton<LogErrorService>();
    })
    .Build();

host.Run();
