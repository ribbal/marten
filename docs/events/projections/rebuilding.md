# Rebuilding Projections

Projections can be completely rebuilt with the [async daemon](/events/projections/async-daemon) subsystem. Both inline
and asynchronous projections can be rebuilt with the async daemon.

<!-- snippet: sample_using_create_in_event_projection -->
<a id='snippet-sample_using_create_in_event_projection'></a>
```cs
public class DistanceProjection: EventProjection
{
    public DistanceProjection()
    {
        ProjectionName = "Distance";
    }

    // Create a new Distance document based on a Travel event
    public Distance Create(Travel travel, IEvent e)
    {
        return new Distance {Id = e.Id, Day = travel.Day, Total = travel.TotalDistance()};
    }
}
```
<sup><a href='https://github.com/JasperFx/marten/blob/master/src/Marten.AsyncDaemon.Testing/event_projections_end_to_end.cs#L160-L176' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_using_create_in_event_projection' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: sample_rebuild-single-projection -->
<a id='snippet-sample_rebuild-single-projection'></a>
```cs
StoreOptions(x => x.Projections.Add(new DistanceProjection(), ProjectionLifecycle.Async));

var agent = await StartDaemon();

// setup test data
await PublishSingleThreaded();

// rebuild projection `Distance`
await agent.RebuildProjectionAsync("Distance", CancellationToken.None);
```
<sup><a href='https://github.com/JasperFx/marten/blob/master/src/Marten.AsyncDaemon.Testing/event_projections_end_to_end.cs#L91-L101' title='Snippet source file'>snippet source</a> | <a href='#snippet-sample_rebuild-single-projection' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
