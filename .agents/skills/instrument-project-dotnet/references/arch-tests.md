# Architecture fitness functions

> Called from **Phase 4** of `SKILL.md`, to install control 7.

This turns the dependency rule from a diagram in `docs/architecture.md` into a **computational
sensor**: a test that goes red.

It is the highest-impact control against an AI coding agent. It is what stops the agent from
"stepping outside the guidelines" with nobody reviewing the PR.

---

> **Read `architecture-discovery.md` first.** The rules below assume a layered design. Which rules
> apply — and whether any of them do — is decided by the shape detected in Phase 1, not by this
> file. Every rule must pass against the current code before it is written.

## What to turn into a rule

**The criterion: any rule written in `AGENTS.md`, `docs/architecture.md`, or an ADR that an agent
could violate.** Documented but unverified is a suggestion, not a rule.

Do not invent rules. If the repo's `Application` layer already references EF Core everywhere,
writing that rule produces a red build and no value — report it as a finding instead.

| Rule | Why it matters |
|---|---|
| Domain depends on no other layer | It is the pure core; if it leaks, everything leaks |
| Application does not depend on Infrastructure or Presentation | Dependency inversion is the whole point of the architecture |
| Application does not reference the ORM | Persistence is a detail, not a contract |
| No cycles between layers | A cycle makes the architecture unlearnable |
| Services return a result type instead of throwing for flow | Team convention, invisible to the compiler |
| No direct console writes outside adapters | Output goes through the right port |
| Naming conventions (suffixes, async naming) | Legibility of generated code |

Start with the first three. Add the rest once those are green.

---

## Creating the project

Match the repo's test framework and runner — do not introduce a second one.

**Copy the repo's existing test project; do not reach for `dotnet new`.** The in-box templates
cover xUnit v2, NUnit and MSTest, but not every framework a repo may be on — `dotnet new xunit3`,
for one, does not exist until someone runs `dotnet new install xunit.v3.templates`. Cloning is
also simply more accurate: the existing project already carries the right framework, the right
runner, the right SDK attribute and whatever `Directory.Build.props` overrides the repo needs.

```bash
mkdir -p <tests-dir>/<Root>.ArchitectureTests
cp <existing-test-project>/<Name>.csproj <tests-dir>/<Root>.ArchitectureTests/<Root>.ArchitectureTests.csproj
# then: strip the <ProjectReference> elements that project used, keep its test packages
dotnet sln <solution> add <tests-dir>/<Root>.ArchitectureTests
dotnet add <tests-dir>/<Root>.ArchitectureTests package TngTech.ArchUnitNET
dotnet add <tests-dir>/<Root>.ArchitectureTests package <the extension from the table below>
dotnet build <tests-dir>/<Root>.ArchitectureTests
```

Add `--version` to nothing. Read the resolved version back from the `.csproj` and report it. Build
the empty project before writing a single rule — a project that does not compile turns every
later failure into a guess.

The project needs a `<ProjectReference>` to **every layer it asserts about**. A rule that targets
the ORM needs the ORM's types loadable — but check before adding anything: referencing Persistence
usually brings the ORM in transitively, and the assembly is already there. Adding a direct
`<PackageReference>` you do not need puts a dependency in the test project that the rule then
pretends to forbid. Verify with `dotnet list <arch-test-project> package --include-transitive`.

### Pick the extension that matches the runner

The core `TngTech.ArchUnitNET` package evaluates rules; the extension package adds the fluent
`.Check()` that reports the failure through the test framework. **They are not interchangeable —
an extension built for another framework fails to load, and the symptom is a test project that
does not run at all rather than a clear error.**

| Repo's test framework | Extension package |
|---|---|
| xUnit v2 | `TngTech.ArchUnitNET.xUnit` |
| xUnit v3 (Microsoft.Testing.Platform) | `TngTech.ArchUnitNET.xUnitV3` |
| NUnit | `TngTech.ArchUnitNET.NUnit` |
| MSTest v2 | `TngTech.ArchUnitNET.MSTestV2` |
| MSTest v4 | `TngTech.ArchUnitNET.MSTestV4` |
| TUnit | `TngTech.ArchUnitNET.TUnit` |

