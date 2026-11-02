using System;
using System.Collections.Generic;
using Compose.Net;

namespace Compose.Net.Unity.Samples.Behaviors.Content
{
    public sealed class ClickableDemoState
    {
        public readonly ClickableDemoCase Basic = new();
        public readonly ClickableDemoCase EnabledCase = new();
        public readonly ClickableDemoCase Tracked = new();
        public readonly ClickableDemoCase Silent = new();
        public readonly ClickableDemoCase Implicit = new();
        public readonly ClickableDemoCase Custom = new();
        public readonly ClickableDemoCase Local = new();
        public readonly ClickableDemoCase Callback = new();
        public readonly ClickableDemoCase Lifetime = new();
        public readonly ClickableDemoCase Shared = new();
        public readonly ClickableDemoCase SharedSecond = new();
        public readonly ClickableDemoCase Recompose = new();
        public readonly ClickableDemoCase Disabled = new();
        public readonly IMutableState<bool> Enabled = MutableStateOf(true);
        public readonly IMutableState<bool> TrackedEnabled = MutableStateOf(true);
        public readonly IMutableState<bool> Visible = MutableStateOf(true);
        public readonly IMutableState<bool> LargeStep = MutableStateOf(false);
        public readonly IMutableState<bool> Decorated = MutableStateOf(false);
        public readonly Action ToggleEnabled, ToggleTracked, ToggleVisible, ToggleCallback, ToggleDecoration, AddOne, AddTen;

        public ClickableDemoState()
        {
            ToggleEnabled = () => Enabled.Value = !Enabled.Value;
            ToggleTracked = () => TrackedEnabled.Value = !TrackedEnabled.Value;
            ToggleVisible = () => Visible.Value = !Visible.Value;
            ToggleCallback = () => LargeStep.Value = !LargeStep.Value;
            ToggleDecoration = () => Decorated.Value = !Decorated.Value;
            AddOne = () => Callback.Count.Value++;
            AddTen = () => Callback.Count.Value += 10;
        }
    }

    public sealed class ClickableDemoCase
    {
        public readonly IMutableInteractionSource Source = MutableInteractionSource();
        public readonly IMutableState<int> Count = MutableStateOf(0);
        public readonly IMutableState<int> Presses = MutableStateOf(0);
        public readonly IMutableState<int> Releases = MutableStateOf(0);
        public readonly IMutableState<int> Cancels = MutableStateOf(0);
        // Bounded history is useful when inspecting the demo, including pairing by object identity.
        public readonly List<IInteraction> Events = new();
        public readonly Action Click;
        public ClickableDemoCase() => Click = () => Count.Value++;
        public void Record(IInteraction interaction)
        {
            if (Events.Count == 64) Events.RemoveAt(0);
            Events.Add(interaction);
            switch (interaction)
            {
                case IPressInteraction.Press: Presses.Value++; break;
                case IPressInteraction.Release: Releases.Value++; break;
                case IPressInteraction.Cancel: Cancels.Value++; break;
            }
        }
    }
}
