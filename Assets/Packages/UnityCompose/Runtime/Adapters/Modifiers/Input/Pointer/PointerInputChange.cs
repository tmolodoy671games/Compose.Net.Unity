// ReSharper disable CheckNamespace

using Compose.Net;
using SharpExtensions;
using UnityCompose.Packages.UnityCompose.Runtime.Adapters.Utils;
using UnityCompose.Packages.UnityCompose.Runtime.Extensions;
using UnityEngine;
using UnityEngine.UIElements;
using PointerId = Compose.Net.PointerId;
using PointerType = Compose.Net.PointerType;

namespace UnityCompose;

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
    EventBase Event
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
            ScrollDelta: evt.deltaPosition.ToOffset(),
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
            ScrollDelta: evt.deltaPosition.ToOffset(),
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
            ScrollDelta: evt.deltaPosition.ToOffset(),
            Event: evt
        );
    }

    public static IPointerInputChange Create(PointerCancelEvent evt)
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
            ScrollDelta: evt.deltaPosition.ToOffset(),
            Event: evt
        );
    }

    public bool IsConsumed { get; private set; }

    public void Consume()
    {
        IsConsumed = true;
        Event.StopPropagation();
    }
}