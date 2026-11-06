# Compose.Net previews in Rider and Unity

Click the green run icon beside a `[UnityPreview]` method to open a static,
Unity-rendered image in the right-hand Rider panel. The editor context action
**Open Compose.Net Preview** is also available (`⌘⌥P` on macOS, `Ctrl+Alt+P` elsewhere).

The panel has a ▶ button, a Fit button, and a zoom slider.
**▶ brings Unity to the foreground and opens the selected preview there in
interactive mode.** Live interaction and animation run in Unity's native window,
which has its own ▶/■ button. Rider keeps the static image; it does not stream a
running UI or forward input.

Unity also adds a play icon beside preview scripts in the Project browser and
launch buttons in the script Inspector. They open the same native preview.
No Tools menu is required.

## Automatic size

When `Width` or `Height` is omitted (`-1`; `0` is also accepted), that axis follows
the available preview viewport, both in Rider and the native Unity window.
Positive dimensions fix the logical canvas size independently of the available
viewport. In Unity, each positive axis also locks the native window size (with
space for the toolbar); omitted axes remain resizable. Docked previews retain
the fixed canvas inside their available area. Downscaling is permitted when a fixed
dimension exceeds the monitor's usable area. The logical layout remains fixed,
while the displayed canvas is reduced uniformly. Native window bounds are
constrained to that monitor.

Rider fits the complete canvas into the panel by default, preserving its aspect
ratio. The zoom slider scales it from 25% to 400% relative to Fit, with scrolling
when enlarged. **Fit** resets the slider. Resizing the panel recalculates Fit;
zooming changes only the displayed image, preserving the preview layout.
Values below
`-1` are rejected. Resizing a running native preview preserves its composition
and remembered state.

```csharp
using Compose.Net;
using Compose.Net.Unity;

public static partial class ScreenPreviews
{
    [UnityPreview("Screen")]
    [Composable]
    private static void Screen() => ScreenContent();
}
```

The `UnityPreview` attribute belongs to **Compose.Net.Unity**, independently of
the portable Compose.Net assembly. Both constructor arguments and named properties
are supported, for example `[UnityPreview("Phone", 360, 640)]` or
`[UnityPreview(Name = "Phone", Width = 360, Height = 640)]`.

Methods must be static, parameterless, non-generic `void` methods in non-generic
partial types. Declare previews in Unity-specific source/assemblies. Multiple
attributes declare variants: a method's gutter action opens its first variant;
the context action on a particular attribute line opens that variant. Put variants
on separate lines to address them individually.

## Install

Build against an installed Rider SDK (tested with Rider 2026.2.4.1):

```sh
python3 Tools/RiderPreview/build.py --test --rider "$HOME/Applications/Rider.app"
```

In Rider, use **Settings → Plugins → gear → Install Plugin from Disk**, select
`Tools/RiderPreview/build/compose-net-unity-preview-0.2.0.zip`, and reload/restart
as Rider requests. The companion Unity package's `Preview/Editor` and runtime
`UnityPreview` attribute must also be present. This implementation uses Unity's
public VisualElement capture API and is tested on Unity 7000.0.0a16.

Unity Editor renders the static image in the background without Play Mode or a
native preview window. The plugin starts the editor version specified by
`ProjectSettings/ProjectVersion.txt` when needed. For a .NET project outside the
Unity project, select the companion Unity project once on first use; Rider stores
that choice in its project settings.

## Source and lifecycle

Source lookup uses compiled debug symbols, including private methods and source
mapped generated composables. Save and compile first. Rebuild/install imported
.NET libraries with their symbols. Missing source maps appear as an error in the
panel.

Commands, state and atomic PNG snapshots use a per-project mailbox under the
ignored `Library/ComposeNetPreview` directory. There is no network listener.
Static images settle at one refresh per second and resize with the viewport.
Hiding/closing the Rider panel releases its background rendering resources; lost
clients expire after ten seconds. A native preview explicitly opened with ▶ stays
open independently. Compilation reconnects static previews automatically.

The [tool window SDK](https://plugins.jetbrains.com/docs/intellij/tool-windows.html),
[gutter SDK](https://plugins.jetbrains.com/docs/intellij/line-marker-provider.html),
and [Unity capture API](https://docs.unity.com/en-us/engine/7000.0/script-reference/unityengine/uielements/visualelementcaptureextensions/capturetorendertexture)
cover the platform APIs used here.
