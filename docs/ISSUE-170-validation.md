# ISSUE-170：HarmonyOS 平台实现与验证

验证日期：2026-11-20；Framework 迁移复验：2026-11-21；正式 workload 升级复验：2026-11-23。

## 2026-11-23：正式 workload 11.0.0

- Anime.HarmonyOS 和 IonicDemo.HarmonyOS 改为标准 `Microsoft.NET.Sdk` + `net11.0-ohos`。
- 移除应用中的 `Player.Sdk` 引用、手动 NativeAOT/RID 配置、本地预览包源和跨仓库构建脚本依赖。
- Framework、SkiaSharp/HarfBuzzSharp HarmonyOS 原生包均使用 NuGet 正式版 `11.0.0`。
- `Miko.HarmonyOS` 继续作为 `net11.0` 适配类库，显式引用 Framework `11.0.0`，保留桌面托管测试；应用入口使用 workload TFM。
- 构建脚本调用标准 `dotnet publish` / `dotnet run`。输出目录为 `bin/<Configuration>/net11.0-ohos/<RID>/publish/`。
- 使用独立 SDK `G:/projects/claude/harmonyos-net/artifacts/dotnet-sdk11/dotnet.exe`，从 NuGet 安装 HarmonyOS `11.0.0`，CLI 自动识别本机 Native SDK API 26。
- 两个应用的 Release 编译与 x86_64 HAP 发布成功；Anime 经标准 `dotnet run` 完成签名、安装、启动，首帧和视频画面已检查。
- `Miko.HarmonyOS.Tests` 7 项通过；适配库打包成功。日志和截图位于 `artifacts/harmonyos-release-11/`。

本机 PATH 默认的 `C:/Program Files/dotnet` 是 MSI SDK，未安装 HarmonyOS workload；应用命令需选择上述独立 SDK，
或使用脚本的 `-Dotnet` 参数。下面保留此前迁移与验收记录；正式版的配置和验证范围以本节为准。

## Framework 迁移及复验

| 所属仓库 | 内容 |
| --- | --- |
| harmonyos-net / NDK.Bindings | AVPlayer、NativeImage listener/V2 matrix、NativeWindow、EGL 原始 ABI |
| harmonyos-net / OpenHarmony.Framework | `MediaPlayer`、`NativeImage`、`NativeWindow`、`EglContext`、`SurfaceView`、`SurfaceApplication`、线程队列及错误/资源生命周期 |
| harmonyos-net / Framework 包 | 两 ABI 的 `libopenharmony_framework.so`、通用 ArkTS Stage、native assets 和 HAP 打包 targets |
| miko-v5 / Miko.HarmonyOS | C# SurfaceView 渲染/输入适配、视频事件映射和 Skia texture 包装 |

Miko 不再维护 C++、ArkTS、CMake 或 Stage 模板；原视频 C++ bridge 已被 Framework 的直接 C# NDK 调用替代。
原源码在本地 `artifacts/framework-migration-baseline` 留有备份，不参与构建。
两个仓库通过本地 NuGet 制品连接，不使用跨仓库 ProjectReference。
`Player.Sdk` 依赖 Framework；`net11.0-ohos` 自动引用同版本 Framework，应用无需显式 PackageReference。

MediaPlayer 与 NativeImage/NativeWindow 分离，未来 Camera 等 producer 可以复用输出 surface。
Framework 不依赖 Miko、Skia 或 IVideoSession。所有能力在 owner thread 操作，native callback 复制数据并入队；
事件经 PlatformDispatcher 派发，销毁前撤销 callback cookie，先停止 producer，再释放 surface/GL texture。
纹理暴露完整矩阵和原始 producer timestamp；实测 AVPlayer timestamp 为微秒，播放时钟使用 Position。
Camera/Location/Bluetooth 尚未实现，后续按独立 namespace 加入，不以空实现占位。

迁移后验收结果：

