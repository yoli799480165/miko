using System.Text.Json;
using System.Text.Json.Serialization;
using Miko.Platform;
using OpenHarmony.Views;

namespace Miko.HarmonyOS;

internal sealed class HarmonyInputMethod : InputMethodBase
{
    private InputMethodState? _state;
    private int _revision;
    private int _showRequest;
    private string? _serialized;
    public override void SetState(InputMethodState? state)
    {
        if (_state == state) return;
        _state = state;
        _revision++;
        _serialized = null;
    }

    public override void ShowKeyboard() { _showRequest++; _revision++; _serialized = null; }
    public void Insert(string text) => CommitText(text);
    public TextEditorState GetTextEditorState() => new(_revision, _state != null, _state?.Text ?? "", _state?.CursorPosition ?? 0,
        _state?.InputType switch
        {
            InputMethodType.Password => TextInputType.Password,
            InputMethodType.Number => TextInputType.Number,
            InputMethodType.Multiline => TextInputType.Multiline,
            _ => TextInputType.Text
        }, _showRequest);
    public string Serialize() => _serialized ??= JsonSerializer.Serialize(new HarmonyEditorState(
        _revision, _state != null, _state?.Text ?? "", _state?.CursorPosition ?? 0,
        (int)(_state?.InputType ?? InputMethodType.Text), _showRequest), HarmonyJsonContext.Default.HarmonyEditorState);
}

internal sealed record HarmonyEditorState(int Revision, bool Focused, string Text, int Cursor, int InputType, int ShowRequest);

[JsonSerializable(typeof(HarmonyEditorState))]
internal partial class HarmonyJsonContext : JsonSerializerContext;
