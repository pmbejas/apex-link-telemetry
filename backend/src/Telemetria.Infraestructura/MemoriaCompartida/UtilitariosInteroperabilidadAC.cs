namespace Telemetria.Infraestructura.MemoriaCompartida;

public static class UtilitariosInteroperabilidadAC
{
    public static unsafe string LeerCadenaFija(char* puntero, int longitudMaxima)
    {
        int longitudReal = 0;
        while (longitudReal < longitudMaxima && puntero[longitudReal] != '\0')
        {
            longitudReal++;
        }
        return new string(puntero, 0, longitudReal);
    }

    public static unsafe string LeerTiempoTextoNormalizado(
        char* puntero,
        int longitudMaxima,
        string valorPorDefecto)
    {
        string texto = LeerCadenaFija(puntero, longitudMaxima).Trim();
        return string.IsNullOrWhiteSpace(texto) ? valorPorDefecto : texto;
    }

    public static int NormalizarMarcha(int marchaCrudaAssettoCorsa)
    {
        return marchaCrudaAssettoCorsa switch
        {
            <= 0 => -1, // Reversa (0 en AC) o valor no inicializado
            1 => 0,     // Neutral (Punto Muerto)
            _ => marchaCrudaAssettoCorsa - 1 // 2 -> 1, 3 -> 2, etc.
        };
    }

    public static string MapearBanderaTexto(int codigoBandera)
    {
        return codigoBandera switch
        {
            1 => "BLUE",
            2 => "YELLOW",
            3 => "BLACK",
            4 => "WHITE",
            5 => "CHECKERED",
            6 => "PENALTY",
            _ => "GREEN" // 0 (AC_NO_FLAG) representa pista limpia / bandera verde
        };
    }
}
