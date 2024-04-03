using System;
using Marten.Internal;
using Marten.Testing.Documents;
using Shouldly;
using Xunit;

namespace CoreTests.Internals;

public class VersionTrackerTests
{
    private readonly StringDoc stringDoc = new StringDoc {Id = "bar"};
    private readonly IntDoc intDoc = new IntDoc{Id = 4};
    private readonly LongDoc longDoc = new LongDoc {Id = 5};
    private readonly GuidDoc guidDoc = new GuidDoc {Id = Guid.NewGuid()};
    private readonly VersionTracker theTracker = new VersionTracker();

    [Fact]
    public void can_retrieve_dictionary_for_document_type()
    {
        var dict = theTracker.ForType<IntDoc, int>();
        var dict2 = theTracker.ForType<IntDoc, int>();
        var dict3 = theTracker.ForType<IntDoc, int>();

        dict.ShouldNotBeNull();
        dict.ShouldBeSameAs(dict2);
        dict.ShouldBeSameAs(dict3);
    }

    [Fact]
    public void can_get_version_when_it_is_empty_always_null()
    {
        theTracker.VersionFor<StringDoc, string>(stringDoc.Id)
            .ShouldBeNull();

        theTracker.VersionFor<IntDoc, int>(intDoc.Id)
            .ShouldBeNull();

        theTracker.VersionFor<LongDoc, long>(longDoc.Id)
            .ShouldBeNull();

        theTracker.VersionFor<GuidDoc, Guid>(guidDoc.Id)
            .ShouldBeNull();
    }

    [Fact]
    public void store_and_retrieve()
    {
        var intVersion = Guid.NewGuid();
        var stringVersion = Guid.NewGuid();
        theTracker.StoreVersion<IntDoc, int>(intDoc.Id, intVersion);
        theTracker.StoreVersion<StringDoc, string>(stringDoc.Id, stringVersion);

        theTracker.VersionFor<IntDoc, int>(intDoc.Id)
            .ShouldBe(intVersion);

        theTracker.VersionFor<StringDoc, string>(stringDoc.Id)
            .ShouldBe(stringVersion);
    }

    [Fact]
    public void override_the_version()
    {
        var intVersion = Guid.NewGuid();
        var intVersion2 = Guid.NewGuid();
        theTracker.StoreVersion<IntDoc, int>(intDoc.Id, intVersion);
        theTracker.StoreVersion<IntDoc, int>(intDoc.Id, intVersion2);

        theTracker.VersionFor<IntDoc, int>(intDoc.Id)
            .ShouldBe(intVersion2);

    }

    [Fact]
    public void store_and_then_clear()
    {
        var intVersion = Guid.NewGuid();
        var stringVersion = Guid.NewGuid();
        theTracker.StoreVersion<IntDoc, int>(intDoc.Id, intVersion);
        theTracker.StoreVersion<StringDoc, string>(stringDoc.Id, stringVersion);

        theTracker.ClearVersion<IntDoc, int>(intDoc.Id);

        theTracker.VersionFor<IntDoc, int>(intDoc.Id)
            .ShouldBeNull();

        // Not cleared
        theTracker.VersionFor<StringDoc, string>(stringDoc.Id)
            .ShouldBe(stringVersion);
    }

    /*** START Revisions ******/

    [Fact]
    public void can_retrieve_revision_dictionary_for_document_type()
    {
        var dict = theTracker.RevisionsFor<IntDoc, int>();
        var dict2 = theTracker.RevisionsFor<IntDoc, int>();
        var dict3 = theTracker.RevisionsFor<IntDoc, int>();

        dict.ShouldNotBeNull();
        dict.ShouldBeSameAs(dict2);
        dict.ShouldBeSameAs(dict3);
    }

    [Fact]
    public void can_get_revision_when_it_is_empty_always_null()
    {
        theTracker.RevisionFor<StringDoc, string>(stringDoc.Id)
            .ShouldBeNull();

        theTracker.RevisionFor<IntDoc, int>(intDoc.Id)
            .ShouldBeNull();

        theTracker.RevisionFor<LongDoc, long>(longDoc.Id)
            .ShouldBeNull();

        theTracker.RevisionFor<GuidDoc, Guid>(guidDoc.Id)
            .ShouldBeNull();
    }

    [Fact]
    public void store_and_retrieve_revision()
    {
        var intVersion = 3;
        var stringVersion = 4;
        theTracker.StoreRevision<IntDoc, int>(intDoc.Id, intVersion);
        theTracker.StoreRevision<StringDoc, string>(stringDoc.Id, stringVersion);

        theTracker.RevisionFor<IntDoc, int>(intDoc.Id)
            .ShouldBe(intVersion);

        theTracker.RevisionFor<StringDoc, string>(stringDoc.Id)
            .ShouldBe(stringVersion);
    }

    [Fact]
    public void override_the_revision()
    {
        var intVersion = 3;
        var intVersion2 = 11;
        theTracker.StoreRevision<IntDoc, int>(intDoc.Id, intVersion);
        theTracker.StoreRevision<IntDoc, int>(intDoc.Id, intVersion2);

        theTracker.RevisionFor<IntDoc, int>(intDoc.Id)
            .ShouldBe(intVersion2);

    }

    [Fact]
    public void store_and_then_clear_revision()
    {
        var intVersion = 12;
        var stringVersion = 25;
        theTracker.StoreRevision<IntDoc, int>(intDoc.Id, intVersion);
        theTracker.StoreRevision<StringDoc, string>(stringDoc.Id, stringVersion);

        theTracker.ClearRevision<IntDoc, int>(intDoc.Id);

        theTracker.RevisionFor<IntDoc, int>(intDoc.Id)
            .ShouldBeNull();

        // Not cleared
        theTracker.RevisionFor<StringDoc, string>(stringDoc.Id)
            .ShouldBe(stringVersion);
    }

}
