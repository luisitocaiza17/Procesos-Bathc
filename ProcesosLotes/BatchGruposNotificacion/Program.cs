using SW.Common;
using SW.Salud.DataAccess;
using SW.Salud.DataAccess.SigmepTableAdapters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchGruposNotificacion
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // ESTE BATCH SIRVE PARA ENVIAR DE FORMA ASINCRÓNICA LOS GRUPOS DE NOTIFICACIÓN
                // ESTE PROCESO DEBE CORRER CADA 5 MINUTOS

                using (PortalContratante context = new PortalContratante())
                {
                    // Itero por todos los pedidos de notificación
                    foreach (var grupo in context.CORP_GrupoNotificacion.Where(g => g.Estado == 0).ToList())
                    {
                        // Obtengo los ids de registro
                        string[] parts = grupo.Listado.Split(',');
                        int[] IdRegistros = parts.Select(p => int.Parse(p)).ToArray();

                        // obtengo la lista de registros
                        var lstregs = context.CORP_Registro.Where(r => IdRegistros.Contains(r.IdRegistro));

                        foreach (var registro in lstregs)
                        {
                            if (registro != null)
                            {
                                String MailTO = registro.Email;

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
                                                    ContenidoAdjuntos.Add(ss._sucursal_alias.Trim().Replace(" ", "") + ".pdf", DescargaPublicidadLista(registro.IdEmpresa.Value, s.id));
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // no hace nada, si no encuentra el archivo, no lo adjunta, la lista queda vacía nomas
                                }

                                // Realizar el envío de mail al titular
                                String LinkPortalUsuarios = ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                                Dictionary<string, string> ParamValues = new Dictionary<string, string>();
                                //ParamValues.Add("LinkPortalUsuarios", LinkPortalUsuarios);
                                //ParamValues.Add("NombreTitular", registro.Apellidos + " " + registro.Nombres);

                                // TODO: Obtener la información acerca de las credenciales del titular ya sea vía BD o invocando un servicio
                                //ParamValues.Add("NombreUsuario", "");
                                //ParamValues.Add("Clave", "");

                                ParamValues.Add("NOMBRE", registro.Nombres + " " + (string.IsNullOrEmpty(registro.Apellidos) ? "" : registro.Apellidos));
                                ParamValues.Add("USUARIO", registro.NumeroDocumento);
                                ParamValues.Add("CLAVE", registro.RC_FechaNacimiento.HasValue ? registro.RC_FechaNacimiento.Value.ToString("ddMMyyyy") : registro.NumeroDocumento);

                            string link = string.Empty;
                            link += System.Configuration.ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                            //link += "/Views/ActivacionUsuario.html?p=";
                            //string data = registro.IdEmpresa.ToString() + "," + registro.IdUsuario + "," + registro.IdRegistro;
                            //link += Base64Encode(data);

                                ParamValues.Add("LINK", link);
                                ParamValues.Add("FECHAMAXIMA", registro.FechaCreacion.HasValue ? (registro.FechaCreacion.Value.AddDays(30).ToString("dd/MM/yyyy")) : " día 30 después de la activación de titulares realizada por parte de la empresa");

                                //string path = ConfigurationManager.AppSettings["PathTemplates"];
                                //string ContenidoMail = SW.Common.Utils.GenerarContenido(path + "T4_RecordatorioEnrolamiento.html", ParamValues);
                                //SW.Common.Utils.SendMail(MailTO, "", ContenidoMail, "Saludsa - Recordatorio Carga Información de Enrolamiento");

                            SW.Common.Utils.SendMail(MailTO, "", SW.Common.TipoNotificacionEnum.RecordatorioEnrolamiento, ParamValues, new Dictionary<string, byte[]>());
                            
                        }
                    }

                        grupo.Estado = 1; // completo envío
                        grupo.FechaEnvio = DateTime.Now;
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
            }
        }

        public static byte[] DescargaPublicidadLista(int IDEmpresa, int IDSucursal)
        {
            string Dominio = ConfigurationManager.AppSettings["DescargaArchivosPortal_Dominio"];
            string Usuario = ConfigurationManager.AppSettings["DescargaArchivosPortal_Usuario"];
            string Password = ConfigurationManager.AppSettings["DescargaArchivosPortal_Password"];
            string Path = ConfigurationManager.AppSettings["DescargaArchivosPortal_Path"];

            cl02_empresa_sucursalesTableAdapter sucursalta = new cl02_empresa_sucursalesTableAdapter();
            var sucursales = sucursalta.GetDataByEmpresaSucursal(IDEmpresa, IDSucursal);
            var sucursal = sucursales.FirstOrDefault();

            byte[] fileContent = null;

            // ImpersonationHelper.Impersonate(Dominio, Usuario, Password, delegate
            using (new NetworkConnection(Path, new System.Net.NetworkCredential(Usuario, Password, Dominio)))
            {
                // Si no existe la carpeta de la empresa
                if (!System.IO.Directory.Exists(Path + @"\" + IDEmpresa.ToString()))
                {
                    fileContent = null;
                }

                //si existe el archivo por alias
                if (sucursal != null && System.IO.File.Exists(Path + @"\" + IDEmpresa.ToString() + @"\PUBLICIDAD_" + sucursal._sucursal_alias.Trim() + ".pdf"))
                {
                    fileContent = System.IO.File.ReadAllBytes(Path + @"\" + IDEmpresa.ToString() + @"\PUBLICIDAD_" + sucursal._sucursal_alias.Trim() + ".pdf");
                }

                // si existe el archivo por numero
                if (System.IO.File.Exists(Path + @"\" + IDEmpresa.ToString() + @"\PUBLICIDAD_" + IDSucursal.ToString() + ".pdf"))
                {
                    fileContent = System.IO.File.ReadAllBytes(Path + @"\" + IDEmpresa.ToString() + @"\PUBLICIDAD_" + IDSucursal.ToString() + ".pdf");
                }

                return fileContent;
            }
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
