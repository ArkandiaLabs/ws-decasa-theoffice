using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using TheOffice.Persistence;

namespace TheOffice.Api.Tests;

/// <summary>
/// La API completa — enrutamiento, versionado, model binding y serializacion — sobre una base
/// SQLite en memoria. Es el unico sitio donde se prueba lo que el `ProductController` hace de
/// verdad: depende de la clase concreta `ProductService`, cuyos metodos no son `virtual`, asi
/// que no hay forma de cubrirlo con dobles.
///
/// El entorno es `Development` a proposito: es el unico donde `Program` aplica migraciones al
/// arrancar, de modo que la base en memoria queda con el esquema y el catalogo sembrado sin
/// montar nada aparte. La conexion se mantiene abierta porque una base `:memory:` muere con ella.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
  private readonly SqliteConnection _connection = new("Data Source=:memory:");

  public ApiFactory()
  {
    _connection.Open();
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Development");

    // Se ejecuta despues de lo que registra `Program`, que es lo que permite reemplazarlo.
    builder.ConfigureServices(services =>
    {
      // `AddDbContext` deja mas de un registro: las opciones y su configuracion. Se quitan
      // todos; dejar uno vivo haria que el contexto siguiera apuntando al archivo SQLite del
      // repositorio y las pruebas escribirian sobre la base de desarrollo.
      var doomed = services
        .Where(x => x.ServiceType.FullName?.Contains("DbContextOptions", StringComparison.Ordinal) == true)
        .ToList();

      foreach (var descriptor in doomed)
      {
        services.Remove(descriptor);
      }

      services.AddDbContext<TheOfficeDbContext>(options => options.UseSqlite(_connection));
    });
  }

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);

    if (disposing)
    {
      _connection.Dispose();
    }
  }
}
