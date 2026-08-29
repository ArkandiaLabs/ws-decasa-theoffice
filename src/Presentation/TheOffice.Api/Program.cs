using Asp.Versioning;

using Microsoft.EntityFrameworkCore;

using Scalar.AspNetCore;

using TheOffice.Adapters;
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
        policy.AllowAnyOrigin();
      }
      else
      {
        policy.WithOrigins(allowedOrigins);
      }

      policy.AllowAnyHeader().AllowAnyMethod();
    });
  });
}

var app = builder.Build();
{
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

  app.UseCors(CorsPolicy);
  app.MapControllers();
  app.Run();
}
