using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace TheOffice.ArchitectureTests;

/// <summary>
/// Team conventions from AGENTS.md and docs/architecture.md that the compiler cannot see.
///
/// Every selector here is scoped to TheOffice's own namespaces. The fixture loads EF Core, which
/// ships types matching these shapes (IUpdateAdapter, for one); an unscoped selector would flag
/// them on the day the rule is written.
/// </summary>
public class ConventionTests
{
  private const string OwnNs = "^TheOffice\\.";
  private const string DomainNs = "^TheOffice\\.Domain";
  private const string ApplicationNs = "^TheOffice\\.Application";
  private const string PersistenceNs = "^TheOffice\\.Persistence";
  private const string ApiNs = "^TheOffice\\.Api";

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

  // Ports are declared by Application, never by infrastructure. A repository interface that
  // appears next to its EF Core implementation has inverted the dependency.
  [Fact]
  public void Repository_ports_should_live_in_application() =>
    Check(Interfaces().That().ResideInNamespaceMatching(OwnNs)
      .And().HaveNameEndingWith("Repository")
      .Should().ResideInNamespaceMatching("^TheOffice\\.Application\\.Interfaces"));

  // Same rule for the outbound ports.
  [Fact]
  public void Adapter_ports_should_live_in_application() =>
    Check(Interfaces().That().ResideInNamespaceMatching(OwnNs)
      .And().HaveNameEndingWith("Adapter")
      .Should().ResideInNamespaceMatching("^TheOffice\\.Application\\.Interfaces"));

  // Output goes through the right port. Every own production layer is named here on purpose:
  // listing only Domain and Application would leave Persistence and Api free to write to the
  // console while the test still passed. Add any new layer to this list.
  [Fact]
  public void Only_adapters_should_write_to_the_console() =>
    Check(Types().That().ResideInNamespaceMatching(DomainNs)
      .Or().ResideInNamespaceMatching(ApplicationNs)
      .Or().ResideInNamespaceMatching(PersistenceNs)
      .Or().ResideInNamespaceMatching(ApiNs)
      .Should().NotDependOnAny(Types().That().HaveFullName("System.Console")));
}
