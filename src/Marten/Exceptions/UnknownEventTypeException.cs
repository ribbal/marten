using System.Runtime.Serialization;

namespace Marten.Exceptions;

public class UnknownEventTypeException: MartenException
{
    public string EventTypeName { get; }

    public UnknownEventTypeException(string eventTypeName): base(
        $"Unknown event type name alias '{eventTypeName}.' You may need to register this event type through StoreOptions.Events.AddEventType(type)")
    {
        EventTypeName = eventTypeName;
    }

    protected UnknownEventTypeException(SerializationInfo info, StreamingContext context): base(info, context)
    {
    }
}
