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

    // A cancellation is neither a down nor an up; gesture detectors see an
    // unpressed change without a release transition and can cancel the gesture.
    public static IPointerInputChange Create(PointerCancelEvent evt) => Cancel(evt.pointerId, evt);

    public static IPointerInputChange Cancel(int pointerId, EventBase evt) => new PointerInputChange(
        new PointerId(pointerId), evt.timestamp, Offset.Zero, false, 0,
        evt.timestamp, Offset.Zero, false, false, PointerType.Mouse, Offset.Zero, evt);

    public bool IsConsumed { get; private set; }

    public void Consume()
    {
        IsConsumed = true;
        Event.StopPropagation();
    }
}
