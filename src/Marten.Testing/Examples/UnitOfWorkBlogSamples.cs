using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JasperFx.Core;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace Marten.Testing.Examples;

public class UnitOfWorkBlogSamples: IntegrationContext
{
    [Fact]
    public void show_unit_of_work()
    {
        // theStore is a DocumentStore
        using var session = theStore.LightweightSession();
        // All I'm doing here is recording references
        // to all the ADO.Net commands executed by
        // this session
        var logger = new RecordingLogger();
        session.Logger = logger;

        // Insert some new documents
        session.Store(new User { UserName = "luke", FirstName = "Luke", LastName = "Skywalker" });
        session.Store(new User { UserName = "leia", FirstName = "Leia", LastName = "Organa" });
        session.Store(new User { UserName = "wedge", FirstName = "Wedge", LastName = "Antilles" });

        // Delete all users matching a certain criteria
        session.DeleteWhere<User>(x => x.UserName == "hansolo");

        // deleting a single document by Id, if you had one
        session.Delete<User>(Guid.NewGuid());

        // Persist in a single transaction
        session.SaveChanges();

        // All of this was done in one batched command
        // in the same transaction
        logger.Commands.Count.ShouldBe(5);

        // I'm just writing out the Sql executed here
        var sql = logger.Commands[0].CommandText;
        Debug.WriteLine(sql);
    }

    public UnitOfWorkBlogSamples(DefaultStoreFixture fixture) : base(fixture)
    {
    }
}
