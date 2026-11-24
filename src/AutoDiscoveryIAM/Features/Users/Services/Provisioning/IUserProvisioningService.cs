// -----------------------------------------------------------------------
// <copyright file="IUserProvisioningService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.Provisioning;

/// <summary>
/// Alta de usuarios desde el código de la app que consume la librería, con las mismas reglas que
/// <c>POST /users/by-admin</c>: validación de formato (usuario, correo, fuerza de la contraseña),
/// correo o usuario repetido (409), rol por defecto, correo de invitación o de activación,
/// <c>MustChangePassword</c> en <c>true</c> por defecto y correo sin verificar. Es un uso soportado y
/// estable entre versiones: un endpoint propio (por ejemplo <c>POST /empleados</c>) lo inyecta para
/// crear el usuario junto con su entidad propia. El servicio no revisa permisos: los decide el
/// endpoint que lo llama, con su propio permiso.
/// </summary>
public interface IUserProvisioningService
{
  /// <summary>
  /// Valida y crea el usuario, y guarda.
  /// </summary>
  /// <param name="request">Datos del alta.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>
  /// El usuario creado, o el error: 400 (<see cref="ErrorInfo.Status400Validation"/>) si un dato no
  /// cumple las reglas, 409 si el correo o el usuario ya existen, 424 si no se pudo enviar el correo.
  /// </returns>
  Task<OperationResult<ProvisionedUser>> CreateAsync(ProvisionUserRequest request, CancellationToken cancellationToken);

  /// <summary>
  /// Valida y crea el usuario, y antes de guardar llama a <paramref name="beforeSave"/> con el
  /// usuario nuevo (todavía sin guardar), para que el consumidor agregue al mismo contexto su entidad
  /// ligada a él (por navegación, porque el usuario aún no tiene Id). Todo se guarda en un solo
  /// <c>SaveChangesAsync</c>: si <paramref name="beforeSave"/> lanza una excepción, no se guarda el
  /// usuario ni se envía ningún correo, y la excepción sale tal cual. Requiere que la entidad del
  /// consumidor viva en el mismo <c>DbContext</c> (un contexto que hereda de
  /// <c>AutoDiscoveryIAMDbContext</c>). <paramref name="beforeSave"/> solo debe enlazar: si cambia
  /// los datos del usuario que fija esta regla de alta (correo, usuario, contraseña, estado,
  /// verificación, banderas de administración), el alta se cancela con
  /// <see cref="InvalidOperationException"/>.
  /// </summary>
  /// <param name="request">Datos del alta.</param>
  /// <param name="beforeSave">Enlaza la entidad propia del consumidor con el usuario nuevo.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>El usuario creado, o el error (los mismos que la otra sobrecarga).</returns>
  Task<OperationResult<ProvisionedUser>> CreateAsync(ProvisionUserRequest request, Func<User, CancellationToken, Task> beforeSave, CancellationToken cancellationToken);
}
