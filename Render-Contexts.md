# Render Contexts

There are three contexts in which Blazor Components are rendered.  Each contains:

 - A RenderTree for each root component managed by the Renderer.
 - A Scoped Service container to provide services.

## The HttpRequest Context

This is the classic old school Server Side Render context, MVC, Razor, Asp,....  The context is created in the HttpRequest pipeline to handle the page request.  The context only exists for the duration of the request.  Once the content is returned to the caller in the HttpRequest pipeline, the context is destroyed and services disposed.

Note that the top level services container is owned by the running web application.  All *Singleton* services are provided this container and exist for the lifetime of the web application.

In component `SetParametersAsync` is called once: so the the normal lifecycle methods execute once.  There are no UI events, so there's no `OnAfterRender{Async}`.

In Blazor this is the Pre-Render.  The page is statically rendered and return to the caller.

The configuration of the HttpRequest Context is dictated by `Program` in the Web Application project.  The service configuration is shared by the HttpRequest Context and the Blazor Server Hub.

In the services configuration `AddRazorComponents()` add the necessary services for rendering Razor pages in the HttpRequest Context.

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
```

In app, `MapRazorComponents<App>()` maps all requests to the `App` component and `AddAdditionalAssemblies(typeof(Counter).Assembly)` tells Razor to also search the provided assemblies for `RouteAttribute` components.

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Counter).Assembly);
```

This is `App`.  It's a standard web page with one or more components.  Note that the two root components `HeadOutlet` and `Routes` are also statically rendered.

```csharp
<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link rel="stylesheet" href="bootstrap/bootstrap.min.css" />
    <link rel="stylesheet" href="app.css" />
    <link rel="stylesheet" href="Blazor.Auto.styles.css" />
    <link rel="icon" type="image/png" href="favicon.png" />
    <HeadOutlet />
</head>

<body>
    <Routes />
    <script src="_framework/blazor.web.js"></script>
</body>

</html>
```

## The Blazor Server Hub Context

When the web page is returned by by an HttpRequest, the `blazor.web.js` code establishes a *SingalR* session with the server.  This creates and maintains the Server Hub context while the *SignalR* session remains active.  The context creates RenderTrees for any base interactive components.  The root of the render tree is the highest level component declaring a `@rendermode`.  Any lower components declaring a render mode are either ignored or throw exceptions.  Having a `@rendermode="InteractiveWebAssembly"` below a `@rendermode="InteractiveServer"` will cause an exception.

```text
 Cannot create a component of type 'Blazor.Auto.Client.Pages.Counter' because its render mode 'Microsoft.AspNetCore.Components.Web.InteractiveWebAssemblyRenderMode' is not supported by interactive server-side rendering.
 ```
 
 This context shares it's singleton services with the HttpRequest context.

 ## The Blazor WebAssembly Hub Context

 This is the same as the Server Hub Context, but runs in the browser.  It's totally separate from the server.  It's *Singleton* and *Scoped* services are the essentially the same: *Singleton* services exists for the lifespan of the SPA, the same as *Scoped* services.



