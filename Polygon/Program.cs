using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Polygon;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<PolygonApp>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

await builder.Build().RunAsync();

