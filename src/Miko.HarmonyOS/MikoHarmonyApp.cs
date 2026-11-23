using Miko.Hosting;
using Miko.Platform;
using OpenHarmony.Hosting;
using OpenHarmony.Views;

namespace Miko.HarmonyOS;

/// <summary>C# adapter from the reusable OpenHarmony SurfaceView host to Miko.</summary>
public static class MikoHarmonyApp
{
    public static nint Initialize(Func<MikoAppContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        return SurfaceApplication.Initialize(() => new MikoSurfaceView(contextFactory()));
    }

    private sealed class MikoSurfaceView(MikoAppContext app) : SurfaceView
    {
        private readonly HarmonyHost _host = new(app);
        private string? _renderedRoute;
        public override void OnCreated() => _host.CreateGraphics(GraphicsContext.GetProcAddress);
        public override bool OnFrame(SurfaceFrame frame)
        {
            if (!_host.Render(frame.Width, frame.Height, frame.Density, frame.Seconds)) return false;
            if (_renderedRoute != _host.CurrentRoute)
            {
                _renderedRoute = _host.CurrentRoute;
                SurfaceApplication.Log($"MIKO_HARMONY rendered route={_renderedRoute} pixels={frame.Width}x{frame.Height} density={frame.Density}");
            }
            return true;
        }
        public override void OnDestroyed() => _host.Dispose();
        public override void OnTouch(int action, int id, float x, float y) => _host.Touch(action, id, x, y);
        public override bool OnBack() => _host.Back();
        public override void OnActiveChanged(bool active) => _host.SetActive(active);
        public override void OnText(string text) => _host.Text(text);
        public override void OnKey(EditingKey key) => _host.Key(key switch
        {
            EditingKey.Backspace => MikoKey.Backspace, EditingKey.Delete => MikoKey.Delete,
            EditingKey.Left => MikoKey.Left, EditingKey.Right => MikoKey.Right,
            EditingKey.Home => MikoKey.Home, EditingKey.End => MikoKey.End,
            EditingKey.Enter => MikoKey.Enter, _ => MikoKey.Unknown
        });
        public override TextEditorState GetTextEditorState() => _host.GetTextEditorState();
        public override void OnNavigate(string path) => _host.Navigate(path);
    }
}
