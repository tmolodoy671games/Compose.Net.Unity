// ReSharper disable CheckNamespace

using Compose.Net;
using StableCollections;
using UnityEngine.UIElements;

namespace Compose.Net.Unity;

public interface IForegroundShadowModifier : IAppearanceModifier
{
}

public abstract record ForegroundShadowUnityModifier : UnityModifier, IForegroundShadowModifier
{
    protected sealed override void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        Apply(
            node: node,
            element: element,
            shadow: node.SetupForegroundShadow(),
            newModifiers: newModifiers
        );
    }

    protected sealed override void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        IStableList<IModifier> newModifiers
    )
    {
        Revert(
            node: node,
            element: element,
            shadow: node.SetupForegroundShadow(),
            newModifiers: newModifiers
        );
        foreach (var newModifier in newModifiers)
        {
            if (newModifier is IForegroundShadowModifier)
                return;
        }
        node.RemoveForegroundShadow();
    }

    protected abstract void Apply(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement shadow,
        IStableList<IModifier> newModifiers
    );

    protected abstract void Revert(
        UnityReusableComposeNode node,
        VisualElement element,
        VisualElement shadow,
        IStableList<IModifier> newModifiers
    );
}