using System.Text.Json;
using EventStore.Client;

namespace IntroductionToEventSourcing.BusinessLogic.Mutable;

public static class EventStoreClientExtensions
{
    public static async Task<TAggregate> Get<TAggregate>(
        this EventStoreClient eventStore,
        string streamName,
        CancellationToken cancellationToken = default
    ) where TAggregate : Aggregate
    {
        var result = eventStore.ReadStreamAsync(
            Direction.Forwards,
            streamName,
            StreamPosition.Start,
            cancellationToken: cancellationToken
        );

        if (await result.ReadState == ReadState.StreamNotFound)
            throw new InvalidOperationException("Shopping Cart was not found!");

        var empty = (TAggregate)Activator.CreateInstance(typeof(TAggregate), true)!;

        return await result
            .Select(@event =>
                JsonSerializer.Deserialize(
                    @event.Event.Data.Span,
                    Type.GetType(@event.Event.EventType)!
                )!
            )
            .AggregateAsync(
                empty,
                ((aggregate, @event) =>
                {
                    aggregate.Evolve(@event!);
                    return aggregate;
                }),
                cancellationToken
            );
    }

    public static Task Add<TAggregate, TCommand>(
        this EventStoreClient eventStore,
        Func<TCommand, string> getStreamName,
        Func<TCommand, TAggregate> action,
        TCommand command,
        CancellationToken cancellationToken = default
    ) where TAggregate : Aggregate
    {
        var aggregate = action(command);

        return eventStore.AppendToStreamAsync(
            getStreamName(command),
            StreamState.Any,
            aggregate.DequeueUncommittedEvents()
                .Select(@event =>
                    new EventData(
                        Uuid.NewUuid(),
                        @event.GetType().FullName!,
                        JsonSerializer.SerializeToUtf8Bytes(@event)
                    )
                ),
            cancellationToken: cancellationToken
        );
    }

    public static async Task GetAndUpdate<TAggregate, TCommand>(
        this EventStoreClient eventStore,
        Func<TCommand, string> getStreamName,
        Action<TCommand, TAggregate> action,
        TCommand command,
        CancellationToken cancellationToken = default
    ) where TAggregate : Aggregate
    {
        var streamName = getStreamName(command);
        var current = await eventStore.Get<TAggregate>(streamName, cancellationToken);

        action(command, current);

        await eventStore.AppendToStreamAsync(
            getStreamName(command),
            StreamState.Any,
            current.DequeueUncommittedEvents()
                .Select(@event =>
                    new EventData(
                        Uuid.NewUuid(),
                        @event.GetType().FullName!,
                        JsonSerializer.SerializeToUtf8Bytes(@event)
                    )
                ),
            cancellationToken: cancellationToken
        );
    }
}
