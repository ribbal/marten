using System.Linq;
using Marten.Services.Json;
using Marten.Testing.Documents;
using Marten.Testing.Harness;

namespace LinqTests.Bugs;

public class Bug_1189_can_select_transform_without_an_id : IntegrationContext
{
    public Bug_1189_can_select_transform_without_an_id(DefaultStoreFixture fixture) : base(fixture)
    {
    }

    public class TargetView
    {
        public Colors Color { get; set; }
        public int Number { get; set; }
    }


    [SerializerTypeTargetedFact(RunFor = SerializerType.Newtonsoft)]
    public void can_select()
    {
        var targets = Target.GenerateRandomData(100).ToArray();
        theStore.BulkInsert(targets);

        var view = theSession.Query<Target>().Select(x => new TargetView {Color = x.Color, Number = x.Number})
            .FirstOrDefault();

        view.ShouldNotBeNull();
    }
}
