// ReSharper disable CheckNamespace

using Compose.Net;
using SharpExtensions;
using Compose.Net.Unity.Runtime.Adapters.Utils;
using Compose.Net.Unity.Runtime.Extensions;
using UnityEngine;
using UnityEngine.UIElements;
using PointerId = Compose.Net.PointerId;
using PointerType = Compose.Net.PointerType;

namespace Compose.Net.Unity;

internal record PointerInputChange(
    PointerId Id,
    long UptimeMillis,
    Offset Position,
    bool Pressed,
    float Pressure,
    long PreviousUptimeMillis,
    Offset PreviousPosition,
    bool PreviousPressed,
    bool IsInitiallyConsumed,
    PointerType Type,
    Offset ScrollDelta,
    EventBase Event,
    bool IsCancelled = false
) : IPointerInputChange
{
    public static IPointerInputChange Create(PointerDownEvent evt)
    {
        return new PointerInputChange(
            Id: new PointerId(evt.pointerId),
            UptimeMillis: evt.timestamp,
            Position: evt.localPosition.ToOffset(),
            Pressed: true,
            Pressure: evt.pressure,
            PreviousUptimeMillis: (Time.time - evt.deltaTime).ToLongTime(),
            PreviousPosition: (evt.localPosition - evt.deltaPosition).ToOffset(),
            PreviousPressed: false,
            IsInitiallyConsumed: false,
            Type: evt.pointerType.ToPointerType(),
            ScrollDelta: Offset.Zero,
            Event: evt
        );
    }

    public static IPointerInputChange Create(PointerMoveEvent evt)
    {
        return new PointerInputChange(
            Id: new PointerId(evt.pointerId),
            UptimeMillis: evt.timestamp,
            Position: evt.localPosition.ToOffset(),
            Pressed: evt.pressedButtons != 0,
            Pressure: evt.pressure,
            PreviousUptimeMillis: (Time.time - evt.deltaTime).ToLongTime(),
            PreviousPosition: (evt.localPosition - evt.deltaPosition).ToOffset(),
            PreviousPressed: evt.pressedButtons != 0,
            IsInitiallyConsumed: false,
            Type: evt.pointerType.ToPointerType(),
            ScrollDelta: Offset.Zero,
            Event: evt
        );
    }

    public static IPointerInputChange Create(PointerUpEvent evt)
    {
        return new PointerInputChange(
            Id: new PointerId(evt.pointerId),
            UptimeMillis: evt.timestamp,
            Position: evt.localPosition.ToOffset(),
            Pressed: false,
            Pressure: evt.pressure,
            PreviousUptimeMillis: (Time.time - evt.deltaTime).ToLongTime(),
            PreviousPosition: (evt.localPosition - evt.deltaPosition).ToOffset(),
            PreviousPressed: true,
            IsInitiallyConsumed: false,
            Type: evt.pointerType.ToPointerType(),
            ScrollDelta: Offset.Zero,
            Event: evt
        );
    }

    public static IPointerInputChange Create(PointerCancelEvent evt)
    {
        return new PointerInputChange(
            Id: new PointerId(evt.pointerId),
            UptimeMillis: evt.timestamp,
            Position: evt.localPosition.ToOffset(),
            Pressed: false,
            Pressure: evt.pressure,
            PreviousUptimeMillis: (Time.time - evt.deltaTime).ToLongTime(),
            PreviousPosition: (evt.localPosition - evt.deltaPosition).ToOffset(),
            PreviousPressed: true,
            IsInitiallyConsumed: true,
            Type: evt.pointerType.ToPointerType(),
            ScrollDelta: Offset.Zero,
            Event: evt,
            IsCancelled: true
        );
    }

    internal sealed class Consumption { public bool Value; }
    internal Consumption Consumed { get; init; } = new() { Value = IsInitiallyConsumed };
    public bool IsConsumed => Consumed.Value;

    public void Consume()
    {
        Consumed.Value = true;
    }
}