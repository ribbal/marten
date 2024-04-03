// <auto-generated/>
#pragma warning disable
using Marten;
using Marten.AsyncDaemon.Testing.TestingSupport;
using Marten.Events.Aggregation;
using Marten.Internal.Storage;
using System;
using System.Linq;

namespace Marten.Generated.EventStore
{
    // START: TripProjectionWithCustomNameLiveAggregation2035656378
    public class TripProjectionWithCustomNameLiveAggregation2035656378 : Marten.Events.Aggregation.SyncLiveAggregatorBase<Marten.AsyncDaemon.Testing.TestingSupport.Trip>
    {
        private readonly Marten.AsyncDaemon.Testing.TestingSupport.TripProjectionWithCustomName _tripProjectionWithCustomName;

        public TripProjectionWithCustomNameLiveAggregation2035656378(Marten.AsyncDaemon.Testing.TestingSupport.TripProjectionWithCustomName tripProjectionWithCustomName)
        {
            _tripProjectionWithCustomName = tripProjectionWithCustomName;
        }


        public System.Func<Marten.AsyncDaemon.Testing.TestingSupport.Breakdown, bool> ShouldDelete1 {get; set;}

        public System.Func<Marten.AsyncDaemon.Testing.TestingSupport.Trip, Marten.AsyncDaemon.Testing.TestingSupport.VacationOver, bool> ShouldDelete2 {get; set;}


        public override Marten.AsyncDaemon.Testing.TestingSupport.Trip Build(System.Collections.Generic.IReadOnlyList<Marten.Events.IEvent> events, Marten.IQuerySession session, Marten.AsyncDaemon.Testing.TestingSupport.Trip snapshot)
        {
            if (!events.Any()) return null;
            Marten.AsyncDaemon.Testing.TestingSupport.Trip trip = null;
            var usedEventOnCreate = snapshot is null;
            snapshot ??= Create(events[0], session);;
            if (snapshot is null)
            {
                usedEventOnCreate = false;
                snapshot = CreateDefault(events[0]);
            }

            foreach (var @event in events.Skip(usedEventOnCreate ? 1 : 0))
            {
                snapshot = Apply(@event, snapshot, session);
            }

            return snapshot;
        }


        public Marten.AsyncDaemon.Testing.TestingSupport.Trip Create(Marten.Events.IEvent @event, Marten.IQuerySession session)
        {
            switch (@event)
            {
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.TripStarted> event_TripStarted1:
                    return _tripProjectionWithCustomName.Create(event_TripStarted1.Data);
                    break;
            }

            return null;
        }


        public Marten.AsyncDaemon.Testing.TestingSupport.Trip Apply(Marten.Events.IEvent @event, Marten.AsyncDaemon.Testing.TestingSupport.Trip aggregate, Marten.IQuerySession session)
        {
            switch (@event)
            {
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.Arrival> event_Arrival2:
                    _tripProjectionWithCustomName.Apply(event_Arrival2.Data, aggregate);
                    break;
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.Travel> event_Travel3:
                    _tripProjectionWithCustomName.Apply(event_Travel3.Data, aggregate);
                    break;
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.TripEnded> event_TripEnded4:
                    _tripProjectionWithCustomName.Apply(event_TripEnded4.Data, aggregate);
                    break;
            }

            return aggregate;
        }

    }

    // END: TripProjectionWithCustomNameLiveAggregation2035656378
    
    
    // START: TripProjectionWithCustomNameInlineHandler2035656378
    public class TripProjectionWithCustomNameInlineHandler2035656378 : Marten.Events.Aggregation.AggregationRuntime<Marten.AsyncDaemon.Testing.TestingSupport.Trip, System.Guid>
    {
        private readonly Marten.IDocumentStore _store;
        private readonly Marten.Events.Aggregation.IAggregateProjection _projection;
        private readonly Marten.Events.Aggregation.IEventSlicer<Marten.AsyncDaemon.Testing.TestingSupport.Trip, System.Guid> _slicer;
        private readonly Marten.Internal.Storage.IDocumentStorage<Marten.AsyncDaemon.Testing.TestingSupport.Trip, System.Guid> _storage;
        private readonly Marten.AsyncDaemon.Testing.TestingSupport.TripProjectionWithCustomName _tripProjectionWithCustomName;

