using ApiIntegrationLog.Api.Database;
using MassTransit;
using MediatR;
using Outbox.Application;
using OutboxMongoDb;
using Quartz;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

MongoDbPersistence.Configure();

builder.Services.AddSingleton(serviceProvider =>
{
    var connectionString = "mongodb://localhost:28017";
    var databaseName = "OutboxMongoDb";
    var logger = serviceProvider.GetRequiredService<ILogger<MongoDbContext>>();

    return new MongoDbContext(connectionString, databaseName, logger);
});

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly)
        .RegisterServicesFromAssembly(typeof(IOutboxApplicationMarker).Assembly)
);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddQuartz(
    configure =>
    {
        var jobKey = new JobKey(nameof(OutboxMessageProcessorJob));

        configure.AddJob<OutboxMessageProcessorJob>(jobKey)
            .AddTrigger(
                trigger =>
                trigger.ForJob(jobKey)
                    .WithSimpleSchedule(
                        schedule =>
                        schedule.WithIntervalInSeconds(30)
                            .RepeatForever()
                    )
            );
    }
);

builder.Services.AddQuartzHostedService();

builder.Services.AddMassTransit(
    x =>
    {
        x.SetKebabCaseEndpointNameFormatter();
        x.SetInMemorySagaRepositoryProvider();

        var assembly = typeof(Program).Assembly;

        x.AddConsumers(assembly);
        x.AddSagaStateMachines(assembly);
        x.AddSagas(assembly);
        x.AddActivities(assembly);

        x.UsingRabbitMq(
            (context, cfg) =>
            {
                cfg.Host("localhost", "/", hostConfigurator =>
                {
                    hostConfigurator.Username("guest");
                    hostConfigurator.Password("guest");
                });
                cfg.ConfigureEndpoints(context);
            }
        );
    }
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/users", async (CreateUserRequest request, IMediator mediator) =>
{
    var user = await mediator.Send(new CreateUserCommand(request.Name));
    return Results.Created($"/users/{user.Id}", user);
})
.WithName("CreateUser");

app.Run();

internal record CreateUserRequest(string Name);