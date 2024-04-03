using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Marten;
using Marten.Services;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Shouldly;
using Weasel.Core;
using Xunit.Abstractions;
using Issue = Marten.Testing.Documents.Issue;
using User = Marten.Testing.Documents.User;

namespace LinqTests.Includes;

public class end_to_end_query_with_include: OneOffConfigurationsContext
{
    private readonly ITestOutputHelper _output;

    [Fact]
    public async Task include_within_batch_query()
    {
        var user1 = new User();
        var user2 = new User();

        var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted #1" };
        var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted #2" };
        var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted #3" };

        await using var session = theStore.IdentitySession();
        session.Store(user1, user2);
        session.Store(issue1, issue2, issue3);
        await session.SaveChangesAsync();

        await using var query = theStore.QuerySession();
        User included = null;
        var list = new List<User>();
        var dict = new Dictionary<Guid, User>();

        #region sample_batch_include

        var batch = query.CreateBatchQuery();

        var found = batch.Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .Where(x => x.Title == issue1.Title)
            .Single();

        #endregion

        var toList = batch.Query<Issue>()
            .Include<User>(x => x.AssigneeId, list).ToList();

        var toDict = batch.Query<Issue>()
            .Include(x => x.AssigneeId, dict).ToList();

        await batch.Execute();


        (await found).Id.ShouldBe(issue1.Id);

        included.ShouldNotBeNull();
        included.Id.ShouldBe(user1.Id);

        (await toList).Count.ShouldBe(3);

        list.Count.ShouldBe(2); // Only 2 users


        (await toDict).Count.ShouldBe(3);

        dict.Count.ShouldBe(2);

        dict.ContainsKey(user1.Id).ShouldBeTrue();
        dict.ContainsKey(user2.Id).ShouldBeTrue();
    }

    #region sample_simple_include

    [Fact]
    public void simple_include_for_a_single_document()
    {
        var user = new User();
        var issue = new Issue { AssigneeId = user.Id, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store<object>(user, issue);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        query.Logger = new TestOutputMartenLogger(_output);

        User included = null;
        var issue2 = query
            .Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .Single(x => x.Title == issue.Title);

        included.ShouldNotBeNull();
        included.Id.ShouldBe(user.Id);

        issue2.ShouldNotBeNull();
    }

    #endregion

    [Fact]
    public void include_with_containment_where_for_a_single_document()
    {
        var user = new User();
        var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store<object>(user, issue);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        User included = null;
        var issue2 = query
            .Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .Single(x => x.Tags.Contains("DIY"));

        included.ShouldNotBeNull();
        included.Id.ShouldBe(user.Id);

        issue2.ShouldNotBeNull();
    }


    [Fact]
    public void include_with_containment_where_for_a_single_document_with_camel_casing()
    {
        StoreOptions(_ => _.UseDefaultSerialization(casing: Casing.CamelCase));

        var user = new User();
        var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store<object>(user, issue);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        User included = null;
        var issue2 = query.Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .Where(x => x.Tags.Contains("DIY"))
            .Single();

        SpecificationExtensions.ShouldNotBeNull(included);
        included.Id.ShouldBe(user.Id);

        SpecificationExtensions.ShouldNotBeNull(issue2);
    }

    [Fact]
    public void include_with_any_containment_where_for_a_single_document()
    {
        var user = new User();
        var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store<object>(user, issue);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        User included = null;
        var issue2 = query
            .Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .Single(x => x.Tags.Any<string>(t => t == "DIY"));

        SpecificationExtensions.ShouldNotBeNull(included);
        included.Id.ShouldBe(user.Id);

        SpecificationExtensions.ShouldNotBeNull(issue2);
    }

    [Fact]
    public void include_with_any_containment_where_for_a_single_document_with_camel_casing_2()
    {
        StoreOptions(_ => _.UseDefaultSerialization(EnumStorage.AsString, Casing.CamelCase));

        var user = new User();
        var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store<object>(user, issue);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        User included = null;
        var issue2 = query.Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .Where(x => x.Tags.Any(t => t == "DIY"))
            .Single();

        SpecificationExtensions.ShouldNotBeNull(included);
        included.Id.ShouldBe(user.Id);

        SpecificationExtensions.ShouldNotBeNull(issue2);
    }

    [Fact]
    public void include_with_any_containment_where_for_a_single_document_with_snake_casing_2()
    {
        StoreOptions(_ => _.UseDefaultSerialization(EnumStorage.AsString, Casing.SnakeCase));

        var user = new User();
        var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store<object>(user, issue);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        User included = null;
        var issue2 = query.Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .Where(x => x.Tags.Any(t => t == "DIY"))
            .Single();

        included.ShouldNotBeNull();
        included.Id.ShouldBe(user.Id);

        issue2.ShouldNotBeNull();
    }

    [Fact]
    public void include_with_any_containment_where_for_a_single_document_with_camel_casing()
    {
        StoreOptions(_ => _.UseDefaultSerialization(casing: Casing.CamelCase));

        var user = new User();
        var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store<object>(user, issue);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        User included = null;
        var issue2 = query.Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .Where(x => x.Tags.Any(t => t == "DIY"))
            .Single();

        included.ShouldNotBeNull();
        included.Id.ShouldBe(user.Id);

        issue2.ShouldNotBeNull();
    }

    [Fact]
    public void include_with_any_containment_where_for_a_single_document_with_snake_casing()
    {
        StoreOptions(_ => _.UseDefaultSerialization(casing: Casing.SnakeCase));

        var user = new User();
        var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store<object>(user, issue);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        User included = null;
        var issue2 = query.Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .Where(x => x.Tags.Any(t => t == "DIY"))
            .Single();

        included.ShouldNotBeNull();
        included.Id.ShouldBe(user.Id);

        issue2.ShouldNotBeNull();
    }

    [Fact]
    public void include_with_any_array_containment_where_for_a_single_document()
    {
        var user = new User();
        var issue1 = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };
        var issue2 = new Issue { AssigneeId = user.Id, Tags = new[] { "TAG" }, Title = "Garage Door is busted" };
        var issue3 = new Issue { AssigneeId = user.Id, Tags = new string[] { }, Title = "Garage Door is busted" };

        var requestedTags = new[] { "DIY", "TAG" };

        using var session = theStore.IdentitySession();
        session.Store(user);
        session.Store(issue1, issue2, issue3);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        var users = new List<User>();
        var issues = query.Query<Issue>()
            .Include(x => x.AssigneeId, users)
            .Where(x => x.Tags.Any(t => requestedTags.Contains(t)))
            .ToList();

        users.Count.ShouldBe(1);
        users.ShouldContain(x => x.Id == user.Id);

        issues.Count.ShouldBe(2);
        issues.ShouldContain(x => x.Id == issue1.Id);
        issues.ShouldContain(x => x.Id == issue2.Id);
    }

