using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using RestSharp;

namespace BatchNotPagoInteligente.UtilsRest
{
    class RestHelper
    {
        public static RestRequest GenerarPedido(ApplicationHeaders headers, Method method)
        {
            var token = HttpContext.Current.GetOwinContext().Request.Headers["Authorization"];
            var request = new RestRequest(method);
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Authorization", token);
            request.AddHeader("CodigoAplicacion", headers.CodigoAplicacion.ToString());
            request.AddHeader("CodigoPlataforma", headers.CodigoPlataforma.ToString());
            request.AddHeader("SistemaOperativo", headers.SistemaOperativo.ToString());
            request.AddHeader("DispositivoNavegador", headers.DispositivoNavegador.ToString());
            request.AddHeader("DireccionIP", headers.DireccionIP.ToString());
            request.AddHeader("Content-Type", "application/json");
            request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
            return request;

        }
    }
}
