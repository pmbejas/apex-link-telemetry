namespace Telemetria.Api.Configuracion;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

public static class ValidadorOrigenRedLocal
{
    public static bool EsOrigenPermitido(string? origen, IReadOnlyCollection<string>? origenesConfigurados = null)
    {
        if (string.IsNullOrWhiteSpace(origen))
        {
            return false;
        }

        if (origenesConfigurados is not null && origenesConfigurados.Contains(origen, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Uri.TryCreate(origen, UriKind.Absolute, out var uriOrigen))
        {
            return false;
        }

        if (uriOrigen.Scheme is not ("http" or "https"))
        {
            return false;
        }

        if (string.Equals(uriOrigen.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(uriOrigen.Host, out var direccionIp))
        {
            return false;
        }

        if (IPAddress.IsLoopback(direccionIp))
        {
            return true;
        }

        return EsIpPrivadaRfc1918(direccionIp);
    }

    private static bool EsIpPrivadaRfc1918(IPAddress direccionIp)
    {
        if (direccionIp.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        byte[] octetos = direccionIp.GetAddressBytes();

        // 10.0.0.0/8 (10.0.0.0 - 10.255.255.255)
        if (octetos[0] == 10)
        {
            return true;
        }

        // 172.16.0.0/12 (172.16.0.0 - 172.31.255.255)
        if (octetos[0] == 172 && octetos[1] >= 16 && octetos[1] <= 31)
        {
            return true;
        }

        // 192.168.0.0/16 (192.168.0.0 - 192.168.255.255)
        return octetos[0] == 192 && octetos[1] == 168;
    }
}
