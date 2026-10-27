namespace Telemetria.Api.Autorizacion;

using System;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Atributo de autorización que exige un permiso granular bajo la convención universal recurso:accion.
/// Asocia dinámicamente el prefijo de política evaluado por ProveedorPoliticasPermiso.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PREFIJO_POLITICA = "Permiso:";

    public HasPermissionAttribute(string permiso)
        : base($"{PREFIJO_POLITICA}{permiso}")
    {
        Permiso = permiso;
    }

    public string Permiso { get; }
}
