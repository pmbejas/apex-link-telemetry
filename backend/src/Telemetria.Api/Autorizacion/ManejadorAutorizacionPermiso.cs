namespace Telemetria.Api.Autorizacion;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Manejador de autorización que evalúa si el usuario autenticado posee el claim de permiso requerido
/// o si la estación opera en modo local Edge sin token según la configuración del entorno.
/// </summary>
public sealed class ManejadorAutorizacionPermiso : AuthorizationHandler<RequisitoPermiso>
{
    private const string TIPO_CLAIM_PERMISO = "permiso";
    private readonly bool _permitirAccesoLocalEdgeSinToken;

    public ManejadorAutorizacionPermiso(IConfiguration configuracion)
    {
        _permitirAccesoLocalEdgeSinToken = configuracion.GetValue<bool>(
            "Seguridad:PermitirAccesoLocalEdgeSinToken", true);
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext contexto,
        RequisitoPermiso requisito)
    {
        bool poseePermisoExplicito = contexto.User.HasClaim(
            claim => claim.Type == TIPO_CLAIM_PERMISO &&
                     string.Equals(claim.Value, requisito.Permiso, StringComparison.OrdinalIgnoreCase));

        if (poseePermisoExplicito || _permitirAccesoLocalEdgeSinToken)
        {
            contexto.Succeed(requisito);
        }

        return Task.CompletedTask;
    }
}
