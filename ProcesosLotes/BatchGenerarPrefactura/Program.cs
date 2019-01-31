using RestSharp;
using SW.Common;
using SW.Salud.DataAccess;
using SW.Salud.DataAccess.SigmepPortalCorpTableAdapters;
using SW.Salud.DataAccess.SigmepTableAdapters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace BatchGenerarPrefactura
{
    class Program
    {
        static void Main(string[] args)
        {
            // PROCESO BATCH QUE PROCESA LAS SOLICITUDES DE PREFACTURACIÓN, ENVÍA MAIL CUANDO ESTÁN TERMINADAS
            // DEBE CORRER CADA 5 MINUTOS
            try
            {

                using (PortalContratante context = new PortalContratante())
                {

                    string AddressServicioPrefacturacion = ConfigurationManager.AppSettings["AddressServicioPrefacturacion"];
                    string AddressServicioBorrarPrefactura = ConfigurationManager.AppSettings["AddressServicioBorrarPrefactura"];

                    string usuarioSIGMEP = ConfigurationManager.AppSettings["usuarioSIGMEP"];

                    // trae las solicituds que primero deben ser borradas las prefacturas, para luego regenerarse
                    foreach (var Solicitud in context.CORP_SolicitudPrefactura.Where(s => s.Estado == 2).ToList())
                    {
                        try
                        {
                            string[] parts = null;

                            if (!string.IsNullOrEmpty(Solicitud.Listas))
                            {
                                parts = Solicitud.Listas.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            }

                            // Obtengo la lista de sucursales
                            cl02_empresa_sucursalesTableAdapter cl02ta = new cl02_empresa_sucursalesTableAdapter();
                            foreach (var sucursal in cl02ta.GetDataByEmpresa(Solicitud.IdEmpresa.Value))
                            {
                                if (!string.IsNullOrEmpty(Solicitud.Listas))
                                {
                                    // si no contiene en la lista del pedido, no la procesa
                                    if (!parts.Contains(sucursal._sucursal_empresa.ToString()))
                                    {
                                        continue;
                                    }
                                }

                                sucursal_cuotaTableAdapter cuotata = new sucursal_cuotaTableAdapter();
                                var cuotas = cuotata.GetData(Solicitud.IdEmpresa, sucursal._sucursal_empresa);

                                if (cuotas.Rows.Count == 0)
                                {
                                    break;
                                }

                                string oficinaCodigo = "";
                                if (sucursal.IsregionNull())
                                    oficinaCodigo = ConfigurationManager.AppSettings["OFICINA_CORPORATIVO_UIO"];
                                else if (sucursal.region.ToLower().Trim() == "sierra")
                                    oficinaCodigo = ConfigurationManager.AppSettings["OFICINA_CORPORATIVO_UIO"];
                                else if (sucursal.region.ToLower().Trim() == "costa")
                                    oficinaCodigo = ConfigurationManager.AppSettings["OFICINA_CORPORATIVO_GYE"];
                                else if (sucursal.region.ToLower().Trim() == "austro")
                                    oficinaCodigo = ConfigurationManager.AppSettings["OFICINA_CORPORATIVO_CUE"];
                                else
                                {
                                    if (sucursal.Is_sucursal_regionNull())
                                        oficinaCodigo = ConfigurationManager.AppSettings["OFICINA_CORPORATIVO_UIO"];
                                    else
                                        oficinaCodigo = sucursal._sucursal_region.ToString();
                                }

                                List<AnualCuota> lstcuotas = new List<AnualCuota>();
                                foreach (var cuota in cuotas)
                                {
                                    AnualCuota nueva = new AnualCuota();
                                    nueva.CodigoMotivoAnulacion = 0; // debe ir un còdigo fijo
                                    nueva.Cuota = cuota.cuota;
                                    nueva.EmpresaNumero = Solicitud.IdEmpresa.Value;
                                    nueva.OficinaCodigo = int.Parse(oficinaCodigo);
                                    nueva.SucursalNumero = cuota._sucursal_empresa;
                                    nueva.Usuario = usuarioSIGMEP;
                                    lstcuotas.Add(nueva);
                                }
                                //Ejemplo invocación
                                var respToLog = "";
                                string address = AddressServicioBorrarPrefactura;
                                var client = new RestClient(address);
                                var request = new RestRequest(Method.DELETE);
                                request.AddHeader("Content-Type", "application/json");
                                request.AddParameter("application/json", Newtonsoft.Json.JsonConvert.SerializeObject(lstcuotas.ToArray()), ParameterType.RequestBody);

                                // Este servicio no solicita cabeceras
                                //request.AddHeader("Content-Type", "application/json");
                                //request.AddHeader("CodigoAplicacion", "1");
                                //request.AddHeader("DispositivoNavegador", "Chrome");
                                //request.AddHeader("DireccionIP", "1.1.1.1");
                                //request.AddHeader("SistemaOperativo", "Windows");
                                //request.AddHeader("CodigoPlataforma", "1");


                                request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
                                IRestResponse response = client.Execute(request);
                                respToLog = response.Content;
                                var respuesta = new JavaScriptSerializer().Deserialize<object>(response.Content);

                            }

                            Solicitud.Estado = 0; // se pone en 0 para que pueda procesarse la prefacutra
                            context.SaveChanges();


                        }
                        catch (Exception ex)
                        {
                            Solicitud.Estado = 3; // ERROR
                            Solicitud.FechaProcesamiento = DateTime.Now;
                            Solicitud.MensajeError = "Proceso Borrado: IDEmpresa: " + Solicitud.IdEmpresa + " : " + ex.Message + " - " + ex.StackTrace;
                            context.SaveChanges();
                        }
                    }

                    // trae las solicituds pendientes
                    foreach (var Solicitud in context.CORP_SolicitudPrefactura.Where(s => s.Estado == 0).ToList())
                    //foreach (var Solicitud in context.CORP_SolicitudPrefactura.Where(s => s.IDSolicitudPrefactura == 71).ToList())
                    {
                        StringBuilder mensaje = new StringBuilder();
                        try
                        {
                            string[] parts = null;

                            if (!string.IsNullOrEmpty(Solicitud.Listas))
                            {
                                parts = Solicitud.Listas.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            }

                            // Obtengo la lista de sucursales
                            cl02_empresa_sucursalesTableAdapter cl02ta = new cl02_empresa_sucursalesTableAdapter();
                            foreach (var sucursal in cl02ta.GetDataByEmpresa(Solicitud.IdEmpresa.Value))
                            {

                                if (!string.IsNullOrEmpty(Solicitud.Listas))
                                {
                                    // si no contiene en la lista del pedido, no la procesa
                                    if (!parts.Contains(sucursal._sucursal_empresa.ToString()))
                                    {
                                        continue;
                                    }
                                }

                                // el proceso de prefacturación se hace uno por cada sucursal
                                // Llamada al servicio de prefacturacion
                                var respToLog = "";

                                int oficinaCodigo = 1;
                                if (sucursal.IsregionNull())
                                    oficinaCodigo = int.Parse(ConfigurationManager.AppSettings["OFICINA_CORPORATIVO_UIO"]);
                                else if (sucursal.region.ToLower().Trim() == "sierra")
                                    oficinaCodigo = int.Parse(ConfigurationManager.AppSettings["OFICINA_CORPORATIVO_UIO"]);
                                else if (sucursal.region.ToLower().Trim() == "costa")
                                    oficinaCodigo = int.Parse(ConfigurationManager.AppSettings["OFICINA_CORPORATIVO_GYE"]);
                                else if (sucursal.region.ToLower().Trim() == "austro")
                                    oficinaCodigo = int.Parse(ConfigurationManager.AppSettings["OFICINA_CORPORATIVO_CUE"]);
                                else
                                {
                                    if (sucursal.Is_sucursal_regionNull())
                                        oficinaCodigo = int.Parse(ConfigurationManager.AppSettings["OFICINA_CORPORATIVO_UIO"]);
                                    else
                                        oficinaCodigo = sucursal._sucursal_region;
                                }

                                ParametrosTableAdapter parametrosta = new ParametrosTableAdapter();
                                var segcamp = parametrosta.GetData("SEGURO CAMPESINO");
                                var segcampret = parametrosta.GetData("SEGURO CAMPESINO RET");
                                #region Calculo numero de ajustes y fecha
                                int nroajuste;
                                DateTime fechacorte = DateTime.Today;
                                //consideracion cuando es la primera vez se verifica contra cl02
                                if (sucursal.Is_hasta_ultimo_periodoNull())
                                {
                                    nroajuste = 1;
                                    //fecha de corte
                                    if (!sucursal.Is_meses_forma_pagoNull())
                                    {
                                        fechacorte = SumaMeses(sucursal._fecha_inicio_sucursal, sucursal._meses_forma_pago);
                                    }
                                    else if (!sucursal.Is_forma_pagoNull())
                                    {
                                        fechacorte = SumaMeses(sucursal._fecha_inicio_sucursal, sucursal._forma_pago);
                                    }
                                }
                                else // nuevas cuotas
                                {
                                    fc01_facturas_corporativoTableAdapter ajusteta = new fc01_facturas_corporativoTableAdapter();
                                    var ajuste = ajusteta.ObtenerNroAjuste(Solicitud.IdEmpresa.Value, sucursal._sucursal_empresa);

                                    if (Convert.IsDBNull(ajuste))
                                    {
                                        nroajuste = 1;
                                    }
                                    else if (ajuste == null)
                                    {
                                        nroajuste = 1;
                                    }
                                    else if (!IsNumber(ajuste))
                                    {
                                        nroajuste = 1;
                                    }
                                    else
                                    {
                                        nroajuste = ((int)ajuste) + 1;
                                    }
                                    //fecha de corte
                                    if (!sucursal.Is_meses_forma_pagoNull())
                                    {
                                        fechacorte = SumaMeses(sucursal._hasta_ultimo_periodo, sucursal._meses_forma_pago);
                                    }
                                    else if (!sucursal.Is_forma_pagoNull())
                                    {
                                        fechacorte = SumaMeses(sucursal._hasta_ultimo_periodo, sucursal._forma_pago);
                                    }
                                }

                                #endregion

                                //Ejemplo invocación
                                //?DatoCotizacion.empresaNumero = 43739 & DatoCotizacion.sucursalNumero = 187000 & DatoCotizacion.fechaCorte = 2018 % 2F05 % 2F14 & DatoCotizacion.usuario = jualmeida & DatoCotizacion.oficinaCodigo = 1 & DatoCotizacion.porcentajeSeguroCampesino = 0.005 & DatoCotizacion.porcentajeSeguroXCampesinoRet = 0 & DatoCotizacion.numeroAjuste = 1
                                string address = AddressServicioPrefacturacion;
                                var client = new RestClient(address);
                                var request = new RestRequest(Method.POST);
                                request.AddHeader("Content-Type", "application/json");
                                //+ "?" + "DatoCotizacion.empresaNumero=" + Solicitud.IdEmpresa.ToString() +
                                //"&DatoCotizacion.sucursalNumero=" + sucursal._sucursal_empresa.ToString() + 
                                //"&DatoCotizacion.fechaCorte=" + DateTime.Today.ToString("yyy/MM/dd") + 
                                //"&DatoCotizacion.usuario=" + usuarioSIGMEP + 
                                //"&DatoCotizacion.oficinaCodigo=" + oficinaCodigo + 
                                //"&DatoCotizacion.porcentajeSeguroCampesino=" + (segcamp.FirstOrDefault() != null ? segcamp.FirstOrDefault().valor : "0.5") + 
                                //"&DatoCotizacion.porcentajeSeguroXCampesinoRet=" + (segcampret.FirstOrDefault() != null ? segcampret.FirstOrDefault().valor : "0") + 
                                //"&DatoCotizacion.numeroAjuste=" + ((int)nroajuste).ToString();

                                DatoCotizacion parametros = new DatoCotizacion();
                                parametros.EmpresaNumero = Solicitud.IdEmpresa.Value;
                                parametros.SucursalNumero = sucursal._sucursal_empresa;
                                parametros.FechaCorte = fechacorte;//DateTime.Today;
                                parametros.Usuario = usuarioSIGMEP;
                                parametros.OficinaCodigo = oficinaCodigo;
                                parametros.PorcentajeSeguroCampesino = double.Parse(segcamp.FirstOrDefault().valor) / 100; // (segcamp.FirstOrDefault() != null ? double.Parse(segcamp.FirstOrDefault().valor) / 100 : 0.005);
                                parametros.PorcentajeSeguroXCampesinoRet = double.Parse(segcampret.FirstOrDefault().valor); // (segcampret.FirstOrDefault() != null ? double.Parse(segcampret.FirstOrDefault().valor) / 100 : 0);
                                parametros.NumeroAjuste = nroajuste;//((int)nroajuste);
                                mensaje.AppendLine(Newtonsoft.Json.JsonConvert.SerializeObject(parametros));

                                //request.AddBody(parametros);
                                request.AddParameter("DatoCotizacion", Newtonsoft.Json.JsonConvert.SerializeObject(parametros), ParameterType.RequestBody);
                                // Este servicio no solicita cabeceras
                                //request.AddHeader("Content-Type", "application/json");
                                //request.AddHeader("CodigoAplicacion", "1");
                                //request.AddHeader("DispositivoNavegador", "Chrome");
                                //request.AddHeader("DireccionIP", "1.1.1.1");
                                //request.AddHeader("SistemaOperativo", "Windows");
                                //request.AddHeader("CodigoPlataforma", "1");


                                request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
                                IRestResponse response = client.Execute(request);
                                //respToLog = response.Content;
                                var respuesta = new JavaScriptSerializer().Deserialize<object>(response.Content);
                                mensaje.AppendLine(response.Content);

                            }

                            Solicitud.MensajeError = mensaje.ToString();
                            Solicitud.Estado = 1;
                            Solicitud.FechaProcesamiento = DateTime.Now;
                            context.SaveChanges();

                            SEG_Usuario usuario = context.SEG_Usuario.FirstOrDefault(u => u.Id == Solicitud.IdUsuario);

                            if (usuario != null)
                            {

                                // Envìo del mail de confirmaciòn, solamente al usuario que hizo la acción
                                String MailTO = usuario.Email;

                                // Realizar el envío de mail al titular
                                String LinkPortalUsuarios = ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                                Dictionary<string, string> ParamValues = new Dictionary<string, string>();
                                ParamValues.Add("LINK", LinkPortalUsuarios);

                                string path = ConfigurationManager.AppSettings["PathTemplates"];
                                string ContenidoMail = Utils.GenerarContenido(path + "T8_PrefacturacionCompleta.html", ParamValues);
                                //Utils.SendMail(MailTO, "", ContenidoMail, "Saludsa - Pre-factura disponible");
                                // PEDIDO EN MAIL INDICANDO QUE NO DEBE ENVIARSE MAIL AQUI
                            }
                        }
                        catch (Exception ex)
                        {
                            Solicitud.Estado = 3; // ERROR
                            Solicitud.FechaProcesamiento = DateTime.Now;
                            Solicitud.MensajeError = "Proceso Prefacturación: IDEmpresa: " + Solicitud.IdEmpresa + " : " + ex.Message + " - " + ex.StackTrace;
                            context.SaveChanges();
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
            }
        }

        private static DateTime SumaMeses(DateTime fecha, int numemroMeses)
        {
            if (numemroMeses == 1)
                return fecha.AddMonths(1);
            else if (numemroMeses == 2)
                return fecha.AddMonths(2);
            else if (numemroMeses == 3)
                return fecha.AddMonths(3);
            else if (numemroMeses == 4)
                return fecha.AddMonths(6);
            else if (numemroMeses == 5)
                return fecha.AddMonths(12);
            else if (numemroMeses == 6)
                return fecha.AddMonths(4);
            else
                return fecha.AddMonths(1);
        }
        public static bool IsNumber(object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }
    }
}