- 无 Miko 的 `harmonyos-net/examples/FrameworkVideo` 在模拟器输出 Ready/Playing、320×180 视频帧、纹理 ID、timestamp 和完整矩阵；完成暂停、seek=2000ms、恢复、释放、损坏媒体报错及释放后重新播放，无 `OH_FRAMEWORK_ERROR`。
- Anime 的 x86_64 / arm64-v8a NativeAOT 和 HAP 构建通过；arm64 仅编译打包，未做真机运行。
- x86_64 模拟器确认播放/暂停、暂停 seek 后时钟更新、恢复后目标帧、循环、应用内全屏/退出、弹幕弹层/空内容校验、Home 后恢复、返回首页释放。
- 修复 paused seek 时 `OH_AVPlayer_GetCurrentTime` 暂时失败导致渲染停止的问题：`TryGetPosition` 表示采样是否成功，Position 保留最后有效/已接受的 seek 位置，真正媒体错误仍由 Failed 报告。
- `Anime.HarmonyOS/verify.ps1` 验证 8 个页面在同一进程中成功渲染并留存截图。IonicDemo 也通过新的打包/启动流程，并复验 checkbox、input、modal 页面。
- 独立 SDK 完成 workload 安装/卸载/恢复、隐式 Framework API 编译及两个 ABI 的 HAP 发布：相邻仓库 `artifacts/wl-test/29a69ea0/result.json`。
- Framework 新增 16 项线程队列、迟到回调、窗口 lease、倍速 ABI 测试；harmonyos-net 全部 95 项测试通过。
- Miko 回归：HarmonyOS 适配 7 项、核心 2889 项、Ionic 1788 项全部通过。原 HarmonyOS 倍速 ABI 测试已迁至 Framework。
- 包内容检查验证 managed API、两 ABI 原生库、Stage 和 targets；消费者不接收原生源码。两个 Anime HAP 都含 `libopenharmony_framework.so`，无 `libmiko.so`，ELF 架构匹配。
- 最终 `dotnet build miko.slnx -c Release`：0 errors、32 warnings；`Miko.HarmonyOS` 打包成功，确认依赖 Framework 且包中没有 C++/ArkTS 源文件。

证据位于 `artifacts/anime-framework-*`、`artifacts/ionic-framework-*`，Framework 日志位于相邻仓库 `artifacts/framework-*`。
关键截图为 `anime-framework-final-playback`、`paused-v3`、`seek-v3`、`resumed-v3`、`fullscreen-v3`、
`danmaku-validation`、`foreground`、`released`，8 个页面截图在 `artifacts/anime-framework-routes`。

当前限制：Miko 纹理适配仅处理已验证的竖直翻转；Framework 已保留完整矩阵，旋转/裁剪视频仍需扩展 Miko 渲染适配。
paused seek 的模拟器画面可能保留旧帧直到恢复。全屏为应用 viewport 全屏。网络/HLS、实际扬声器音频、HDR/DRM、
旋转视频和 arm64 真机未验收。宿主目前支持单 SurfaceView/单应用；Camera、Location、Bluetooth 和 Miko.Native 服务待后续实现。

以下保留 2026-11-20 的初版实现和验收记录；其中原生源码归属、测试数量以本节迁移结果为准。

## 已实现

- `src/Miko.HarmonyOS`：.NET 11 宿主、XComponent / EGL ES 2 / Skia GPU 渲染、按需重绘、
  DPI 与窗口尺寸变化、主触点跟踪与取消、惯性滚动、返回键、前后台生命周期、UI dispatcher、资源释放。
- ArkTS 输入法桥接提交文字、删除、光标移动、确认键和编辑状态；JSON 使用源码生成以支持 NativeAOT。
- 系统字体注册及默认字体别名，修复鸿蒙环境中英文空格与实际字形使用不同字体度量的问题。
- `examples/Ionic/IonicDemo.HarmonyOS`：共享 IonicDemo 的独立启动项目、Stage 壳、构建/签名/安装脚本、
  按路由验证并截图的脚本。普通 solution build 不要求安装 HarmonyOS workload。
