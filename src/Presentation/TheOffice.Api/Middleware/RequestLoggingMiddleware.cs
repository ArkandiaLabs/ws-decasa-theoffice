using System.Diagnostics;

namespace TheOffice.Api.Middleware;

// Trazabilidad de cada peticion: una linea al entrar y otra al salir, ambas con el mismo trace
// id para poder emparejarlas en un log con trafico. El id tambien vuelve en la cabecera
// X-Trace-Id, asi un reporte del frontend apunta a la peticion exacta.
public class RequestLoggingMiddleware
{
  public const string TraceIdHeader = "X-Trace-Id";

  private readonly RequestDelegate _next;
  private readonly ILogger<RequestLoggingMiddleware> _logger;

  public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  // Sin sufijo Async, como el resto del repo. ASP.NET Core acepta Invoke o InvokeAsync.
  public async Task Invoke(HttpContext context)
  {
    var traceId = context.TraceIdentifier;
    context.Response.Headers[TraceIdHeader] = traceId;

    // El scope propaga el trace id a todo lo que loguee el endpoint dentro de esta peticion,
    // asi ningun controller tiene que repetirlo en su propio mensaje.
    using var scope = _logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = traceId });

    _logger.LogInformation(
      "Request started: {Method} {Path}{Query} [trace {TraceId}]",
      context.Request.Method,
      context.Request.Path,
      context.Request.QueryString,
      traceId);

    var stopwatch = Stopwatch.StartNew();
    try
    {
      await _next(context);
    }
    catch (Exception ex)
    {
      // El handler de errores traduce la excepcion a 500 pero no la registra: sin esto el log
      // solo mostraria el status, nunca la causa.
      _logger.LogError(
        ex,
        "Request failed: {Method} {Path} [trace {TraceId}]",
        context.Request.Method,
        context.Request.Path,
        traceId);

      throw;
    }
    finally
    {
      stopwatch.Stop();

      _logger.LogInformation(
        "Request finished: {Method} {Path} responded {StatusCode} in {ElapsedMs} ms [trace {TraceId}]",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        stopwatch.ElapsedMilliseconds,
        traceId);
    }
  }
}
