// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMEndpointRouteBuilderExtensionsTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Composition;
using Microsoft.AspNetCore.Builder;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.Composition;

public class AutoDiscoveryIAMEndpointRouteBuilderExtensionsTests
{
  [Fact(DisplayName = "Mapear los endpoints dentro de un grupo falla: los límites por defecto dejarían de aplicarse")]
  public async Task MapAutoDiscoveryIAMEndpoints_InsideAGroup_Throws()
  {
    await using var app = WebApplication.CreateBuilder().Build();

    var act = () => app.MapGroup("/api").MapAutoDiscoveryIAMEndpoints();

    act.Should().Throw<InvalidOperationException>().WithMessage("*UsePathBase*");
  }
}
