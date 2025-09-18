using Arceus.Application.Common.Interfaces;
using Arceus.Infrastructure.Persistence;
using Arceus.Infrastructure.Persistence.Repositories;
using Arceus.Infrastructure.Services;
using Arceus.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add MediatR
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

// Register application interfaces
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register repositories
builder.Services.AddScoped<IContractorRepository, ContractorRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

// Register services
builder.Services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    //Swagger YAHHH 
    app.MapOpenApi();
}

//how would u register middlewares here X_X
app.UseMiddleware<GlobalExceptionMiddleware>();

// app.UseHttpsRedirection();
app.MapControllers();

app.Run();