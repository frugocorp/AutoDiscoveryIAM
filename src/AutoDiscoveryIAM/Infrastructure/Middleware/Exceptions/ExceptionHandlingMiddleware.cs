// -----------------------------------------------------------------------
// <copyright file="ExceptionHandlingMiddleware.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Reflection;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Exceptions;

public class ExceptionHandlingMiddleware(
    ILoggerBase<ExceptionHandlingMiddleware> logger,
    RequestDelegate next,
    IWebHostEnvironment environment)
{
  private readonly ILoggerBase<ExceptionHandlingMiddleware> _logger = logger;
  private readonly RequestDelegate _next = next;
  private readonly IWebHostEnvironment _environment = environment;

  public async Task Invoke(HttpContext context)
  {
    try
    {
      await _next(context); // Ejecuta el pipeline
    }
    catch (Exception ex)
    {
      await HandleExceptionAsync(context, ex);
    }
  }

  private async Task HandleExceptionAsync(HttpContext context, Exception ex)
  {
    // Manejar excepciones específicas de autorización
    if (await TryHandleAuthorizationExceptionAsync(context, ex))
    {
      return;
    }

    // Manejar otras excepciones como antes
    await HandleGenericExceptionAsync(context, ex);
  }

  private static async Task<bool> TryHandleAuthorizationExceptionAsync(HttpContext context, Exception ex)
  {
    return ex switch
    {
      AuthenticationRequiredException authEx => await WriteAuthorizationResponseAsync(context, authEx.Message, authEx.ErrorInfo),
      InsufficientPermissionsException permEx => await WriteAuthorizationResponseAsync(context, permEx.Message, permEx.ErrorInfo),
      DevelopmentAccessRequiredException devEx => await WriteAuthorizationResponseAsync(context, devEx.Message, devEx.ErrorInfo),
      SuperAdminAccessRequiredException suAdminEx => await WriteAuthorizationResponseAsync(context, suAdminEx.Message, suAdminEx.ErrorInfo),
      _ => false,
    };
  }

  private static async Task<bool> WriteAuthorizationResponseAsync(HttpContext context, string message, ErrorInfo errorInfo)
  {
    context.Response.StatusCode = errorInfo.StatusCode;
    context.Response.ContentType = "application/json";

    var response = new ApiResponse<object>(
        false,
        null,
        message,
        errorInfo);

    await context.Response.WriteAsJsonAsync(response);
    return true;
  }

  private async Task HandleGenericExceptionAsync(HttpContext context, Exception ex)
  {
    var logLocation = GetExceptionLocation(ex);

    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
    context.Response.ContentType = "application/problem+json";

    var errorMessage = BuildErrorMessage(ex, logLocation, _environment.IsDevelopment());

    // El log siempre tiene la ruta completa y el detalle real, sin importar el ambiente
    _logger.LogError($"Error inesperado {logLocation}: {ex.Message}", ex);

    var response = new ApiResponse<object>(
        false,
        null,
        errorMessage,
        ErrorInfo.Status500Unknown);

    await context.Response.WriteAsJsonAsync(response);
  }

  private static string GetExceptionLocation(Exception ex)
  {
    var st = new System.Diagnostics.StackTrace(ex, true);
    var frames = st.GetFrames();
    System.Diagnostics.StackFrame? frame = null;

    if (frames != null)
    {
      // Busca el primer frame con archivo y línea válidos
      frame = frames.FirstOrDefault(f =>
          !string.IsNullOrEmpty(f.GetFileName()) &&
          f.GetFileLineNumber() > 0);
      // Si no hay archivo, toma el primer frame con clase y método
      if (frame == null)
      {
        frame = frames.FirstOrDefault(f => f.GetMethod()?.DeclaringType != null);
      }
    }

    return frame == null ? "[Sin información de ubicación]" : BuildLocationString(frame);
  }

  private static string BuildLocationString(System.Diagnostics.StackFrame frame)
  {
    var method = frame.GetMethod();
    var className = method?.DeclaringType?.FullName ?? "Desconocido";
    var methodName = method?.Name ?? "Desconocido";
    var fileName = frame.GetFileName() ?? "Desconocido";
    var lineNumber = frame.GetFileLineNumber();
    var columnNumber = frame.GetFileColumnNumber();
    var ilOffset = frame.GetILOffset();
    var nativeOffset = frame.GetNativeOffset();
    var isStatic = method?.IsStatic ?? false;
    var parameters = method?.GetParameters();
    var paramList = parameters != null ? string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}")) : "";
    var returnType = (method as MethodInfo)?.ReturnType?.Name ?? "void";
    var assembly = method?.DeclaringType?.Assembly.FullName ?? "Desconocido";

    return
        $"method: {methodName}({paramList}) | " +
        $"class: {className} | " +
        $"file: {fileName} | " +
        $"line: {lineNumber} | " +
        $"column: {columnNumber} | " +
        $"IL offset: {ilOffset} | " +
        $"Native offset: {nativeOffset} | " +
        $"isStatic: {isStatic} | " +
        $"returnType: {returnType} | " +
        $"assembly: {assembly}";
  }

  private static string BuildErrorMessage(Exception ex, string logLocation, bool isDevelopment)
  {
    // Fuera de Development, el cliente no recibe ex.Message ni ubicación: ambos pueden filtrar
    // detalles internos (SQL, rutas de archivo, tipos internos) a un llamante anónimo. El detalle
    // completo siempre queda en el log (ver HandleGenericExceptionAsync), nunca solo en la respuesta.
    if (!isDevelopment)
    {
      return "Error inesperado. Contacte al administrador del sistema.";
    }

    return $"Error inesperado {logLocation}: {ex.Message} | DEBUG Stacktrace: {ex}";
  }
}
