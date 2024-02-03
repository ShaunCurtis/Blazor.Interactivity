# Render Contexts

There are three contexts in which Blazor Components are rendered.  All the contexts provide:

 - A RenderTree for each root component managed by the Renderer.
 - A Scoped Service container to provide services that the Renderer injects into components.

## The HttpRequest Context

This is the classic old school Server Side Render context, MVC, Razor, Asp,....  The context is created in the HttpRequest pipeline to handle the page request.  

In Blazor this is the Pre-Render mode.  The page is statically rendered and return to the caller.

Some key points to understand:

1. It only exists for the duration of the request.  Once the handler completes the context is destroyed and services disposed.

2. The top level services container is owned by the running web application.  The context's scoped container in created in that container.  All *Singleton* services are provided the application container and exist for the lifetime of the web application.

3. `SetParametersAsync` is called once on a component: the the normal lifecycle methods execute once.  

4. There are no UI events, so no `OnAfterRender{Async}`.

5. The configuration of the HttpRequest Context is dictated by `Program` in the Web Application project.  The service configuration is shared by the HttpRequest Context and the Blazor Server Hub.

6. In the services configuration `AddRazorComponents()` add the necessary services for rendering Razor pages in the HttpRequest Context.

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
```

7. In the HttpRequest pipeline configuration, `MapRazorComponents<App>()` maps all requests to the `App` component

```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Counter).Assembly);
```

8. `AddAdditionalAssemblies(typeof(Counter).Assembly)` tells Razor to also search the provided assemblies for `RouteAttribute` components.


This is `App`.  It's a standard web page with one or more components.  

The render context builds two render trees: a stub one for `HeadOutlet` and the main one for `Routes`.  Both are statically rendered.

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

When the web page is returned by an HttpRequest, the browser loads the page and `blazor.web.js` is run.  It establishes a *SignalR* session with the Blazor Hub running on  the server. The *SignalR* session remains active thoughout the life of the SPA session.

At this point, the Blazor Hub session loads `App`, traverses the component render tree and establishes the interactivity roots.  In the App above, these are `HeadOutlet` and `Routes`.  However, if you are using *Per Page/Component* interactivity this may be a single tree with `Home` as it's root.

Once this is sorted, the Server Hub Context builds out the Render Tree, injects the services and runs a render on the tree.  This builds the Renderer's version of the DOM.  It runs a Diff against the statically rendered version and transmits the changes to the browser.  The browser applies the changes and updates.

Important Points:

1. Interactivity is established at the highest node in the page's component tree.  That interactivity is applied to all sub components.  You can't change to something else.  The render directive will either be ignored, or if you try and switch between Server and Web Assembly, throw an exception.

```text
 Cannot create a component of type 'Blazor.Auto.Client.Pages.Counter' because its render mode 'Microsoft.AspNetCore.Components.Web.InteractiveWebAssemblyRenderMode' is not supported by interactive server-side rendering.
 ```
 
 1. This context shares it's singleton services with the HttpRequest context.

 ## The Blazor WebAssembly Hub Context

 The way I conceptualize this is a hard wired Server Hub Session without *SignalR*.

 The Hub Session runs in the Web Assembly container in the browser.  It operates in the same way, but only has access to services from within the container.  It's totally separate from the server.  
 
 Important Points:

 1. It's *Singleton* and *Scoped* services are the essentially the same: *Singleton* services exists for the lifespan of the SPA, the same as *Scoped* services.

 1. There's no access to the server services.  You can't access databases directly.

 1. All the code is effectively an *Open Book*.  Anyone can take the code modules downloaded to the broswer and disassemble them.




## End

