using System;
using System.Reflection;
using Compose.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Compose.Net.Unity.Tests;

public class AnimatedVisibilityLayoutTests
{
    [TestCase(false, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, false)]
    [TestCase(false, false, true)]
    [TestCase(false, true, true)]
    public void EnterReachesNaturalSizeWithPaddedShell(bool measureBeforeEnter, bool horizontal, bool cancelBeforeLayout)
    {
        var owner = ScriptableObject.CreateInstance<PanelOwner>();
        var panelType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.Panel", true);
        var panel = (IPanel)panelType.GetMethod("CreateEditorPanel", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { owner });
        var validate = panelType.GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        try
        {
            panel.visualTree.style.width = 1000;
            panel.visualTree.style.height = 1000;
            panel.visualTree.style.alignItems = Align.Center;
            panel.visualTree.style.justifyContent = Justify.Center;
            var shell = new Box { style = { flexShrink = 0, paddingTop = 16, paddingBottom = 16, paddingLeft = 16, paddingRight = 16 } };
            var content = new Box();
            var shellNode = new UnityReusableComposeNode(shell);
            var contentNode = new UnityReusableComposeNode(content);
            panel.visualTree.Add(shell);
            shellNode.Insert(0, contentNode);
            contentNode.Insert(0, new UnityReusableComposeNode(new VisualElement { style = { width = 120, height = 80, flexShrink = 0 } }));
            var factoryType = typeof(UnityReusableComposeNode).Assembly.GetType("Compose.Net.Unity.Runtime.Adapters.Transitions.Enter.EnterTransitionsFactoryImpl", true);
            var factory = (IEnterTransitionsFactory)Activator.CreateInstance(factoryType, true);
            var spec = new AnimationSpec(TimeSpan.FromSeconds(1), TimeSpan.Zero, new LinearEasing());
            var transition = horizontal ? factory.ExpandHorizontally(spec, Alignment.Right, true, _ => 0)
                : factory.ExpandVertically(spec, Alignment.Bottom, true, _ => 0);
            if (cancelBeforeLayout)
            {
                transition.Apply(contentNode, TimeSpan.Zero);
                transition.Revert(contentNode, TimeSpan.Zero);
                validate.Invoke(panel, null);
                Assert.That(shell.layout.width, Is.EqualTo(152).Within(.6f), "cancelled enter must not run after layout");
                Assert.That(shell.layout.height, Is.EqualTo(112).Within(.6f), "cancelled enter must not run after layout");
                Assert.That(shell.style.overflow.keyword, Is.EqualTo(StyleKeyword.Null), "cancelled enter must not acquire clipping");
            }
            if (measureBeforeEnter) validate.Invoke(panel, null);
            for (var frame = 0; frame <= 10; frame++)
            {
                var time = TimeSpan.FromMilliseconds(frame * 100);
                if (frame > 0) transition.Revert(contentNode, time - TimeSpan.FromMilliseconds(100));
                transition.Apply(contentNode, time);
                validate.Invoke(panel, null);
                Assert.That(content.layout.width, Is.EqualTo(120).Within(.6f), "natural width during enter");
                Assert.That(content.layout.height, Is.EqualTo(80).Within(.6f), "natural height during enter");
                Assert.That(horizontal ? shell.layout.width : shell.layout.height,
                    Is.EqualTo(32 + (horizontal ? 120 : 80) * frame / 10f).Within(.6f), "animated shell size");
            }
            Assert.That(content.layout.height, Is.EqualTo(80).Within(.6f), "natural content height");
            Assert.That(shell.layout.width, Is.EqualTo(152).Within(.6f), "fully expanded shell width includes padding");
            Assert.That(shell.layout.height, Is.EqualTo(112).Within(.6f), "fully expanded shell includes padding");
            var exitFactory = (IExitTransitionsFactory)Activator.CreateInstance(typeof(UnityReusableComposeNode).Assembly.GetType(
                "Compose.Net.Unity.Runtime.Adapters.Transitions.Exit.ExitTransitionsFactoryImpl", true), true);
            var exit = horizontal ? exitFactory.ShrinkHorizontally(spec, Alignment.Right, true, _ => 0)
                : exitFactory.ShrinkVertically(spec, Alignment.Bottom, true, _ => 0);
            for (var frame = 0; frame <= 10; frame++)
            {
                var time = TimeSpan.FromMilliseconds(frame * 100);
                if (frame > 0) exit.Revert(contentNode, time - TimeSpan.FromMilliseconds(100));
                exit.Apply(contentNode, time);
                validate.Invoke(panel, null);
                Assert.That(content.layout.width, Is.EqualTo(120).Within(.6f), "natural width during exit");
                Assert.That(content.layout.height, Is.EqualTo(80).Within(.6f), "natural height during exit");
                Assert.That(horizontal ? shell.layout.width : shell.layout.height,
                    Is.EqualTo(32 + (horizontal ? 120 : 80) * (1 - frame / 10f)).Within(.6f), "animated exit size");
            }
            exit.Revert(contentNode, TimeSpan.FromSeconds(1));
            validate.Invoke(panel, null);
            Assert.That(shell.layout.width, Is.EqualTo(152).Within(.6f), "restored width");
            Assert.That(shell.layout.height, Is.EqualTo(112).Within(.6f), "restored height");
            Assert.That(content.style.width.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(content.style.height.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(shell.style.overflow.keyword, Is.EqualTo(StyleKeyword.Null), "balanced clipping after skipped/unmeasured apply");
        }
        finally
        {
            ((IDisposable)panel).Dispose();
            Object.DestroyImmediate(owner);
        }
    }
    private sealed class PanelOwner : ScriptableObject { }
    private sealed class LinearEasing : IEasing { public float Transform(float fraction) => fraction; }
}
