using System.Threading.Tasks;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using Microsoft.AspNetCore.Http;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Transactions;

public class UnitOfWorkMiddleware(RequestDelegate next)
{
  private readonly RequestDelegate _next = next;

  public async Task InvokeAsync(HttpContext context, IAutoDiscoveryIAMUnitOfWork unitOfWork)
  {
    // Solo para métodos que modifican datos (POST, PUT, DELETE, PATCH)
    if (HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method) ||
        HttpMethods.IsPatch(context.Request.Method))
    {
      await unitOfWork.BeginTransactionAsync();

      try
      {
        await _next(context); // Ejecuta el siguiente middleware/handler

        // Si no hubo excepción, confirma la transacción
        await unitOfWork.CommitAsync();
      }
      catch
      {
        // Si ocurre excepción, revierte la transacción
        await unitOfWork.RollbackAsync();
        throw; // Deja que ExceptionHandlingMiddleware gestione la respuesta
      }
    }
    else
    {
      // Para GET u otros métodos seguros, no inicia transacción
      await _next(context);
    }
  }
}
