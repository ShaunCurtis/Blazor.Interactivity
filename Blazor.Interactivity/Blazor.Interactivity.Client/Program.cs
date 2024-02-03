using Blazr.UI.Common;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped<IRenderModeProvider, WebAssemblyRenderModeProvider>();

await builder.Build().RunAsync();
