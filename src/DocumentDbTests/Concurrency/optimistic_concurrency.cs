using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JasperFx.Core.Reflection;
using Marten;
using Marten.Exceptions;
using Marten.Schema;
using Marten.Services;
using Marten.Storage.Metadata;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Shouldly;
using Xunit;

namespace DocumentDbTests.Concurrency;

public class OptimisticConcurrencyStoreFixture: StoreFixture
{
    public OptimisticConcurrencyStoreFixture() : base("optimistic_concurrency")
    {
        Options.Policies.AllDocumentsEnforceOptimisticConcurrency();
        Options.Schema.For<Shop>().AddSubClass<CoffeeShop>();
    }
}

public class optimistic_concurrency: StoreContext<OptimisticConcurrencyStoreFixture>, IClassFixture<OptimisticConcurrencyStoreFixture>
{
    public optimistic_concurrency(OptimisticConcurrencyStoreFixture fixture) : base(fixture)
    {
    }

    public void example_configuration()
    {
        #region sample_configuring-optimistic-concurrency
        var store = DocumentStore.For(_ =>
        {
            // Adds optimistic concurrency checking to Issue
            _.Schema.For<Issue>().UseOptimisticConcurrency(true);
        });
        #endregion
    }

    [Fact]
    public void can_insert_with_optimistic_concurrency_95()
    {
        using var session = theStore.LightweightSession();
        var coffeeShop = new CoffeeShop();
        session.Store(coffeeShop);
        session.SaveChanges();

        SpecificationExtensions.ShouldNotBeNull(session.Load<CoffeeShop>(coffeeShop.Id));
    }

    [Fact]
    public async Task can_insert_with_optimistic_concurrency_95_async()
    {
        await using var session = theStore.LightweightSession();
        var coffeeShop = new CoffeeShop();
        session.Store(coffeeShop);
        await session.SaveChangesAsync();

        SpecificationExtensions.ShouldNotBeNull((await session.LoadAsync<CoffeeShop>(coffeeShop.Id)));
    }

    [Fact]
    public void can_store_same_document_multiple_times_with_optimistic_concurrency()
    {
        var doc1 = new CoffeeShop();
        using var session = theStore.LightweightSession();
        session.Store(doc1);
        session.Store(doc1);

        session.SaveChanges();
    }

    [Fact]
    public void can_update_with_optimistic_concurrency_95()
    {
        var doc1 = new CoffeeShop();
        using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            session.SaveChanges();
        }

        using (var session = theStore.LightweightSession())
        {
            var doc2 = session.Load<CoffeeShop>(doc1.Id);
            doc2.Name = "Mozart's";

            session.Store(doc2);
            session.SaveChanges();
        }

