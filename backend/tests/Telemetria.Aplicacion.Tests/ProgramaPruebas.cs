namespace Telemetria.Aplicacion.Tests;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

public static class ProgramaPruebas
{
    public static async Task<int> Main()
    {
        Assembly ensamblado = typeof(ProgramaPruebas).Assembly;
        int totalEjecutadas = 0;
        int totalFallidas = 0;

        foreach (Type tipo in ensamblado.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            foreach (MethodInfo metodo in tipo.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                bool esFact = metodo.GetCustomAttribute<FactAttribute>() is not null;
                var atributosInline = metodo.GetCustomAttributes<InlineDataAttribute>().ToArray();

                if (!esFact && atributosInline.Length == 0)
                {
                    continue;
                }

                var casosArgumentos = atributosInline.Length > 0
                    ? atributosInline.Select(a => a.GetData(metodo).First()).ToArray()
                    : new object?[][] { Array.Empty<object?>() };

                foreach (object?[] argumentos in casosArgumentos)
                {
                    totalEjecutadas++;
                    object? instancia = Activator.CreateInstance(tipo);
                    try
                    {
                        object? resultado = metodo.Invoke(instancia, argumentos);
                        if (resultado is Task tarea)
                        {
                            await tarea;
                        }

                        Console.WriteLine($"[OK] {tipo.Name}.{metodo.Name}");
                    }
                    catch (Exception excepcion)
                    {
                        totalFallidas++;
                        Exception causa = excepcion is TargetInvocationException tie && tie.InnerException is not null
                            ? tie.InnerException
                            : excepcion;
                        Console.Error.WriteLine($"[FALLO] {tipo.Name}.{metodo.Name}: {causa.GetType().Name} - {causa.Message}");
                    }
                    finally
                    {
                        if (instancia is IAsyncDisposable desechableAsync)
                        {
                            await desechableAsync.DisposeAsync();
                        }
                        else if (instancia is IDisposable desechable)
                        {
                            desechable.Dispose();
                        }
                    }
                }
            }
        }

        Console.WriteLine($"Resumen de pruebas ({ensamblado.GetName().Name}): {totalEjecutadas - totalFallidas}/{totalEjecutadas} exitosas, {totalFallidas} fallidas.");
        return totalFallidas == 0 ? 0 : 1;
    }
}
