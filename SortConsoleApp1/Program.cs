using SortConsoleApp1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

// var configBuilder = new ConfigurationBuilder();
// BuildConfig(configBuilder);

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(serviceCollection =>
    {
        //serviceCollection.AddSingleton<IConfiguration, ConfigurationBuilder>();
        serviceCollection.AddSingleton<ISortingService<Row>, SortingService>();
        serviceCollection.AddTransient<App>();
    })
    .Build();

// Getting App service to run
var myApp = ActivatorUtilities.CreateInstance<App>(host.Services);
await myApp.Run();

await host.RunAsync();

// static void BuildConfig(IConfigurationBuilder configBuilder)
// {
//     configBuilder.SetBasePath(Directory.GetCurrentDirectory())
//         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//         .Build();
// }
