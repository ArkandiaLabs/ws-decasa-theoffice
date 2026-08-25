using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TheOffice.ArchitectureTests;

/// <summary>
/// The dependency rule from docs/architecture.md, turned into a sensor. Until now it was held up
/// only by project references and discipline; here it fails the build when a layer reaches
/// somewhere it must not.
///
/// Every rule in this file passed against the code as it stood when the file was written. A rule
/// the repository does not already follow belongs in a conversation, not in a test.
/// </summary>
public class LayerDependencyTests
{
  private const string DomainNs = "^TheOffice\\.Domain";
  private const string ApplicationNs = "^TheOffice\\.Application";
  private const string PersistenceNs = "^TheOffice\\.Persistence";
  private const string AdaptersNs = "^TheOffice\\.Adapters";
  private const string ApiNs = "^TheOffice\\.Api";
  private const string EfCoreNs = "^Microsoft\\.EntityFrameworkCore";

  // Load every layer once. The EF Core assembly is loaded too so the ORM rules have concrete
  // target types to match: ArchUnitNET only evaluates dependencies against types present in the
  // loaded architecture.
  //
  // Fully qualified on purpose. A bare `Architecture` can resolve to a namespace segment rather
  // than to the ArchUnitNET type, and the compiler then stops with CS0118.
  private static readonly ArchUnitNET.Domain.Architecture Arch = new ArchLoader()
    .LoadAssemblies(
      typeof(Domain.Entities.Product).Assembly,
      typeof(Application.Services.ProductService).Assembly,
      typeof(Persistence.TheOfficeDbContext).Assembly,
      typeof(Adapters.Notification.ConsoleNotificationAdapter).Assembly,
      typeof(Api.Controllers.ProductController).Assembly,
      typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly)
    .Build();

  private static void Check(IArchRule rule)
  {
    var failures = rule.Evaluate(Arch).Where(r => !r.Passed).ToList();

    Assert.True(
      failures.Count == 0,
      string.Join("\n", failures.Select(f => f.Description)));
  }

  // Domain is the pure core: it knows nobody. See ADR-0001 and docs/architecture.md.
  [Fact]
  public void Domain_should_not_depend_on_other_layers() =>
    Check(Types().That().ResideInNamespaceMatching(DomainNs)
      .Should().NotDependOnAny(
        Types().That().ResideInNamespaceMatching(ApplicationNs)
          .Or().ResideInNamespaceMatching(PersistenceNs)
          .Or().ResideInNamespaceMatching(AdaptersNs)
          .Or().ResideInNamespaceMatching(ApiNs)));

  // Application defines the ports; infrastructure implements them. Dependencies point inwards.
  [Fact]
  public void Application_should_not_depend_on_infrastructure_or_presentation() =>
    Check(Types().That().ResideInNamespaceMatching(ApplicationNs)
      .Should().NotDependOnAny(
        Types().That().ResideInNamespaceMatching(PersistenceNs)
          .Or().ResideInNamespaceMatching(AdaptersNs)
          .Or().ResideInNamespaceMatching(ApiNs)));

  // Persistence is a detail, not a contract. A service never touches TheOfficeDbContext.
  [Fact]
  public void Application_should_not_depend_on_the_orm() =>
    Check(Types().That().ResideInNamespaceMatching(ApplicationNs)
      .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(EfCoreNs)));

  // The same rule, one layer further in. The domain entities are not EF Core models.
  [Fact]
  public void Domain_should_not_depend_on_the_orm() =>
    Check(Types().That().ResideInNamespaceMatching(DomainNs)
      .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(EfCoreNs)));

  // The two infrastructure projects are siblings. Neither may reach into the other; anything
  // shared between them belongs in Application or Domain.
  [Fact]
  public void Persistence_and_Adapters_should_not_depend_on_each_other()
  {
    Check(Types().That().ResideInNamespaceMatching(PersistenceNs)
      .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(AdaptersNs)));

    Check(Types().That().ResideInNamespaceMatching(AdaptersNs)
      .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(PersistenceNs)));
  }

  // TheOffice.Api is the entry point and therefore a leaf: nothing in the solution depends on it.
  // Anything the inner layers appear to need from it is a port that belongs in Application.
  [Fact]
  public void Nothing_should_depend_on_the_entry_point() =>
    Check(Types().That().ResideInNamespaceMatching(DomainNs)
      .Or().ResideInNamespaceMatching(ApplicationNs)
      .Or().ResideInNamespaceMatching(PersistenceNs)
      .Or().ResideInNamespaceMatching(AdaptersNs)
      .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ApiNs)));
}
