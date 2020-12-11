using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LamarCodeGeneration;
using LamarCodeGeneration.Frames;
using LamarCodeGeneration.Model;
using LamarCodeGeneration.Util;
using Marten.Schema;

namespace Marten.Events.V4Concept.CodeGeneration
{
    internal class MethodSlot
    {
        public Setter Setter { get; }
        public MethodInfo Method { get; }

        public MethodSlot(MethodInfo method)
        {
            Method = method;
            EventType = method.GetEventType();
        }

        public Type EventType { get; }

        public MethodSlot(Setter setter, MethodInfo method, Type eventType)
        {
            Setter = setter;
            Method = method;
            EventType = eventType;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal class IgnoreProjectionMethodAttribute: Attribute
    {

    }

    internal abstract class MethodCollection
    {
        private int _lambdaNumber = 0;


        public Type ProjectionType { get; }

        protected MethodCollection(string methodName, Type projectionType)
        {
            ProjectionType = projectionType;

            Methods = projectionType
                .GetMethods()
                .Where(x => x.Name == methodName)
                .Where(x => !x.HasAttribute<IgnoreProjectionMethodAttribute>())
                .Select(x => new MethodSlot(x)).ToList();

            IsAsync = Methods.Any(x => x.Method.IsAsync());
            LambdaName = methodName;
        }

        public string LambdaName { get; protected set; }

        public void AddLambda<T>(T lambda, Type eventType)
        {
            var name = LambdaName + (++_lambdaNumber).ToString();
            var method = lambda.GetType().GetMethod("Invoke");
            var setter = new Setter(typeof(T), name);
            var slot = new MethodSlot(setter, method, eventType);
            Methods.Add(slot);
        }

        public abstract IEventHandlingFrame CreateEventTypeHandler(Type aggregateType,
            DocumentMapping aggregateMapping, MethodInfo method);

        public List<MethodSlot> Methods { get; }

        public bool IsAsync { get;}

        public static IList<Frame> AddEventHandling(Type aggregateType, DocumentMapping mapping,
            params MethodCollection[] collections)
        {
            var byType = new Dictionary<Type, EventProcessingFrame>();

            // TODO -- later we'll worry about abstract/interface applications
            // of events

            var frames = new List<Frame>();

            var ifStyle = IfStyle.If;

            foreach (var collection in collections)
            {
                foreach (var slot in collection.Methods)
                {
                    var frame = collection.CreateEventTypeHandler(aggregateType, mapping, slot.Method);
                    if (byType.TryGetValue(frame.EventType, out var container))
                    {
                        container.Add((Frame) frame);
                    }
                    else
                    {
                        container = new EventProcessingFrame(aggregateType, frame)
                        {
                            IfStyle = ifStyle
                        };

                        ifStyle = IfStyle.ElseIf;

                        byType.Add(frame.EventType, container);

                        frames.Add(container);
                    }

                }
            }

            return frames;
        }
    }
}