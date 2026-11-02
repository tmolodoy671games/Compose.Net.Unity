using System;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace Compose.Net.Kaspresso.Unity
{
    [AiGenerated]
    public static class KaspressoUnity
    {
        public static Kaspresso Create(PanelSimulator simulator, Func<VisualElement> root = null,
            TimeSpan? timeout = null, Action<string> log = null)
        {
            return new Kaspresso(new UnityTestDriver(simulator, root), timeout, log);
        }
    }
}