- `Miko.HarmonyOS` 仅包含 C# 适配器；`Chaldea.OpenHarmony.Framework` 发布预编译原生宿主和 ArkTS Stage。
- `UseHarmonyOSVideo()`：系统 AVPlayer 播放本地文件与 HTTP(S) 视频，支持播放/暂停、seek、循环、
  音量/静音和系统支持的倍速。`OH_NativeImage` 将解码帧交给 EGL 外部纹理，由 Skia 合成控件与弹幕；
  读取 NativeImage 变换以修正视频上下方向，释放时依次销毁播放器、输出 surface 和纹理。
- `examples/App/Anime/Anime.HarmonyOS`：共享 Anime 应用的 NativeAOT 启动项目及构建、签名、安装脚本。
  修复 Anime 浮动弹幕/退出全屏按钮继承 overlay host 的 `pointer-events: none` 导致无法点击的问题。

## 环境与运行方法

- Windows、.NET SDK 11.0.100、PowerShell 7。
- 使用独立 .NET SDK 11.0.100 安装的 `harmonyos` workload `11.0.0`、NuGet 上的 Framework/图形包 `11.0.0` 和 API 15+ Native SDK。
- HarmonyOS API 17 x86_64 模拟器，HDC `127.0.0.1:5555`。
- 显示屏 1260 × 2720，XComponent 1260 × 2503，density 3.25。
- x86_64 及 arm64-v8a 均完成 NativeAOT 编译和 HAP 打包；只有 x86_64 做了设备运行验证。

```powershell
# 在 miko-v5 根目录；pwsh 需要在 PATH，或使用其绝对路径。
pwsh ./examples/Ionic/IonicDemo.HarmonyOS/build.ps1 `
  -CommandLineTools 'F:\.huawei\command-line-tools' `
  -JavaHome 'D:\Program Files\Huawei\DevEco Studio\jbr' -Run

pwsh ./examples/Ionic/IonicDemo.HarmonyOS/verify.ps1 `
  -CommandLineTools 'F:\.huawei\command-line-tools'

pwsh ./examples/Ionic/IonicDemo.HarmonyOS/build.ps1 `
  -CommandLineTools 'F:\.huawei\command-line-tools' `
  -JavaHome 'D:\Program Files\Huawei\DevEco Studio\jbr' -Abi arm64-v8a
