using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TheOffice.Persistence.Tests;

/// <summary>
/// Una base SQLite en memoria por prueba, con el esquema real que produce
/// <see cref="TheOfficeDbContext"/> — incluido el <c>HasConversion&lt;double&gt;()</c> de
/// <c>Price</c>, que es justo lo que un doble en memoria no ejercitaria.
///
/// La conexion se abre en el constructor y se mantiene abierta: una base <c>:memory:</c> vive
/// mientras viva su conexion, asi que cerrarla entre contextos borraria las tablas.
///
/// Los datos sembrados por <c>HasData</c> llegan con el esquema y se vacian de inmediato: estas
/// pruebas describen su propio catalogo, y una fila mas en un seeder no puede mover un assert.
/// </summary>
internal sealed class TestDatabase : IDisposable
{
  private readonly SqliteConnection _connection;

  public TestDatabase()
  {
    _connection = new SqliteConnection("Data Source=:memory:");
    _connection.Open();

    using var context = NewContext();
    context.Database.EnsureCreated();

    // Los productos primero: Product -> Category es `Restrict`, y las fotos y presentaciones
    // se van en cascada con su producto.
    context.Products.RemoveRange(context.Products.ToList());
    context.SaveChanges();
    context.Categories.RemoveRange(context.Categories.ToList());
    context.Customers.RemoveRange(context.Customers.ToList());
    context.SaveChanges();
  }

  /// <summary>
  /// Un contexto nuevo por operacion. Compartir uno haria que un `Add` quedara visible en la
  /// consulta siguiente por el rastreador y no por la base, que es lo que se quiere probar.
  /// </summary>
  public TheOfficeDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<TheOfficeDbContext>()
      .UseSqlite(_connection)
      .Options;

    return new TheOfficeDbContext(options);
  }

  public void Seed(Action<TheOfficeDbContext> arrange)
  {
    using var context = NewContext();
    arrange(context);
    context.SaveChanges();
  }

  public void Dispose()
  {
    _connection.Dispose();
    GC.SuppressFinalize(this);
  }
}
