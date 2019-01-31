using ClosedXML.Excel;
using SW.Salud.DataAccess;
using SW.Salud.DataAccess.SigmepTableAdapters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SW.Common;

namespace BatchNotificacionEnrolamiento
{
    class Program
    {
        static void Main(string[] args)
        {
            // Este proceso batch envía un mail a los titulares con la información del avance del proceso de enrolamiento de sus trabajadores.
            // corre cada 3 dias
            int PeriodicidadNotificacion = Convert.ToInt32(ConfigurationManager.AppSettings["PeriodicidadNotificacion"]);
            int LapsoHasta = Convert.ToInt32(ConfigurationManager.AppSettings["LapsoHasta"]);

            using (PortalContratante model = new PortalContratante())
            {
                DateTime FechaDesde = DateTime.Today.AddDays(-LapsoHasta);
                var lstRegistros = model.CORP_Registro.Where(x => x.Estado == (int)EnumEstadoRegistroTemporalMasivo.Incluido && x.FechaCreacion >= FechaDesde).ToList();

                foreach (var registro in lstRegistros)
                {
                    //if (registro.Email != "ejimenez@saludsa.com.ec") continue;

                    // Si ha transcurrido exactamente los x días enviar la notificación
                    int DiasTranscurridos = DateTime.Today.DayOfYear - registro.FechaCreacion.Value.DayOfYear;
                    if ((DiasTranscurridos % PeriodicidadNotificacion) == 0)
                    {
                        if (!registro.CompletadoEnrolamiento.HasValue || registro.CompletadoEnrolamiento == false)
                        {
                            #region Envio de mail de enrolamiento
                            Dictionary<string, byte[]> ContenidoAdjuntos = new Dictionary<string, byte[]>();
                            try
                            {
                                cl02_empresa_sucursalesTableAdapter sucursalta = new cl02_empresa_sucursalesTableAdapter();
                                var sucursales = sucursalta.GetDataByEmpresaSucursal(registro.IdEmpresa.Value, int.Parse(registro.IdProducto));
                                var sucursal = sucursales.FirstOrDefault();

                                if (sucursal != null)
                                {
                                    if (sucursal.Is_sucursal_configuracionNull() == false && sucursal._sucursal_configuracion != "")
                                    {
                                        List<SubSucursal> subsucursales = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SubSucursal>>(sucursal._sucursal_configuracion);

                                        foreach (SubSucursal s in subsucursales.Where(s => s.opcional == true))
                                        {
                                            var ss = sucursalta.GetDataByEmpresaSucursal(registro.IdEmpresa.Value, s.id).FirstOrDefault();
                                            if (ss != null)
                                                ContenidoAdjuntos.Add(ss._sucursal_alias.Trim().Replace(" ", "") + ".pdf", ArchivosHelper.DescargaPublicidadLista(registro.IdEmpresa.Value, s.id));
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // no hace nada, si no encuentra el archivo, no lo adjunta, la lista queda vacía nomas
                            }

                            Dictionary<string, string> ParamValues = new Dictionary<string, string>();
                            ParamValues.Add("NOMBRE", registro.Nombres + " " + (string.IsNullOrEmpty(registro.Apellidos) ? "" : registro.Apellidos));
                            ParamValues.Add("USUARIO", registro.NumeroDocumento);
                            ParamValues.Add("CLAVE", registro.RC_FechaNacimiento.HasValue ? registro.RC_FechaNacimiento.Value.ToString("ddMMyyyy") : registro.NumeroDocumento);
                            string usarq = System.Configuration.ConfigurationManager.AppSettings["UsarQueryString"];
                            string link = string.Empty;
                            link += System.Configuration.ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                            if (usarq.ToUpper().Equals("SI"))
                            {
                                link += "/Views/ActivacionUsuario.html?p=";
                                string data = registro.IdEmpresa.ToString() + "," + registro.IdUsuario + "," + registro.IdRegistro;
                                link += Base64Encode(data);
                            }
                            ParamValues.Add("LINK", link);

                            //string path = ConfigurationManager.AppSettings["PathTemplates"];
                            //string ContenidoMail = SW.Common.Utils.GenerarContenido(path + "T4_RecordatorioEnrolamiento.html", ParamValues);
                            //SW.Common.Utils.SendMail(registro.Email, "", ContenidoMail, "Recordatorio Carga Información de Enrolamiento",ContenidoAdjuntos);

                            SW.Common.Utils.SendMail(registro.Email, "", TipoNotificacionEnum.RecordatorioEnrolamiento, ParamValues, ContenidoAdjuntos);

                            #endregion

                            #region Envío de notificación a los titulares

                            //String MailTO = registro.Email;

                            //// Realizar el envío de mail al titular
                            //String LinkPortalUsuarios = ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                            //Dictionary<string, string> ParamValues = new Dictionary<string, string>();
                            ////ParamValues.Add("LinkPortalUsuarios", LinkPortalUsuarios);
                            ////ParamValues.Add("NombreTitular", registro.Apellidos + " " + registro.Nombres);

                            //// TODO: Obtener la información acerca de las credenciales del titular ya sea vía BD o invocando un servicio
                            ////ParamValues.Add("NombreUsuario", "");
                            ////ParamValues.Add("Clave", "");

                            //ParamValues.Add("NOMBRE", registro.Nombres + " " + registro.Apellidos);
                            //ParamValues.Add("USUARIO", registro.NumeroDocumento);
                            //ParamValues.Add("CLAVE", registro.RC_FechaNacimiento.HasValue ? registro.RC_FechaNacimiento.Value.ToString("ddMMyyyy") : registro.NumeroDocumento);

                            //string link = string.Empty;
                            //link += System.Configuration.ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                            //link += "/Views/ActivacionUsuario.html?p=";
                            //string data = registro.IdEmpresa.ToString() + "," + registro.IdUsuario + "," + registro.IdRegistro;
                            //link += Base64Encode(data);

                            //ParamValues.Add("LINK", link);
                            //ParamValues.Add("FECHAMAXIMA", registro.FechaCreacion.HasValue ? (registro.FechaCreacion.Value.AddDays(30).ToString("dd/MM/yyyy")) : " día 30 después de la activación de titulares realizada por parte de la empresa");

                            //string path = ConfigurationManager.AppSettings["PathTemplates"];
                            //string ContenidoMail = Utils.GenerarContenido(path + "T4_RecordatorioEnrolamiento.html", ParamValues);
                            //Utils.SendMail(MailTO, "", ContenidoMail, "Saludsa - Recordatorio Carga Información de Enrolamiento");

                        }
                        #endregion
                    }

                }

            }
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public enum EnumEstadoRegistroTemporalMasivo
        {
            LeidoCorrecto = 1,
            LeidoConError = 2,
            Corregido = 3,
            Descartado = 4,
            Aprobado = 5,
            Incluido = 6
        }
    }
}