Verify the package resolved before writing rules against it: `dotnet restore` on the new project,
then confirm a trivial test runs. **If no extension matches the repo's framework, or the version
NuGet resolves is old enough not to carry it, fall back to the core package alone** and assert by
hand — the rules stay identical, only the assertion changes:

```csharp
private static void Check(IArchRule rule)
{
  var failures = rule.Evaluate(Arch).Where(r => !r.Passed).ToList();
  Assert.True(
    failures.Count == 0,
    string.Join("\n", failures.Select(f => f.Description)));
}
```

This keeps the failure message useful whichever extension package you end up with. **The assertion
call itself is framework-specific** — the examples in this file use xUnit's `[Fact]` and
`Assert.True`; swap them for `[Test]`/`Assert.That` on NUnit, `[TestMethod]`/`Assert.IsTrue` on
MSTest. Only the rule expressions are portable.

Under central package management, add a `<PackageVersion Include="TngTech.ArchUnitNET" …>` entry
per package and leave every `<PackageReference>` version-less.

---

## Baseline rules

Adapt the namespace constants and the assembly-anchor types to the repository. The anchor types
must be real types you have read — a wrong type name is a compile error, and a type from the wrong
assembly silently loads the wrong thing.

```csharp
using System.Linq;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Slices;   // only needed for SliceRuleDefinition
using ArchUnitNET.Loader;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace <Root>.ArchitectureTests;

// Clean Architecture's dependency rule — today held up only by project references —
// turned into a sensor that fails `dotnet test` when a layer reaches somewhere it must not.
public class LayerDependencyTests
{
  private const string DomainNs = "^<Root>\\.Domain";
  private const string ApplicationNs = "^<Root>\\.Application";
  private const string PersistenceNs = "^<Root>\\.Persistence";
  private const string AdaptersNs = "^<Root>\\.Adapters";
  private const string ApiNs = "^<Root>\\.Api";

  // Load every layer once. The ORM assembly is loaded too so rule 3 has concrete target
  // types to match: ArchUnitNET only evaluates dependencies against types present in the
  // loaded architecture.
  // Fully qualified on purpose. When the project's own namespace contains a segment called
  // Architecture — <Root>.Architecture.Tests, which mirroring the repo's naming often produces —
  // the bare name resolves to that namespace and the compiler stops with CS0118.
  private static readonly ArchUnitNET.Domain.Architecture Arch = new ArchLoader()
    .LoadAssemblies(
      typeof(Domain.Entities.<AnEntity>).Assembly,
      typeof(Application.Services.<AService>).Assembly,
      typeof(Persistence.<TheDbContext>).Assembly,
      typeof(Adapters.<AnAdapter>).Assembly,
      typeof(Api.Controllers.<AController>).Assembly,
      typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly)
    .Build();

  private static void Check(IArchRule rule)
  {
    var failures = rule.Evaluate(Arch).Where(r => !r.Passed).ToList();
    Assert.True(
      failures.Count == 0,
      string.Join("\n", failures.Select(f => f.Description)));
  }

  // Rule 1: Domain is the pure core.
  [Fact]
  public void Domain_should_not_depend_on_other_layers() =>
    Check(Types().That().ResideInNamespaceMatching(DomainNs)
      .Should().NotDependOnAny(
        Types().That().ResideInNamespaceMatching(ApplicationNs)
          .Or().ResideInNamespaceMatching(PersistenceNs)
          .Or().ResideInNamespaceMatching(AdaptersNs)
          .Or().ResideInNamespaceMatching(ApiNs)));

  // Rule 2: Application depends on Domain only.
  [Fact]
  public void Application_should_not_depend_on_infrastructure_or_presentation() =>
    Check(Types().That().ResideInNamespaceMatching(ApplicationNs)
      .Should().NotDependOnAny(
        Types().That().ResideInNamespaceMatching(PersistenceNs)
          .Or().ResideInNamespaceMatching(AdaptersNs)
          .Or().ResideInNamespaceMatching(ApiNs)));

  // Rule 3: persistence is a detail. Application does not couple to the ORM.
  [Fact]
  public void Application_should_not_depend_on_the_orm() =>
    Check(Types().That().ResideInNamespaceMatching(ApplicationNs)
      .Should().NotDependOnAny(
        Types().That().ResideInNamespaceMatching("^Microsoft\\.EntityFrameworkCore")));
}
```

