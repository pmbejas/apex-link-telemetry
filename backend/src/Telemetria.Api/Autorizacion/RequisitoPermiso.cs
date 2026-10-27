namespace Telemetria.Api.Autorizacion;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Requisito de autorización de ASP.NET Core que almacena el código de permiso requerido.
/// </summary>
public sealed class RequisitoPermiso : IAuthorizationRequirement
{
    public string Permiso { get; }

    public RequisitoPermiso(string permiso)
    {
        Permiso = permiso;
    }
}
