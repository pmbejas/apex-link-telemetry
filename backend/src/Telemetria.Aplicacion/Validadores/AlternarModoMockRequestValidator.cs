namespace Telemetria.Aplicacion.Validadores;

using FluentValidation;
using Telemetria.Aplicacion.Dtos.Requests;

/// <summary>
/// Validador de reglas FluentValidation para la solicitud de conmutación de modo mock.
/// </summary>
public sealed class AlternarModoMockRequestValidator : AbstractValidator<AlternarModoMockRequest>
{
    public AlternarModoMockRequestValidator()
    {
        // UsarMock es opcional: si es nulo, el controlador invoca la alternancia dinámica del estado vigente.
    }
}
