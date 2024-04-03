using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Frames;
using JasperFx.CodeGeneration.Model;
using JasperFx.Core;
using JasperFx.Core.Reflection;
using JasperFx.RuntimeCompiler;
using Marten.Events.Archiving;
using Marten.Events.Operations;
using Marten.Events.Querying;
using Marten.Events.Schema;
using Marten.Internal;
using Marten.Internal.CodeGeneration;
using Marten.Schema;
using Marten.Storage;
using Marten.Storage.Metadata;
using Npgsql;
using Weasel.Postgresql;

namespace Marten.Events.CodeGeneration;

internal static class EventDocumentStorageGenerator
{
    private const string StreamStateSelectorTypeName = "GeneratedStreamStateQueryHandler";
    private const string InsertStreamOperationName = "GeneratedInsertStream";
    private const string UpdateStreamVersionOperationName = "GeneratedStreamVersionOperation";
    internal const string EventDocumentStorageTypeName = "GeneratedEventDocumentStorage";

    /// <summary>
    ///     Only for testing support
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static EventDocumentStorage GenerateStorage(StoreOptions options)
    {
        var assembly =
            new GeneratedAssembly(new GenerationRules(SchemaConstants.MartenGeneratedNamespace + ".EventStore"));
        var builderType = AssembleTypes(options, assembly);


        var compiler = new AssemblyGenerator();
        compiler.ReferenceAssembly(typeof(IMartenSession).Assembly);
        compiler.Compile(assembly);


        return (EventDocumentStorage)Activator.CreateInstance(builderType.CompiledType, options);
    }

    public static GeneratedType AssembleTypes(StoreOptions options, GeneratedAssembly assembly)
    {
        assembly.ReferenceAssembly(typeof(EventGraph).Assembly);

        var builderType = assembly.AddType(EventDocumentStorageTypeName, typeof(EventDocumentStorage));

        buildSelectorMethods(options, builderType);

        var appendEventOperationType = buildAppendEventOperation(options.EventGraph, assembly);

        builderType.MethodFor(nameof(EventDocumentStorage.AppendEvent))
            .Frames.ReturnNewGeneratedTypeObject(appendEventOperationType, "stream", "e");


        buildInsertStream(builderType, assembly, options.EventGraph);

        buildStreamQueryHandlerType(options.EventGraph, assembly);

        buildQueryForStreamMethod(options.EventGraph, builderType);

        buildUpdateStreamVersion(builderType, assembly, options.EventGraph);
        return builderType;
    }

    private static void buildSelectorMethods(StoreOptions options, GeneratedType builderType)
    {
        var sync = builderType.MethodFor(nameof(EventDocumentStorage.ApplyReaderDataToEvent));
        var async = builderType.MethodFor(nameof(EventDocumentStorage.ApplyReaderDataToEventAsync));

        // The json data column has to go first
        var table = new EventsTable(options.EventGraph);
        var columns = table.SelectColumns();

        for (var i = 3; i < columns.Count; i++)
        {
            columns[i].GenerateSelectorCodeSync(sync, options.EventGraph, i);
            columns[i].GenerateSelectorCodeAsync(async, options.EventGraph, i);
        }
    }

    private static GeneratedType buildUpdateStreamVersion(GeneratedType builderType, GeneratedAssembly assembly,
        EventGraph graph)
    {
        var operationType = assembly.AddType(UpdateStreamVersionOperationName, typeof(UpdateStreamVersion));

        var sql = $"update {graph.DatabaseSchemaName}.mt_streams set version = ? where id = ? and version = ?";
        if (graph.TenancyStyle == TenancyStyle.Conjoined)
        {
            sql += $" and {TenantIdColumn.Name} = ?";
        }

        var setter = operationType.AddStringConstant("SQL", sql);

        var configureCommand = operationType.MethodFor("ConfigureCommand");
        configureCommand.DerivedVariables.Add(
            new Variable(typeof(StreamAction), nameof(UpdateStreamVersion.Stream)));

        configureCommand.Frames.Code($"var parameters = {{0}}.{nameof(CommandBuilder.AppendWithParameters)}(SQL);",
            Use.Type<ICommandBuilder>());

        configureCommand.SetParameterFromMember<StreamAction>(0, x => x.Version);

        if (graph.StreamIdentity == StreamIdentity.AsGuid)
        {
            configureCommand.SetParameterFromMember<StreamAction>(1, x => x.Id);
        }
        else
        {
            configureCommand.SetParameterFromMember<StreamAction>(1, x => x.Key);
        }

        configureCommand.SetParameterFromMember<StreamAction>(2, x => x.ExpectedVersionOnServer);

        if (graph.TenancyStyle == TenancyStyle.Conjoined)
        {
            new TenantIdColumn().As<IStreamTableColumn>().GenerateAppendCode(configureCommand, 3);
        }

        builderType.MethodFor(nameof(EventDocumentStorage.UpdateStreamVersion))
            .Frames.Code($"return new {assembly.Namespace}.{UpdateStreamVersionOperationName}({{0}});",
                Use.Type<StreamAction>());

        return operationType;
    }

