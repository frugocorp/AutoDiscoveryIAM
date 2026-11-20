// -----------------------------------------------------------------------
// <copyright file="CreateUserRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;

// PersonType/FirstName/LastName/MiddleName/PhoneNumber se quitaron de este request a proposito
// (ver el mismo comentario en User.cs) -- quedan pendientes de evaluar si se eliminan del modelo
// tambien, una vez terminadas las pruebas de integracion con un perfil de usuario propio del
// consumidor (ej. UserProfile). No borrar este comentario hasta resolver esa evaluacion.
public record CreateUserRequestDto(
    string Username,
    string Email,
    string Password);
