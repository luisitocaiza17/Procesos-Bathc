using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace SW.Common
{
    public static class RestHelper
    {
        static public TokenInfo token { get; set; }
        //usos para bytec
        static public short CodigoAplicacion { get; set; }
        static public short CodigoPlataforma { get; set; }
        static public string DireccionIP { get; set; }
        static public string DispositivoNavegador { get; set; }
        static public string SistemaOperativo { get; set; }

        

        public static void GenerarToken()
        {
            string SeguridadesUsername = ConfigurationManager.AppSettings["SeguridadesUsername"];
            string SeguridadesPassword = ConfigurationManager.AppSettings["SeguridadesPassword"];
            string SeguridadesGrantType = ConfigurationManager.AppSettings["SeguridadesGrantType"];
            string SeguridadesClientID = ConfigurationManager.AppSettings["SeguridadesClientID"];
            string address_token = ConfigurationManager.AppSettings["AddressToken"];
            // Obtener un token
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

            //generar otras variables para procesamiento de pedidos
            token = respuesta_token;
            short code;
            short.TryParse(ConfigurationManager.AppSettings["CodigoAplicacion"], out code);
            CodigoAplicacion = code;
            short.TryParse(ConfigurationManager.AppSettings["CodigoPlataforma"], out code);
            CodigoPlataforma = code;
            DireccionIP = "1.1.1.1";
            DispositivoNavegador = "Chrome";
            SistemaOperativo = Environment.OSVersion.ToString();
        }

        public static RestRequest GenerarPedido(Method method)
        {
            //var token = HttpContext.Current.GetOwinContext().Request.Headers["Authorization"];
            var request = new RestRequest(method);
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Authorization", "bearer " + token.access_token);
            request.AddHeader("CodigoAplicacion", CodigoAplicacion.ToString());
            request.AddHeader("CodigoPlataforma", CodigoPlataforma.ToString());
            request.AddHeader("SistemaOperativo", SistemaOperativo);
            request.AddHeader("DispositivoNavegador", DispositivoNavegador);
            request.AddHeader("DireccionIP", DireccionIP);
            request.AddHeader("Content-Type", "application/json");
            request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
            return request;

        }

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