        public TripProjectionWithCustomNameInlineHandler2035656378(Marten.IDocumentStore store, Marten.Events.Aggregation.IAggregateProjection projection, Marten.Events.Aggregation.IEventSlicer<Marten.AsyncDaemon.Testing.TestingSupport.Trip, System.Guid> slicer, Marten.Internal.Storage.IDocumentStorage<Marten.AsyncDaemon.Testing.TestingSupport.Trip, System.Guid> storage, Marten.AsyncDaemon.Testing.TestingSupport.TripProjectionWithCustomName tripProjectionWithCustomName) : base(store, projection, slicer, storage)
        {
            _store = store;
            _projection = projection;
            _slicer = slicer;
            _storage = storage;
            _tripProjectionWithCustomName = tripProjectionWithCustomName;
        }


        public System.Func<Marten.AsyncDaemon.Testing.TestingSupport.Breakdown, bool> ShouldDelete1 {get; set;}

        public System.Func<Marten.AsyncDaemon.Testing.TestingSupport.Trip, Marten.AsyncDaemon.Testing.TestingSupport.VacationOver, bool> ShouldDelete2 {get; set;}


        public override async System.Threading.Tasks.ValueTask<Marten.AsyncDaemon.Testing.TestingSupport.Trip> ApplyEvent(Marten.IQuerySession session, Marten.Events.Projections.EventSlice<Marten.AsyncDaemon.Testing.TestingSupport.Trip, System.Guid> slice, Marten.Events.IEvent evt, Marten.AsyncDaemon.Testing.TestingSupport.Trip aggregate, System.Threading.CancellationToken cancellationToken)
        {
            switch (evt)
            {
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.Arrival> event_Arrival7:
                    aggregate ??= CreateDefault(evt);
                    _tripProjectionWithCustomName.Apply(event_Arrival7.Data, aggregate);
                    return aggregate;
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.Breakdown> event_Breakdown11:
                    aggregate ??= CreateDefault(evt);
                    if (aggregate == null) return null;
                    var result_of_Invoke1 = ShouldDelete1.Invoke(event_Breakdown11.Data);
                    if (result_of_Invoke1)
                    {
                        return null;
                    }

                    return aggregate;
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.Travel> event_Travel8:
                    aggregate ??= CreateDefault(evt);
                    _tripProjectionWithCustomName.Apply(event_Travel8.Data, aggregate);
                    return aggregate;
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.TripAborted> event_TripAborted6:
                    return null;
                    aggregate ??= CreateDefault(evt);
                    return aggregate;
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.TripEnded> event_TripEnded9:
                    aggregate ??= CreateDefault(evt);
                    _tripProjectionWithCustomName.Apply(event_TripEnded9.Data, aggregate);
                    return aggregate;
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.TripStarted> event_TripStarted10:
                    aggregate = _tripProjectionWithCustomName.Create(event_TripStarted10.Data);
                    return aggregate;
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.VacationOver> event_VacationOver12:
                    aggregate ??= CreateDefault(evt);
                    if (aggregate == null) return null;
                    var result_of_Invoke2 = ShouldDelete2.Invoke(aggregate, event_VacationOver12.Data);
                    if (result_of_Invoke2)
                    {
                        return null;
                    }

                    return aggregate;
            }

            return aggregate;
        }


        public Marten.AsyncDaemon.Testing.TestingSupport.Trip Create(Marten.Events.IEvent @event, Marten.IQuerySession session)
        {
            switch (@event)
            {
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.TripStarted> event_TripStarted5:
                    return _tripProjectionWithCustomName.Create(event_TripStarted5.Data);
                    break;
            }

            return null;
        }

    }

    // END: TripProjectionWithCustomNameInlineHandler2035656378
    
    
}

