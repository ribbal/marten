using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Marten.Events.Daemon.Internals;

internal class ResilientEventLoader: IEventLoader
{
    private readonly ResiliencePipeline _pipeline;
    private readonly EventLoader _inner;

    internal record EventLoadExecution(EventRequest Request, IEventLoader Loader)
    {
        public async ValueTask<EventPage> ExecuteAsync(CancellationToken token)
        {
            var results = await Loader.LoadAsync(Request, token).ConfigureAwait(false);
            return results;
        }
    }

    public ResilientEventLoader(ResiliencePipeline pipeline, EventLoader inner)
    {
        _pipeline = pipeline;
        _inner = inner;
    }

    public Task<EventPage> LoadAsync(EventRequest request, CancellationToken token)
    {
        try
        {
            var execution = new EventLoadExecution(request, _inner);
            return _pipeline.ExecuteAsync(static (x, t) => x.ExecuteAsync(t),
                execution, token).AsTask();
        }
        catch (Exception e)
        {
            throw new EventLoaderException(request.Name, _inner.Database, e);
        }
    }
}
