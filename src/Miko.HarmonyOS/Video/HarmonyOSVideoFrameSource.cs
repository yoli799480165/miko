using Miko.Graphics;
using Miko.Platform.Video;
using OpenHarmony.Graphics;
using OpenHarmony.Media;

namespace Miko.HarmonyOS;

internal sealed class HarmonyOSVideoFrameSource(MediaPlayer player, EglContext context, Action<string> fail) : IVideoFrameSource, IDisposable
{
    private NativeImage? _output;
    private Image? _image;
    private bool _disposed, _failed;
    internal event Action? FrameAvailable;
    public bool FlipVertically { get; private set; }
    public Image? AcquireCurrentFrame(GraphicsDevice device)
    {
        if (_disposed || _failed || device.Backend != GraphicsBackend.OpenGl) return null;
        try
        {
            if (_output == null)
            {
                _output = new NativeImage(context);
                _output.FrameAvailable += () => FrameAvailable?.Invoke();
                player.SetVideoSurface(_output.Surface);
                _ = ObservePreparation(player.PrepareAsync());
            }
            if (player.VideoWidth > 0 && player.VideoHeight > 0 && _output.TryAcquireFrame(out var frame))
            {
                FlipVertically = frame.TextureTransform.M22 < -.5f && frame.TextureTransform.M11 > .5f;
                var image = device.WrapGlTexture(NativeImage.TextureTarget, frame.TextureId, 0x8058,
                    player.VideoWidth, player.VideoHeight, ImagePixelFormat.Rgbx8888);
                if (image != null) { _image?.Dispose(); _image = image; }
            }
            return _image;
        }
        catch (Exception error) { _failed = true; fail(error.Message); return null; }
    }
    private async Task ObservePreparation(Task task)
    {
        try { await task.ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        catch (Exception error) { context.Dispatcher.Post(() => { if (!_disposed) fail(error.Message); }); }
    }
    public void ReleaseCurrentFrame() { }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        FrameAvailable = null;
        _image?.Dispose(); _image = null;
        _output?.Dispose(); _output = null;
    }
}
