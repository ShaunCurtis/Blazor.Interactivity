namespace Blazr.UI.Common;

public interface IRenderModeProvider
{
    public string Id { get; }
    public ComponentRenderMode RenderMode { get; }

    public string Mode => RenderMode switch
    {
        ComponentRenderMode.Server => "Blazor Server Rendered",
        ComponentRenderMode.WebAssembly => "Blazor WebAssembly Rendered",
        _ => "Pre-Rendered"
    };
}


public class ServerRenderModeProvider : IRenderModeProvider
{
    private readonly Guid _id = Guid.NewGuid();
    private IHttpContextAccessor _httpContextAccessor;
    public string Id => _id.ToString().Substring(0, 4);

    public ComponentRenderMode RenderMode =>
        !(_httpContextAccessor.HttpContext is not null && _httpContextAccessor.HttpContext.Response.HasStarted)
            ? ComponentRenderMode.PreRender
            : ComponentRenderMode.Server;

    public ServerRenderModeProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
}

public class WebAssemblyRenderModeProvider : IRenderModeProvider
{
    private readonly Guid _id = Guid.NewGuid();
    public string Id => _id.ToString().Substring(0, 4);

    public ComponentRenderMode RenderMode => ComponentRenderMode.WebAssembly;
}

public enum ComponentRenderMode
{
    PreRender,
    Server,
    WebAssembly
}
