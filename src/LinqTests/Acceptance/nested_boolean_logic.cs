﻿using System;
using System.Linq;
using Marten.Testing.Documents;
using Marten.Testing.Harness;
using Shouldly;
using Xunit.Abstractions;

namespace LinqTests.Acceptance;

public class nested_boolean_logic : IntegrationContext
{
    private readonly ITestOutputHelper _output;

    [Fact]
    public void TestModalOrQuery()
    {
        var target1 = new Target { String = "Bert", Date = new DateTime(2016, 03, 10) };
        var target2 = new Target { String = null, Date = new DateTime(2016, 03, 10) };

        theSession.Store(target1, target2);
        theSession.SaveChanges();

        var startDate = new DateTime(2016, 03, 01);
        var endDate = new DateTime(2016, 04, 01);

        var query = theSession.Query<Target>().Where(item => (item.String != null && item.Date >= startDate && item.Date <= endDate)
                                                             || (item.String == null && item.Date >= startDate && item.Date <= endDate));

        query.ToList().Count.ShouldBeGreaterThanOrEqualTo(2);

    }

    public nested_boolean_logic(DefaultStoreFixture fixture, ITestOutputHelper output) : base(fixture)
    {
        _output = output;
    }
}
