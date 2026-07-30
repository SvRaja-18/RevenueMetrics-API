using Microsoft.EntityFrameworkCore;
using RevenueMetrics.Infrastructure.Persistence;
using RevenueMetrics.Application.Interfaces;
using RevenueMetrics.Application.Services;
using RevenueMetrics.Application.Services;
using RevenueMetrics.Infrastructure.Repositories;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
	options.KnownNetworks.Clear();
	options.KnownProxies.Clear();
});


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

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
app.MapOpenApi();
app.MapScalarApiReference();

// Redirect root to Scalar UI
app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.UseHttpsRedirection();

app.MapControllers();

app.Run();