# Blazor Interactivity

Net 8 introduced the concept of *Interactivity* and provided ways to set it.  It's blurred the boundaries between Blazor Server, Blazor WebAssemby and classic server side applications. 

  Great, but it's been a double edged sword.  Many programmers don't understand the complexity and pitfalls in moving from one of the classic modes to hybrid mode operation.  In this article I'll try and help you understand what *interactivity* really is, and it's impact on rendering and services.

## Demo Solution

If you want to walk along with this dicsussion create a Blazor Solution using the *Blazor Web App* template.  Set *Interactive Render Mode* to Auto and *Interactivity Location* to Per Page/Component.

The coding the logging and adding it to the components is described in the Appendix.

> TODO - Add this to the appendix

##  Blazor Server

The original mode of operation.

In the Server project modify `App.razor` by adding `@rendermode="InteractiveServer"` to the `HeadOutlet` and `Routes` components.

```csharp
//...
    <HeadOutlet @rendermode="InteractiveServer" />
    @* <HeadOutlet /> *@
//...
    <Routes @rendermode="InteractiveServer" />
    @* <Routes /> *@
//...
```

What you will see is this:

```text
==> Initial Pre Render
Routes - Pre-Rendered - ServiceId: d32d
MainLayout - Pre-Rendered - ServiceId: d32d
Home - Pre-Rendered - ServiceId: d32d

==> Blazor Server SPA Render
Routes - Blazor Server Rendered - ServiceId: 2b8f
MainLayout - Blazor Server Rendered - ServiceId: 2b8f
Home - Blazor Server Rendered - ServiceId: 2b8f

==> Clicking through pages

Counter - Blazor Server Rendered - ServiceId: 2b8f
Weather - Blazor Server Rendered - ServiceId: 2b8f
Counter - Blazor Server Rendered - ServiceId: 2b8f
Home - Blazor Server Rendered - ServiceId: 2b8f
Counter - Blazor Server Rendered - ServiceId: 2b8f
```

You get the intital pre-render of the components in the HttpRequest context.  The page downloads to the client.  The Blazor client JS code establishes an SPA session with the Blazor Hub [running on the server].  This rebuilds the interactive parts of the page and passes them back to the client to apply to the DOM.

For the purposes of this discussion, the SPA session has:
1. One or more RenderTrees maintained by the Renderer
1. A scoped service container which exists for the duration of the SPA session.

It's important to understand that services are injected into components from their render context.  Scoped and Transient Services required by a component rendered by a RenderTree in the HttpRequest context come from the HttpRequest's Scoped container.  This container only exists for the duration of the HttpRequest context.  Once the server returns the page to the client that context is destroyed and with it all it's services.  Scoped and Transient services that implement `IDisposable/IAsyncDisposable` will be disposed.

You can see this in the console log. The `ServiceId` for the `RenderModeProvider` instance is different between the HttpRequest context and the Blazor Server Hub Context.  
 
In the `App` setup above the interactive parts of the page are the `HeadOutlet` and `Routes` components.  `HeadOutlet` is a short stub which we'll cover later.  The SPA [running in the Blazor Server Hub session] renderer creates a RenderTree with the `Routes` component as it's root.  The `Router` and `MainLayout` are in this tree.  Importantly, `Router` runs within the SPA and listens for navigation events raised by the `NavigationManager` scoped service provided by the SPA context's service container. Routing is just the changing out of the rendered component in the layout.  There's no round trips to the server.

You will see the pre-render of the intial page, and pre-rendering when you click `<F5>` and reset the SPA session.

```text
Routes - Pre-Rendered - ServiceId: 54da
MainLayout - Pre-Rendered - ServiceId: 54da
Home - Pre-Rendered - ServiceId: 54da
Routes - Blazor Server Rendered - ServiceId: f299
MainLayout - Blazor Server Rendered - ServiceId: f299
Home - Blazor Server Rendered - ServiceId: f299
Weather - Blazor Server Rendered - ServiceId: f299

==> Hit <F5>

Routes - Pre-Rendered - ServiceId: e20c
MainLayout - Pre-Rendered - ServiceId: e20c
Weather - Pre-Rendered - ServiceId: e20c
Routes - Blazor Server Rendered - ServiceId: c8e3
MainLayout - Blazor Server Rendered - ServiceId: c8e3
Weather - Blazor Server Rendered - ServiceId: c8e3
```