    private static void buildQueryForStreamMethod(EventGraph graph, GeneratedType builderType)
    {
        var arguments = new List<string>
        {
            graph.StreamIdentity == StreamIdentity.AsGuid
                ? $"stream.{nameof(StreamAction.Id)}"
                : $"stream.{nameof(StreamAction.Key)}"
        };

        if (graph.TenancyStyle == TenancyStyle.Conjoined)
        {
            arguments.Add($"stream.{nameof(StreamAction.TenantId)}");
        }

        builderType.MethodFor(nameof(EventDocumentStorage.QueryForStream))
            .Frames.Code(
                $"return new {builderType.ParentAssembly.Namespace}.{StreamStateSelectorTypeName}({arguments.Join(", ")});");
    }

    private static GeneratedType buildStreamQueryHandlerType(EventGraph graph, GeneratedAssembly assembly)
    {
        var streamQueryHandlerType =
            assembly.AddType(StreamStateSelectorTypeName, typeof(StreamStateQueryHandler));

        streamQueryHandlerType.AllInjectedFields.Add(graph.StreamIdentity == StreamIdentity.AsGuid
            ? new InjectedField(typeof(Guid), "streamId")
            : new InjectedField(typeof(string), "streamId"));

        buildConfigureCommandMethodForStreamState(graph, streamQueryHandlerType);

        var sync = streamQueryHandlerType.MethodFor("Resolve");
        var async = streamQueryHandlerType.MethodFor("ResolveAsync");


        sync.Frames.Add(new ConstructorFrame<StreamState>(() => new StreamState()));
        async.Frames.Add(new ConstructorFrame<StreamState>(() => new StreamState()));

        if (graph.StreamIdentity == StreamIdentity.AsGuid)
        {
            sync.AssignMemberFromReader<StreamState>(streamQueryHandlerType, 0, x => x.Id);
            async.AssignMemberFromReaderAsync<StreamState>(streamQueryHandlerType, 0, x => x.Id);
        }
        else
        {
            sync.AssignMemberFromReader<StreamState>(streamQueryHandlerType, 0, x => x.Key);
            async.AssignMemberFromReaderAsync<StreamState>(streamQueryHandlerType, 0, x => x.Key);
        }

        sync.AssignMemberFromReader<StreamState>(streamQueryHandlerType, 1, x => x.Version);
        async.AssignMemberFromReaderAsync<StreamState>(streamQueryHandlerType, 1, x => x.Version);

        sync.Frames.Call<StreamStateQueryHandler>(x => x.SetAggregateType(null, null, null), call =>
        {
            call.IsLocal = true;
        });

#pragma warning disable 4014
        async.Frames.Call<StreamStateQueryHandler>(
            x => x.SetAggregateTypeAsync(null, null, null, CancellationToken.None), call =>
#pragma warning restore 4014
            {
                call.IsLocal = true;
            });

        sync.AssignMemberFromReader<StreamState>(streamQueryHandlerType, 3, x => x.LastTimestamp);
        async.AssignMemberFromReaderAsync<StreamState>(streamQueryHandlerType, 3, x => x.LastTimestamp);

        sync.AssignMemberFromReader<StreamState>(streamQueryHandlerType, 4, x => x.Created);
        async.AssignMemberFromReaderAsync<StreamState>(streamQueryHandlerType, 4, x => x.Created);

        sync.AssignMemberFromReader<StreamState>(streamQueryHandlerType, 5, x => x.IsArchived);
        async.AssignMemberFromReaderAsync<StreamState>(streamQueryHandlerType, 5, x => x.IsArchived);

        sync.Frames.Return(typeof(StreamState));
        async.Frames.Return(typeof(StreamState));

        return streamQueryHandlerType;
    }