```

详细参数见 [示例运行说明](../examples/Ionic/IonicDemo.HarmonyOS/README.md)。构建产物位于
`examples/Ionic/IonicDemo.HarmonyOS/bin/<Configuration>/net11.0-ohos/<RID>/publish/`。

## 本地 SDK 配套修复

`harmonyos-net/src/PublishAotCross/package/Crosscompile.targets` 增加在 ILC 写响应文件之前
移除 `System.Net.Security.Native` direct P/Invoke 的步骤。原有过滤执行过早或仅在链接阶段处理，
含 HttpClient 的 NativeAOT 应用会留下 `NetSecurityNative_AcceptSecContext` 等未解析符号，导致 HAP 无法加载。
对应行为测试位于同仓库 `tests/PublishAotCross.Tests/TargetsTests.cs`，验证仅影响 OHOS 且保留 OpenSSL。

修改后已重新打包 PublishAotCross，并纳入 `11.0.0` workload。应用通过正式 workload 使用该 targets，
不再从相邻仓库选择本地预览 nupkg。
两种 ABI 的最终 ELF 架构均匹配，未解析的 `NetSecurityNative_*` 符号数量均为 0。

本地 OHOS Skia native 版本为 157，需要 SkiaSharp 4.157.0 / HarfBuzzSharp 14.4.0.200。
Miko 共享依赖最低版本已对齐；Browser / Windowing 显式保留其 4.158 对应依赖组合。

## 验证结果

| 检查 | 结果 |
| --- | --- |
| `dotnet build miko.slnx` | 成功，0 errors；存在仓库已有 warnings |
| Miko.Tests | 2889 通过；新增视频上下方向及裁剪的像素回归检查 |
| Miko.Ionic.Tests | 1788 通过 |
| Miko.HarmonyOS.Tests | 7 通过；包含触点坐标、取消、生命周期、Unicode 状态、文本编辑和视频注册 |
| PublishAotCross.Tests（配套仓库） | 14 通过 |
| x86_64 NativeAOT / HAP | 构建、开发签名、安装、启动成功 |
| arm64-v8a NativeAOT / HAP | 构建成功，未做真机验证 |
| 最终模拟器遍历 | 48 个路由均输出 rendered 日志；始终为进程 11873，无宿主渲染错误 |
| `dotnet pack src/Miko.HarmonyOS/Miko.HarmonyOS.csproj -c Release` | 成功；包只包含 C# 适配器并依赖 Framework 11.0.0 |

日志确认实际启动 .NET 11 并使用 OpenGL，随后出现 `MIKO_HARMONY first_frame=1`。
48 个路由涵盖共享 Demo 的全部页面。逐页截图确认了布局、文本、图标、图片及控件显示；
这是页面渲染覆盖，不代表穷举每个控件的所有交互状态。

代表性交互使用 HDC 注入真实触屏及系统按键后检查截图：

| 操作 | 观察结果 |
| --- | --- |
| Checkbox / Jon Snow | 点击后由选中变为未选中 |
| Toggle / Blueberry | 点击后开关关闭，其余行保持原状态 |
| Alert | 点击 Open Alert 显示遮罩与弹窗；系统 Back 关闭弹窗并保留当前页面 |
| List | 向上滑动后列表滚动到 Yoda，顶部工具栏保持固定 |
| Tabs | 点击 Movies 切换标题、内容和选中图标；系统 Back 返回首页 |
| Home 后重新启动 Ability | 同一进程恢复 List 页面，并保留其滚动位置 |
| 默认字体 | 英文单词间距修复，Checkbox 等页面重新检查通过 |

本次本地证据（`artifacts/` 为忽略的生成目录）：

- `issue170-final-x64-build.log`、`issue170-final-arm64-build.log`、`issue170-elf.json`。
- `issue170-final-solution-build.log`、`issue170-core-tests.log`、`issue170-ionic-tests.log`、`issue170-final-host-tests.log`。
- `issue170-final-routes.log`、`issue170-device/routes.json`、`issue170-device/hilog.log` 及 48 张页面截图。
- `issue170-checkbox-final.jpeg`、`issue170-toggle-before.jpeg` / `issue170-toggle-after.jpeg`。
- `issue170-alert-open.jpeg` / `issue170-alert-back.jpeg`、`issue170-list-before.jpeg` / `issue170-list-after.jpeg`。
- `issue170-tabs-before.jpeg` / `issue170-tabs-after.jpeg` / `issue170-tabs-back.jpeg`、`issue170-resume.jpeg`。

## 尚未覆盖

设备端软键盘测试遇到模拟器“小艺输入法”的首次使用协议，已取消该页面。因此不能声称设备端文字提交、
中文候选、键盘避让已验证；托管文字提交和退格路径已有通过的测试。IME composition/preedit、剪贴板操作、
硬件键盘快捷键和无障碍尚未适配。

摄像头及可选 `Miko.Native` 服务留待后续。OS 销毁 surface/process 后会重建应用上下文，
不提供跨销毁的导航状态恢复。

## Anime 视频验证

`harmonyos-net` 的 `Chaldea.OpenHarmony.Player.Sdk` 是 NativeAOT 构建 SDK，并不是视频控件。
媒体后端使用系统 `OH_AVPlayer` C API；ArkUI `Video` 是另一种由 ArkUI 承担显示和控件的方案。
Miko 自己绘制播放器和弹幕，因此使用 AVPlayer + NativeImage 输出到现有 Skia 场景。
接口约束也对照了本地 SDK 的 `multimedia/player_framework/avplayer.h`、`avplayer_base.h`
及 `native_image/native_image.h`。

参考：[HarmonyOS AVPlayer C API](https://developer.huawei.com/consumer/cn/doc/harmonyos-references/capi-avplayer)、
[HarmonyOS Video 组件](https://developer.huawei.com/consumer/cn/doc/harmonyos-references/ts-media-components-video)、
[OpenHarmony 官方 AVPlayer C/C++ 视频播放流程](https://github.com/openharmony/docs/blob/master/en/application-dev/media/media/using-ndk-avplayer-for-video-playback.md)。

```powershell
pwsh ./examples/App/Anime/Anime.HarmonyOS/build.ps1 `
  -CommandLineTools 'F:\.huawei\command-line-tools' `
  -JavaHome 'D:\Program Files\Huawei\DevEco Studio\jbr' -Run

