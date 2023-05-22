using SortConsoleApp1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;
using SortConsoleApp1.Services;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(serviceCollection =>
    {
        //serviceCollection.AddSingleton<IConfiguration, ConfigurationBuilder>();
        //serviceCollection.AddSingleton<ISortingService<Row>, RowSortingService>();
        serviceCollection.AddSingleton<ISortingService<string>, LineSortingService>();
        serviceCollection.AddTransient<App>();
    })
    .Build();

// Getting App service to run
var myApp = ActivatorUtilities.CreateInstance<App>(host.Services);
await myApp.Run();

await host.RunAsync();

