namespace Telemetria.Dominio.Tests;

using System;
using Telemetria.Dominio.Constantes;
using Telemetria.Dominio.Excepciones;
using Telemetria.Dominio.Modelos;
using Xunit;

public sealed class EntidadesYExcepcionesTests
{
    private sealed class EntidadDePrueba : EntidadBase { }

    [Fact]
    public void EntidadBase_Instanciacion_GeneraGuidNoVacioYMarcaUtc()
    {
        // Act
        var entidad = new EntidadDePrueba();

        // Assert
        Assert.NotEqual(Guid.Empty, entidad.Id);
        Assert.True(entidad.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(entidad.UpdatedAt);
    }

    [Fact]
    public void EntidadBase_ModificacionMarcaTemporal_PermiteAsignarUpdatedAt()
    {
        // Arrange
        var entidad = new EntidadDePrueba();
        var marcaModificacion = DateTimeOffset.UtcNow;

        // Act
        entidad.UpdatedAt = marcaModificacion;

        // Assert
        Assert.NotNull(entidad.UpdatedAt);
        Assert.Equal(marcaModificacion, entidad.UpdatedAt);
    }

    [Fact]
    public void SesionInvalidaExcepcion_InstanciacionConMensaje_PreservaMensaje()
    {
        // Arrange & Act
        const string mensajeError = "La sesión no cuenta con suficientes vueltas válidas.";
        var excepcion = new SesionInvalidaExcepcion(mensajeError);

        // Assert
        Assert.Equal(mensajeError, excepcion.Message);
    }

    [Fact]
    public void SimuladorDesconectadoExcepcion_InstanciacionConMensajeYCausa_PreservaDetalles()
    {
        // Arrange
        var causaInterna = new InvalidOperationException("MMF no encontrado");
        const string mensaje = "El simulador Assetto Corsa no se encuentra en ejecución.";

        // Act
        var excepcion = new SimuladorDesconectadoExcepcion(mensaje, causaInterna);

        // Assert
        Assert.Equal(mensaje, excepcion.Message);
        Assert.Same(causaInterna, excepcion.InnerException);
    }

    [Fact]
    public void MetadatosEstaticos_ValoresIniciales_PreservaPropiedadesInmutables()
    {
        // Act
        var metadatos = new MetadatosEstaticos
        {
            NombreAuto = "ferrari_488_gt3",
            NombrePista = "monza",
            ConfiguracionPista = "gp",
            MaxRpm = 8500,
            MaxCombustible = 110.0f,
            CantidadSectores = 3,
            LongitudPistaMetros = 5793.0f
        };

        // Assert
        Assert.Equal("ferrari_488_gt3", metadatos.NombreAuto);
        Assert.Equal("monza", metadatos.NombrePista);
        Assert.Equal(8500, metadatos.MaxRpm);
        Assert.Equal(110.0f, metadatos.MaxCombustible);
        Assert.Equal(3, metadatos.CantidadSectores);
        Assert.Equal(5793.0f, metadatos.LongitudPistaMetros);
    }
}
