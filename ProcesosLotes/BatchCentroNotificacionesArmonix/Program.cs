using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SW.Common;
using SW.Salud.DataAccess;

namespace BatchNotificacionesCorredoresArmx
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // ESTE BATCH SIRVE PARA ENVIAR DE FORMA ASINCRÓNICA LOS GRUPOS DE NOTIFICACIÓN
                // ESTE PROCESO DEBE CORRER CADA 5 MINUTOS

                using (PortalCorredores model = new PortalCorredores())
                {
                    // Itero por todos los pedidos de notificación
                    foreach (var notif in model.NOT_Envio.Where(x => x.Estado == 1).ToList())
                    {
                        // Obtengo la lista de archivos adjuntos
                        Dictionary<string, byte[]> ContenidoAdjuntos = new Dictionary<string, byte[]>();
                        foreach (var adj in model.NOT_EnvioAdjuntos.Where(x => x.IDEnvio == notif.IDEnvio))
                        {
                            if (!ContenidoAdjuntos.ContainsKey(adj.Nombre))
                                ContenidoAdjuntos.Add(adj.Nombre, adj.Contenido);
                        }

                        // Obtengo la lista de destinatarios
                        var lstDestinatarios = model.NOT_Destinatario.Where(x => x.IDEnvio == notif.IDEnvio);
                        foreach (var destino in lstDestinatarios)
                        {
                            if (destino != null)
                            {
                                String MailTO = destino.Mail;

                                // Realizar el envío de la notificación al destinatario
                                //string link = string.Empty;
                                //link += System.Configuration.ConfigurationManager.AppSettings["LinkPortalUsuarios"];

                                bool enviado = SW.Common.Utils.SendMail(MailTO, "", notif.Titulo, notif.Mensaje, 
                                                            SW.Common.TipoNotificacionEnum.NotificacionGeneralArmonix, ContenidoAdjuntos);
                                if (enviado)
                                    destino.Estado = 2;
                                else destino.Estado = 3;
                                destino.NumIntentos = 1;
                            }
                        }

                        notif.Estado = 2; // Envío de la notificación completado
                        notif.FechaEnvio = DateTime.Now;
                        model.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
            }
        }
    }
}
