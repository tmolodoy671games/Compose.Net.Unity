using System;
using System.IO;
using Compose.Net;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using UnityEditor;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Samples.Behaviors.Stability;

internal static partial class ModifierNodeChecks
{
    private const string RequestPath = "Library/ComposeModifierNodeChecks.request";
    private const string ResultPath = "Library/ComposeModifierNodeChecks.result.txt";

    [InitializeOnLoadMethod]
    private static void RunWhenRequested()
    {
        if (!File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        EditorApplication.delayCall += () =>
        {
            try { Run(); File.WriteAllText(ResultPath, "PASS"); }
            catch (Exception error) { File.WriteAllText(ResultPath, error.ToString()); UnityEngine.Debug.LogException(error); }
        };
    }

    [MenuItem("Tools/Compose/Validate Modifier Nodes")]
    internal static void Run()
    {
        var harness = new Harness();
        using var view = new ComposeView();
        view.SetContent(harness.Content);
        var element = harness.Element;
        var initialWidth = new StyleLength(new Length(37, LengthUnit.Percent));
        element.style.opacity = 0.8f;
        element.style.width = initialWidth;
        void Set(IModifier modifier)
        {
            harness.Chain.Value = modifier;
            ComposeUnityAdapter.Instance.ComposeInvalidatorInstance!.Tick();
        }
        Set(Modifier.Alpha(0.5f).Alpha(0.2f));
        Check(element.style.opacity.value == 0.2f, "Duplicate opacity");
        Set(Modifier.Alpha(0.6f).Alpha(0.2f));
        Check(element.style.opacity.value == 0.2f, "Updating an overridden opacity");
        Set(Modifier.Alpha(0.6f));
        Check(element.style.opacity.value == 0.6f, "Removing the winning opacity");
        Set(Modifier);
        Check(element.style.opacity.value == 0.8f, "Restoring inline opacity");
        Set(Modifier.Width(100.Dp()));
        Set(Modifier.Height(50.Dp()));
        Check(element.style.width == initialWidth, "Restoring an optional dimension and its unit");
        Set(Modifier);
        Check(element.style.height.keyword == StyleKeyword.Null, "Restoring USS inheritance");
        Set(Modifier.Offset(10.Dp(), 20.Dp()).Offset(30.Dp(), 40.Dp()));
        Check(element.style.translate.value.x.value == 40, "Adding offsets");
        Set(Modifier.Offset(15.Dp(), 20.Dp()).Offset(30.Dp(), 40.Dp()));
        Check(element.style.translate.value.x.value == 45, "Updating an additive offset without accumulation");
        Set(Modifier.Offset(15.Dp(), 20.Dp()));
        Check(element.style.translate.value.x.value == 15, "Removing an additive offset");
        Set(Modifier);
        Check(element.style.translate.keyword == StyleKeyword.Null, "Restoring transform inheritance");
        var tween = Tween(100, easing: LinearEasing);
        Set(Modifier.Alpha(0.7f).EnterTransition(FadeIn(tween), TimeSpan.FromMilliseconds(25)));
        Check(Math.Abs(element.style.opacity.value - 0.25f) < 0.001f, "Transition contribution");
        Set(Modifier.Alpha(0.7f));
        Check(element.style.opacity.value == 0.7f, "Removing a fade must reveal base alpha");
        Set(Modifier.Clip().Clip());
        Set(Modifier.Clip());
        Check(element.style.overflow.value == Overflow.Hidden, "Removing one clip");
        Set(Modifier);
        Check(element.style.overflow.keyword == StyleKeyword.Null, "Removing all clips");

        Action<Offset>? emit = null;
        var attaches = 0;
        var detaches = 0;
        Func<Action<Offset>, IDisposable> subscribe = callback =>
        {
            attaches++;
            emit = callback;
            return new Subscription(() => { detaches++; emit = null; });
        };
        Set(Modifier.Offset(10.Dp()).Offset(subscribe));
        emit!(new Offset(3, 4));
        Check(element.style.translate.value.x.value == 13, "Observed offset contribution");
        Set(Modifier.Offset(20.Dp()).Offset(subscribe));
        Check(attaches == 1 && detaches == 0 && element.style.translate.value.x.value == 23, "Reusing an offset subscription");
        Set(Modifier);
        Check(detaches == 1 && element.style.translate.keyword == StyleKeyword.Null, "Disposing an observed offset");
        UnityEngine.Debug.Log("Compose modifier node checks passed");
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
    }
    private sealed class Subscription : IDisposable
    {
        private Action? _dispose;
        internal Subscription(Action dispose) => _dispose = dispose;
        public void Dispose() { var dispose = _dispose; _dispose = null; dispose?.Invoke(); }
    }
    private sealed partial class Harness
    {
        internal readonly VisualElement Element = new();
        internal readonly IMutableState<IModifier> Chain = MutableStateOf(Modifier);
        private readonly UnityReusableComposeNode _node;
        internal Harness() => _node = new UnityReusableComposeNode(Element);
        [Composable]
        internal void Content() => ReusableComposeNode(() => _node, Chain.Value);
    }
}
