using System;
using Compose.Net;
using Compose.Net.Showcase;
using UnityEngine;
using UnityEngine.UIElements;
using static Compose.Net.ComposeFunctions;

namespace Compose.Net.Unity.Showcase;

public sealed partial class OrbitUnityHost : IOrbitHost
{
    [Composable] public void TextInput(string value, Action<string> onChange, string placeholder, string tag, IModifier modifier, OrbitPalette palette)
    {
        ReusableComposeNode(nodeFactory: () => new InputNode(), modifier: modifier.TestTag(tag), initializer: node =>
        {
            var input = (InputNode)node;
            input.Changed = onChange;
            input.Field.SetValueWithoutNotify(value);
            input.Field.textEdition.placeholder = placeholder;
            var textInput = input.Field.Q<VisualElement>(className: TextField.inputUssClassName);
            textInput.style.backgroundColor = ToUnity(palette.Raised);
            textInput.style.color = ToUnity(palette.Ink);
            textInput.style.borderTopColor = textInput.style.borderBottomColor = textInput.style.borderLeftColor = textInput.style.borderRightColor = ToUnity(palette.Line);
        });
    }
    private static UnityEngine.Color ToUnity(Compose.Net.Color color) => new(color.Red, color.Green, color.Blue, color.Alpha);
    private sealed class InputNode : UnityReusableComposeNode
    {
        internal readonly TextField Field;
        internal Action<string> Changed = _ => { };
        internal InputNode() : this(new TextField()) { }
        private InputNode(TextField field) : base(field)
        {
            Field = field; field.pickingMode = UnityEngine.UIElements.PickingMode.Position;
            field.style.marginLeft = field.style.marginRight = field.style.marginTop = field.style.marginBottom = 0;
            var input = field.Q<VisualElement>(className: TextField.inputUssClassName);
            input.style.backgroundColor = new UnityEngine.Color(.13f, .145f, .18f);
            input.style.color = new UnityEngine.Color(.94f, .95f, .92f);
            input.style.fontSize = 13; input.style.paddingLeft = input.style.paddingRight = 12;
            input.style.borderTopLeftRadius = input.style.borderTopRightRadius = input.style.borderBottomLeftRadius = input.style.borderBottomRightRadius = 10;
            input.style.borderTopWidth = input.style.borderBottomWidth = input.style.borderLeftWidth = input.style.borderRightWidth = 1;
            input.style.borderTopColor = input.style.borderBottomColor = input.style.borderLeftColor = input.style.borderRightColor = new UnityEngine.Color(.25f, .27f, .31f);
            field.RegisterValueChangedCallback(evt => Changed(evt.newValue));
        }
    }
}
