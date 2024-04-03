using System;
using System.Linq;
using System.Threading.Tasks;
using Marten;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Shouldly;
using Xunit.Abstractions;

namespace LinqTests.Acceptance;

public class using_dynamic_props: IntegrationContext
{
    private readonly ITestOutputHelper _output;

    [Fact]
    public async Task order_by()
    {
        var toList = await theSession.Query<User>().OrderBy("FirstName").ToListAsync();
        toList.Select(x => x.FirstName)
            .ShouldHaveTheSameElementsAs("Harry", "Harry", "Justin", "Justin", "Michael", "Michael");
    }

    [Fact]
    public async Task batch_order_by()
    {
        var batch = theSession.CreateBatchQuery();
        var query = batch.Query<User>().OrderBy("FirstName").ToList();
        await batch.Execute();
        var toList = await query;
        toList.Select(x => x.FirstName)
              .ShouldHaveTheSameElementsAs("Harry", "Harry", "Justin", "Justin", "Michael", "Michael");
    }

    [Fact]
    public async Task order_by_descending()
    {
        var toList = await theSession.Query<User>().OrderByDescending("FirstName").ToListAsync();
        toList.Select(x => x.FirstName)
            .ShouldHaveTheSameElementsAs("Michael", "Michael", "Justin", "Justin", "Harry", "Harry");
    }

    [Fact]
    public async Task batch_order_by_descending()
    {
        var batch = theSession.CreateBatchQuery();
        var query = batch.Query<User>().OrderByDescending("FirstName").ToList();
        await batch.Execute();
        var toList = await query;
        toList.Select(x => x.FirstName)
              .ShouldHaveTheSameElementsAs("Michael", "Michael", "Justin", "Justin", "Harry", "Harry");
    }

    [Theory]
    [InlineData("ASC")]
    [InlineData("asc")]
    public async Task order_by_prop_with_sort_order_asc_text(string sortOrder)
    {
        var toList = await theSession.Query<User>().OrderBy($"FirstName {sortOrder}").ToListAsync();
        toList.Select(x => x.FirstName)
            .ShouldHaveTheSameElementsAs("Harry", "Harry", "Justin", "Justin", "Michael", "Michael");
    }

    [Theory]
    [InlineData("ASC")]
    [InlineData("asc")]
    public async Task batch_order_by_prop_with_sort_order_asc_text(string sortOrder)
    {
        var batch = theSession.CreateBatchQuery();
        var query = batch.Query<User>().OrderBy($"FirstName {sortOrder}").ToList();
        await batch.Execute();
        var toList = await query;
        toList.Select(x => x.FirstName)
            .ShouldHaveTheSameElementsAs("Harry", "Harry", "Justin", "Justin", "Michael", "Michael");
    }

    [Theory]
    [InlineData("DESC")]
    [InlineData("desc")]
    public async Task order_by_prop_with_sort_order_desc_text(string sortOrder)
    {
        var toList = await theSession.Query<User>().OrderBy($"FirstName {sortOrder}").ToListAsync();
        toList.Select(x => x.FirstName)
            .ShouldHaveTheSameElementsAs("Michael", "Michael", "Justin", "Justin", "Harry", "Harry");
    }

    [Theory]
    [InlineData("DESC")]
    [InlineData("desc")]
    public async Task batch_order_by_prop_with_sort_order_desc_text(string sortOrder)
    {
        var batch = theSession.CreateBatchQuery();
        var query = batch.Query<User>().OrderBy($"FirstName {sortOrder}").ToList();
        await batch.Execute();
        var toList = await query;
        toList.Select(x => x.FirstName)
            .ShouldHaveTheSameElementsAs("Michael", "Michael", "Justin", "Justin", "Harry", "Harry");
    }

    [Fact]
    public async Task order_by_multiple_props()
    {
        var toList = await theSession.Query<User>().OrderBy($"FirstName DESC", "LastName").ToListAsync();
        toList.Select(x => x.FirstName).ShouldHaveTheSameElementsAs("Michael", "Michael", "Justin", "Justin", "Harry", "Harry");
        toList.Select(x => x.LastName).ShouldHaveTheSameElementsAs("Bean", "Brown", "Houston", "White", "Smith", "Somerset");
    }