# 共享应用的完整交互验证；此程序在 Windows 使用桌面系统解码器。
dotnet run --project examples/App/Anime/Anime.Verification -- artifacts/anime-verification
```

完整验证程序通过导航、分类/搜索筛选、资料编辑、收藏/历史、奖励、视频加载、选集、评论、弹幕、
全屏弹幕、离开后释放和重新打开等断言，并输出 19 张截图。这项结果与下述 HarmonyOS 设备检查分开记录。

模拟器使用应用内置 `sample.mp4`（320 × 180、3950 ms、80 帧）。日志确认 `SetFDSource`、
surface 绑定和 prepare 均返回 0；NativeImage 给出垂直翻转矩阵，合成后文字方向正常。
触屏暂停时帧和进度停止，恢复后帧继续变化；暂停时点击进度条后原生 seek 完成事件报告 `2877 ms`，
恢复播放后直接显示后段帧；返回列表时日志为 `player released stop=0 release=0`。

最终 HAP 在进程 8862 中完成以下检查，未出现 Miko 宿主或视频错误日志：

| 检查 | 结果 |
| --- | --- |
| x86_64 / arm64-v8a | 均完成 NativeAOT 和 HAP 构建；x86_64 开发签名、安装、启动通过 |
| 首页、排行榜、排期表、收藏、历史、资料、福利、我的 | 8 个路由均有 rendered 日志及截图，图片、文字、布局正常 |
| 详情页播放器 | 本地视频自动播放、循环、暂停/恢复及 seek 后继续播放通过 |
| 全屏弹幕面板 | 真实触屏打开面板；空内容发送显示校验提示；系统 Back 关闭面板；退出全屏按钮恢复详情布局 |
| Home 后恢复 | 同一进程回到详情页，AVPlayer 从暂停恢复到播放 |
| 返回列表 | AVPlayer stop/release 及 NativeImage listener 解除均成功 |

已知范围：当前模拟器暂停 seek 后保留上一张解码画面，恢复播放才显示目标帧。全屏目前填满应用视口，
系统横屏锁定、隐藏系统栏和常亮依赖尚未实现的原生平台服务。设备检查使用本地静音样片，
未据此宣称 HTTP/HLS、实际声音输出、旋转视频、HDR/DRM 或 arm64 真机播放均已验证。

本轮证据位于忽略的 `artifacts/`：`anime-host-tests.log`、`anime-core-tests.log`、`anime-ionic-tests.log`、
`anime-verification.log` 和 `anime-verification/`，以及 `anime-final-x64-build.log`、
`anime-final-arm64-build.log`、`anime-final-video.log`、`anime-routes.json`、`anime-route-*.jpeg`、
`anime-final-playing.jpeg`、`anime-final-paused.jpeg`、`anime-final-fullscreen.jpeg`、
`anime-final-danmaku-modal.jpeg`、`anime-final-danmaku-validation.jpeg`、`anime-final-resume.jpeg`。
