using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Library.Infrastructure.Data;
using Library.ApplicationCore;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true);
IConfiguration configuration = builder.Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddScoped<IPatronRepository, JsonPatronRepository>();
services.AddScoped<ILoanRepository, JsonLoanRepository>();
services.AddScoped<ILoanService, LoanService>();
services.AddScoped<IPatronService, PatronService>();
services.AddSingleton<JsonData>();
services.AddSingleton<ConsoleApp>();

var serviceProvider = services.BuildServiceProvider();

var consoleApp = serviceProvider.GetRequiredService<ConsoleApp>();
await consoleApp.Run();
