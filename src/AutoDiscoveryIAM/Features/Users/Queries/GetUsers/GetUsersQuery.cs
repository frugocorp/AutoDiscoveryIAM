// -----------------------------------------------------------------------
// <copyright file="GetUsersQuery.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;

public record GetUsersQuery(int PageNumber, int PageSize);
