namespace DGA.Web.Data.Entities;

/// <summary>Familia de aduana (ZONAS FRANCAS, ADUANAS DE FRONTERAS, PUERTOS, AEROPUERTO, COURIER, ADUANAS INTERNAS).</summary>
public class TipoAduana
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public short Orden { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Aduana> Aduanas { get; set; } = new List<Aduana>();
}

/// <summary>Aduana específica (código + nombre) dentro de un Tipo de Aduana.</summary>
public class Aduana
{
    public int Id { get; set; }
    public byte TipoAduanaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public short Orden { get; set; }
    public bool Activo { get; set; } = true;

    public TipoAduana TipoAduana { get; set; } = null!;

    /// <summary>"04 - TERRESTRE LAS CHINAMAS", tal como se muestra en los combos.</summary>
    public string NombreCompleto => $"{Codigo} - {Nombre}";
}

/// <summary>Cargo del responsable de la solicitud (Subdirector, Jefe de Departamento, etc.).</summary>
public class Cargo
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public short Orden { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>Nivel 1 del catálogo en cascada de ítems.</summary>
public class Componente
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public short Orden { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Subcomponente> Subcomponentes { get; set; } = new List<Subcomponente>();
}

/// <summary>Nivel 2 del catálogo en cascada.</summary>
public class Subcomponente
{
    public int Id { get; set; }
    public byte ComponenteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public short Orden { get; set; }
    public bool Activo { get; set; } = true;

    public Componente Componente { get; set; } = null!;
    public ICollection<Elemento> Elementos { get; set; } = new List<Elemento>();
}

/// <summary>Nivel 3 del catálogo en cascada. Si <see cref="TieneDetalle"/> es true, la UI habilita un 4º nivel (Detalle).</summary>
public class Elemento
{
    public int Id { get; set; }
    public int SubcomponenteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool TieneDetalle { get; set; }
    public short Orden { get; set; }
    public bool Activo { get; set; } = true;

    public Subcomponente Subcomponente { get; set; } = null!;
    public ICollection<Detalle> Detalles { get; set; } = new List<Detalle>();
}

/// <summary>Nivel 4 (más granular) del catálogo en cascada — solo existe para Elementos con TieneDetalle = true.</summary>
public class Detalle
{
    public int Id { get; set; }
    public int ElementoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public short Orden { get; set; }
    public bool Activo { get; set; } = true;

    public Elemento Elemento { get; set; } = null!;
}

/// <summary>Prioridad de un ítem de solicitud (Alta / Media / Baja).</summary>
public class Prioridad
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public short Orden { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>Estado del ciclo de vida de una solicitud. Catálogo cerrado (antes era texto libre en el original).</summary>
public class EstadoSolicitud
{
    public byte Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public short Orden { get; set; }
    public bool EsInicial { get; set; }
    public bool EsFinal { get; set; }
}
