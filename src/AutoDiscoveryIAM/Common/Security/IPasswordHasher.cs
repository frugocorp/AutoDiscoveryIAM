// -----------------------------------------------------------------------
// <copyright file="IPasswordHasher.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;

public interface IPasswordHasher
{
  string HashPassword(string password);
  bool VerifyPassword(string password, string hashedPassword);
}
