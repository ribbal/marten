using System;
using System.Linq;
using Marten.Schema;
using Marten.Storage;
using Marten.Testing.Harness;
using Xunit;

namespace Marten.Testing.Examples;

public sealed class Policies
{
    private class MultiTenantType: IRequireMultiTenancy
    {
        public Guid Id { get; set; }
    }

    [Fact]
    public void SetTenancyThroughPolicy()
    {
        #region sample_sample-policy-configure
        var store = DocumentStore.For(storeOptions =>
        {
            // Apply custom policy
            storeOptions.Policies.OnDocuments<TenancyPolicy>();
            #endregion
            storeOptions.Connection(ConnectionSource.ConnectionString);
        });

        Assert.Equal(TenancyStyle.Conjoined, store.StorageFeatures.MappingFor(typeof(MultiTenantType)).TenancyStyle);
    }

    #region sample_sample-policy-implementation
    public interface IRequireMultiTenancy
    {
    }

    public class TenancyPolicy: IDocumentPolicy
    {
        public void Apply(DocumentMapping mapping)
        {
            if (mapping.DocumentType.GetInterfaces().Any(x => x == typeof(IRequireMultiTenancy)))
            {
                mapping.TenancyStyle = TenancyStyle.Conjoined;
            }
        }
    }

    #endregion
}
