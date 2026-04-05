using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using AichmeeLab.Api.LocalModels;
using AichmeeLab.Api.Middleware;
using AichmeeLab.Api.Services.ArticleService;
using AichmeeLab.Api.Services.AuthenticatorService;
using AichmeeLab.Api.Services.ImageService;
using Azure.Storage.Blobs;
using AichmeeLab.Api.Services.ContentService;

var builder = FunctionsApplication.CreateBuilder(args);

builder.UseMiddleware<AuthenticationMiddleware>(); ;

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    // Retrieve the settings from the DI container
    var config = sp.GetRequiredService<IConfiguration>();

    if (string.IsNullOrEmpty(config["ConnectionString"]))
    {
        throw new InvalidOperationException("ConnectionString is missing in configuration!");
    }
    return new MongoClient(config["ConnectionString"]);
});

builder.Services.Configure<AlexandriaDbSettings>(
    builder.Configuration.GetSection("AlexandriaDbSettings"));

builder.Services.Configure<MonitorDbSettings>(
builder.Configuration.GetSection("MonitorDbSettings"));

builder.Services.AddSingleton(x => new BlobServiceClient(
    builder.Configuration["AzureStorageConnectionString"]));


builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IAuthenticatorService, AuthenticatorService>();
builder.Services.AddScoped<IImageService,ImageService>();
builder.Services.AddScoped<IContentService, ContentService>();

builder.Build().Run();
