# Logging The Render Mode

An important part of understanding Interactivity is:
 - knowing the actual render mode of a component.
 - knowing the Scoped Service Container state.

First a enum:

```csharp
public enum ComponentRenderMode
{
    PreRender,
    Server,
    WebAssembly
}
```

We define an interface `IRenderModeProvider`.
```csharp
public interface IRenderModeProvider
{
    public string Id { get; }
    public ComponentRenderMode RenderMode { get; }

    public string Mode => RenderMode switch {
        ComponentRenderMode.Server => "Blazor Server Rendered",
        ComponentRenderMode.WebAssembly => "Blazor WebAssembly Rendered",
        _ => "Pre-Rendered"
    }; 
}
```

A server implementation.  This uses the `IHttpContextAccessor` service to detect the presence of a `HttpContext` and it's state.

```csharp
public class ServerRenderModeProvider : IRenderModeProvider
{
    private readonly Guid _id = Guid.NewGuid();
    private IHttpContextAccessor _httpContextAccessor;
    public string Id => _id.ToString().Substring(0,4);

    public ComponentRenderMode RenderMode =>
        !(_httpContextAccessor.HttpContext is not null && _httpContextAccessor.HttpContext.Response.HasStarted)
            ? ComponentRenderMode.PreRender
            : ComponentRenderMode.Server;

    public ServerRenderModeProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
}
```

Registered as follows:

```csharp

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRenderModeProvider, ServerRenderModeProvider>();
```

And a client implementation:

```csharp
public class WebAssemblyRenderModeProvider : IRenderModeProvider
{
    private readonly Guid _id = Guid.NewGuid();
    public string Id => _id.ToString().Substring(0, 4);

    public ComponentRenderMode RenderMode => ComponentRenderMode.WebAssembly;
}
```

Registered as follows:

```csharp

builder.Services.AddScoped<IRenderModeProvider, WebAssemblyRenderModeProvider>();
```


We can use the service in a `ComponentBase` implementation.

This inherits from `ComponentBase` and logs the render mode and the service Id to the console when the component first renders.

```csharp
public class LoggingComponentBase : ComponentBase
{
   [Inject] private IRenderModeProvider RenderModeProvider { get; set; } = default!;

    private bool _firstRender = true;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (_firstRender)
        {
            Console.WriteLine($"{this.GetType().Name} - {this.RenderModeProvider.Mode} - ServiceId: {this.RenderModeProvider.Id}");
            _firstRender = false;
        }
        return base.SetParametersAsync(ParameterView.Empty);
    }
}
```

There's a `Layout` version:

```csharp
public class LoggingLayoutComponentBase : LayoutComponentBase
{
   [Inject] private IRenderModeProvider RenderModeProvider { get; set; } = default!;

    private bool _firstRender = true;

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (_firstRender)
        {
            Console.WriteLine($"{this.GetType().Name} - {this.RenderModeProvider.Mode} - ServiceId: {this.RenderModeProvider.Id}");
            _firstRender = false;
        }
        return base.SetParametersAsync(ParameterView.Empty);
    }
}
```

Finally update `Routes`, `MainLayout`, `Home`, `Counter`, `Weather`

```csharp
@page "/"
@inherits LoggingComponentBase
```

```csharp
@inherits LoggingComponentBase

<Router AppAssembly="typeof(Program).Assembly">
    <Found Context="routeData">
```

```csharp
@inherits LoggingLayoutComponentBase

<div class="page">
    <div class="sidebar">
        <NavMenu />
```

An example output looks like this:

```text
Routes - Pre-Rendered - ServiceId: 5f85
MainLayout - Pre-Rendered - ServiceId: 5f85
Home - Pre-Rendered - ServiceId: 5f85
Routes - Blazor Server Rendered - ServiceId: b286
MainLayout - Blazor Server Rendered - ServiceId: b286
Home - Blazor Server Rendered - ServiceId: b286
```

Note: you will need to open the Browser Dev Tools `<F12>` and look in the Browser console to see the Client side log.