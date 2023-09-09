using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Outbox.Application;
using Outbox.Infrastructure.EfCore;
using OutboxEfCore;
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

builder.Services.AddDbContext<DataContext>(
    options =>
    options
        .UseSqlServer("Server=localhost,1433;User ID=sa;Password=yourStrong(!)Password;Initial Catalog=OutboxEfDb;TrustServerCertificate=true;")
        .UseLoggerFactory(
            LoggerFactory.Create(builder => builder.AddConsole())
        )
);

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

builder.Services.AddQuartzHostedService();

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