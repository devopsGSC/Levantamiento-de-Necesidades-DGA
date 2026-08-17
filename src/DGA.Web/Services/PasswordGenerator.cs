using System.Security.Cryptography;

namespace DGA.Web.Services;

/// <summary>Genera contraseñas temporales que cumplen la política de Identity (mín. 12, mayúscula, minúscula, número, especial).</summary>
public static class PasswordGenerator
{
    private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijkmnpqrstuvwxyz";
    private const string Numeros = "23456789";
    private const string Especiales = "!@#$%^&*?";
    private const string Todos = Mayusculas + Minusculas + Numeros + Especiales;

    public static string Generar(int longitud = 14)
    {
        Span<char> resultado = stackalloc char[longitud];
        resultado[0] = Elegir(Mayusculas);
        resultado[1] = Elegir(Minusculas);
        resultado[2] = Elegir(Numeros);
        resultado[3] = Elegir(Especiales);
        for (var i = 4; i < longitud; i++)
        {
            resultado[i] = Elegir(Todos);
        }

        // Mezclar para que los 4 primeros caracteres no sigan siempre el mismo patrón.
        for (var i = resultado.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (resultado[i], resultado[j]) = (resultado[j], resultado[i]);
        }

        return new string(resultado);
    }

    private static char Elegir(string alfabeto) => alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];
}
