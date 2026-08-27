namespace DGA.Web.Services;

public static class CredencialesEmailTemplate
{
    public const string Asunto = "Credenciales de acceso — Levantamiento de Necesidades";

    private const string EnlaceSistema = "https://centrodesolicitudes.gcslatam.com/";

    // Se referencia el logo ya publicado en el propio sitio en vez de embeberlo (data URI o
    // Content-Id): varios filtros de seguridad corporativos (Defender/Safe Attachments,
    // Mimecast, etc.) reescriben o rompen las imágenes incrustadas en el correo, mientras que
    // una imagen alojada normalmente por HTTPS se muestra igual que en cualquier sitio web.
    private const string LogoUrl = "https://centrodesolicitudes.gcslatam.com/images/logo-gcs-blanco.png";

    public static string ConstruirCuerpo(string email, string contrasena, bool esNuevaCuenta)
    {
        var intro = esNuevaCuenta
            ? "Se ha creado una cuenta a su nombre en el Sistema de Levantamiento de Necesidades."
            : "Se ha actualizado la contraseña de su cuenta en el Sistema de Levantamiento de Necesidades.";
        var etiquetaContrasena = esNuevaCuenta ? "Contraseña temporal" : "Contraseña nueva";

        return $"""
            <div style="background:#f4f5f7;padding:32px 16px;font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;margin:0 auto;background:#ffffff;border-radius:8px;overflow:hidden;border:1px solid #e5e7eb;">
                <tr>
                  <td style="background:#2563EB;padding:20px 32px;">
                    <img src="{LogoUrl}" alt="Global Customs Solutions" width="150" height="49" style="display:block;border:0;width:150px;height:49px;max-width:150px;" />
                  </td>
                </tr>
                <tr>
                  <td style="padding:32px;color:#1f2937;font-size:14px;line-height:1.6;">
                    <p style="margin:0 0 16px;">{intro}</p>
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#DBEAFE;border-radius:6px;margin:0 0 20px;">
                      <tr>
                        <td style="padding:16px 20px;">
                          <p style="margin:0 0 6px;color:#1D4ED8;font-size:12px;text-transform:uppercase;letter-spacing:.04em;">Correo</p>
                          <p style="margin:0 0 14px;font-size:15px;font-weight:600;">{email}</p>
                          <p style="margin:0 0 6px;color:#1D4ED8;font-size:12px;text-transform:uppercase;letter-spacing:.04em;">{etiquetaContrasena}</p>
                          <p style="margin:0;font-size:15px;font-weight:600;font-family:Consolas,Menlo,monospace;">{contrasena}</p>
                        </td>
                      </tr>
                    </table>
                    <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 0 24px;">
                      <tr>
                        <td style="border-radius:6px;background:#2563EB;">
                          <a href="{EnlaceSistema}" style="display:inline-block;padding:12px 24px;color:#ffffff;font-size:14px;font-weight:600;text-decoration:none;">Ingresar al sistema</a>
                        </td>
                      </tr>
                    </table>
                    <p style="margin:0 0 8px;color:#4b5563;">Por seguridad, le recomendamos iniciar sesión y cambiar esta contraseña lo antes posible.</p>
                    <p style="margin:0 0 8px;color:#4b5563;">Próximamente recibirá una capacitación sobre el uso del Centro de Solicitudes.</p>
                    <p style="margin:0;color:#6b7280;font-size:12.5px;">Si no esperaba este correo, comuníquese con un administrador del sistema.</p>
                  </td>
                </tr>
              </table>
            </div>
            """;
    }
}
