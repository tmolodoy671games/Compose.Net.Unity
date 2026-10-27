using System;
using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

internal interface IUnityStyleTransition
{
    void UpdateStyle(UnityStyleModifierNode node, TimeSpan timeElapsed);
}

internal record UnityTransitionModifier(IUnityStyleTransition Transition, TimeSpan TimeElapsed) : UnityStyleModifier
{
    public override object NodeKey => Transition.GetType();
    public override bool DependsOnChain => true;
    public override void UpdateStyle(UnityStyleModifierNode node) => Transition.UpdateStyle(node, TimeElapsed);

}