    private static void buildConfigureCommandMethodForStreamState(EventGraph graph,
        GeneratedType streamQueryHandlerType)
    {
        var sql =
            $"select id, version, type, timestamp, created as timestamp, is_archived from {graph.DatabaseSchemaName}.mt_streams where id = ?";
        if (graph.TenancyStyle == TenancyStyle.Conjoined)
        {
            streamQueryHandlerType.AllInjectedFields.Add(new InjectedField(typeof(string), "tenantId"));
            sql += $" and {TenantIdColumn.Name} = ?";
        }

        var setter = streamQueryHandlerType.AddStringConstant("SQL", sql);

        var configureCommand = streamQueryHandlerType.MethodFor("ConfigureCommand");
        configureCommand.Frames.Call<ICommandBuilder>(x => x.AppendWithParameters(""), call =>
        {
            call.Arguments[0] = setter;
            call.ReturnAction = ReturnAction.Initialize;
        });

        var idDbType = graph.StreamIdentity == StreamIdentity.AsGuid ? DbType.Guid : DbType.String;
        configureCommand.Frames.Code("{0}[0].Value = _streamId;", Use.Type<NpgsqlParameter[]>());
        configureCommand.Frames.Code("{0}[0].DbType = {1};", Use.Type<NpgsqlParameter[]>(), idDbType);

        if (graph.TenancyStyle == TenancyStyle.Conjoined)
        {
            configureCommand.Frames.Code("{0}[1].Value = _tenantId;", Use.Type<NpgsqlParameter[]>());
            configureCommand.Frames.Code("{0}[1].DbType = {1};", Use.Type<NpgsqlParameter[]>(), DbType.String);
        }
    }

    private static GeneratedType buildAppendEventOperation(EventGraph graph, GeneratedAssembly assembly)
    {
        var operationType = assembly.AddType("AppendEventOperation", typeof(AppendEventOperationBase));

        var configure = operationType.MethodFor(nameof(AppendEventOperationBase.ConfigureCommand));
        configure.DerivedVariables.Add(new Variable(typeof(IEvent), nameof(AppendEventOperationBase.Event)));
        configure.DerivedVariables.Add(new Variable(typeof(StreamAction), nameof(AppendEventOperationBase.Stream)));

        var columns = new EventsTable(graph).SelectColumns()

            // Hokey, use an explicit model for writeable vs readable columns some day
            .Where(x => !(x is IsArchivedColumn)).ToList();

        var sql =
            $"insert into {graph.DatabaseSchemaName}.mt_events ({columns.Select(x => x.Name).Join(", ")}) values ({columns.Select(_ => "?").Join(", ")})";

        operationType.AddStringConstant("SQL", sql);

        configure.Frames.Code($"var parameters = {{0}}.{nameof(CommandBuilder.AppendWithParameters)}(SQL);",
            Use.Type<ICommandBuilder>());

        for (var i = 0; i < columns.Count; i++)
        {
            columns[i].GenerateAppendCode(configure, graph, i);
        }

        return operationType;
    }

    private static GeneratedType buildInsertStream(GeneratedType builderType, GeneratedAssembly generatedAssembly,
        EventGraph graph)
    {
        var operationType = generatedAssembly.AddType(InsertStreamOperationName, typeof(InsertStreamBase));

        var columns = new StreamsTable(graph)
            .Columns
            .OfType<IStreamTableColumn>()
            .Where(x => x.Writes)
            .ToArray();

        var sql =
            $"insert into {graph.DatabaseSchemaName}.mt_streams ({columns.Select(x => x.Name).Join(", ")}) values ({columns.Select(_ => "?").Join(", ")})";
        operationType.AddStringConstant("SQL", sql);

        var configureCommand = operationType.MethodFor("ConfigureCommand");
        configureCommand.DerivedVariables.Add(new Variable(typeof(StreamAction), nameof(InsertStreamBase.Stream)));

        configureCommand.Frames.Code($"var parameters = {{0}}.{nameof(CommandBuilder.AppendWithParameters)}(SQL);",
            Use.Type<ICommandBuilder>());

        for (var i = 0; i < columns.Length; i++)
        {
            columns[i].GenerateAppendCode(configureCommand, i);
        }

        builderType.MethodFor(nameof(EventDocumentStorage.InsertStream))
            .Frames.ReturnNewGeneratedTypeObject(operationType, "stream");

        return operationType;
    }
}
