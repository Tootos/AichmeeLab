using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using AichmeeLab.Api.LocalModels;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// 1. Bind the MongoDbSettings class to the configuration section
builder.Services.Configure<AlexandriaDBSettings>(
    builder.Configuration.GetSection("AlexandriaDB"));

// 2. Register IMongoClient using the settings
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    // Retrieve the settings from the DI container
    var settings = sp.GetRequiredService<IOptions<AlexandriaDBSettings>>().Value;

    if (string.IsNullOrEmpty(settings.ConnectionString))
    {
        throw new InvalidOperationException("MongoDb:ConnectionString is missing in configuration!");
    }
    return new MongoClient(settings.ConnectionString);
});

builder.Build().Run();
