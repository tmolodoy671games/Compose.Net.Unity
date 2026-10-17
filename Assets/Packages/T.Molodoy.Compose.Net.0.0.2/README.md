# Compose.Net

.Net implementation of Compose.

Animations and `NextFrame` share `IComposeAdapter.FrameClock`. The default clock uses
one `StartTimeCoroutine` iterator (yielding `null` for each UI frame) and a monotonic
`Stopwatch` timestamp. The iterator stops when there are no pending waits.
Adapters can override `FrameClock` to integrate their own frame loop or time source.
Cancellation applies to each wait independently. `Delay` remains a timed delay.
