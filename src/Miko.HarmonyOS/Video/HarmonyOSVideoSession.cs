using Miko.Platform.Video;
using OpenHarmony;
using OpenHarmony.Hosting;
using OpenHarmony.Media;

namespace Miko.HarmonyOS;

internal sealed class HarmonyOSVideoSession : IVideoSession
{
    private readonly MediaPlayer _player;
    private readonly HarmonyOSVideoFrameSource _frames;
    private bool _loaded, _disposed;
    private string? _error;
    public HarmonyOSVideoSession(VideoSourceDescriptor source, VideoSessionOptions options)
    {
        var view = SurfaceApplication.Current ?? throw new InvalidOperationException("A HarmonyOS SurfaceView is required.");
        _player = new MediaPlayer(source.ResolveForBackend(), view.Dispatcher);
        _frames = new HarmonyOSVideoFrameSource(_player, view.GraphicsContext, Fail);
        _frames.FrameAvailable += () => Event?.Invoke(new VideoSessionEvent.FrameAvailable(Position));
        _player.StateChanged += OnStateChanged;
        _player.SeekCompleted += () => Event?.Invoke(new VideoSessionEvent.StateChanged());
        _player.PlaybackEnded += () => Event?.Invoke(new VideoSessionEvent.Ended());
        _player.Failed += error => Fail(error.Message);
        _player.Volume = float.IsFinite(options.InitialVolume) ? options.InitialVolume : 0;
        _player.Muted = options.Muted;
        _player.Loop = options.Loop;
        if (options.AutoPlay) _player.Play();
    }
    public event Action<VideoSessionEvent>? Event;
    public VideoSessionState State => _error != null ? VideoSessionState.Error : _player.State switch
    {
        MediaPlayerState.Ready => VideoSessionState.Ready,
        MediaPlayerState.Playing => VideoSessionState.Playing,
        MediaPlayerState.Paused => VideoSessionState.Paused,
        MediaPlayerState.Ended => VideoSessionState.Ended,
        MediaPlayerState.Error => VideoSessionState.Error,
        _ => VideoSessionState.Loading
    };
    public TimeSpan Duration => _player.Duration;
    public TimeSpan Position => _disposed || _error != null ? TimeSpan.Zero : _player.Position;
    public int VideoWidth => _player.VideoWidth;
    public int VideoHeight => _player.VideoHeight;
    public bool IsBuffering => State == VideoSessionState.Loading;
    public string? ErrorMessage => _error;
    public IVideoFrameSource FrameSource => _frames;
    public void Play() => Control(_player.Play);
    public void Pause() => Control(_player.Pause);
    public void Seek(TimeSpan position) { if (State != VideoSessionState.Loading) Control(() => _player.Seek(position)); }
    public void SetVolume(float volume) => Control(() => _player.Volume = float.IsFinite(volume) ? volume : 0);
    public void SetMuted(bool muted) => Control(() => _player.Muted = muted);
    public void SetLoop(bool loop) => Control(() => _player.Loop = loop);
    public void SetPlaybackRate(float rate) { if (float.IsFinite(rate) && rate > 0) Control(() => _player.Rate = rate); }
    private void Control(Action action)
    {
        if (_disposed || _error != null) return;
        try { action(); } catch (PlatformException error) { Fail(error.Message); }
    }
    private void OnStateChanged()
    {
        if (!_loaded && _player.State == MediaPlayerState.Ready)
        {
            _loaded = true;
            Event?.Invoke(new VideoSessionEvent.Loaded(VideoWidth, VideoHeight, Duration));
        }
        Event?.Invoke(new VideoSessionEvent.StateChanged());
    }
    private void Fail(string message)
    {
        if (_disposed || _error != null) return;
        _error = message;
        Event?.Invoke(new VideoSessionEvent.Error(message, null));
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _player.Dispose(); // Stop the producer and release its surface lease first.
        _frames.Dispose();
        Event = null;
    }
}
