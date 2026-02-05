using AichmeeLab;
using AichmeeLab.Services.LibraryService;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string baseAddr = builder.HostEnvironment.IsDevelopment()
                  ? "http://localhost:7203/"//for testing
                  : builder.HostEnvironment.BaseAddress;



builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddr) });


builder.Services.AddScoped<ILibraryService, LibraryService>();


await builder.Build().RunAsync();
