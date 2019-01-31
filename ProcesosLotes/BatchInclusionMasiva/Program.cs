using Newtonsoft.Json;
using SW.Common;
using SW.Salud.DataAccess;
using SW.Salud.DataAccess.SigmepTableAdapters;
using SW.Salud.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace BatchInclusionMasiva
{
    class Program
    {
        static void Main(string[] args)
        {
            // este proceso batch lee los registros que han sido confirmados, y pasan a inclusión masiva
            try
            {

                using (PortalContratante context = new PortalContratante())
                {
                    //ESTADOS ARCHIVO
                    //1 - PROCESANDO
                    //2 - EN REVISIÓN
                    //3 - GENERANDO INCLUSIONES
                    //4 - COMPLETADO
                    //5 - ERROR

                    var archivos = context.CORP_FileMasivos.Where(f => f.EstadoProceso == 3).OrderBy(f => f.FechaCreacion).ToList();



                    foreach (var archivo in archivos)
                    {
                        // tomo solamente los registros que han sido aprobados
                        var registros = context.CORP_Registro.Where(r => r.IdArchivo == archivo.FileMasivosID && r.Estado == 5).ToList();
                        List<Inclusion> inclusiones = null;
                        Persona persona = null;
                        int Avance = 0;
                        int Total = archivos.Count;
                        int errorpersona = 0;
                        foreach (var registro in registros)
                        {
                            bool esnuevo = true;
                            if (!string.IsNullOrEmpty(registro.Datos))
                            {
                                //deserealizar los datos para el envio a ingreso en sigmep
                                List<object> Datos = JsonConvert.DeserializeObject<List<object>>(registro.Datos);
                                if (Datos.Count > 0)
                                {
                                    if (Datos[0] != null && Datos[1] != null)
                                    {
                                        try
                                        {
                                            inclusiones = JsonConvert.DeserializeObject<List<Inclusion>>(Datos[0].ToString());
                                            foreach (var item in inclusiones)
                                                item.IDRegistro = registro.IdRegistro;

                                            persona = JsonConvert.DeserializeObject<Persona>(Datos[1].ToString());
                                            //Procedimiento de Inclusión
                                            SW.Salud.Services.Sigmep.Logic sigmep = new SW.Salud.Services.Sigmep.Logic();
                                            inclusiones = sigmep.GuardarInclusion(inclusiones, persona, false, false, true).ToList();




                                            //verificar si hubo algun error en las inclusiones
                                            //Mensajes de proceso
                                            StringBuilder Message = new StringBuilder();
                                            int error = 0;

                                            if (inclusiones[0].Usuario == "existe")
                                                esnuevo = false;
                                            //devolucion de resultados correctos de la inclusion
                                            foreach (Inclusion i in inclusiones)
                                            {
                                                if (i.Observacion == "OK")
                                                {
                                                    Message.Append("Contrato " + i.ContratoNumero.ToString() + " en la lista " + i.SucursalID.ToString() + " creado correctamente.\\n");
                                                    //registro.Estado = 6; //incluido
                                                }
                                                else if (i.Observacion == "EXISTE")
                                                {
                                                    Message.Append("El beneficiario ya existe en el Contrato" + i.ContratoNumero.ToString() + "\\n");
                                                    //registro.Estado = 4; // descartado, ya existe ese contrato
                                                }
                                                else if (i.Observacion == "NO")
                                                {
                                                    Message.Append("No se pudo incluir al cliente en la lista " + i.SucursalID.ToString() + ", intentelo nuevamente.\\n");
                                                    //registro.Estado = 7; //pendiente no se puede procesar
                                                }
                                                else if (i.Observacion == "INCLUIDO")
                                                {
                                                    Message.Append("El cliente ya se encuentra incluido en la lista" + i.SucursalID.ToString() + "\\n");
                                                    //registro.Estado = 4; //descartado ya existe el contrato
                                                }
                                                else
                                                {
                                                    Message.Append(i.Observacion + "\\n");
                                                    //registro.Estado = 7; //error enla inclusión
                                                    error++;
                                                }
                                            }

                                            if (error == 0)
                                            {
                                                registro.Estado = 6;
                                                registro.Observaciones = Message.ToString();
                                                persona.PersonaNumero = inclusiones.FirstOrDefault().PersonaNumero;

                                                //guardar los datos
                                                if (registro.Estado != 5)
                                                    registro.Datos = JsonConvert.SerializeObject(new List<object>() { inclusiones, persona });

                                                context.SaveChanges();
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            errorpersona++;
                                            SW.Common.ExceptionManager.ReportException(ex, SW.Common.ExceptionManager.ExceptionSources.Server);
                                        }
                                    }
                                }

                            }
                            //Solo enviar notificacion a los ingresados correctamente
                            if (registro.Estado == 6)
                            {
                                string opcionales = string.Empty;
                                int nopcionales = 0;
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
                                                {
                                                    ContenidoAdjuntos.Add(ss._sucursal_alias.Trim().Replace(" ", "") + ".pdf", ArchivosHelper.DescargaPublicidadLista(registro.IdEmpresa.Value, s.id));
                                                    nopcionales++;
                                                    opcionales += (ss._sucursal_alias + ",");
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // no hace nada, si no encuentra el archivo, no lo adjunta, la lista queda vacía nomas
                                }

                                Dictionary<string, string> ParamValues = new Dictionary<string, string>();
                                ParamValues.Add("NOMBRE", registro.Nombres + (string.IsNullOrEmpty(registro.Apellidos) ? "" : " " + registro.Apellidos));
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
                                //servicios opcionales
                                if (nopcionales == 1)
                                    ParamValues.Add("BENEFICIOS", "el beneficio " + opcionales);
                                else
                                    ParamValues.Add("BENEFICIOS", "los beneficios " + opcionales);


                                string path = ConfigurationManager.AppSettings["PathTemplates"];
                                string ContenidoMail = string.Empty;
                                try
                                {
                                    if (esnuevo)
                                    {
                                        if (nopcionales > 0)
                                            SW.Common.Utils.SendMail(registro.Email, "", TipoNotificacionEnum.BienvenidaTitularOpcional, ParamValues, ContenidoAdjuntos);
                                        else
                                            SW.Common.Utils.SendMail(registro.Email, "", TipoNotificacionEnum.BienvenidaTitular, ParamValues, ContenidoAdjuntos);
                                    }
                                    else
                                    {
                                        if (nopcionales > 0)
                                            SW.Common.Utils.SendMail(registro.Email, "", TipoNotificacionEnum.BienvenidaTitularExisteOpcional, ParamValues, ContenidoAdjuntos);
                                        else
                                            SW.Common.Utils.SendMail(registro.Email, "", TipoNotificacionEnum.BienvenidaTitularExiste, ParamValues, ContenidoAdjuntos);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    SW.Common.ExceptionManager.ReportException(ex, SW.Common.ExceptionManager.ExceptionSources.Server);
                                }
                            }

                            Avance++;
                            archivo.PorcentajeAvance = (int)((double)Avance / (double)Total * 100);
                            context.SaveChanges();
                        }

                        if (errorpersona == 0)
                        {
                            archivo.PorcentajeAvance = 100;
                            archivo.EstadoProceso = 4; // COMPLETADO
                            context.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SW.Common.ExceptionManager.ReportException(ex, SW.Common.ExceptionManager.ExceptionSources.Server);
            }
        }
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }




    }
}
