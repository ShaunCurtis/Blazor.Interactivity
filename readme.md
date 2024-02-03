# Blazor Interactivity

Net 8 introduced the concept of *Interactivity* and provided ways to set it.  It's blurred the boundaries between Blazor Server, Blazor WebAssemby and classic server side applications. 

  Great, but it's been a double edged sword.  Many programmers don't understand the complexity and pitfalls in moving from one of the classic modes to hybrid mode operation.  In this article I'll try and help you understand what *interactivity* really is, and it's impact on rendering and services.

## Demo Solution

If you want to walk along with this discussion create a Blazor Solution using the *Blazor Web App* template.  Set *Interactive Render Mode* to WebAssembly and *Interactivity Location* to Global.

##  Blazor Server

The original mode of operation.

In the Server project modify `App.razor` by changing the `rendermode` on `HeadOutlet` and `Routes` to `@rendermode="InteractiveServer"`.

```csharp
//...
    <HeadOutlet @rendermode="InteractiveServer" />
    @* <HeadOutlet /> *@
//...
    <Routes @rendermode="InteractiveServer" />
    @* <Routes /> *@
//...
```

Update the Server project program to include Interactive server rendering.

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddInteractiveServerComponents();

//...

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Blazor.Interactivity.Client._Imports).Assembly);
```

Run the solution.

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

Everything is initially pre-rendered in the *HttpRequest Context*.  Once the static page downloads to the client, the Blazor client JS code establishes a *SignalR* session with the Blazor Hub running on the server.  This sets up a SPA session, establishes the interactivity root components, builds the render trees and  passes the DOM changes back to the client to apply to the DOM.

### Services

It's important to understand that services are injected into components from their render context.  Scoped and Transient Services injected into components rendered by a RenderTree in the HttpRequest Context, come from the HttpRequest's Scoped container.  This container only exists for the duration of the HttpRequest context.

You can see this in the console log. The `ServiceId` for the `RenderModeProvider` instance is different between the HttpRequest context and the Blazor Server Hub Context.  

## Interactivity

In `App` the interactive components are `HeadOutlet` and `Routes`.  `HeadOutlet` is a short stub which we'll cover later.  The Blazor Server Hub session [SPA] renderer creates a RenderTree with `Routes` as it's root.  The `Router` and `MainLayout` are in this tree.  Importantly, `Router` runs within the SPA and listens for navigation events raised by the `NavigationManager` scoped service provided by the SPA context's service container. Routing is the changing out of the rendered component in the layout: there are no round trips to the server.

You can see the pre-render of the initial page when you click `<F5>` and reset the SPA session.

```text
Routes - Pre-Rendered - ServiceId: 54da
MainLayout - Pre-Rendered - ServiceId: 54da
Home - Pre-Rendered - ServiceId: 54da
Routes - Blazor Server Rendered - ServiceId: f299
MainLayout - Blazor Server Rendered - ServiceId: f299
Home - Blazor Server Rendered - ServiceId: f299
Weather - Blazor Server Rendered - ServiceId: f299

==> Hit <F5> on Weather

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

We set the render mode globally for the application by setting the interactivity root to the highest component in the application.  This overrides settings in any lower level components.  

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

When you run this you see the pre-render on the server, and then the initial re-render on the client.  All subsequent navigation events are restricted to the SPA session running on the browser. 

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

However, I would suggest you detect pre-rendering in `Router` and provide some sort of loading screen.

Some points:

1. All the components are in the Client project.  They can't reside in the Server project for two reasons: the *Server* project already references the *Client* project, and even without that the *Server* project contains some WebAssembly incompatible assemblies that will cause compilation errors.


## Per Page/Component Interactivity

Set `App` and rebuild the solution.

```csharp
//...
    <HeadOutlet/>
//...
    <Routes/>
//...
```

And make sure there are no `@rendermode` settings on any of the pages. 

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

Everything is now being statically rendered.  There's no interactivity.

Update Counter:

 ```csharp
 @page "/counter"
@inherits LoggingComponentBase
@rendermode InteractiveAuto
```

What you will now see is:

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

What we see is:

 1. Home and Weather are being statically rendered.  No Interactivity.
 
 2. Counter is initially statically rendered, but switches to Web Assembly once the SPA loads.

 3. Only the `Counter` component is being rendered in Web Assembly.  `Routes` and `MainLayout` are statically rendered.  The interactivity root is `Counter`.

 If you hit `<F5>` in the counter page and try and navigate to *Home* quickly, nothing happens.  The WebAssembly SPA is loading and the UI is unresponsive.

 Now take a look at the Service Id's.  They are new for each navigation event.

 Set *Home* to `InteractiveServer`:

 ```csharp
 @page "/"
@inherits LoggingComponentBase
@rendermode InteractiveServer

<PageTitle>Home</PageTitle>
```

Now we see:

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

Home is initially statically rendered and then rendered in Blazor Server.  When you navigate to *Counter* it's now rendered in Blazor Server mode, not WebAssembly.

In all cases `Routes` and `MainLayout` are statically rendered.

This has a significant impact on services.  The scoped service set used by `Router` is that from the HttpRequest context.  Those used by interactive pages are SPA context services.

Consider Authentication/Authorization.  The Authentication services used by `Router` and `NavMenu` are a different set to those used interactive pages.  Furthermore, any cascaded parameters set up in `Routes` or `MainLayout` don't exist in interactive pages.  It's the RenderTree and Renderer that keep track of cascaded parameters.  `Routes` and `MainLayout` don't exist in a renderTree whose root is the page.  `CascadingAuthenticationState` is a very good example.

## Conclusions

Knowing what I've discovered since Net8 arrived, I would not embark on a mixed mode application unless I was doing a phased migration of an existing server side rendered application.  Even then I would be using a very modular approach with a few steps as possible, migrating whole logical sections at a time.

I would also not embark on a mixed Web Assembly/Server project, without a lot of money, a large built in contingency, and a very compelling reason to do.

In either case, you really do need to know what you're doing taking a mixed mode journey.  You need to seriously consider complexity vs gain.
