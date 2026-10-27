using System;
using System.Threading.Tasks;
using Compose.Net;
using Compose.Net.Unity.Runtime.Adapters.Modifiers.Other;

namespace Compose.Net.Unity.Runtime.Adapters.Modifiers;

internal class ModifiersFactoryImpl : IModifiersFactory
{
    public IAlignmentModifiersFactory Alignment { get; } = new AlignmentModifiersFactoryImpl();
    public IAppearanceModifiersFactory Appearance { get; } = new AppearanceModifiersFactoryImpl();
    public ITransformModifiersFactory Transform { get; } = new TransformModifiersFactoryImpl();
    public IInsetsModifiersFactory Insets { get; } = new InsetsModifiersFactoryImpl();
    public ISizeModifiersFactory Size { get; } = new SizeModifiersFactoryImpl();
    public IInputModifiersFactory Input { get; } = new InputModifiersFactoryImpl();
    public IPositionModifiersFactory Position { get; } = new  PositionModifiersFactoryImpl();
    
    public IModifier TestTag(string tag) => new TestTagModifierImpl(tag);
}
