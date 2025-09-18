using Arceus.Application.Common.Interfaces;
using Arceus.Infrastructure.Persistence;
using Arceus.Infrastructure.Persistence.Repositories;
using Arceus.Infrastructure.Services;
using Arceus.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// yah service container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add MediatR it is a .net core packget for Mediator pattern for simple, in-process messaging
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Arceus.Application.Common.Interfaces.IApplicationDbContext).Assembly);
});

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
        .UseSnakeCaseNamingConvention()
);


// like how would u register in laravel app()->singleton(some thing)
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

//think of it like a db wrapper that saves your shit implementations
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// like how would u register in laravel app()->singleton(some thing)
builder.Services.AddScoped<IContractorRepository, ContractorRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// Register services
builder.Services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    //Swagger YAHHH
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Arceus API v1");
        options.RoutePrefix = "swagger"; // Access via /swagger
    });
}

//how would u register middlewares here X_X
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();