using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compose.Net;
using static Compose.Net.ComposeFunctions;
using Compose.Net.Unity.Packages.Compose.Net.Unity.Runtime.Adapters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Compose.Net.Unity.Tests;

public sealed partial class DelegatedStateTests : UITestFixture
{
    private IComposer _composer;
    private TestAdapter _adapter;
    private IMutableState<int> _revision, _key;
    private IMutableState<bool> _visible;
    private Action _increment;
    private Action<int> _set;
    private Func<int> _read;
    private Func<string> _format;
    private int _initializations, _renders, _rootRenders;
    private IMutableState<int> _holder;
    private int Shown => int.Parse(rootVisualElement.Q<TextElement>("by.count").text);

    [SetUp] public void SetUpState()
    {
        panelSize = new Vector2(500, 400);
        simulate.timePerSimulatedFrameMs = 0;
        _adapter = new TestAdapter();
        _revision = MutableStateOf(0); _key = MutableStateOf(0); _visible = MutableStateOf(true);
        _initializations = _renders = _rootRenders = 0;
        _composer = IComposer.Create();
        Bootstrap(_composer, _adapter, false, new ComposeConfig(Logging: false), new RootNode(rootVisualElement), Content);
        Flush();
    }

    [TearDown] public void TearDownState()
    {
        _composer?.Dispose();
        simulate.ResetTimePerSimulatedFrameToDefault();
    }

    private void Flush()
    {
        for (var i = 0; i < 3; i++) { _adapter.Invalidator.Tick(); simulate.FrameUpdateMs(0); }
    }

    [Composable] private void Content()
    {
        _rootRenders++;
        if (_visible.Value) Counter(_revision.Value);
    }

    [Composable] private void Counter(int revision)
    {
        var count = By(Remember(_key.Value, () => NewState()));
        _renders++;
        _increment = () => count++;
        _set = value => count = value;
        _read = () => count;
        _format = count.ToString;
        Column(content: () =>
        {
            Text(count.ToString(), modifier: Modifier.TestTag("by.count"));
            Box(modifier: Modifier.Size(120.Dp(), 40.Dp()).TestTag("by.increment").Clickable(() => count++),
                content: () => Text("Increment"));
        });
    }

    private IMutableState<int> NewState() { _initializations++; return _holder = MutableStateOf(1); }

    [Test] public void InitialValueAppearsInNativeText()
    {
        Assert.That(Shown, Is.EqualTo(1));
        Assert.That(_initializations, Is.EqualTo(1));
    }

    [Test] public void CapturedIncrementUpdatesUiAndUnderlyingState()
    {
        _increment(); Flush();
        Assert.That(Shown, Is.EqualTo(2)); Assert.That(_holder.Value, Is.EqualTo(2));
    }

    [Test] public void OldCallbacksReadAndWriteLatestValue()
    {
        var increment = _increment; var read = _read;
        increment(); increment(); Flush(); increment(); Flush();
        Assert.That(Shown, Is.EqualTo(4)); Assert.That(read(), Is.EqualTo(4));
    }

    [Test] public void EqualAssignmentDoesNotRecompose()
    {
        var before = _renders;
        _set(1); Flush();
        Assert.That(_renders, Is.EqualTo(before));
    }

    [Test] public void StateChangeDoesNotRestartUnsubscribedParent()
    {
        var parentRenders = _rootRenders;
        _set(12); Flush();
        Assert.That(Shown, Is.EqualTo(12)); Assert.That(_rootRenders, Is.EqualTo(parentRenders));
    }

    [Test] public void ParentRecompositionPreservesRememberedValue()
    {
        _set(9); Flush(); _revision.Value++; Flush();
        Assert.That(Shown, Is.EqualTo(9)); Assert.That(_initializations, Is.EqualTo(1));
    }

    [Test] public void RememberKeyReplacesStateIncludingWriteOnlyCallback()
    {
        var oldHolder = _holder; var oldSet = _set;
        _set(8); Flush(); _key.Value++; Flush();
        Assert.That(Shown, Is.EqualTo(1)); Assert.That(_initializations, Is.EqualTo(2));
        _set(23); Flush();
        Assert.That(Shown, Is.EqualTo(23)); Assert.That(oldHolder.Value, Is.EqualTo(8));
        oldSet(44); Flush(); Assert.That(Shown, Is.EqualTo(23));
    }

