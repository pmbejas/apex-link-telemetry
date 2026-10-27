namespace Telemetria.Api.Autorizacion;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

/// <summary>
/// Proveedor dinámico de políticas de autorización que genera en tiempo de ejecución
/// políticas basadas en requisitos de permiso (Permiso:recurso:accion).
/// </summary>
public sealed class ProveedorPoliticasPermiso : DefaultAuthorizationPolicyProvider
{
    public ProveedorPoliticasPermiso(IOptions<AuthorizationOptions> opciones)
        : base(opciones)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string nombrePolitica)
    {
        if (nombrePolitica.StartsWith(HasPermissionAttribute.PREFIJO_POLITICA, StringComparison.OrdinalIgnoreCase))
        {
            string codigoPermiso = nombrePolitica.Substring(HasPermissionAttribute.PREFIJO_POLITICA.Length);
            var constructora = new AuthorizationPolicyBuilder();
            constructora.AddRequirements(new RequisitoPermiso(codigoPermiso));
            return constructora.Build();
        }

        return await base.GetPolicyAsync(nombrePolitica);
    }
}
