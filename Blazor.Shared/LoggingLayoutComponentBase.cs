namespace Blazor.Shared;

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
