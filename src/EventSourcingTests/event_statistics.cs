using System;
using System.Threading.Tasks;
using EventSourcingTests.Aggregation;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace EventSourcingTests;

public class event_statistics : OneOffConfigurationsContext
{
    [Fact]
    public async Task fetch_from_empty_store()
    {
        await theStore.Advanced.Clean.DeleteAllEventDataAsync();

        var statistics = await theStore.Advanced.FetchEventStoreStatistics();

        statistics.EventCount.ShouldBe(0);
        statistics.StreamCount.ShouldBe(0);
        statistics.EventSequenceNumber.ShouldBe(1);
    }

    [Fact]
    public async Task fetch_from_non_empty_event_store()
    {
        await theStore.Advanced.Clean.DeleteAllEventDataAsync();

        theSession.Events.Append(Guid.NewGuid(), new AEvent(), new BEvent(), new CEvent(), new DEvent());
        theSession.Events.Append(Guid.NewGuid(), new AEvent(), new CEvent(), new DEvent());
        theSession.Events.Append(Guid.NewGuid(), new AEvent(), new BEvent(), new CEvent(), new DEvent());
        theSession.Events.Append(Guid.NewGuid(), new BEvent(), new CEvent(), new DEvent());
        theSession.Events.Append(Guid.NewGuid(), new AEvent(), new BEvent(), new CEvent(), new DEvent());

        await theSession.SaveChangesAsync();

        var statistics = await theStore.Advanced.FetchEventStoreStatistics();

        statistics.EventCount.ShouldBe(18);
        statistics.StreamCount.ShouldBe(5);
        statistics.EventSequenceNumber.ShouldBe(18);
    }

}