You can turn off pre-rendering in `App`:

```csharp
//...
    <Routes @rendermode="new InteractiveServerRenderMode(false)" />
    @* <Routes /> *@
//...
```

And you get:

```text
==> No Pre-rendering

Routes - Blazor Server Rendered - ServiceId: e74b
MainLayout - Blazor Server Rendered - ServiceId: e74b
Home - Blazor Server Rendered - ServiceId: e74b
Counter - Blazor Server Rendered - ServiceId: e74b
Weather - Blazor Server Rendered - ServiceId: e74b
```

### Blazor Server Summary

What we did here is set the render mode globally for the application.  We did this by setting  Interactivity root to the highest component in the application.  This overrides settings in any lower level components.  

However, note that if you set any component in the RenderTree to  `InteractiveWebAssembly` you will generate an exception at runtime when you navigate to the page:

```text
 Cannot create a component of type 'Blazor.Auto.Client.Pages.Counter' because its render mode 'Microsoft.AspNetCore.Components.Web.InteractiveWebAssemblyRenderMode' is not supported by interactive server-side rendering.
 ```

## Blazor Web Assembly [Hosted]

Change `App` to `InteractiveWebAssembly`.

```csharp
//...
    <HeadOutlet @rendermode="InteractiveWebAssembly" />
    @* <HeadOutlet /> *@
//...
    <Routes @rendermode="InteractiveWebAssembly" />
    @* <Routes /> *@
//...
```

When you run this:

```csharp
Routes - Pre-Rendered - ServiceId: 67b0
MainLayout - Pre-Rendered - ServiceId: 67b0
Home - Pre-Rendered - ServiceId: 67b0
```

And then an exception:

```text
Root component type 'Blazor.Auto.Components.Routes' could not be found in the assembly 'Blazor.Auto'.
```

The problem is that the client code [in the Client Project] can't see `Routes`.  It's in the `Server` project.

To fix this we need to move `Routes` and the layout files to the client project. Move:
1. *Routes.razor* to the Client Project root folder.
1. *Layout* folder to the Client Project root folder.

Now when you run the application you will see the page intitally rendered, and then a *Not Found* message in the browser. 

The logs will look like this:

```text
Routes - Pre-Rendered - ServiceId: eb1d
MainLayout - Pre-Rendered - ServiceId: eb1d
Home - Pre-Rendered - ServiceId: eb1d
```

And in the browser console:

```text
Routes - Blazor WebAssembly Rendered - ServiceId: 103c
```

Everything renders correctly in pre-render mode, but when the SPA session starts in the browser you can see `Routes` being rendered, but `Router` can't find the `/` route.  That's because *Home.razor* is in the Server project.

Move `Home` and `Weather` to the *Pages* folder in Client project.  Rebuild the solution.

Everything should not work

Server Console:

```text
Routes - Pre-Rendered - ServiceId: f22d
MainLayout - Pre-Rendered - ServiceId: f22d
Home - Pre-Rendered - ServiceId: f22d

==> Navigating in the SPA does not cause a round trip to the server
```

Browser Console:

```text
Routes - Blazor WebAssembly Rendered - ServiceId: 1e28
MainLayout - Blazor WebAssembly Rendered - ServiceId: 1e28
Home - Blazor WebAssembly Rendered - ServiceId: 1e28

==> Navigating in the SPA

Counter - Blazor WebAssembly Rendered - ServiceId: 1e28
Weather - Blazor WebAssembly Rendered - ServiceId: 1e28
Counter - Blazor WebAssembly Rendered - ServiceId: 1e28
Home - Blazor WebAssembly Rendered - ServiceId: 1e28
Counter - Blazor WebAssembly Rendered - ServiceId: 1e28
Weather - Blazor WebAssembly Rendered - ServiceId: 1e28
```

As with Server, you can turn off pre-rendering with:

```csharp
//...
    <Routes @rendermode="new InteractiveWebAssemblyRenderMode(false)" />
    @* <Routes /> *@
//...
```

