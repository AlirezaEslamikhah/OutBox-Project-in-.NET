using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OutboxBroker.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace OutboxBroker
{
    public class OutboxBrokerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<Guid, List<OutboxMessage>> _aggregateMessages = new();

        public OutboxBrokerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("🚀 Outbox Broker started. Listening for new messages...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        // Fetch all messages with Status = New
                        var newMessages = await db.OutboxMessages
                            .Where(x => x.Status == StatusCode.New)
                            .OrderBy(x => x.AggregateId)
                            .ThenBy(x => x.Sequence)
                            .ToListAsync(stoppingToken);

                        if (newMessages.Any())
                        {
                            Console.WriteLine($"\n📬 Found {newMessages.Count} new messages.");

                            foreach (var message in newMessages)
                            {
                                // Track by AggregateId
                                _aggregateMessages.AddOrUpdate(
                                    message.AggregateId,
                                    new List<OutboxMessage> { message },
                                    (_, list) =>
                                    {
                                        list.Add(message);
                                        return list.OrderBy(x => x.Sequence).ToList();
                                    });

                                // Process the message
                                await ProcessMessageAsync(db, message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                }

                // Check again every 5 seconds
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task ProcessMessageAsync(AppDbContext db, OutboxMessage message)
        {
            Console.WriteLine($"➡️  Processing message {message.Id} (Aggregate: {message.AggregateId}, Seq: {message.Sequence}, Type: {message.Type})");

            // Mark as Publishing
            message.Status = StatusCode.Publishing;
            await db.SaveChangesAsync();

            // Simulate broker sending time (5s)
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Mark as Dead (finished)
            message.Status = StatusCode.Dead;
            await db.SaveChangesAsync();

            Console.WriteLine($"✅ Finished processing message {message.Id} (Aggregate {message.AggregateId})");
        }
    }
}