    [Fact]
    public void include_with_generic_type()
    {
        var user = new UserWithInterface { Id = Guid.NewGuid(), UserName = "Jens" };
        var issue = new Issue { AssigneeId = user.Id, Tags = new[] { "DIY" }, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store<object>(user, issue);
        session.SaveChanges();

        IncludeGeneric<UserWithInterface>(user);
    }

    private void IncludeGeneric<T>(UserWithInterface userToCompareAgainst) where T : class, IUserWithInterface
    {
        using var query = theStore.QuerySession();
        T included = default;
        var issue2 = query.Query<Issue>()
            .Include<T>(x => x.AssigneeId, x => included = x)
            .Where(x => x.Tags.Any(t => t == "DIY"))
            .Single();

        included.ShouldNotBeNull();
        included.Id.ShouldBe(userToCompareAgainst.Id);

        issue2.ShouldNotBeNull();
    }

    [Fact]
    public void simple_include_for_a_single_document_using_outer_join()
    {
        var issue = new Issue { AssigneeId = null, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store(issue);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        User included = null;
        var issue2 = query.Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .Where(x => x.Title == issue.Title)
            .Single();

        included.ShouldBeNull();

        issue2.ShouldNotBeNull();
    }

    [Fact]
    public void include_to_list()
    {
        var user1 = new User { FirstName = "Travis", LastName = "Kelce" };
        var user2 = new User { FirstName = "Tyrann", LastName = "Mathieu" };

        var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted 1" };
        var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted 2" };
        var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted 3" };

        using var session = theStore.IdentitySession();
        session.Store(user1, user2);
        session.Store(issue1, issue2, issue3);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        var list = new List<User>();

        var issues = query.Query<Issue>().Include<User>(x => x.AssigneeId, list).ToArray();

        list.Count.ShouldBe(2);

        list.Any(x => x.Id == user1.Id).ShouldBeTrue();
        list.Any(x => x.Id == user2.Id).ShouldBeTrue();
    }

    [Fact]
    public void include_to_list_using_inner_join()
    {
        var user1 = new User();
        var user2 = new User();

        var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
        var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue4 = new Issue { AssigneeId = null, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store(user1, user2);
        session.Store(issue1, issue2, issue3, issue4);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        var list = new List<User>();

        var issues = query.Query<Issue>()
            .Include<User>(x => x.AssigneeId, list)
            .Where(x => x.AssigneeId.HasValue)
            .ToArray();

        list.Count.ShouldBe(2);

        list.Any(x => x.Id == user1.Id).ShouldBeTrue();
        list.Any(x => x.Id == user2.Id).ShouldBeTrue();

        issues.Length.ShouldBe(3);
    }

    [Fact]
    public void include_to_list_using_outer_join()
    {
        var user1 = new User();
        var user2 = new User();

        var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
        var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue4 = new Issue { AssigneeId = null, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store(user1, user2);
        session.Store(issue1, issue2, issue3, issue4);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        var list = new List<User>();

        var issues = query.Query<Issue>().Include<User>(x => x.AssigneeId, list).ToArray();

        list.Count.ShouldBe(2);

        list.Any(x => x.Id == user1.Id).ShouldBeTrue();
        list.Any(x => x.Id == user2.Id).ShouldBeTrue();
        list.Any(x => x == null).ShouldBeFalse();

        issues.Length.ShouldBe(4);
    }

    [Fact]
    public void include_is_running_through_identitymap()
    {
        var user1 = new User();
        var user2 = new User();

        var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
        var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store(user1, user2);
        session.Store(issue1, issue2, issue3);
        session.SaveChanges();

        // This will only work with a non-NulloIdentityMap
        using var query = theStore.IdentitySession();
        var dict = new Dictionary<Guid, User>();

        query.Query<Issue>().Include(x => x.AssigneeId, dict).ToArray();

        query.Load<User>(user1.Id).ShouldBeSameAs(dict[user1.Id]);
        query.Load<User>(user2.Id).ShouldBeSameAs(dict[user2.Id]);
    }

    #region sample_dictionary_include

    [Fact]
    public void include_to_dictionary()
    {
        var user1 = new User();
        var user2 = new User();

        var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
        var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store(user1, user2);
        session.Store(issue1, issue2, issue3);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        var dict = new Dictionary<Guid, User>();

        query.Query<Issue>().Include(x => x.AssigneeId, dict).ToArray();

        dict.Count.ShouldBe(2);
        dict.ContainsKey(user1.Id).ShouldBeTrue();
        dict.ContainsKey(user2.Id).ShouldBeTrue();
    }

    #endregion

    [Fact]
    public void include_to_dictionary_using_inner_join()
    {
        var user1 = new User();
        var user2 = new User();

        var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
        var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue4 = new Issue { AssigneeId = null, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store(user1, user2);
        session.Store(issue1, issue2, issue3, issue4);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        var dict = new Dictionary<Guid, User>();

        var issues = query
            .Query<Issue>()
            .Include(x => x.AssigneeId, dict)
            .Where(x => x.AssigneeId.HasValue).ToArray();

        dict.Count.ShouldBe(2);
        dict.ContainsKey(user1.Id).ShouldBeTrue();
        dict.ContainsKey(user2.Id).ShouldBeTrue();

        issues.Length.ShouldBe(3);
    }

    [Fact]
    public void include_to_dictionary_using_outer_join()
    {
        var user1 = new User();
        var user2 = new User();

        var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
        var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue4 = new Issue { AssigneeId = null, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store(user1, user2);
        session.Store(issue1, issue2, issue3, issue4);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        var dict = new Dictionary<Guid, User>();

        var issues = query.Query<Issue>().Include(x => x.AssigneeId, dict).ToArray();

        dict.Count.ShouldBe(2);
        dict.ContainsKey(user1.Id).ShouldBeTrue();
        dict.ContainsKey(user2.Id).ShouldBeTrue();

        issues.Length.ShouldBe(4);
    }

    [Fact]
    public async Task simple_include_for_a_single_document_async()
    {
        var user = new User();
        var issue = new Issue { AssigneeId = user.Id, Title = "Garage Door is busted" };

        await using var session = theStore.IdentitySession();
        session.Store<object>(user, issue);
        await session.SaveChangesAsync();

        await using var query = theStore.QuerySession();
        query.Logger = new TestOutputMartenLogger(_output);
        User included = null;
        var issue2 = await query.Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .Where(x => x.Title == issue.Title)
            .SingleAsync();

        included.ShouldNotBeNull();
        included.Id.ShouldBe(user.Id);

        issue2.ShouldNotBeNull();
    }

    [Fact]
    public async Task include_to_list_async()
    {
        var user1 = new User();
        var user2 = new User();

        var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
        var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

        await using var session = theStore.IdentitySession();
        session.Store(user1, user2);
        session.Store(issue1, issue2, issue3);
        await session.SaveChangesAsync();

        await using var query = theStore.QuerySession();
        var list = new List<User>();

        query.Logger = new TestOutputMartenLogger(_output);
        await query.Query<Issue>().Include(x => x.AssigneeId, list).ToListAsync();

        list.Count.ShouldBe(2);

        list.Any(x => x.Id == user1.Id).ShouldBeTrue();
        list.Any(x => x.Id == user2.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task include_to_list_with_orderby_descending_async()
    {
        var user1 = new User();
        var user2 = new User();

        var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
        var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

        await using var session = theStore.IdentitySession();
        session.Store(user1, user2);
        session.Store(issue1, issue2, issue3);
        await session.SaveChangesAsync();

        await using var query = theStore.QuerySession();
        var list = new List<User>();

        await query.Query<Issue>().Include(x => x.AssigneeId, list)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        list.Count.ShouldBe(2);

        list.Any(x => x.Id == user1.Id).ShouldBeTrue();
        list.Any(x => x.Id == user2.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task include_to_list_with_orderby_ascending_async()
    {
        var user1 = new User();
        var user2 = new User();

        var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
        var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

        await using var session = theStore.IdentitySession();
        session.Store(user1, user2);
        session.Store(issue1, issue2, issue3);
        await session.SaveChangesAsync();

        await using var query = theStore.QuerySession();
        var list = new List<User>();

        await query.Query<Issue>().Include(x => x.AssigneeId, list)
            .OrderBy(x => x.Id)
            .ToListAsync();

        list.Count.ShouldBe(2);

        list.Any(x => x.Id == user1.Id).ShouldBeTrue();
        list.Any(x => x.Id == user2.Id).ShouldBeTrue();
    }

    [Fact]
    public async Task include_to_dictionary_async()
    {
        var user1 = new User();
        var user2 = new User();

        var issue1 = new Issue { AssigneeId = user1.Id, Title = "Garage Door is busted" };
        var issue2 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };
        var issue3 = new Issue { AssigneeId = user2.Id, Title = "Garage Door is busted" };

        await using var session = theStore.IdentitySession();
        session.Store(user1, user2);
        session.Store(issue1, issue2, issue3);
        await session.SaveChangesAsync();

        await using var query = theStore.QuerySession();

        var dict = new Dictionary<Guid, User>();

        await query.Query<Issue>().Include(x => x.AssigneeId, dict).ToListAsync();

        dict.Count.ShouldBe(2);
        dict.ContainsKey(user1.Id).ShouldBeTrue();
        dict.ContainsKey(user2.Id).ShouldBeTrue();
    }

    #region sample_multiple_include

    [Fact]
    public void multiple_includes()
    {
        var assignee = new User{FirstName = "Assignee"};
        var reporter = new User{FirstName = "Reporter"};

        var issue1 = new Issue { AssigneeId = assignee.Id, ReporterId = reporter.Id, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store(assignee, reporter);
        session.Store(issue1);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        User assignee2 = null;
        User reporter2 = null;

        query.Logger = new TestOutputMartenLogger(_output);
        query
            .Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => assignee2 = x)
            .Include<User>(x => x.ReporterId, x => reporter2 = x)
            .Single()
            .ShouldNotBeNull();

        assignee2.Id.ShouldBe(assignee.Id);
        reporter2.Id.ShouldBe(reporter.Id);
    }

    #endregion

    [Fact]
    public void include_many_to_list()
    {
        var user1 = new User { };
        var user2 = new User { };
        var user3 = new User { };
        var user4 = new User { };
        var user5 = new User { };
        var user6 = new User { };
        var user7 = new User { };

        theStore.BulkInsert(new User[] { user1, user2, user3, user4, user5, user6, user7 });

        var group1 = new Group { Name = "Odds", Users = new[] { user1.Id, user3.Id, user5.Id, user7.Id } };

        var group2 = new Group { Name = "Evens", Users = new[] { user2.Id, user4.Id, user6.Id } };

        using (var session = theStore.LightweightSession())
        {
            session.Store(group1, group2);
            session.SaveChanges();
        }

        using (var query = theStore.QuerySession())
        {
            query.Logger = new TestOutputMartenLogger(_output);

            var list = new List<User>();

            query.Query<Group>()
                .Include(x => x.Users, list)
                .Where(x => x.Name == "Odds")
                .ToList()
                .Single()
                .Name.ShouldBe("Odds");

            list.Count.ShouldBe(4);
            list.Any(x => x.Id == user1.Id).ShouldBeTrue();
            list.Any(x => x.Id == user3.Id).ShouldBeTrue();
            list.Any(x => x.Id == user5.Id).ShouldBeTrue();
            list.Any(x => x.Id == user7.Id).ShouldBeTrue();
        }
    }

    [Fact]
    public void Bug_1751_Include_with_select()
    {
        var user = new User();
        var issue = new Issue { AssigneeId = user.Id, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store<object>(user, issue);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        User included = null;
        var issue2 = query
            .Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .Where(x => x.Title == issue.Title)
            .Select(x => new IssueDTO { Id = x.Id, AssigneeId = x.AssigneeId })
            .Single();

        included.ShouldNotBeNull();
        included.Id.ShouldBe(user.Id);

        issue2.ShouldNotBeNull();
    }

    public class IssueDTO
    {
        public Guid Id { get; set; }

        public Guid? AssigneeId { get; set; }
        public string AssigneeFullName { get; set; }
    }

    [Fact]
    public async Task Bug_1715_simple_include_for_a_single_document_async()
    {
        await using var session = theStore.IdentitySession();
        session.Logger = new TestOutputMartenLogger(_output);

        var user = new User();
        var bug = new Bug();

        session.Store(user);
        session.Store(bug);

        var issue = new Issue { AssigneeId = user.Id, Title = "Garage Door is busted", BugId = bug.Id };

        session.Store(issue);

        await session.SaveChangesAsync();

        await using var query = theStore.QuerySession();
        User includedUser = null;
        Bug includedBug = null;
        var issue2 = await query.Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => includedUser = x)
            .Include<Bug>(x => x.BugId, x => includedBug = x)
            .Where(x => x.Title == issue.Title)
            .SingleAsync();

        includedUser.ShouldNotBeNull();
        includedBug.ShouldNotBeNull();
        includedUser.Id.ShouldBe(user.Id);
        includedBug.Id.ShouldBe(bug.Id);

        issue2.ShouldNotBeNull();
    }

    [Fact]
    public void Bug_1752_simple_include_for_a_single_document()
    {
        var user = new User();
        var issue = new Issue { AssigneeId = user.Id, Title = "Garage Door is busted" };

        using var session = theStore.IdentitySession();
        session.Store<object>(user, issue);
        session.SaveChanges();

        using var query = theStore.QuerySession();
        User included = null;
        var issue2 = query
            .Query<Issue>()
            .Include<User>(x => x.AssigneeId, x => included = x)
            .SingleOrDefault(x => x.Title == "Garage Door is not busted");

        included.ShouldBeNull();
        issue2.ShouldBeNull();
    }

    [Fact]
    public void include_many_to_list_with_empty_parent_collection()
    {
        var user1 = new User();
        var user2 = new User();
        var user3 = new User();

        theStore.BulkInsert(new[] { user1, user2, user3 });

        var group1 = new Group { Name = "Users", Users = new[] { user1.Id, user2.Id, user3.Id } };
        var group2 = new Group { Name = "Empty", Users = new Guid[0] };

        using (var session = theStore.LightweightSession())
        {
            session.Store(group1, group2);
            session.SaveChanges();
        }

        using (var query = theStore.QuerySession())
        {
            query.Logger = new TestOutputMartenLogger(_output);

            var list = new List<User>();

            var groups = query.Query<Group>()
                .Include(x => x.Users, list)
                .Where(x => x.Name == "Users" || x.Name == "Empty")
                .ToList();

            groups.Count.ShouldBe(2);

            list.Count.ShouldBe(3);
            list.Any(x => x.Id == user1.Id).ShouldBeTrue();
            list.Any(x => x.Id == user2.Id).ShouldBeTrue();
            list.Any(x => x.Id == user3.Id).ShouldBeTrue();
        }
    }

    public end_to_end_query_with_include(ITestOutputHelper output)
    {
        _output = output;
    }
}

public class Group
{
    public string Name { get; set; }
    public Guid Id { get; set; }
    public Guid[] Users { get; set; }
}
