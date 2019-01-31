using RestSharp;
using SW.Salud.DataAccess;
using SW.Salud.DataAccess.SigmepPortalCorpTableAdapters;
using SW.Salud.DataAccess.SigmepTableAdapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace BatchNotPagoInteligente
{
    class Program
    {
        static void Main(string[] args)
        {

            // DEFINICIÓN DE ADAPTADORES
            cl01_empresasTableAdapter cl01ta = new cl01_empresasTableAdapter();
            lr02_reclamosTA lr02ta = new lr02_reclamosTA();
            string contenttype = string.Empty;
            try
            {
                // Obtener un token
                var respToLog = " ";
                var data = "username=UsrServiciosSalud&password=UsrS3rv1c1os&grant_type=password&client_id=8a3e4d10b2b24d6b9c55c88a95fdc324";

            //Generacion del cliente a ejecutarse
            //var address_token = "http://pruebas.servicios.saludsa.com.ec/ServicioAutorizacion/oauth2/token";
            var address_token = System.Configuration.ConfigurationManager.AppSettings["AddressToken"];
            var client_token = new RestClient(address_token);

                var request_token = new RestRequest(Method.POST);
                request_token.AddHeader("Content-Type", "application/json");
                request_token.AddHeader("CodigoAplicacion", "1");
                request_token.AddHeader("DispositivoNavegador", "Chrome");
                request_token.AddHeader("DireccionIP", "1.1.1.1");
                request_token.AddHeader("SistemaOperativo", "Windows");
                request_token.AddHeader("CodigoPlataforma", "1");
                request_token.AddParameter("data", data, ParameterType.RequestBody);
                request_token.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

                IRestResponse response_token = client_token.Execute(request_token);
                respToLog = response_token.Content;
                var respuesta_token = new JavaScriptSerializer().Deserialize<TokenInfo>(response_token.Content);

                foreach (var emp in cl01ta.GetDataPortalCorp())
                {
                    int aux = 0;
                    var lstReclamos = lr02ta.GetDataReclamosPorEmpresa(emp._empresa_numero);

                    foreach (var item in lstReclamos)
                    {
                        // Llamar al WS que genera el pdf de liquidación
                        var filter_rec = new ReclamoEntityFilter();
                        filter_rec.CodigoContrato = Convert.ToInt64(item._contrato_numero);
                        filter_rec.NumeroAlcance = item._numero_alcance;
                        filter_rec.NumeroReclamo = item._numero_reclamo;
                        filter_rec.TipoReclamo = item._tipo_reclamo;

                        ////var address_liq = "http://pruebas.servicios.saludsa.com.ec/ServicioArmonix/api/reclamos/generarPdf";
                        //var address_liq = "http://localhost:5150/SC/api/reclamos/generarPdf64";
                        var address_liq = System.Configuration.ConfigurationManager.AppSettings["AddressLiquidacion"];
                        var client_liq = new RestClient(address_liq);

                        var request_liq = new RestRequest(Method.POST);
                        request_liq.AddHeader("Content-Type", "application/json");
                        request_liq.AddHeader("Authorization", "bearer " + respuesta_token.access_token);
                        request_liq.AddHeader("CodigoAplicacion", "3");
                        request_liq.AddHeader("DispositivoNavegador", "Chrome");
                        request_liq.AddHeader("DireccionIP", "1.1.1.1");
                        request_liq.AddHeader("SistemaOperativo", "Windows");
                        request_liq.AddHeader("CodigoPlataforma", "7");
                        request_liq.AddParameter("filter", new JavaScriptSerializer().Serialize(filter_rec), ParameterType.RequestBody);
                        request_liq.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

                        IRestResponse response_liq = client_liq.Execute(request_liq);
                        //var respuesta_liq = new JavaScriptSerializer().Deserialize<object>(response_liq.Content);

                        if (response_liq.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            Dictionary<string, byte[]> lstAdjuntos = new Dictionary<string, byte[]>();
                            //var pdf_liq = response_liq.RawBytes;
                            //response_liq.Content.Replace('"','')
                            var docliquidacion = Convert.FromBase64String(response_liq.Content.Replace("\"", ""));
                            //var docliquidacion = Convert.FromBase64String(pdf_liq);  //response_liq.Content;
                            contenttype = response_liq.ContentType;
                            lstAdjuntos.Add("DETALLE LIQUIDACIÓN AL " + DateTime.Today.ToShortDateString() + ".pdf", docliquidacion);

                            if (!item.Is_numero_copagoNull())
                            {
                                // Llamar al WS que genera el pdf de liquidación
                                var filter_cop = new CopagoFilter();
                                filter_cop.NumeroCopago = item._numero_copago;
                                filter_cop.NumeroLineaPago = item._numero_linea_pago;
                                filter_cop.Ciudad = (item.COPAGOREGION == "SIERRA" ? 1 : item.COPAGOREGION == "COSTA" ? 2 : 3);

                                //var address_cop = "http://pruebas.servicios.saludsa.com.ec/ServicioArmonix/api/copago/pdfCopago";
                                var address_cop = System.Configuration.ConfigurationManager.AppSettings["AddressCopago"];
                                var client_cop = new RestClient(address_cop);

                                var request_cop = new RestRequest(Method.POST);
                                request_cop.AddHeader("Content-Type", "application/json");
                                request_cop.AddHeader("Authorization", "bearer " + respuesta_token.access_token);
                                request_cop.AddHeader("CodigoAplicacion", "3");
                                request_cop.AddHeader("DispositivoNavegador", "Chrome");
                                request_cop.AddHeader("DireccionIP", "1.1.1.1");
                                request_cop.AddHeader("SistemaOperativo", "Windows");
                                request_cop.AddHeader("CodigoPlataforma", "7");
                                request_cop.AddParameter("filter", new JavaScriptSerializer().Serialize(filter_cop), ParameterType.RequestBody);
                                request_cop.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

                                IRestResponse response_cop = client_cop.Execute(request_cop);

                                if (response_cop.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    contenttype = response_cop.ContentType;
                                    //var docliquidacion = Convert.FromBase64String(response_liq.Content.Replace("\"", ""));
                                    //var doccopago = Convert.FromBase64String(response_cop.Content.Replace("\"", "")); 
                                    //var doccopago = Encoding.UTF8.GetBytes(response_liq.Content);
                                    //lstAdjuntos.Add("DETALLE COPAGO AL " + DateTime.Today.ToShortDateString() + ".pdf", doccopago);
                                    lstAdjuntos.Add("DETALLE COPAGO AL " + DateTime.Today.ToShortDateString() + ".pdf", response_cop.RawBytes);
                                }


                                // Enviar la notificación al titular
                                #region Envío de notificación a los titulares
                                Dictionary<string, string> tokens = new Dictionary<string, string>();
                                tokens.Add("Cliente", item._persona_nombres + " " + item._persona_apellidos);
                                tokens.Add("TiempoMaxPago", System.Configuration.ConfigurationManager.AppSettings["TiempoMaxPago"]);
                                tokens.Add("FechaIncurrencia", item._fecha_incurrencia.ToShortDateString());
                                tokens.Add("NumeroCopago", item._numero_reclamo.ToString());
                                tokens.Add("Colaborador", item._persona_nombres + " " + item._persona_apellidos);
                                tokens.Add("Valor", item._valor_copago.ToString());

                                //string PathTemplates = System.Configuration.ConfigurationManager.AppSettings["PathTemplates"];
                                //string ContenidoMail = SW.Common.Utils.GenerarContenido(PathTemplates + "T11_NotificacionCopagosUsuario.html", tokens);
                                //SW.Common.Utils.SendMail(item.PERSONAEMAIL, "", ContenidoMail, "Salud SA - Notificación de Liquidación", lstAdjuntos);

                                SW.Common.Utils.SendMail(item.PERSONAEMAIL, "", SW.Common.TipoNotificacionEnum.NotificacionCopagosUsuario, tokens, lstAdjuntos);


                                using (PortalContratante model = new PortalContratante())
                                {

                                    // Obtener los correos de los admins de la empresa actual
                                    var lstAdmins = model.UsuarioAdmin_VTA.Where(x => x.IdEmpresa == emp._empresa_numero);
                                    foreach (var adm in lstAdmins)
                                    {
                                        if (model.SEG_PermisoUsuario.Count(x => x.IDUsuario == adm.Id && x.IDPermiso == 14) == 0)
                                            continue;

                                        //SW.Common.Utils.SendMail(adm.Email, "", ContenidoMail, "Salud SA - Notificación de Liquidación", lstAdjuntos);
                                        SW.Common.Utils.SendMail(adm.Email, "", SW.Common.TipoNotificacionEnum.NotificacionCopagosUsuario, tokens, lstAdjuntos);


                                    }
                                }
                                #endregion

                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                SW.Common.ExceptionManager.ReportException(ex, SW.Common.ExceptionManager.ExceptionSources.Server);
            }
        }

        public class ReclamoEntityFilter
        {
            public int NumeroReclamo { get; set; }
            public int NumeroAlcance { get; set; }
            public int? NumeroContrato { get; set; }
            public long CodigoContrato { get; set; }
            public int NumeroPersona { get; set; }
            public string NumeroSobre { get; set; }
            public string TipoReclamo { get; set; }
            public string Producto { get; set; }
            public string CodigoPlan { get; set; }
            public string NombreTitular { get; set; }
            public int PersonaNumero { get; set; }
            public string NombreBeneficiario { get; set; }
            public decimal MontoPresentado { get; set; }
            public decimal MontoCubierto { get; set; }
            public decimal MontoBonificado { get; set; }
            public decimal MontoCopago { get; set; }
            public decimal MontoArancel { get; set; }
            public string EstadoReclamo { get; set; }
            public string FechaLiquidacion { get; set; }
            public string Region { get; set; }
            public string Prestador { get; set; }
            public string Diagnostico { get; set; }
            public string OficinaLiquidacion { get; set; }
            public string Digitador { get; set; }
            public int NivelPrestadorDesde { get; set; }
            public int NivelPrestadorHasta { get; set; }
            public int NivelCliente { get; set; }
            public string Especialidad { get; set; }
            public DateTime? FechaDesde { get; set; }
            public DateTime? FechaHasta { get; set; }
            public int NumeroPrestador { get; set; }
            public string NumeroConvenio { get; set; }
            public string RucPrestador { get; set; }
        }

        public class CopagoFilter
        {
            public int NumeroCopago { get; set; }
            public int NumeroLineaPago { get; set; }
            public int Ciudad { get; set; }
        }

        public class TokenInfo
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
            public string token_type { get; set; }
            public string user_data { get; set; }
            public string error { get; set; }
            public string error_description { get; set; }
            public int token_retrieve { get; set; }
        }

    }
}
