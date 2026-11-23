using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Core.DomElements;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Platform;
using Miko.Styling;
using Shouldly;

namespace Miko.HarmonyOS.Tests;

public sealed class HarmonyHostTests
{
    [Fact]
    public void Should_ConvertPhysicalTouchCoordinates_AndIgnoreSecondaryFingers()
    {
        var input = new InputElement { Type = InputType.Range, Min = 0, Max = 100, NumericValue = 0,
            Style = new Style { Width = Length.Px(200), Height = Length.Px(20) } };
        using var app = MikoAppBuilder.CreateDefault().UseRootComponent(() => new DivElement { Children = { input } }).Build();
        using var host = new HarmonyHost(app);
        using var surface = GraphicsDevice.Software.CreateSurface(400, 200)!;
        app.Controller.Initialize(surface.Context, 400, 200);
        var touch = new HarmonyTouchTracker();
        touch.Dispatch(app.Controller, 0, 7, 292, 20, 2);
        input.NumericValue.ShouldBe(75, 1);
        touch.Dispatch(app.Controller, 0, 8, 16, 20, 2);
        touch.Dispatch(app.Controller, 2, 8, 16, 20, 2);
        touch.Dispatch(app.Controller, 1, 8, 16, 20, 2);
        input.NumericValue.ShouldBe(75, 1);
        touch.ActivePointer.ShouldBe(7);
        touch.Dispatch(app.Controller, 1, 7, 292, 20, 2);
        touch.ActivePointer.ShouldBeNull();
    }

    [Fact]
    public void Should_CancelGestureWithoutTogglingCheckbox()
    {
        var input = new InputElement { Type = InputType.Checkbox,
            Style = new Style { Width = Length.Px(40), Height = Length.Px(40) } };
        using var app = MikoAppBuilder.CreateDefault().UseRootComponent(() => new DivElement { Children = { input } }).Build();
        using var host = new HarmonyHost(app);
        using var surface = GraphicsDevice.Software.CreateSurface(200, 200)!;
        app.Controller.Initialize(surface.Context, 200, 200);
        var touch = new HarmonyTouchTracker();
        touch.Dispatch(app.Controller, 0, 1, 10, 10, 1);
        touch.Dispatch(app.Controller, 3, 1, 10, 10, 1);
        touch.Dispatch(app.Controller, 1, 1, 10, 10, 1);
        input.Checked.ShouldBeFalse();
        touch.ActivePointer.ShouldBeNull();
        touch.Dispatch(app.Controller, 0, 2, 10, 10, 1);
        touch.Dispatch(app.Controller, 1, 2, 10, 10, 1);
        input.Checked.ShouldBeTrue();
    }

    [Fact]
    public void Should_ExposeMobilePlatform_AndSuspendResumeController()
    {
        using var app = MikoAppBuilder.CreateDefault().UseRootComponent(() => new DivElement()).Build();
        using var host = new HarmonyHost(app);
        app.Services.GetRequiredService<IPlatformInfo>().Platform.ShouldBe(HostPlatform.HarmonyOS);
        app.Services.GetRequiredService<IPlatformService>().Platform.ShouldBe(HostPlatform.HarmonyOS);
        PlatformScrollbars.GetDefault(HostPlatform.HarmonyOS).ShouldBe(ScrollbarStyle.None);
        host.SetActive(false);
        app.Controller.IsSuspended.ShouldBeTrue();
        host.SetActive(true);
        app.Controller.IsSuspended.ShouldBeFalse();
        app.Engine.HasPendingVisualWork.ShouldBeTrue();
    }

    [Fact]
    public void Should_ReopenKeyboardWithoutChangingText_AndSerializeUnicodeForNativeAot()
    {
        var method = new HarmonyInputMethod();
        var state = new InputMethodState("鸿蒙😀", 4, false, InputMethodType.Text, new RectF(1, 2, 3, 4));
        method.SetState(state);
        var initial = method.Serialize();
        method.SetState(state);
        method.Serialize().ShouldBe(initial);
        method.ShowKeyboard();
        using var json = JsonDocument.Parse(method.Serialize());
        json.RootElement.GetProperty("Text").GetString().ShouldBe("鸿蒙😀");
        json.RootElement.GetProperty("ShowRequest").GetInt32().ShouldBe(1);
        method.SetState(null);
        using var blurred = JsonDocument.Parse(method.Serialize());
        blurred.RootElement.GetProperty("Focused").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void Should_CommitTextAndEditingKeysThroughController()
    {
        var input = new InputElement { Style = new Style { Width = Length.Px(200), Height = Length.Px(40) } };
        using var app = MikoAppBuilder.CreateDefault().UseRootComponent(() => new DivElement { Children = { input } }).Build();
        using var host = new HarmonyHost(app);
        using var surface = GraphicsDevice.Software.CreateSurface(300, 200)!;
        app.Controller.Initialize(surface.Context, 300, 200);
        host.Touch(0, 1, 10, 10);
        host.Touch(1, 1, 10, 10);
        host.Text("Harmony鸿蒙");
        input.Value.ShouldBe("Harmony鸿蒙");
        host.Key(MikoKey.Backspace);
        input.Value.ShouldBe("Harmony鸿");
    }
}
