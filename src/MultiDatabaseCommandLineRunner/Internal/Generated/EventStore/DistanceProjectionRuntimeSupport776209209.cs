// <auto-generated/>
#pragma warning disable
using Marten.AsyncDaemon.Testing;
using System.Linq;

namespace Marten.Generated.EventStore
{
    // START: DistanceProjectionInlineProjection776209209
    public class DistanceProjectionInlineProjection776209209 : Marten.Events.Projections.SyncEventProjection<Marten.AsyncDaemon.Testing.DistanceProjection>
    {
        private readonly Marten.AsyncDaemon.Testing.DistanceProjection _projection;

        public DistanceProjectionInlineProjection776209209(Marten.AsyncDaemon.Testing.DistanceProjection projection) : base(projection)
        {
            _projection = projection;
        }



        public override void ApplyEvent(Marten.IDocumentOperations operations, Marten.Events.StreamAction streamAction, Marten.Events.IEvent e)
        {
            switch (e)
            {
                case Marten.Events.IEvent<Marten.AsyncDaemon.Testing.TestingSupport.Travel> event_Travel21:
                    var distance1 = Projection.Create(event_Travel21.Data, e);
                    operations.Store(distance1);
                    break;
            }

        }

    }

    // END: DistanceProjectionInlineProjection776209209
    
    
}