---

## Second-round rules

Add these only once the baseline is green, and only when the repo's docs actually state them.

**Scope every subject selector to your own namespace.** The fixture loads third-party assemblies —
the ORM at minimum, because rule 3 needs its types to compare against. A selector that filters only
by name suffix or shape will match their types too: EF Core alone ships `IUpdateAdapter`, which a
naive "every interface ending in Adapter lives in Application" rule flags on the day you install
it. The suite is then red on arrival, which is exactly what this skill refuses to ship.

Define `OwnNs` once and put `.ResideInNamespaceMatching(OwnNs)` in every `That()` clause that
selects by name rather than by namespace.

```csharp
private const string OwnNs = "^<Root>\\.";

// Ports live in the application layer, not in infrastructure.
[Fact]
public void Ports_should_live_in_application() =>
  Check(Interfaces().That().ResideInNamespaceMatching(OwnNs)
    .And().HaveNameEndingWith("Repository")
    .Should().ResideInNamespaceMatching("^<Root>\\.Application\\.Interfaces"));

// No direct console writes outside adapters.
// NAME THE LAYERS EXPLICITLY, all of them. Selecting only Domain and Application makes the test
// pass while Persistence and Api write to the console freely — a green test whose name promises
// something it never checked, which is worse than no test. Add every own production namespace
// except the ones allowed to write, and update this list when a layer is added.
[Fact]
public void Only_adapters_should_write_to_console() =>
  Check(Types().That().ResideInNamespaceMatching(DomainNs)
    .Or().ResideInNamespaceMatching(ApplicationNs)
    .Or().ResideInNamespaceMatching(PersistenceNs)
    .Or().ResideInNamespaceMatching(ApiNs)
    .Should().NotDependOnAny(Types().That().HaveFullName("System.Console")));

// No cycles between layers. Slices() lives in its own namespace:
//   using ArchUnitNET.Fluent.Slices;
// CAUTION — verify what this actually slices before shipping it. Matching("<Root>.(*)") cuts on
// the namespace segment after the root, which is only "one slice per layer" when the layers are
// exactly one segment deep. In a repo with <Root>.Persistence and <Root>.Persistence.Repositories
// it produces two slices inside a single project and reports a cycle between them — a false
// positive that makes the whole suite look unreliable.
// Run it, read which slices it found, and only keep it if they are the layers. Otherwise assert
// the pairs explicitly, which is what the layers actually mean:
[Fact]
public void Layers_should_be_free_of_cycles() =>
  Check(SliceRuleDefinition.Slices().Matching("<Root>.(*)").Should().BeFreeOfCycles());

// Explicit alternative — no slicing, no surprises. One fact per pair.
[Fact]
public void Persistence_and_Adapters_should_not_depend_on_each_other()
{
  Check(Types().That().ResideInNamespaceMatching(PersistenceNs)
    .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(AdaptersNs)));
  Check(Types().That().ResideInNamespaceMatching(AdaptersNs)
    .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(PersistenceNs)));
}
```

---

## Verification

Add a forbidden dependency to a real file — the ORM `using` inside an application service is the
clearest — and run the tests. The failure message must name the rule.

That is the moment worth showing: **the agent asked for database access from the application layer,
and the repository refused on its own.**

Revert afterwards.
