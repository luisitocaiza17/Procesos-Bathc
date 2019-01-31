using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SW.Salud.DataAccess;
using SW.Salud.DataAccess.SigmepTableAdapters;

namespace BatchNotificacionPrefactura
{
    class Program
    {
        // PROCEDIMIENTO INHABILITADO
        // DEBE RETOMARSE EN FASE 2, ATÁNDOLO A CONFIGURACION DE FECHA DE FACTURACIÓN INDIVIDUAL DE EMPRESA (CONFIGURACIÓN PORTAL)
        static void Main(string[] args)
        {
            // este proceso batch notifica a todas las empresas, en una fecha específica
            // avisa que la pre-factura está lista para ser visualizada en el portal contratante
            // Este proceso debe configurarse para correr solamente una vez al día

            // Este proceso batch envía un mail a los administradores con la información del avance del proceso de enrolamiento de sus trabajadores.
            // Debería correr el 30 de cada mes, pero mejor sería hacerlo configurable
            int FrecuenciaEjecucion = Convert.ToInt32(ConfigurationManager.AppSettings["FrecuenciaEjecucionXDefecto"]);

            // Definición de adaptadores
            cl01_empresasTableAdapter cl01ta = new cl01_empresasTableAdapter();

            DateTime firstOfNextMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
            DateTime lastOfThisMonth = firstOfNextMonth.AddDays(-1);
            if (FrecuenciaEjecucion > lastOfThisMonth.Day)
                FrecuenciaEjecucion = lastOfThisMonth.Day;

            using (PortalContratante context = new PortalContratante())
            {
                var UsuariosNotificacion = context.SEG_Usuario.Where(u => u.UsuarioRol.Count(r => r.IdRol == 1) > 0 && u.Estado == 0).ToList();
                string link = System.Configuration.ConfigurationManager.AppSettings["SiteURL"];

                foreach (var Usuario in UsuariosNotificacion)
                {
                    // Obtener el día de ejecución de acuerdo a lo que este almacenado en la configuración del portal de la empresa
                    var data_empresa = cl01ta.GetDataByEmpresaNumero(Usuario.IdEmpresa);
                    if (data_empresa != null)
                    {
                        if (data_empresa.Count() > 0 && data_empresa.FirstOrDefault()._notificacion_prefactura > 0)
                        {
                            FrecuenciaEjecucion = data_empresa.FirstOrDefault()._notificacion_prefactura;
                            if (FrecuenciaEjecucion > lastOfThisMonth.Day)
                                FrecuenciaEjecucion = lastOfThisMonth.Day;
                        }
                    }

                    if (DateTime.Today.Day != FrecuenciaEjecucion)
                        continue;

                    if (Usuario.Email == null || Usuario.Email == "") continue;
                    // Envío de correo electrónico de notificación
                    Dictionary<string, string> tokens = new Dictionary<string, string>();
                    tokens.Add("DirigidoA", Usuario.NombreUsuario);
                    tokens.Add("LINK", link);


                    // Plantilla no existe
                    //string PathTemplates = System.Configuration.ConfigurationManager.AppSettings["PathTemplates"];
                    //string ContenidoMail = SW.Common.Utils.GenerarContenido(AppDomain.CurrentDomain.BaseDirectory + @"Templates\RevisionPrefactura.html", tokens);
                    //SW.Common.Utils.SendMail(Usuario.Email, "", ContenidoMail, "Notificación de Pre-factura");

                    // Enviar la notificación de correo electrónico
                    SW.Common.Utils.SendMail(Usuario.Email, "", SW.Common.TipoNotificacionEnum.NotificacionRevisionPrefactura, tokens, new Dictionary<string, byte[]>());
                }
            }
        }
    }
}
