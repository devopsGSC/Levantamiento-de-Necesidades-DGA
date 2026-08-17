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
    public async Task<string> NuevoIdAsync()
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
            comando.CommandText = "SELECT NEXT VALUE FOR dbo.SolicitudIdSequence";
            var resultado = (int)(await comando.ExecuteScalarAsync())!;
            return $"SOL-{resultado:D5}";
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