    [Fact]
    public async Task batch_order_by_multiple_props()
    {
        var batch = theSession.CreateBatchQuery();
        var query = batch.Query<User>().OrderBy($"FirstName DESC", "LastName").ToList();
        await batch.Execute();
        var toList = await query;
        toList.Select(x => x.FirstName).ShouldHaveTheSameElementsAs("Michael", "Michael", "Justin", "Justin", "Harry", "Harry");
        toList.Select(x => x.LastName).ShouldHaveTheSameElementsAs("Bean", "Brown", "Houston", "White", "Smith", "Somerset");
    }

    [Fact]
    public async Task order_by_then_by()
    {
        var toList = await theSession.Query<User>().OrderBy("FirstName").ThenBy("LastName").ToListAsync();
        toList.Select(x => x.FirstName).ShouldHaveTheSameElementsAs("Harry", "Harry", "Justin", "Justin", "Michael", "Michael");
        toList.Select(x => x.LastName).ShouldHaveTheSameElementsAs("Smith", "Somerset", "Houston", "White", "Bean", "Brown");
    }

    [Fact]
    public async Task batch_order_by_then_by()
    {
        var batch = theSession.CreateBatchQuery();
        var query = batch.Query<User>().OrderBy("FirstName").ThenBy("LastName").ToList();
        await batch.Execute();
        var toList = await query;
        toList.Select(x => x.FirstName).ShouldHaveTheSameElementsAs("Harry", "Harry", "Justin", "Justin", "Michael", "Michael");
        toList.Select(x => x.LastName).ShouldHaveTheSameElementsAs("Smith", "Somerset", "Houston", "White", "Bean", "Brown");
    }

    [Fact]
    public async Task order_by_descending_then_by()
    {
        var toList = await theSession.Query<User>().OrderByDescending("FirstName").ThenBy("LastName").ToListAsync();
        toList.Select(x => x.FirstName).ShouldHaveTheSameElementsAs("Michael", "Michael", "Justin", "Justin", "Harry", "Harry");
        toList.Select(x => x.LastName).ShouldHaveTheSameElementsAs("Bean", "Brown", "Houston", "White", "Smith", "Somerset");
    }

    [Fact]
    public async Task batch_order_by_descending_then_by()
    {
        theSession.Logger = new TestOutputMartenLogger(_output);

        var batch = theSession.CreateBatchQuery();
        var query = batch.Query<User>().OrderByDescending("FirstName").ThenBy("LastName").ToList();

        await batch.Execute();
        var toList = await query;
        toList.Select(x => x.FirstName).ShouldHaveTheSameElementsAs("Michael", "Michael", "Justin", "Justin", "Harry", "Harry");
        toList.Select(x => x.LastName).ShouldHaveTheSameElementsAs("Bean", "Brown", "Houston", "White", "Smith", "Somerset");
    }

    [Fact]
    public async Task when_order_by_props_not_passed_throw_exception()
    {
        Func<Task> func = async () =>
        {
            await theSession.Query<User>().OrderBy().ToListAsync();
        };
        await func.ShouldThrowAsync<ArgumentException>();
    }

    [Fact]
    public void when_batch_order_by_props_not_passed_throw_exception()
    {
        var func = () => theSession.CreateBatchQuery().Query<User>().OrderBy();
        func.ShouldThrow<ArgumentException>();
    }

    protected override Task fixtureSetup()
    {
        theSession.Store(
            new User { FirstName = "Justin", LastName = "Houston" },
            new User { FirstName = "Justin", LastName = "White" },
            new User { FirstName = "Michael", LastName = "Bean" },
            new User { FirstName = "Michael", LastName = "Brown" },
            new User { FirstName = "Harry", LastName = "Smith" },
            new User { FirstName = "Harry", LastName = "Somerset" }
        );

        return theSession.SaveChangesAsync();
    }

    public using_dynamic_props(DefaultStoreFixture fixture, ITestOutputHelper output): base(fixture)
    {
        _output = output;
    }
}
