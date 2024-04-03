using System;
using System.Threading;
using System.Threading.Tasks;
using Marten.Events;
using Marten.Events.Projections;
using Marten.Exceptions;
using Marten.Storage;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace EventSourcingTests.Aggregation;

public class fetching_async_aggregates_for_writing : OneOffConfigurationsContext
{
    private readonly ITestOutputHelper _output;

    public fetching_async_aggregates_for_writing(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task fetch_new_stream_for_writing_Guid_identifier()
    {
        StoreOptions(opts => opts.Projections.Snapshot<SimpleAggregate>(SnapshotLifecycle.Async));

        var streamId = Guid.NewGuid();

        var stream = await theSession.Events.FetchForWriting<SimpleAggregate>(streamId);
        stream.Aggregate.ShouldBeNull();
        stream.CurrentVersion.ShouldBe(0);

        stream.AppendOne(new AEvent());
        stream.AppendMany(new BEvent(), new BEvent(), new BEvent());
        stream.AppendMany(new CEvent(), new CEvent());

        await theSession.SaveChangesAsync();

        var document = await theSession.Events.AggregateStreamAsync<SimpleAggregate>(streamId);
        document.ACount.ShouldBe(1);
        document.BCount.ShouldBe(3);
        document.CCount.ShouldBe(2);
    }

    [Fact]
    public async Task fetch_new_stream_for_writing_Guid_identifier_exception_handling()
    {
        StoreOptions(opts => opts.Projections.Snapshot<SimpleAggregate>(SnapshotLifecycle.Async));

        var streamId = Guid.NewGuid();

        var stream = await theSession.Events.FetchForWriting<SimpleAggregate>(streamId);
        SpecificationExtensions.ShouldBeNull(stream.Aggregate);
        stream.CurrentVersion.ShouldBe(0);

        stream.AppendOne(new AEvent());
        stream.AppendMany(new BEvent(), new BEvent(), new BEvent());
        stream.AppendMany(new CEvent(), new CEvent());

        await theSession.SaveChangesAsync();

        var sameStream = theSession.Events.StartStream(streamId, new AEvent());
        await Exception<ExistingStreamIdCollisionException>.ShouldBeThrownByAsync(async () =>
        {
            await theSession.SaveChangesAsync();
        });

        var document = await theSession.Events.AggregateStreamAsync<SimpleAggregate>(streamId);
        document.ACount.ShouldBe(1);
        document.BCount.ShouldBe(3);
        document.CCount.ShouldBe(2);
    }

    [Fact]
    public async Task fetch_existing_stream_for_writing_Guid_identifier()
    {
        StoreOptions(opts => opts.Projections.Snapshot<SimpleAggregate>(SnapshotLifecycle.Async));

        var streamId = Guid.NewGuid();

        theSession.Events.StartStream<SimpleAggregate>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        var stream = await theSession.Events.FetchForWriting<SimpleAggregate>(streamId);

        SpecificationExtensions.ShouldNotBeNull(stream.Aggregate);
        stream.CurrentVersion.ShouldBe(6);

        var document = stream.Aggregate;

        document.Id.ShouldBe(streamId);

        document.ACount.ShouldBe(1);
        document.BCount.ShouldBe(3);

        document.CCount.ShouldBe(2);
    }

    [Fact]
    public async Task fetch_existing_stream_for_writing_Guid_identifier_multi_tenanted()
    {
        StoreOptions(opts =>
        {
            opts.Events.TenancyStyle = TenancyStyle.Conjoined;
            opts.Projections.Snapshot<SimpleAggregate>(SnapshotLifecycle.Async).MultiTenanted();
        });

        var streamId = Guid.NewGuid();

        theSession.Events.StartStream<SimpleAggregate>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        var stream = await theSession.Events.FetchForWriting<SimpleAggregate>(streamId);
        SpecificationExtensions.ShouldNotBeNull(stream.Aggregate);
        stream.CurrentVersion.ShouldBe(6);

        var document = stream.Aggregate;

        document.Id.ShouldBe(streamId);

        document.ACount.ShouldBe(1);
        document.BCount.ShouldBe(3);
        document.CCount.ShouldBe(2);
    }

    [Fact]
    public async Task fetch_new_stream_for_writing_string_identifier()
    {
        StoreOptions(opts =>
        {
            opts.Projections.Snapshot<SimpleAggregateAsString>(SnapshotLifecycle.Async);
            opts.Events.StreamIdentity = StreamIdentity.AsString;
        });

        var streamId = Guid.NewGuid().ToString();

        var stream = await theSession.Events.FetchForWriting<SimpleAggregateAsString>(streamId);
        SpecificationExtensions.ShouldBeNull(stream.Aggregate);
        stream.CurrentVersion.ShouldBe(0);

        stream.AppendOne(new AEvent());
        stream.AppendMany(new BEvent(), new BEvent(), new BEvent());
        stream.AppendMany(new CEvent(), new CEvent());

        await theSession.SaveChangesAsync();

        var document = await theSession.Events.AggregateStreamAsync<SimpleAggregateAsString>(streamId);
        document.ACount.ShouldBe(1);
        document.BCount.ShouldBe(3);
        document.CCount.ShouldBe(2);
    }

    [Fact]
    public async Task fetch_existing_stream_for_writing_string_identifier()
    {
        StoreOptions(opts =>
        {
            opts.Projections.Snapshot<SimpleAggregateAsString>(SnapshotLifecycle.Async);
            opts.Events.StreamIdentity = StreamIdentity.AsString;
        });


        var streamId = Guid.NewGuid().ToString();

        theSession.Events.StartStream<SimpleAggregateAsString>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        var stream = await theSession.Events.FetchForWriting<SimpleAggregateAsString>(streamId);
        SpecificationExtensions.ShouldNotBeNull(stream.Aggregate);
        stream.CurrentVersion.ShouldBe(6);

        var document = stream.Aggregate;

        document.Id.ShouldBe(streamId);

        document.ACount.ShouldBe(1);
        document.BCount.ShouldBe(3);
        document.CCount.ShouldBe(2);
    }

    [Fact]
    public async Task fetch_existing_stream_for_writing_string_identifier_multi_tenanted()
    {
        StoreOptions(opts =>
        {
            opts.Projections.Snapshot<SimpleAggregateAsString>(SnapshotLifecycle.Async).MultiTenanted();
            opts.Events.StreamIdentity = StreamIdentity.AsString;
            opts.Events.TenancyStyle = TenancyStyle.Conjoined;
        });


        var streamId = Guid.NewGuid().ToString();

        theSession.Events.StartStream<SimpleAggregateAsString>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        var stream = await theSession.Events.FetchForWriting<SimpleAggregateAsString>(streamId);
        SpecificationExtensions.ShouldNotBeNull(stream.Aggregate);
        stream.CurrentVersion.ShouldBe(6);

        var document = stream.Aggregate;

        document.Id.ShouldBe(streamId);

        document.ACount.ShouldBe(1);
        document.BCount.ShouldBe(3);
        document.CCount.ShouldBe(2);
    }

    [Fact]
    public async Task fetch_existing_stream_exclusively_happy_path_for_writing_Guid_identifier()
    {
        StoreOptions(opts => opts.Projections.Snapshot<SimpleAggregate>(SnapshotLifecycle.Async));

        var streamId = Guid.NewGuid();

        theSession.Events.StartStream<SimpleAggregate>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        theSession.Logger = new TestOutputMartenLogger(_output);

        var stream = await theSession.Events.FetchForExclusiveWriting<SimpleAggregate>(streamId);
        stream.Aggregate.ShouldNotBeNull();
        stream.CurrentVersion.ShouldBe(6);

        var document = stream.Aggregate;

        document.Id.ShouldBe(streamId);

        document.ACount.ShouldBe(1);
        document.BCount.ShouldBe(3);
        document.CCount.ShouldBe(2);
    }

    [Fact]
    public async Task fetch_existing_stream_for_writing_Guid_identifier_sad_path()
    {
        StoreOptions(opts => opts.Projections.Snapshot<SimpleAggregate>(SnapshotLifecycle.Async));


        var streamId = Guid.NewGuid();

        theSession.Events.StartStream<SimpleAggregate>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();


        await using var otherSession = theStore.LightweightSession();
        var otherStream = await otherSession.Events.FetchForExclusiveWriting<SimpleAggregate>(streamId);

        await Should.ThrowAsync<StreamLockedException>(async () =>
        {
            // Try to load it again, but it's locked
            var stream = await theSession.Events.FetchForExclusiveWriting<SimpleAggregate>(streamId);
        });
    }

    [Fact]
    public async Task fetch_existing_stream_exclusively_happy_path_for_writing_string_identifier()
    {
        StoreOptions(opts =>
        {
            opts.Projections.Snapshot<SimpleAggregateAsString>(SnapshotLifecycle.Async);
            opts.Events.StreamIdentity = StreamIdentity.AsString;
        });

        var streamId = Guid.NewGuid().ToString();

        theSession.Events.StartStream<SimpleAggregateAsString>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        var stream = await theSession.Events.FetchForExclusiveWriting<SimpleAggregateAsString>(streamId);
        SpecificationExtensions.ShouldNotBeNull(stream.Aggregate);
        stream.CurrentVersion.ShouldBe(6);

        var document = stream.Aggregate;

        document.Id.ShouldBe(streamId);

        document.ACount.ShouldBe(1);
        document.BCount.ShouldBe(3);
        document.CCount.ShouldBe(2);
    }

    [Fact]
    public async Task fetch_existing_stream_for_writing_string_identifier_sad_path()
    {
        StoreOptions(opts =>
        {
            opts.Projections.Snapshot<SimpleAggregateAsString>(SnapshotLifecycle.Async);
            opts.Events.StreamIdentity = StreamIdentity.AsString;
        });

        var streamId = Guid.NewGuid().ToString();

        theSession.Events.StartStream<SimpleAggregateAsString>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();


        await using var otherSession = theStore.LightweightSession();
        var otherStream = await otherSession.Events.FetchForExclusiveWriting<SimpleAggregateAsString>(streamId);

        await Should.ThrowAsync<StreamLockedException>(async () =>
        {
            // Try to load it again, but it's locked
            var stream = await theSession.Events.FetchForExclusiveWriting<SimpleAggregateAsString>(streamId);
        });
    }







        [Fact]
    public async Task fetch_existing_stream_for_writing_Guid_identifier_with_expected_version()
    {
        StoreOptions(opts => opts.Projections.Snapshot<SimpleAggregate>(SnapshotLifecycle.Async));


        var streamId = Guid.NewGuid();

        theSession.Events.StartStream<SimpleAggregate>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        var stream = await theSession.Events.FetchForWriting<SimpleAggregate>(streamId, 6);
        SpecificationExtensions.ShouldNotBeNull(stream.Aggregate);
        stream.CurrentVersion.ShouldBe(6);

        stream.AppendOne(new EEvent());
        await theSession.SaveChangesAsync();
    }

    [Fact]
    public async Task fetch_existing_stream_for_writing_Guid_identifier_with_expected_version_immediate_sad_path()
    {
        StoreOptions(opts => opts.Projections.Snapshot<SimpleAggregate>(SnapshotLifecycle.Async));


        var streamId = Guid.NewGuid();

        theSession.Events.StartStream<SimpleAggregate>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        await Should.ThrowAsync<ConcurrencyException>(async () =>
        {
            var stream = await theSession.Events.FetchForWriting<SimpleAggregate>(streamId, 5);
        });
    }

    [Fact]
    public async Task fetch_existing_stream_for_writing_Guid_identifier_with_expected_version_sad_path_on_save_changes()
    {
        StoreOptions(opts => opts.Projections.Snapshot<SimpleAggregate>(SnapshotLifecycle.Async));


        var streamId = Guid.NewGuid();

        theSession.Events.StartStream<SimpleAggregate>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        // This should be fine
        var stream = await theSession.Events.FetchForWriting<SimpleAggregate>(streamId, 6);
        stream.AppendOne(new EEvent());

        // Get in between and run other events in a different session
        await using (var otherSession = theStore.LightweightSession())
        {
            otherSession.Events.Append(streamId, new EEvent());
            await otherSession.SaveChangesAsync();
        }

        // The version is now off
        await Should.ThrowAsync<ConcurrencyException>(async () =>
        {
            await theSession.SaveChangesAsync();
        });
    }



    [Fact]
    public async Task fetch_existing_stream_for_writing_string_identifier_with_expected_version()
    {
        StoreOptions(opts =>
        {
            opts.Projections.Snapshot<SimpleAggregateAsString>(SnapshotLifecycle.Async);
            opts.Events.StreamIdentity = StreamIdentity.AsString;
        });

        var streamId = Guid.NewGuid().ToString();

        theSession.Events.StartStream<SimpleAggregateAsString>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        var stream = await theSession.Events.FetchForWriting<SimpleAggregateAsString>(streamId, 6);
        stream.Aggregate.ShouldNotBeNull();
        stream.CurrentVersion.ShouldBe(6);

        stream.AppendOne(new EEvent());
        await theSession.SaveChangesAsync();
    }

    [Fact]
    public async Task fetch_existing_stream_for_writing_string_identifier_with_expected_version_immediate_sad_path()
    {
        StoreOptions(opts =>
        {
            opts.Projections.Snapshot<SimpleAggregateAsString>(SnapshotLifecycle.Async);
            opts.Events.StreamIdentity = StreamIdentity.AsString;
        });

        var streamId = Guid.NewGuid().ToString();

        theSession.Events.StartStream<SimpleAggregateAsString>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        await Should.ThrowAsync<ConcurrencyException>(async () =>
        {
            var stream = await theSession.Events.FetchForWriting<SimpleAggregateAsString>(streamId, 5);
        });
    }

    [Fact]
    public async Task fetch_existing_stream_for_writing_string_identifier_with_expected_version_sad_path_on_save_changes()
    {
        StoreOptions(opts =>
        {
            opts.Projections.Snapshot<SimpleAggregateAsString>(SnapshotLifecycle.Async);
            opts.Events.StreamIdentity = StreamIdentity.AsString;
        });

        var streamId = Guid.NewGuid().ToString();

        theSession.Events.StartStream<SimpleAggregateAsString>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        // This should be fine
        var stream = await theSession.Events.FetchForWriting<SimpleAggregateAsString>(streamId, 6);
        stream.AppendOne(new EEvent());

        // Get in between and run other events in a different session
        await using (var otherSession = theStore.LightweightSession())
        {
            otherSession.Events.Append(streamId, new EEvent());
            await otherSession.SaveChangesAsync();
        }

        // The version is now off
        await Should.ThrowAsync<ConcurrencyException>(async () =>
        {
            await theSession.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task fetch_aggregate_that_is_completely_caught_up()
    {
        StoreOptions(opts =>
        {
            opts.Projections.Snapshot<SimpleAggregateAsString>(SnapshotLifecycle.Async);
            opts.Events.StreamIdentity = StreamIdentity.AsString;
        });

        var streamId = Guid.NewGuid().ToString();

        theSession.Events.StartStream<SimpleAggregateAsString>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        var daemon = await theStore.BuildProjectionDaemonAsync();
        await daemon.RebuildProjectionAsync<SimpleAggregateAsString>(CancellationToken.None);

        var stream = await theSession.Events.FetchForWriting<SimpleAggregateAsString>(streamId, 6);
        stream.Aggregate.ShouldNotBeNull();
        stream.CurrentVersion.ShouldBe(6);

        stream.AppendOne(new EEvent());
        await theSession.SaveChangesAsync();
    }

    [Fact]
    public async Task fetch_aggregate_that_is_in_progress()
    {
        StoreOptions(opts =>
        {
            opts.Projections.Snapshot<SimpleAggregateAsString>(SnapshotLifecycle.Async);
            opts.Events.StreamIdentity = StreamIdentity.AsString;
        });

        var streamId = Guid.NewGuid().ToString();

        theSession.Events.StartStream<SimpleAggregateAsString>(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent(),
            new CEvent(), new CEvent());
        await theSession.SaveChangesAsync();

        var daemon = await theStore.BuildProjectionDaemonAsync();
        await daemon.RebuildProjectionAsync<SimpleAggregateAsString>(CancellationToken.None);
        await daemon.StopAllAsync();

        var existing = await theSession.LoadAsync<SimpleAggregateAsString>(streamId);

        var stream = await theSession.Events.FetchForWriting<SimpleAggregateAsString>(streamId, 6);
        stream.Aggregate.ShouldNotBeNull();
        stream.CurrentVersion.ShouldBe(6);

        existing = await theSession.LoadAsync<SimpleAggregateAsString>(streamId);

        stream.AppendOne(new EEvent());
        await theSession.SaveChangesAsync();

        using (var session = theStore.LightweightSession())
        {
            session.Events.Append(streamId, new AEvent(), new BEvent(), new BEvent(), new BEvent());
            await session.SaveChangesAsync();
        }

        stream = await theSession.Events.FetchForWriting<SimpleAggregateAsString>(streamId, 11);
        stream.Aggregate.ShouldNotBeNull();
        stream.CurrentVersion.ShouldBe(11);

        stream.Aggregate.BCount.ShouldBe(6);
    }
}
