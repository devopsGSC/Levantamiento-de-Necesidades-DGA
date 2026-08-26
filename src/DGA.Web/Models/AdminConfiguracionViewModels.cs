namespace DGA.Web.Models;

/// <summary>Fila de un catálogo simple: Cargo, Prioridad o TipoAduana — todos comparten
/// la misma forma (Id, Nombre, Orden, Activo), sin relación a otra tabla.</summary>
public class CatalogoSimpleItemViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public short Orden { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Datos para renderizar la tabla + formulario de un catálogo simple (usado 3
/// veces en Admin/Configuracion: Cargos, Prioridades, Tipos de Aduana).</summary>
public class CatalogoSimpleSeccionViewModel
{
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Ej. "/Admin/Configuracion/Cargos" — se le agrega /Crear, /{id}/Editar, /{id}/Activo.</summary>
    public string RutaBase { get; set; } = string.Empty;
    public List<CatalogoSimpleItemViewModel> Items { get; set; } = new();
}

public class AdminConfiguracionIndexViewModel
{
    public string SoporteTelefono { get; set; } = string.Empty;
    public string SoporteCorreo { get; set; } = string.Empty;
    public string SoporteHorario { get; set; } = string.Empty;

    public List<CatalogoSimpleItemViewModel> Cargos { get; set; } = new();
    public List<CatalogoSimpleItemViewModel> UnidadesEjecutoras { get; set; } = new();
    public List<CatalogoSimpleItemViewModel> Prioridades { get; set; } = new();
    public List<CatalogoSimpleItemViewModel> TiposAduana { get; set; } = new();
}

public class AdminAduanaItemViewModel
{
    public int Id { get; set; }
    public byte TipoAduanaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class AdminCatalogoAduanasViewModel
{
    public byte? TipoFiltro { get; set; }
    public List<OpcionCatalogo> TiposAduanaOptions { get; set; } = new();
    public List<AdminAduanaItemViewModel> Aduanas { get; set; } = new();
}

public class AdminDetalleNodoViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class AdminElementoNodoViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool TieneDetalle { get; set; }
    public bool Activo { get; set; }
    public List<AdminDetalleNodoViewModel> Detalles { get; set; } = new();
}

public class AdminSubcomponenteNodoViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public List<AdminElementoNodoViewModel> Elementos { get; set; } = new();
}

public class AdminComponenteNodoViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public List<AdminSubcomponenteNodoViewModel> Subcomponentes { get; set; } = new();
}