        using (var session = theStore.QuerySession())
        {
            session.Load<CoffeeShop>(doc1.Id).Name.ShouldBe("Mozart's");
        }
    }

    [Fact]
    public async Task can_update_with_optimistic_concurrency_95_async()
    {
        var doc1 = new CoffeeShop();
        await using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            await session.SaveChangesAsync();
        }

        await using (var session = theStore.LightweightSession())
        {
            var doc2 = await session.LoadAsync<CoffeeShop>(doc1.Id);
            doc2.Name = "Mozart's";

            session.Store(doc2);
            await session.SaveChangesAsync();
        }

        await using (var session = theStore.QuerySession())
        {
            (await session.LoadAsync<CoffeeShop>(doc1.Id)).Name.ShouldBe("Mozart's");
        }
    }

    #region sample_update_with_stale_version_standard
    [Fact]
    public void update_with_stale_version_standard()
    {
        var doc1 = new CoffeeShop();
        using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            session.SaveChanges();
        }

        var session1 = theStore.DirtyTrackedSession();
        var session2 = theStore.DirtyTrackedSession();

        var session1Copy = session1.Load<CoffeeShop>(doc1.Id);
        var session2Copy = session2.Load<CoffeeShop>(doc1.Id);

        try
        {
            session1Copy.Name = "Mozart's";
            session2Copy.Name = "Dominican Joe's";

            // Should go through just fine
            session2.SaveChanges();

            var ex = Exception<ConcurrencyException>.ShouldBeThrownBy(() =>
            {
                session1.SaveChanges();
            });

            ex.Message.ShouldBe($"Optimistic concurrency check failed for {typeof(Shop).FullName} #{doc1.Id}");
        }
        finally
        {
            session1.Dispose();
            session2.Dispose();
        }

        using (var query = theStore.QuerySession())
        {
            query.Load<CoffeeShop>(doc1.Id).Name.ShouldBe("Dominican Joe's");
        }
    }

    #endregion

    [Fact]
    public void overwrite_with_stale_version_standard()
    {
        var doc1 = new CoffeeShop();
        using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            session.SaveChanges();
        }
        #region sample_sample-override-optimistic-concurrency
        var session1 = theStore.DirtyTrackedSession(new SessionOptions
        {
            ConcurrencyChecks = ConcurrencyChecks.Disabled
        });
        #endregion

        var session2 = theStore.DirtyTrackedSession();

        var session1Copy = session1.Load<CoffeeShop>(doc1.Id);
        var session2Copy = session2.Load<CoffeeShop>(doc1.Id);

        try
        {
            session1Copy.Name = "Mozart's";
            session2Copy.Name = "Dominican Joe's";

            // Should go through just fine
            session2.SaveChanges();

            session1.SaveChanges();
        }
        finally
        {
            session1.Dispose();
            session2.Dispose();
        }

        using (var query = theStore.QuerySession())
        {
            query.Load<CoffeeShop>(doc1.Id).Name.ShouldBe("Mozart's");
        }
    }

    [Fact]
    public async Task update_with_stale_version_standard_async()
    {
        var doc1 = new CoffeeShop();
        await using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            await session.SaveChangesAsync();
        }

        var session1 = theStore.DirtyTrackedSession();
        var session2 = theStore.DirtyTrackedSession();

        var session1Copy = await session1.LoadAsync<CoffeeShop>(doc1.Id);
        var session2Copy = await session2.LoadAsync<CoffeeShop>(doc1.Id);

        try
        {
            session1Copy.Name = "Mozart's";
            session2Copy.Name = "Dominican Joe's";

            // Should go through just fine
            await session2.SaveChangesAsync();

            var ex = await Exception<ConcurrencyException>.ShouldBeThrownByAsync(async () =>
            {
                await session1.SaveChangesAsync();
            });

            ex.Message.ShouldBe($"Optimistic concurrency check failed for {typeof(Shop).FullName} #{doc1.Id}");
        }
        finally
        {
            session1.Dispose();
            session2.Dispose();
        }

        await using (var query = theStore.QuerySession())
        {
            (await query.LoadAsync<CoffeeShop>(doc1.Id)).Name.ShouldBe("Dominican Joe's");
        }
    }

    [Fact]
    public void update_with_stale_version_standard_sync()
    {
        var doc1 = new CoffeeShop();
        using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            session.SaveChanges();
        }

        var session1 = theStore.DirtyTrackedSession();
        var session2 = theStore.DirtyTrackedSession();

        var session1Copy = session1.Load<CoffeeShop>(doc1.Id);
        var session2Copy = session2.Load<CoffeeShop>(doc1.Id);

        try
        {
            session1Copy.Name = "Mozart's";
            session2Copy.Name = "Dominican Joe's";

            // Should go through just fine
            session2.SaveChanges();

            var ex = Exception<ConcurrencyException>.ShouldBeThrownBy(() =>
            {
                session1.SaveChanges();
            });

            ex.Message.ShouldBe($"Optimistic concurrency check failed for {typeof(Shop).FullName} #{doc1.Id}");
        }
        finally
        {
            session1.Dispose();
            session2.Dispose();
        }

        using (var query = theStore.QuerySession())
        {
            query.Load<CoffeeShop>(doc1.Id).Name.ShouldBe("Dominican Joe's");
        }
    }

    [Fact]
    public void can_do_multiple_updates_in_a_row_standard()
    {
        var doc1 = new CoffeeShop();
        using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            session.SaveChanges();
        }

        using (var session = theStore.DirtyTrackedSession())
        {
            var doc2 = session.Load<CoffeeShop>(doc1.Id);
            doc2.Name = "Mozart's";

            session.SaveChanges();

            doc2.Name = "Cafe Medici";

            session.SaveChanges();
        }

        using (var query = theStore.QuerySession())
        {
            query.Load<CoffeeShop>(doc1.Id).Name.ShouldBe("Cafe Medici");
        }
    }

    [Fact]
    public async Task can_do_multiple_updates_in_a_row_standard_async()
    {
        var doc1 = new CoffeeShop();
        await using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            await session.SaveChangesAsync();
        }

        await using (var session = theStore.DirtyTrackedSession())
        {
            var doc2 = await session.LoadAsync<CoffeeShop>(doc1.Id);
            doc2.Name = "Mozart's";

            await session.SaveChangesAsync();

            doc2.Name = "Cafe Medici";

            await session.SaveChangesAsync();
        }

        await using (var query = theStore.QuerySession())
        {
            (await query.LoadAsync<CoffeeShop>(doc1.Id)).Name.ShouldBe("Cafe Medici");
        }
    }

    [Fact]
    public void update_multiple_docs_at_a_time_happy_path()
    {
        var doc1 = new CoffeeShop();
        var doc2 = new CoffeeShop();

        using (var session = theStore.LightweightSession())
        {
            session.Store(doc1, doc2);
            session.SaveChanges();
        }

        using (var session = theStore.DirtyTrackedSession())
        {
            var doc12 = session.Load<CoffeeShop>(doc1.Id);
            doc12.Name = "Mozart's";

            var doc22 = session.Load<CoffeeShop>(doc2.Id);
            doc22.Name = "Dominican Joe's";

            session.SaveChanges();
        }

        using (var query = theStore.QuerySession())
        {
            query.Load<CoffeeShop>(doc1.Id).Name.ShouldBe("Mozart's");
            query.Load<CoffeeShop>(doc2.Id).Name.ShouldBe("Dominican Joe's");
        }
    }

    [Fact]
    public async Task update_multiple_docs_at_a_time_happy_path_async()
    {
        var doc1 = new CoffeeShop();
        var doc2 = new CoffeeShop();

        await using (var session = theStore.LightweightSession())
        {
            session.Store(doc1, doc2);
            await session.SaveChangesAsync();
        }

        await using (var session = theStore.DirtyTrackedSession())
        {
            var doc12 = await session.LoadAsync<CoffeeShop>(doc1.Id);
            doc12.Name = "Mozart's";

            var doc22 = await session.LoadAsync<CoffeeShop>(doc2.Id);
            doc22.Name = "Dominican Joe's";

            await session.SaveChangesAsync();
        }

        await using (var query = theStore.QuerySession())
        {
            (await query.LoadAsync<CoffeeShop>(doc1.Id)).Name.ShouldBe("Mozart's");
            (await query.LoadAsync<CoffeeShop>(doc2.Id)).Name.ShouldBe("Dominican Joe's");
        }
    }

    [Fact]
    public void update_multiple_docs_at_a_time_sad_path()
    {
        var doc1 = new CoffeeShop();
        var doc2 = new CoffeeShop();

        using (var session = theStore.LightweightSession())
        {
            session.Store(doc1, doc2);
            session.SaveChanges();
        }

        using (var session = theStore.DirtyTrackedSession())
        {
            var doc12 = session.Load<CoffeeShop>(doc1.Id);
            doc12.Name = "Mozart's";

            var doc22 = session.Load<CoffeeShop>(doc2.Id);
            doc22.Name = "Dominican Joe's";

            using (var other = theStore.DirtyTrackedSession())
            {
                other.Load<CoffeeShop>(doc1.Id).Name = "Genuine Joe's";
                other.Load<CoffeeShop>(doc2.Id).Name = "Cafe Medici";

                other.SaveChanges();
            }

            var ex = Exception<AggregateException>.ShouldBeThrownBy(() =>
            {
                session.SaveChanges();
            });

            ex.InnerExceptions.OfType<ConcurrencyException>().Count().ShouldBe(2);
        }
    }

    [Fact]
    public async Task update_multiple_docs_at_a_time_sad_path_async()
    {
        var doc1 = new CoffeeShop();
        var doc2 = new CoffeeShop();

        await using (var session = theStore.LightweightSession())
        {
            session.Store(doc1, doc2);
            await session.SaveChangesAsync();
        }

        await using (var session = theStore.DirtyTrackedSession())
        {
            var doc12 = await session.LoadAsync<CoffeeShop>(doc1.Id);
            doc12.Name = "Mozart's";

            var doc22 = await session.LoadAsync<CoffeeShop>(doc2.Id);
            doc22.Name = "Dominican Joe's";

            await using (var other = theStore.DirtyTrackedSession())
            {
                (await other.LoadAsync<CoffeeShop>(doc1.Id)).Name = "Genuine Joe's";
                (await other.LoadAsync<CoffeeShop>(doc2.Id)).Name = "Cafe Medici";

                await other.SaveChangesAsync();
            }

            var ex = await Exception<AggregateException>.ShouldBeThrownByAsync(async () =>
            {
                await session.SaveChangesAsync();
            });

            ex.InnerExceptions.OfType<ConcurrencyException>().Count().ShouldBe(2);
        }
    }

    #region sample_store_with_the_right_version
    [Fact]
    public void store_with_the_right_version()
    {
        var doc1 = new CoffeeShop();
        using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            session.SaveChanges();
        }

        DocumentMetadata metadata;
        using (var session = theStore.QuerySession())
        {
            metadata = session.MetadataFor(doc1);
        }

        using (var session = theStore.LightweightSession())
        {
            doc1.Name = "Mozart's";
            session.UpdateExpectedVersion(doc1, metadata.CurrentVersion);

            session.SaveChanges();
        }

        using (var query = theStore.QuerySession())
        {
            query.Load<CoffeeShop>(doc1.Id).Name
                .ShouldBe("Mozart's");
        }
    }

    #endregion

    [Fact]
    public async Task store_with_the_right_version_async()
    {
        var doc1 = new CoffeeShop();
        await using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            await session.SaveChangesAsync();
        }

        DocumentMetadata metadata;
        await using (var session = theStore.QuerySession())
        {
            metadata = await session.MetadataForAsync(doc1);
        }

        await using (var session = theStore.LightweightSession())
        {
            doc1.Name = "Mozart's";
            session.UpdateExpectedVersion(doc1, metadata.CurrentVersion);

            await session.SaveChangesAsync();
        }

        await using (var query = theStore.QuerySession())
        {
            (await query.LoadAsync<CoffeeShop>(doc1.Id)).Name
                .ShouldBe("Mozart's");
        }
    }

    [Fact]
    public void store_with_the_right_version_sad_path()
    {
        var doc1 = new CoffeeShop();
        using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            session.SaveChanges();
        }

        using (var session = theStore.LightweightSession())
        {
            doc1.Name = "Mozart's";

            // Some random version that won't match
            session.UpdateExpectedVersion(doc1, Guid.NewGuid());

            Exception<ConcurrencyException>.ShouldBeThrownBy(() =>
            {
                session.SaveChanges();
            });
        }
    }

    [Fact]
    public async Task store_with_the_right_version_sad_path_async()
    {
        var doc1 = new CoffeeShop();
        await using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            await session.SaveChangesAsync();
        }

        await using (var session = theStore.LightweightSession())
        {
            doc1.Name = "Mozart's";

            // Some random version that won't match
            session.UpdateExpectedVersion(doc1, Guid.NewGuid());

            await Exception<ConcurrencyException>.ShouldBeThrownByAsync(async () =>
            {
                await session.SaveChangesAsync();
            });
        }
    }

    [Fact]
    public async Task can_update_and_delete_related_documents()
    {
        var emp1 = new CoffeeShopEmployee();
        var doc1 = new CoffeeShop();
        doc1.Employees.Add(emp1.Id);

        await using (var session = theStore.LightweightSession())
        {
            session.Store(emp1);
            session.Store(doc1);
            await session.SaveChangesAsync();
        }

        await using (var session = theStore.DirtyTrackedSession())
        {
            var emp = session.Load<CoffeeShopEmployee>(emp1.Id);
            var doc = session.Load<CoffeeShop>(doc1.Id);

            doc.Employees.Remove(emp.Id);
            session.Delete(emp);

            await session.SaveChangesAsync();
        }
    }

    [Fact]
    public void can_update_and_delete_related_documents_synchronous()
    {
        var emp1 = new CoffeeShopEmployee();
        var doc1 = new CoffeeShop();
        doc1.Employees.Add(emp1.Id);

        using (var session = theStore.LightweightSession())
        {
            session.Store(emp1);
            session.Store(doc1);
            session.SaveChanges();
        }

        using (var session = theStore.DirtyTrackedSession())
        {
            var emp = session.Load<CoffeeShopEmployee>(emp1.Id);
            var doc = session.Load<CoffeeShop>(doc1.Id);

            doc.Employees.Remove(emp.Id);
            session.Delete(emp);

            session.SaveChanges();
        }
    }

    [Fact]
    public void Bug_669_can_store_and_update_same_document_with_optimistic_concurrency_and_dirty_tracking()
    {
        var doc1 = new CoffeeShop();
        using var session = theStore.DirtyTrackedSession();
        session.Store(doc1);
        doc1.Name = "New Name";
        session.SaveChanges();
    }

    [Fact]
    public void can_insert_with_optimistic_concurrency()
    {
        using var session = theStore.LightweightSession();
        var coffeeShop = new CoffeeShop();
        session.Store(coffeeShop);
        session.SaveChanges();

        session.Load<CoffeeShop>(coffeeShop.Id).ShouldNotBeNull();
    }

    [Fact]
    public async Task can_insert_with_optimistic_concurrency_94_async()
    {
        await using var session = theStore.LightweightSession();
        var coffeeShop = new CoffeeShop();
        session.Store(coffeeShop);
        await session.SaveChangesAsync();

        (await session.LoadAsync<CoffeeShop>(coffeeShop.Id)).ShouldNotBeNull();
    }

    [Fact]
    public void can_update_with_optimistic_concurrency()
    {
        var doc1 = new CoffeeShop();
        using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            session.SaveChanges();
        }

        using (var session = theStore.LightweightSession())
        {
            var doc2 = session.Load<Shop>(doc1.Id).As<CoffeeShop>();
            doc2.Name = "Mozart's";

            session.Store(doc2);
            session.SaveChanges();
        }

        using (var session = theStore.QuerySession())
        {
            session.Load<CoffeeShop>(doc1.Id).Name.ShouldBe("Mozart's");
        }
    }

    [Fact]
    public async Task can_update_with_optimistic_concurrenc_async()
    {
        var doc1 = new CoffeeShop();
        await using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            await session.SaveChangesAsync();
        }

        await using (var session = theStore.LightweightSession())
        {
            var doc2 = await session.LoadAsync<Shop>(doc1.Id);
            doc2.As<CoffeeShop>().Name = "Mozart's";

            session.Store(doc2);
            await session.SaveChangesAsync();
        }

        await using (var session = theStore.QuerySession())
        {
            (await session.LoadAsync<CoffeeShop>(doc1.Id)).Name.ShouldBe("Mozart's");
        }
    }

    [Fact]
    public void update_with_stale_version()
    {
        var doc1 = new CoffeeShop();
        using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            session.SaveChanges();
        }

        var session1 = theStore.DirtyTrackedSession();
        var session2 = theStore.DirtyTrackedSession();

        var session1Copy = session1.Load<CoffeeShop>(doc1.Id);
        var session2Copy = session2.Load<CoffeeShop>(doc1.Id);

        try
        {
            session1Copy.Name = "Mozart's";
            session2Copy.Name = "Dominican Joe's";

            // Should go through just fine
            session2.SaveChanges();

            var ex = Exception<ConcurrencyException>.ShouldBeThrownBy(() =>
            {
                session1.SaveChanges();
            });


            ex.Message.ShouldBe($"Optimistic concurrency check failed for {typeof(Shop).FullName} #{doc1.Id}");
        }
        finally
        {
            session1.Dispose();
            session2.Dispose();
        }

        using (var query = theStore.QuerySession())
        {
            query.Load<CoffeeShop>(doc1.Id).Name.ShouldBe("Dominican Joe's");
        }
    }

    [Fact]
    public async Task update_with_stale_version_async()
    {
        var doc1 = new CoffeeShop();
        await using (var session = theStore.LightweightSession())
        {
            session.Store(doc1);
            await session.SaveChangesAsync();
        }

        var session1 = theStore.DirtyTrackedSession();
        var session2 = theStore.DirtyTrackedSession();

        var session1Copy = await session1.LoadAsync<CoffeeShop>(doc1.Id);
        var session2Copy = await session2.LoadAsync<CoffeeShop>(doc1.Id);

        try
        {
            session1Copy.Name = "Mozart's";
            session2Copy.Name = "Dominican Joe's";

            // Should go through just fine
            await session2.SaveChangesAsync();

            var ex = await Exception<ConcurrencyException>.ShouldBeThrownByAsync(async () =>
            {
                await session1.SaveChangesAsync();
            });

            ex.Message.ShouldBe($"Optimistic concurrency check failed for {typeof(Shop).FullName} #{doc1.Id}");
        }
        finally
        {
            session1.Dispose();
            session2.Dispose();
        }

        await using (var query = theStore.QuerySession())
        {
            (await query.LoadAsync<CoffeeShop>(doc1.Id)).Name.ShouldBe("Dominican Joe's");
        }
    }
}

[UseOptimisticConcurrency]
public class Shop
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

#region sample_UseOptimisticConcurrencyAttribute
[UseOptimisticConcurrency]
public class CoffeeShop: Shop
{
    // Guess where I'm at as I code this?
    public string Name { get; set; } = "Starbucks";

    public ICollection<Guid> Employees { get; set; } = new List<Guid>();
}

#endregion

[SoftDeleted]
[UseOptimisticConcurrency]
public class CoffeeShopEmployee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
}
