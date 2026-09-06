namespace DGA.Web.Services;

public static class ResetPasswordEmailTemplate
{
    public const string Asunto = "Recuperación de contraseña — Levantamiento de Necesidades";

    // Se referencia el logo ya publicado en el propio sitio en vez de embeberlo (data URI o
    // Content-Id): varios filtros de seguridad corporativos (Defender/Safe Attachments,
    // Mimecast, etc.) reescriben o rompen las imágenes incrustadas en el correo, mientras que
    // una imagen alojada normalmente por HTTPS se muestra igual que en cualquier sitio web.
    private const string LogoUrl = "https://centrodesolicitudes.gcslatam.com/images/logo-gcs-blanco.png";

    // Azul navy a pedido explícito del usuario — distinto del #2563EB único del resto del sitio,
    // solo para este correo.
    private const string NavyColor = "#0F172A";
    private const string NavySoft = "#E2E8F0";
    private const string NavyText = "#334155";

    public static string ConstruirCuerpo(string enlace)
    {
        return $"""
            <div style="background:#f4f5f7;padding:32px 16px;font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;margin:0 auto;background:#ffffff;border-radius:8px;overflow:hidden;border:1px solid #e5e7eb;">
                <tr>
                  <td style="background:{NavyColor};padding:20px 32px;">
                    <img src="{LogoUrl}" alt="Global Customs Solutions" width="150" height="49" style="display:block;border:0;width:150px;height:49px;max-width:150px;" />
                  </td>
                </tr>
                <tr>
                  <td style="padding:32px;color:#1f2937;font-size:14px;line-height:1.6;">
                    <p style="margin:0 0 16px;">Recibimos una solicitud para restablecer la contraseña de su cuenta en el Sistema de Levantamiento de Necesidades.</p>
                    <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 0 24px;">
                      <tr>
                        <td style="border-radius:6px;background:{NavyColor};">
                          <a href="{enlace}" style="display:inline-block;padding:12px 24px;color:#ffffff;font-size:14px;font-weight:600;text-decoration:none;">Restablecer contraseña</a>
                        </td>
                      </tr>
                    </table>
                    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{NavySoft};border-radius:6px;margin:0 0 20px;">
                      <tr>
                        <td style="padding:14px 20px;">
                          <p style="margin:0 0 6px;color:{NavyText};font-size:12px;text-transform:uppercase;letter-spacing:.04em;">Si el botón no funciona, copie y pegue este enlace</p>
                          <p style="margin:0;font-size:13px;word-break:break-all;color:{NavyText};">{enlace}</p>
                        </td>
                      </tr>
                    </table>
                    <p style="margin:0;color:#6b7280;font-size:12.5px;">Si no solicitó este cambio, puede ignorar este correo — su contraseña actual seguirá funcionando.</p>
                  </td>
                </tr>
              </table>
            </div>
            """;
    }
}
