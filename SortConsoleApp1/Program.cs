using SortConsoleApp1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SortConsoleApp1.Extras;
using SortConsoleApp1.Interfaces;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddCommandLine(args)
    .Build();
var sortingProvider = new SortingProvider();

var serviceDescriptors = new ServiceCollection();
ConfigureServices(serviceDescriptors, configuration, sortingProvider);
var serviceProvider = serviceDescriptors.BuildServiceProvider();

//application runs here
var appService = serviceProvider.GetService<App>();
if (appService != null)
{
    await appService.Run();
}


static void ConfigureServices(IServiceCollection serviceCollection, IConfigurationRoot config, ISortingProvider<Row> sortingProvider)
{
    serviceCollection.AddSingleton(config);
    serviceCollection.AddSingleton(sortingProvider);
    serviceCollection.AddTransient<App>();
}



