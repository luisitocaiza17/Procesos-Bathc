using BatchSiniestralidad.AccesoDatos;
using BatchSiniestralidad.Entidades;
using RestSharp;
using SW.Salud.DataAccess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace BatchSiniestralidad
{
    class Program
    {
        static void Main(string[] args)
        {
            string SeguridadesUsername = ConfigurationManager.AppSettings["SeguridadesUsername"];
            string SeguridadesPassword = ConfigurationManager.AppSettings["SeguridadesPassword"];
            string SeguridadesGrantType = ConfigurationManager.AppSettings["SeguridadesGrantType"];
            string SeguridadesClientID = ConfigurationManager.AppSettings["SeguridadesClientID"];
            string address_token = ConfigurationManager.AppSettings["AddressToken"];
            string AddressFacturacion = ConfigurationManager.AppSettings["AddressFacturacion"];

            var respToLog = " ";
            var data = "username=" + SeguridadesUsername + "&password=" + SeguridadesPassword + "&grant_type=" + SeguridadesGrantType + "&client_id=" + SeguridadesClientID + "";
            //Generacion del cliente a ejecutarse
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

            try
            {
                //var address_cop = "http://localhost:49928/api/Siniestralidad/SiniestralidadListasBatch";
                var address_cop = System.Configuration.ConfigurationManager.AppSettings["AddressSiniestralidad"];
                var client_cop = new RestClient(address_cop);

                //llamada a servicio de siniestralidad
                var request_liq = new RestRequest(Method.GET);
                request_liq.AddHeader("Content-Type", "application/json");
                request_liq.AddHeader("Authorization", "bearer " + respuesta_token.access_token);
                request_liq.AddHeader("CodigoAplicacion", "3");
                request_liq.AddHeader("DispositivoNavegador", "Chrome");
                request_liq.AddHeader("DireccionIP", "1.1.1.1");
                request_liq.AddHeader("SistemaOperativo", "Windows");
                request_liq.AddHeader("CodigoPlataforma", "7");
                request_liq.Timeout = 900000;
                request_liq.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

                IRestResponse response_liq = client_cop.Execute(request_liq);
                //var respuesta_liq = new JavaScriptSerializer().Deserialize<object>(response_liq.Content);

                if (response_liq.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // response_liq.Content
                    var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<Msg>(response_liq.Content);
                    if (msg.Estado == "OK")
                    {
                        var dti = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Siniestralidad>>(msg.Datos.ToString());
                        List<SEG_Siniestralidad> listaSiniestralidades = new List<SEG_Siniestralidad>();
                        foreach (var si in dti)
                        {
                            SEG_Siniestralidad Seg_Si = new SEG_Siniestralidad();
                            Seg_Si.Anio = si.Anio.ToString();
                            Seg_Si.FechaCreacion = DateTime.Now;
                            Seg_Si.IdEmpresa = si.NumeroEmpresa;
                            Seg_Si.Empresa = si.Empresa;
                            Seg_Si.IdSucursalEmpresa = si.NumeroLista;
                            Seg_Si.Sucursal = si.Nombre;
                            Seg_Si.Mes = si.Mes;
                            Seg_Si.Ruc = si.NumeroRuc;
                            Seg_Si.TotalTarifa = si.Primas;
                            Seg_Si.ValorBonificado = si.Liquidaciones;
                            Seg_Si.ValorBonificadoIBNR = si.LiquidacionesIBNR;
                            Seg_Si.Siniestralidad = (decimal?)si.Porcentaje;
                            Seg_Si.SiniestralidadIBNR = si.SiniestralidadIBNR;
                            Seg_Si.MesIBNR = si.MesIBNR;
                            Seg_Si.ValorIBNR = si.ValorIBNR;
                            Seg_Si.FechaVigencia = si.FechaInicioVigencia;
                            listaSiniestralidades.Add(Seg_Si);
                        }
                        DatosSiniestralidad.guardarSiniestralidad(listaSiniestralidades);
                    }
                }
            }
            catch (Exception e)
            {
                //error
            }

        }
    }
    
}
