using AichmeeLab;
using AichmeeLab.Services.DashboardService;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string baseAddr = builder.Configuration["ApiSettings:BaseUrl"]
                  ?? builder.HostEnvironment.BaseAddress;



builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddr) });


builder.Services.AddScoped<IDashboardService, DashboardService>();


await builder.Build().RunAsync();
