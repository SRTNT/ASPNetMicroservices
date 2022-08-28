using Basket.API.Repositories;
using Basket.API.Repositories.Interfaces;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

#region Register REDIS Location
builder.Services.AddStackExchangeRedisCache(options =>
{
options.Configuration = builder.Configuration.GetValue<string>("CacheSettings:ConnectionString");
});
#endregion

#region General
builder.Services.AddScoped<IBasketRepository, BasketRepository>();
builder.Services.AddAutoMapper(typeof(Program));
#endregion

#region GRPC
builder.Services.AddGrpcClient<Discount.Grpc.Protos.DiscountProtoService.DiscountProtoServiceClient>(opt => opt.Address = new Uri(builder.Configuration.GetValue<string>("GrpcSettings:DiscountUrl")));
builder.Services.AddScoped<Basket.API.GrpcServices.DiscountGrpcService>();
#endregion

#region MassTransit-RabbitMQ Configuration
builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["EventBusSettings:HostAddress"]);
        //cfg.UseHealthCheck(ctx);
    });
});
//builder.Services.AddMassTransitHostedService();
#endregion

#region System
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
#endregion

#region Build & Run
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run(); 
#endregion