    [Test] public void MethodGroupRebindsWhenValueChanges()
    {
        Assert.That(_format(), Is.EqualTo("1"));
        _set(7); Flush(); Assert.That(_format(), Is.EqualTo("7"));
    }

    [Test] public void LeavingAndReenteringCompositionCreatesFreshState()
    {
        _set(20); Flush(); _visible.Value = false; Flush();
        Assert.That(rootVisualElement.Q("by.count"), Is.Null);
        _visible.Value = true; Flush();
        Assert.That(Shown, Is.EqualTo(1)); Assert.That(_initializations, Is.EqualTo(2));
    }

    [UnityTest] public IEnumerator PointerClickUpdatesDisplayedValue()
    {
        var target = rootVisualElement.Q("by.increment");
        simulate.MouseDown(target); yield return null; Flush();
        simulate.MouseUp(target);
        for (var i = 0; i < 60 && Shown == 1; i++) { yield return null; Flush(); }
        Assert.That(Shown, Is.EqualTo(2));
    }

    [Test] public void ExternalHolderMutationIsVisibleThroughDelegation()
    {
        _holder.Value = 31; Flush(); Assert.That(Shown, Is.EqualTo(31)); Assert.That(_read(), Is.EqualTo(31));
    }

    [Test] public void DisposalRemovesSubscriptions()
    {
        _composer.Dispose(); var before = _renders;
        Assert.DoesNotThrow(() => { _increment(); Flush(); });
        Assert.That(_renders, Is.EqualTo(before));
    }

    [Test] public void OperatorsAndLocalFunctionsWriteThrough()
    {
        var count = By(MutableStateOf(1));
        Assert.That(count++, Is.EqualTo(1)); Assert.That(++count, Is.EqualTo(3));
        count += 4; count *= 2;
        void Decrement() { count--; }
        Decrement(); Assert.That(count, Is.EqualTo(13));
    }

    [Test] public void NullAndReferenceTypesUseTheCurrentValue()
    {
        var text = By(MutableStateOf<string>(null)); text ??= "hello";
        Assert.That(text, Is.EqualTo("hello"));
        var item = By(MutableStateOf(new object())); var replacement = new object();
        item = replacement; Assert.That(item, Is.SameAs(replacement));
    }

    [Test] public void NameOfAndInferredNamesArePreserved()
    {
        var count = By(MutableStateOf(5));
        var item = new { count }; var tuple = (count, other: 2);
        Assert.That(nameof(count), Is.EqualTo("count"));
        Assert.That(item.count, Is.EqualTo(5)); Assert.That(tuple.count, Is.EqualTo(5));
    }

    [Test] public void SameSpellingInAnotherScopeIsNotRewritten()
    {
        { var value = By(MutableStateOf(1)); value++; Assert.That(value, Is.EqualTo(2)); }
        { var value = 10; Assert.That(value, Is.EqualTo(10)); }
    }

    private sealed class RootNode(VisualElement element) : UnityReusableComposeNode(element), IReusableComposeNode
    { public new bool IsRoot => true; }
    private sealed class TestAdapter : IComposeAdapter
    {
        internal readonly ComposeInvalidator Invalidator = new();
        private IComposeAdapter Native => ComposeUnityAdapter.Instance;
        public IFrameClock FrameClock => Native.FrameClock;
        public ComposeInvalidator ComposeInvalidatorInstance => Invalidator;
        public IModifiersFactory ModifiersFactory => Native.ModifiersFactory;
        public IReusableNodeFactory NodeFactory => Native.NodeFactory;
        public IComposeLayouts Layouts => Native.Layouts;
        public IComposeLogger Logger => Native.Logger;
        public IEnterTransitionsFactory EnterTransitionsFactory => Native.EnterTransitionsFactory;
        public IExitTransitionsFactory ExitTransitionsFactory => Native.ExitTransitionsFactory;
        public IBrushFactory BrushFactory => Native.BrushFactory;
        public IDisposable StartTimeCoroutine(IEnumerable<TimeSpan?> coroutine) => Native.StartTimeCoroutine(coroutine);
        public int Framerate() => 60;
    }
}
