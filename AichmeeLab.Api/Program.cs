using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using AichmeeLab.Api.LocalModels;
using AichmeeLab.Api.Middleware;

var builder = FunctionsApplication.CreateBuilder(args);

builder.UseMiddleware<AuthenticationMiddleware>();;

builder.Services.Configure<DBSettings>(
    builder.Configuration.GetSection("AlexandriaDBSettings"));


builder.Services.AddSingleton<IMongoClient>(sp =>
{
    // Retrieve the settings from the DI container
    var settings = sp.GetRequiredService<IOptions<DBSettings>>().Value;

    if (string.IsNullOrEmpty(settings.ConnectionString))
    {
        throw new InvalidOperationException("MongoDb:ConnectionString is missing in configuration!");
    }
    return new MongoClient(settings.ConnectionString);
});


builder.Build().Run();
