using DGA.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace DGA.Web.Services;

/// <summary>
/// Genera "SOL-00001" usando la SEQUENCE de SQL Server (dbo.SolicitudIdSequence,
/// database/01_schema_dga.sql). NEXT VALUE FOR es atómico a nivel de motor — corrige
/// la condición de carrera del sitio original, que calculaba el siguiente correlativo
/// en el cliente mirando solo las solicitudes visibles al usuario actual.
///
/// Se usa ADO.NET directo (no Database.SqlQueryRaw) porque EF Core compone ese método
/// como una subconsulta al encadenar operadores LINQ (ToListAsync/SingleAsync), y SQL
/// Server no permite NEXT VALUE FOR dentro de una subconsulta/derived table.
/// </summary>
public class SolicitudIdGenerator(ApplicationDbContext db)
{
    public async Task<string> NuevoIdAsync() => await EjecutarAsync("SELECT NEXT VALUE FOR dbo.SolicitudIdSequence");

    /// <summary>Muestra el próximo correlativo SIN reservarlo (a diferencia de
    /// <see cref="NuevoIdAsync"/>, no consume la secuencia) — para mostrarlo en el
    /// formulario de "Nueva Solicitud" apenas se abre, sin dejar huecos en la numeración
    /// si el usuario nunca llega a guardar. Es el valor MÁS PROBABLE, no garantizado: si
    /// dos personas crean una solicitud a la vez, a una de las dos le va a tocar el
    /// siguiente número real en Guardar(), que sí es atómico.</summary>
    public async Task<string> PrevisualizarProximoIdAsync()
    {
        var actual = await EjecutarEscalarAsync(
            "SELECT current_value FROM sys.sequences WHERE name = 'SolicitudIdSequence'");
        var siguiente = (actual is null or DBNull ? 0 : Convert.ToInt32(actual)) + 1;
        return $"SOL-{siguiente:D5}";
    }

    private async Task<string> EjecutarAsync(string sql)
    {
        var resultado = (int)(await EjecutarEscalarAsync(sql))!;
        return $"SOL-{resultado:D5}";
    }

    private async Task<object?> EjecutarEscalarAsync(string sql)
    {
        var conexion = db.Database.GetDbConnection();
        var abrioAca = conexion.State != System.Data.ConnectionState.Open;
        if (abrioAca)
        {
            await conexion.OpenAsync();
        }
        try
        {
            await using var comando = conexion.CreateCommand();
            comando.CommandText = sql;
            return await comando.ExecuteScalarAsync();
        }
        finally
        {
            if (abrioAca)
            {
                await conexion.CloseAsync();
            }
        }
    }
}
