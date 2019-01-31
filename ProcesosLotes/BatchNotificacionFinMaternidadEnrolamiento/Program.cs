using SW.Salud.DataAccess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace BatchNotificacionFinMaternidadEnrolamiento
{
    class Program
    {
        // ESTE PROCESO TIENE EL OBJETIVO DE NOTIFICAR AL TITULAR DIRECTAMENTE
        // CONTANDO 40 SEMANAS A PARTIR DE LA FECHA FUM (FECHA DE ÚLTIMA MENSTRUACIÓN)
        // REPORTADA DURANTE LA NOTIFICACIÓN DE MATERNIDAD
        // EN ESTE MENSAJE SE LE DIRÁ AL USUARIO QUE TIENE QUE INGRESAR A LA VENTANA DE 
        // ENROLAMIENTO ESPECÍFICO DEL BEBÉ
        // INTERNAMENTE REEMPLAZA EL BENEFICIARIO "POR NACER" Y SE COLOCA YA LOS DATOS DEL BEBÉ NACIDO
        // ESTE PROCESO ESTÁ PENSADO PARA CORRER UNA VEZ AL DIA
        static void Main(string[] args)
        {
            using (PortalContratante model = new PortalContratante())
            {
                // calculo de las 38 semanas (CONFIGURACION)
                int DiasDesdeFechaFUM = int.Parse(ConfigurationManager.AppSettings["DiasDesdeFechaFUM"]);
                
                DateTime maxdt = DateTime.Today.AddDays(-DiasDesdeFechaFUM).Date;

                // búsqueda
                var nts = model.CORP_NotificacionMaternidad.Where(n =>
                       n.FechaFUM.Value == maxdt && // que la fecha fum sea igual a hoy hace x semanas
                       n.Estado == 1 && // estado de notificacion activo // TODO: cómo inactivar en exclusion?
                       n.EnrolamientoBebeCompleto == false).ToList(); // que todavía el bebé no se haya enrolado // nota, esta variable debería moverse en el enrolamiento

                SW.Salud.DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata = new SW.Salud.DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                Sigmep4._cl03_personasRow persona = null; 
                // Itero y notifico por cada una
                foreach  (var notificacion in nts)
                {
                    //obtener persona principal
                    persona = personata.GetDataByEmpresaPersonaActivaNumeroPersona(notificacion.IdEmpresa.Value, notificacion.IdTitular.Value).FirstOrDefault();
                    string usarq = System.Configuration.ConfigurationManager.AppSettings["UsarQueryString"];
                    string link = string.Empty;
                    link += System.Configuration.ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                    if (usarq.ToUpper().Equals("SI"))
                    {
                        link += "/Views/NotificacionNacimiento.html?p=";
                        string data = notificacion.IdEmpresa.ToString() + "," + notificacion.IdTitular + "," + notificacion.IdNotificacionMaternidad;
                        link += Base64Encode(data);
                    }


                    Dictionary<string, string> tokens = new Dictionary<string, string>();
                    tokens.Add("NOMBRE", notificacion.NombreTitular);
                    tokens.Add("LINK", link);

                    string email = persona.Is_domicilio_emailNull() || string.IsNullOrEmpty(persona._domicilio_email) ?
                            persona.Is_domicilio_email_corporativoNull() || string.IsNullOrEmpty(persona._domicilio_email_corporativo) ?
                            persona.Is_trabajo_emailNull() || string.IsNullOrEmpty(persona._trabajo_email) ?
                            string.Empty : persona._trabajo_email :
                            persona._domicilio_email_corporativo :
                            persona._domicilio_email;

                    // notificación de correo electrónico
                    SW.Common.Utils.SendMail(email, "", SW.Common.TipoNotificacionEnum.NotificacionFinMaternidadEnrolamiento, tokens, new Dictionary<string, byte[]>());
                }
            }
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