However, I would suggest you detect pre-rendering in `Router` and provide some sort of loading screen  
### Blazor WebAssembly Summary

This is the NetCore hosted version of Blazor WebAssembly.  We've had to move all content components into the *Client* project.  We can't reference the *Server* project from the *Client* project for two reasons: the *Server* project already references the *Client* project, and even without that the *Server* project comntains some WebAssembly incompatible assemblies that will cause compilation errors.

As with the server project we've set the solution to global `WebAssembly` mode and set our interactivity root to `Routes`.


## Per Page/Component Interactivity

Set `App` and rebuild the solution.

```csharp
//...
    <HeadOutlet/>
//...
    <Routes/>
//...
```

Check the render mode on `Counter`.  If you set up the solution as I suggested you should have [If not the set it as shown]:

```csharp
@page "/counter"
@inherits LoggingComponentBase
@rendermode InteractiveAuto

<PageTitle>Counter</PageTitle>

<h1>Counter</h1>
```

Run the Solution.

Server Console:

```csharp
Routes - Pre-Rendered - ServiceId: c323
MainLayout - Pre-Rendered - ServiceId: c323
Home - Pre-Rendered - ServiceId: c323

==> Navigate to Weather 

Routes - Pre-Rendered - ServiceId: cec0
MainLayout - Pre-Rendered - ServiceId: cec0
Weather - Pre-Rendered - ServiceId: cec0

==> Navigate to Counter 

Routes - Pre-Rendered - ServiceId: 76aa
MainLayout - Pre-Rendered - ServiceId: 76aa
Counter - Pre-Rendered - ServiceId: 76aa
```

Client Console:

```csharp
Counter - Blazor WebAssembly Rendered - ServiceId: 6122
```

We can now see:

 - Home and Weather are being statically rendered.  No Interactivity. 
 - Counter is initially statically rendered, but switches Web Assembly once the SPA loads.

 Note that only the `Counter` component is being rendered in Web Assembly.  `Routes` and `MainLayout` are only statically rendered.  The interactivity root is `Counter`.

 If you hit `<F5>` in the counter page and try and navigate to *Home* quickly, nothing happens.  The WebAssembly SPA is loading and the UI is unresponsive.

 Now take a look at the Service Id's.  They are new for each navigation event.

 Set *Home* to `InteractiveServer`:

 ```csharp
 @page "/"
@inherits LoggingComponentBase
@rendermode InteractiveServer

<PageTitle>Home</PageTitle>
```

```text
Routes - Pre-Rendered - ServiceId: e9d5
MainLayout - Pre-Rendered - ServiceId: e9d5
Home - Pre-Rendered - ServiceId: e9d5
Home - Blazor Server Rendered - ServiceId: aeb7

==> navigated to Counter

Routes - Pre-Rendered - ServiceId: 840a
MainLayout - Pre-Rendered - ServiceId: 840a
Counter - Pre-Rendered - ServiceId: 840a
Counter - Blazor Server Rendered - ServiceId: aeb7
```

Home is now statically rendered and then rendered in Blazor Server.  When you navigate to *Counter* it's now rendered in Blazor Server mode, not WebAssembly.

In all cases `Routes` and `MainLayout` are statically rendered.

This has a significant impact on services.  The scoped service set used by `Router` is that from the HttpRequest context.  Those used by interactive pages are SPA context services.

Consider Authentication/Authorization.  The Authentication services used by `Router` and `NavMenu` are a different set to those used interactiv pages.  Futhermore, any cascaded parameters set up in `Routes` or `MainLayout` don't exist in interactive pages.  It's the RenderTree and Renderer that keep track of cascaded parameters.  `Routes` and `MainLayout` don't exist in a renderTree whose root is the page.  `CascadingAuthenticationState` is a very good example.

## Conclusions

Knowing what I've discovered since Net8 arrived, I would not embark on a mixed mode application unless I was doing a phased migration of an existing server side rendered application.  Even then I would be using a very modular approach, moving whole logic sections at a time.

I would also not embark on a mixed Web Assembly/Server project.

In either case, you really do need to know what you're doing taking amixed mode journey.  You need to seriously consider complexity vs gain.
