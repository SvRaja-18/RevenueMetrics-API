using Microsoft.EntityFrameworkCore;
using RevenueMetrics.Infrastructure.Persistence;
using RevenueMetrics.Application.Interfaces;
using RevenueMetrics.Application.Services;
using RevenueMetrics.Infrastructure.Repositories;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


// Register PostgreSQL / Supabase
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseNpgsql(
		builder.Configuration.GetConnectionString("Supabase")));

builder.Services.AddHttpClient<ISyncProvider, RevenueMetrics.Infrastructure.Services.SyncProviders.HubSpotSyncProvider>();
builder.Services.AddScoped<ISyncProvider, RevenueMetrics.Infrastructure.Services.SyncProviders.GoogleCalendarSyncProvider>();
builder.Services.AddHttpClient<ISyncProvider, RevenueMetrics.Infrastructure.Services.SyncProviders.StripeSyncProvider>();

builder.Services.AddScoped<RevenueMetrics.Infrastructure.Services.SyncOrchestrator>();
builder.Services.AddHostedService<RevenueMetrics.API.HostedServices.DataSyncHostedService>();

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<RevenueCalculator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();