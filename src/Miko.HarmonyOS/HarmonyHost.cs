using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Graphics;
using Miko.Fonts;
using Miko.Hosting;
using Miko.Platform;
using Miko.Routing;

namespace Miko.HarmonyOS;

/// <summary>Owns one Miko application and its GL resources. All calls run on the XComponent UI thread.</summary>
public sealed class HarmonyHost : IDisposable
{
    private readonly MikoAppContext _app;
    private readonly MikoDispatcher _dispatcher;
    private readonly HarmonyTouchTracker _touch = new();
    private readonly HarmonyInputMethod _input = new();
    private GraphicsDevice? _device;
    private WindowRenderTarget? _target;
    private bool _initialized;
    private bool _disposed;
    private float _density = 1;
    private int _pixelWidth;
    private int _pixelHeight;
    private double _lastTime;

    public HarmonyHost(MikoAppContext app)
    {
        _app = app ?? throw new ArgumentNullException(nameof(app));
        _dispatcher = app.Services.GetRequiredService<MikoDispatcher>();
        app.Services.GetRequiredService<PlatformInfo>().Platform = HostPlatform.HarmonyOS;
        app.Controller.AttachInputMethod(_input);
        if (app.Controller.ScrollBehavior is DefaultScrollBehavior)
            app.Controller.SetScrollBehavior(new ElasticScrollBehavior(new InertialScrollBehavior(friction: 4)));
    }

    public void CreateGraphics(Func<string, IntPtr> getProcAddress)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_device != null) throw new InvalidOperationException("The surface is already attached.");
        _device = GraphicsDevice.CreateOpenGl(getProcAddress);
        _device.ResourceCacheLimit = GraphicsDevice.DefaultResourceCacheLimit;
        _target = _device.CreateWindowRenderTarget();
        _app.Engine.GraphicsDevice = _device;
    }

    public bool Render(int width, int height, float density, double seconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (width <= 0 || height <= 0 || !float.IsFinite(density) || density <= 0 || _target == null) return false;
        if (_app.Controller.IsSuspended) return false;
        if (_pixelWidth != width || _pixelHeight != height || _density != density)
            _app.Engine.RequestRepaint();
        _pixelWidth = width;
        _pixelHeight = height;
        _density = density;
        var logicalWidth = width / density;
        var logicalHeight = height / density;
        _app.Controller.SetViewportSize(logicalWidth, logicalHeight);
        // Drain even when idle: completion of an awaited operation can create the next frame.
        _dispatcher.Drain();
        var delta = _lastTime == 0 ? 1f / 60 : (float)Math.Max(0, seconds - _lastTime);
        _lastTime = seconds;
        if (_initialized && !_app.Controller.HasPendingWork) return false;
        var surface = _target.Acquire(width, height, 0, 0, 8)
            ?? throw new InvalidOperationException("Cannot wrap the HarmonyOS window framebuffer.");
        if (!_initialized)
        {
            _app.RegisterFonts();
            RegisterSystemFonts();
            _app.Controller.Initialize(surface.Context, logicalWidth, logicalHeight);
            _app.Controller.RunPostInitHooks();
            _initialized = true;
        }
        _app.Controller.RenderFrame(surface.Context, logicalWidth, logicalHeight, delta, canvas =>
        {
            canvas.Clear(_app.Engine.GetRootBackgroundColor() ?? Color.White);
            var saved = canvas.Save();
            try
            {
                canvas.Scale(density, density);
                _app.Engine.Render(canvas);
            }
            finally { canvas.RestoreToCount(saved); }
        });
        _device!.Flush();
        return true;
    }

    public void Touch(int action, int id, float x, float y) => _touch.Dispatch(_app.Controller, action, id, x, y, _density);
    public bool Back() => _app.Controller.OnBackButton();
    public void SetActive(bool active)
    {
        if (active) { _lastTime = 0; _app.Controller.Resume(); }
        else { _touch.Cancel(_app.Controller); _app.Controller.Suspend(); }
    }
    public void Text(string text) => _input.Insert(text);
    public void Key(MikoKey key) { _app.Controller.OnKeyDown(key, MikoKeyModifiers.None); _app.Controller.OnKeyUp(key, MikoKeyModifiers.None); }
    public string EditorState() => _input.Serialize();
    public OpenHarmony.Views.TextEditorState GetTextEditorState() => _input.GetTextEditorState();
    public string CurrentRoute => _app.Services.GetRequiredService<NavigationManager>().CurrentPath;
    public void Navigate(string path) => _app.Services.GetRequiredService<NavigationManager>().NavigateTo(path);

    private static void RegisterSystemFonts()
    {
        var fonts = FontManager.Instance;
        // The OHOS Skia build does not discover every system family by name. Register
        // the engine's default and CSS generic aliases as well: layout also measures
        // whitespace/line metrics directly through the requested primary family.
        foreach (var (family, file) in new[] {
            ("Arial", "/system/fonts/HarmonyOS_Sans.ttf"),
            ("sans-serif", "/system/fonts/HarmonyOS_Sans.ttf"),
            ("system-ui", "/system/fonts/HarmonyOS_Sans.ttf"),
            ("monospace", "/system/fonts/RobotoMono-Regular.ttf"),
            ("HarmonyOS Sans", "/system/fonts/HarmonyOS_Sans.ttf"),
            ("HarmonyOS Sans SC", "/system/fonts/HarmonyOS_Sans_SC.ttf") })
        {
            if (fonts.IsFontRegistered(family) || !File.Exists(file)) continue;
            fonts.RegisterFont(family, file);
            if (!fonts.DefaultFallbackChain.Contains(family)) fonts.DefaultFallbackChain.Add(family);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _touch.Cancel(_app.Controller);
        _app.Controller.AttachInputMethod(null);
        // Media/component cleanup may still use the graphics device. Keep EGL current until done.
        _app.Dispose();
        _app.Engine.GraphicsDevice = null;
        _target?.Dispose();
        _device?.Dispose();
        if (_app.Services is IDisposable services) services.Dispose();
    }
}
