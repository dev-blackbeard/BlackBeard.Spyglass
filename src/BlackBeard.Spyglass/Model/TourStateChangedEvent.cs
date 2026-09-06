using Prism.Events;

namespace BlackBeard.Spyglass;

/// <summary>
/// Prism <see cref="PubSubEvent{TPayload}"/> mirroring <see cref="ITourService.StateChanged"/>, published
/// whenever an <see cref="IEventAggregator"/> was supplied to <see cref="TourService"/>.
/// </summary>
public sealed class TourStateChangedEvent : PubSubEvent<TourStateChangedEventArgs>
{
}
