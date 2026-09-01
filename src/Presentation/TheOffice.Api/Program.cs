using Asp.Versioning;

using Microsoft.EntityFrameworkCore;

using Scalar.AspNetCore;

using TheOffice.Adapters;
using TheOffice.Api.Middleware;
using TheOffice.Application;
using TheOffice.Persistence;

const string CorsPolicy = "TheOfficeFrontends";

var builder = WebApplication.CreateBuilder(args);
{
  builder.Services
    .AddApplication()
    .AddAdapters()
    .AddPersistence(builder.Configuration);

  builder.Services
    .AddApiVersioning(options =>
    {
      options.DefaultApiVersion = new ApiVersion(1, 0);
      options.ReportApiVersions = true;
      options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
      options.GroupNameFormat = "'v'VVV";
      options.SubstituteApiVersionInUrl = true;
    })
    .AddOpenApi();

  builder.Services.AddControllers();

  // Los frontends del catalogo consumen esta API desde otro origen.
  var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

  builder.Services.AddCors(options =>
  {
    options.AddPolicy(CorsPolicy, policy =>
    {
      if (allowedOrigins.Length == 0)
      {
        // Refleja el Origin de la peticion para que cualquier frontend del catalogo funcione
        // sin listar su dominio, y habilita credenciales para las cookies de sesion futuras.
        policy.SetIsOriginAllowed(_ => true).AllowCredentials();
      }
      else
      {
        policy.WithOrigins(allowedOrigins).AllowCredentials();
      }

      policy.AllowAnyHeader().AllowAnyMethod();
    });
  });
}

var app = builder.Build();
{
  // Va primero para que envuelva todo lo demas: el status que registra al salir ya incluye lo
  // que hizo el handler de excepciones.
  app.UseMiddleware<RequestLoggingMiddleware>();

  if (app.Environment.IsDevelopment())
  {
    using (var scope = app.Services.CreateScope())
    {
      scope.ServiceProvider.GetRequiredService<TheOfficeDbContext>().Database.Migrate();
    }

    app.MapOpenApi().WithDocumentPerVersion();

    // Scalar solo muestra el documento v1 si no se le declaran los demas: los endpoints de
    // v2 quedan invisibles en /scalar aunque /openapi/v2.json exista. Las versiones se leen
    // del versionador, para que una nueva aparezca sin tocar esto.
    var apiVersions = app.DescribeApiVersions().Select(x => x.GroupName).ToArray();
    app.MapScalarApiReference(options => options.AddDocuments(apiVersions));
  }
  else
  {
    app.UseHttpsRedirection();
  }

  // Un handler unico traduce cualquier excepcion no controlada a 500 con el detalle del error,
  // para que el frontend pueda mostrar por que fallo en vez de un cuerpo vacio.
  app.UseExceptionHandler(errorApp =>
  {
    errorApp.Run(async context =>
    {
      var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
      context.Response.StatusCode = 500;
      await context.Response.WriteAsync(feature?.Error.ToString() ?? "Unknown error");
    });
  });

  app.UseCors(CorsPolicy);
  app.MapControllers();
  app.Run();
}

// Las instrucciones de nivel superior compilan a una clase `Program` interna y sin nombre que
// otro ensamblado no puede escribir. `WebApplicationFactory<Program>` necesita nombrarla, asi
// que se declara aqui como parcial y publica. No agrega comportamiento: solo la hace visible.
public partial class Program { }
