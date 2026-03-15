using AichmeeLab;
using AichmeeLab.Services.DashboardService;
using AichmeeLab.Services.SecurityService;
using AichmeeLab.Services.UserService;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string baseAddr = builder.Configuration["ApiSettings:BaseUrl"]
                  ?? builder.HostEnvironment.BaseAddress;



builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddr) });


builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();


await builder.Build().RunAsync();
