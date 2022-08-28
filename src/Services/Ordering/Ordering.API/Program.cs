using MassTransit;
using Ordering.API.EventBusConsumer;
using Ordering.API.Extensions;
using Ordering.Application;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region MassTransit-RabbitMQ Configuration
builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((ctx, cfg) =>
    {
        config.AddConsumer<Ordering.API.EventBusConsumer.BasketCheckoutConsumer>();

        cfg.Host(builder.Configuration["EventBusSettings:HostAddress"]);
        //cfg.UseHealthCheck(ctx);

        cfg.ReceiveEndpoint(EventBus.Messages.Common.EventBusConstants.BasketCheckoutQueue, c =>
        {
            c.ConfigureConsumer<Ordering.API.EventBusConsumer.BasketCheckoutConsumer>(ctx);
        });
    });
});
//builder.Services.AddMassTransitHostedService();
#endregion

builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddScoped<BasketCheckoutConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<OrderContext>>();
    logger.LogInformation("ConnectionString: " + builder.Configuration.GetConnectionString("OrderingConnectionString"));
}

app.MigrateDatabase<OrderContext>((context, services) =>
                                  {
                                      //Microsoft.Extentions.DependencyInjection => .GetService<
                                      var logger = services.GetService<ILogger<OrderContextSeed>>();
                                      OrderContextSeed.SeedAsync(context, logger)
                                                      .Wait();
                                  });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("SRT ------------------------------ ConnectionString:   " + builder.Configuration.GetConnectionString("OrderingConnectionString"));
}

app.Run();