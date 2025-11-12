using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OutboxBroker;

// Build a Host (so we can use DI like in ASP.NET)
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Database connection (same as your API)
        var connectionString = "Server=ESLAMIKHAH-PC;Database=WalletSystemDB;Trusted_Connection=True;TrustServerCertificate=True;";
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register broker service
        services.AddHostedService<OutboxBrokerService>();
    })
    .Build();

await host.RunAsync();
