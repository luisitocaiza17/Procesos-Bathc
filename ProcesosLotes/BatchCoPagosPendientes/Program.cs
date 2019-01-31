using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SW.Salud.DataAccess.SigmepTableAdapters;
using SW.Salud.DataAccess.Sigmep3TableAdapters;
using SW.Salud.DataAccess.SigmepPortalCorpTableAdapters;
using ClosedXML.Excel;
using System.Configuration;
using System.IO;
using SW.Salud.DataAccess;
using RestSharp;
using System.Web.Script.Serialization;
using static BatchCoPagosPendientes.ReportDataSet;
using Microsoft.Reporting.WinForms;
using System.Data;

namespace BatchCoPagosPendientes
{
    class Program
    {
        static void Main(string[] args)
        {
            // PROCESO BATCH QUE PROCESA LOS COPAGOS PENDIENTES POR EMPRESA, ENVÍA NOTIFICACIONES A LOS ADMINISTRADORES

            // DEFINICIÓN DE ADAPTADORES
            cl01_empresasTableAdapter cl01ta = new cl01_empresasTableAdapter();
            lr12_copagos_por_cobrarTA lr12ta = new lr12_copagos_por_cobrarTA();
            cl04_contratosTableAdapter cl04ta = new cl04_contratosTableAdapter();

            int TipoProceso = int.Parse(ConfigurationManager.AppSettings["TipoProceso"]);
            List<CORP_CopagoPendiente> lstCopagos = new List<CORP_CopagoPendiente>();

            foreach (var emp in cl01ta.GetDataPortalCorp())
            {
                Decimal TotalPendiente = 0, TotalPagado = 0, TotalSaldo = 0;
                int TotalDias = 0, TotalBloqueados = 0, TotalDesbloqueados = 0;
                var lstPendientes = lr12ta.GetDataPorIDEmpresa(emp._empresa_numero);

                // Determinar el período de notificación
                int diasParaActivarColor = int.Parse(ConfigurationManager.AppSettings["diasParaActivarColor"]);
                if (!emp.Is_periodo_copagoNull() && emp._periodo_copago > 0)
                    diasParaActivarColor = emp._periodo_copago;

                if (lstPendientes.Count() > 0)
                {
                    // Proceso de envío de notificaciones de alerta
                    if (TipoProceso == 1)
                    {
                        // Armar el excel
                        String pathFileTemplate = "";
                        pathFileTemplate = ConfigurationManager.AppSettings["PathTemplates"] + "PLANTILLA_COPAGOS_PENDIENTES.xlsx";

                    var workbook = new XLWorkbook(pathFileTemplate);
                    var worksheet = workbook.Worksheet(1);
                    worksheet.PageSetup.ShowGridlines = false;

                        // Llenar los datos de la cabecera
                        worksheet.Cell(7, 3).Value = emp._razon_social.ToUpper();
                        worksheet.Cell(8, 3).Value = "COR";
                        worksheet.Cell(9, 3).Value = lstPendientes.FirstOrDefault().region;
                        worksheet.Cell(10, 3).Value = DateTime.Today.ToShortDateString();

                    // Generar la fila cabecera
                    int fila = 12;
                    worksheet.Row(fila).Style.Font.Bold = true;
                    worksheet.Cell(fila, 1).Value = "RECLAMO";
                    worksheet.Cell(fila, 2).Value = "ALC";
                    worksheet.Cell(fila, 3).Value = "No. COPAGO";
                    worksheet.Cell(fila, 4).Value = "CONTRATO";
                    worksheet.Cell(fila, 5).Value = "RUC";
                    worksheet.Cell(fila, 6).Value = "TITULAR";
                    worksheet.Cell(fila, 7).Value = "CÉDULA";
                    worksheet.Cell(fila, 8).Value = "DIAGNÓSTICO";
                    worksheet.Cell(fila, 9).Value = "F. DÉBITO";
                    worksheet.Cell(fila, 10).Value = "F. PAGO";
                    worksheet.Cell(fila, 11).Value = "F. ANULACIÓN";
                    worksheet.Cell(fila, 12).Value = "V. EMITIDO";
                    worksheet.Cell(fila, 13).Value = "V. PAGADO";
                    worksheet.Cell(fila, 14).Value = "SALDO";
                    worksheet.Cell(fila, 15).Value = "ESTADO";
                    worksheet.Cell(fila, 16).Value = "DIAS";
                    worksheet.Cell(fila, 17).Value = "LISTA";

                    for (int i = 1; i <= 17; i++)
                    {
                        worksheet.Cell(fila, i).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                        worksheet.Cell(fila, i).Style.Border.SetOutsideBorderColor(XLColor.Black);
                    }
                    fila++;

                        foreach (var item in lstPendientes)
                        {
                            worksheet.Cell(fila, 1).Value = item._numero_reclamo.ToString();

                            worksheet.Cell(fila, 2).Value = item._numero_alcance.ToString();
                            worksheet.Cell(fila, 3).Value = item._numero_copago.ToString();
                            worksheet.Cell(fila, 4).Value = item._contrato_numero.ToString();
                            worksheet.Cell(fila, 5).Value = emp._ruc_empresa;
                            worksheet.Cell(fila, 6).Value = item.TITULARNOMBRE;
                            worksheet.Cell(fila, 7).Value = item._persona_cedula;
                            worksheet.Cell(fila, 8).Value = item.CODDIAG;
                            worksheet.Cell(fila, 9).Value = item.Is_fecha_emision_copagoNull() ? string.Empty : item._fecha_emision_copago.ToShortDateString();
                            worksheet.Cell(fila, 10).Value = item.Is_fecha_pagoNull() ? string.Empty : item._fecha_pago.ToShortDateString(); ;
                            worksheet.Cell(fila, 11).Value = item.Is_fecha_anulacionNull() ? string.Empty : item._fecha_anulacion.ToShortDateString();
                            worksheet.Cell(fila, 12).Value = item._valor_copago;
                            worksheet.Cell(fila, 13).Value = item._valor_cobrado;
                            var saldo = item._valor_copago - item._valor_cobrado;
                            worksheet.Cell(fila, 14).Value = saldo;
                            worksheet.Cell(fila, 15).Value = item._nombre_estado_contrato;
                            worksheet.Cell(fila, 16).Value = item.NUMDIAS.ToString();
                            worksheet.Cell(fila, 17).Value = item._sucursal_empresa.ToString();

                            // parámetro de días para activar color, se desea que sea por empresa.
                            // se debería consultar en la tabla que sea de configuración del portal
                            // de lo que se graba en armonix
                            if (item.Is_fecha_emision_copagoNull() == false && DateTime.Today.AddDays(-diasParaActivarColor) > item._fecha_emision_copago)
                            {
                                for (int i = 1; i <= 17; i++)
                                    worksheet.Cell(fila, i).Style.Fill.SetBackgroundColor(XLColor.FromArgb(255, 107, 36));
                            }

                        for (int i = 1; i <= 17; i++)
                        {
                            worksheet.Cell(fila, i).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                            worksheet.Cell(fila, i).Style.Border.SetOutsideBorderColor(XLColor.Black);
                        }
                        fila++;

                            // Cálculo de los Totales
                            TotalPendiente += item._valor_copago;
                            TotalPagado += item._valor_cobrado;
                            TotalSaldo += saldo;
                            TotalDias += item.NUMDIAS;
                        }
                        worksheet.Columns().AdjustToContents();

                    // Línea con información resumen de la data
                    worksheet.Row(fila).Style.Font.Bold = true;
                    worksheet.Cell(fila, 6).Value = "TOTAL EMPRESA";
                    worksheet.Cell(fila, 6).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                    worksheet.Cell(fila, 6).Style.Border.SetOutsideBorderColor(XLColor.Black);
                    worksheet.Cell(fila, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range(worksheet.Cell(fila, 6), worksheet.Cell(fila, 11)).Row(fila).Merge();

                    //worksheet.Cell(fila, 11).Value = lstPendientes.Count().ToString();
                    worksheet.Cell(fila, 12).Value = Math.Round(TotalPendiente, 2).ToString();
                    worksheet.Cell(fila, 12).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                    worksheet.Cell(fila, 12).Style.Border.SetOutsideBorderColor(XLColor.Black);

                    worksheet.Cell(fila, 13).Value = Math.Round(TotalPagado, 2).ToString();
                    worksheet.Cell(fila, 13).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                    worksheet.Cell(fila, 13).Style.Border.SetOutsideBorderColor(XLColor.Black);

                    worksheet.Cell(fila, 16).Value = Math.Round(TotalSaldo, 2).ToString();
                    worksheet.Cell(fila, 16).Value = (TotalDias / lstPendientes.Count()).ToString();
                    worksheet.Cell(fila, 16).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                    worksheet.Cell(fila, 16).Style.Border.SetOutsideBorderColor(XLColor.Black);

                    Dictionary<string, byte[]> lstAdjuntos = new Dictionary<string, byte[]>();

                    using (var memoryStream = new MemoryStream())
                    {
                        workbook.SaveAs(memoryStream);
                        //memoryStream.WriteTo(Response.OutputStream);
                        //Convert.ToBase64String(memoryStream.ToArray());
                        lstAdjuntos.Add("CopagosPendientes.xlsx", memoryStream.ToArray());
                    }

                        byte[] AdjuntoFacturas = GenerarReporteFacturacion(emp);
                        if (AdjuntoFacturas != null)
                            lstAdjuntos.Add("FacturasPendientes.xlsx", AdjuntoFacturas);

                        using (PortalContratante model = new PortalContratante())
                        {

                            // Obtener los correos de los admins de la empresa actual
                            var lstAdmins = model.UsuarioAdmin_VTA.Where(x => x.IdEmpresa == emp._empresa_numero);
                            foreach (var item in lstAdmins)
                            {
                                if (model.SEG_PermisoUsuario.Count(x => x.IDUsuario == item.Id && x.IDPermiso == 14) == 0)
                                    continue;

                                // Envió el mail de notificación a los administradores
                                Dictionary<string, string> tokens = new Dictionary<string, string>();
                                tokens.Add("Cliente", item.NombreApellido);
                                tokens.Add("TiempoMaxPago", System.Configuration.ConfigurationManager.AppSettings["TiempoMaxPago"]);
                                tokens.Add("fechahasta", DateTime.Today.ToLongDateString());
                                tokens.Add("fechadesde", DateTime.Today.AddDays(-7).ToLongDateString());

                                //string PathTemplates = System.Configuration.ConfigurationManager.AppSettings["PathTemplates"];
                                //string ContenidoMail = SW.Common.Utils.GenerarContenido(PathTemplates + "T10_NotificacionCopagosContratante.htm", tokens);
                                //SW.Common.Utils.SendMail(item.Email, "", ContenidoMail, "Salud SA - Notificación de Copagos Pendientes", lstAdjuntos);

                                SW.Common.Utils.SendMail(item.Email, "", SW.Common.TipoNotificacionEnum.NotificacionCopagosContratante, tokens, lstAdjuntos);
                            }
                        }
                    }

                    // Proceso de Bloqueo - Desbloqueo
                    if (TipoProceso == 2)
                    {
                        foreach (var item in lstPendientes)
                        {
                            if (item.Is_fecha_emision_copagoNull() == false && DateTime.Today.AddDays(-(diasParaActivarColor + 2)) > item._fecha_emision_copago)
                            {
                                // Bloquear el contrato pasadas 48 horas de la notificación
                                if (cl04ta.EstaBloqueadoContrato(item._contrato_numero, item.region, item._codigo_producto) == 0)
                                {
                                    cl04ta.BloquearContratoEnrolamiento(item._contrato_numero, item.region, item._codigo_producto);

                                    // Agregar al listado de contratos bloqueados a grabar en SQL
                                    lstCopagos.Add(new CORP_CopagoPendiente()
                                    {
                                        CodigoProducto = item._codigo_producto,
                                        Estado = 1, // 1: Bloqueado - 2: Desbloqueado
                                        FechaBloqueo = DateTime.Now,
                                        NumeroContrato = item._contrato_numero,
                                        NumeroEmpresa = item._empresa_numero,
                                        NumeroSucursal = item._sucursal_empresa,
                                        Region = item.region,
                                        NombreTitular = item.TITULARNOMBRE,
                                        IdentificacionTitular = item._persona_cedula,
                                        TipoPendiente = 1 // Tipo: 1 - Copagos
                                    });
                                }
                            }
                        }

                        // Determinar si los contratos bloqueados en SQL deben desbloquearse en el sigmep
                        List<CORP_CopagoPendiente> lstDesbloqueados = new List<CORP_CopagoPendiente>();
                        using (PortalContratante model = new PortalContratante())
                        {
                            var lstBloqueados = model.CORP_CopagoPendiente.Where(x => x.NumeroEmpresa == emp._empresa_numero && x.Estado == 1
                                                && x.TipoPendiente == 1);
                            foreach (var copago in lstBloqueados)
                            {
                                if (lstPendientes.Count(x => x._contrato_numero == copago.NumeroContrato && x.region == copago.Region
                                     && x._codigo_producto == copago.CodigoProducto) == 0)
                                {
                                    // Desbloquear el contrato si no lo esta todavía
                                    if (cl04ta.EstaBloqueadoContrato(copago.NumeroContrato, copago.Region, copago.CodigoProducto) > 0)
                                    {
                                        cl04ta.DesbloquearContratoEnrolamiento(copago.NumeroContrato, copago.Region, copago.CodigoProducto);

                                        // Actualizar la información en SQL
                                        copago.Estado = 2;
                                        copago.FechaDesbloqueo = DateTime.Now;

                                        lstDesbloqueados.Add(copago);
                                    }
                                }
                            }

                            model.SaveChanges();
                        }

                        #region Envio de Notificaciones
                        // Armar el excel
                        String pathFileTemplate = "";
                        pathFileTemplate = ConfigurationManager.AppSettings["PathTemplates"] + "PLANTILLA_COPAGOS_BLOQUEADOS.xlsx";

                        TotalBloqueados = TotalDesbloqueados = 0;
                        var workbook = new XLWorkbook(pathFileTemplate);
                        var worksheet = workbook.Worksheet(1);

                        // Llenar los datos de la cabecera
                        worksheet.Cell(7, 3).Value = emp._razon_social.ToUpper();
                        worksheet.Cell(8, 3).Value = "COR";
                        worksheet.Cell(9, 3).Value = DateTime.Today.ToShortDateString();

                        // Generar la fila cabecera
                        int fila = 11;
                        worksheet.Row(fila).Style.Font.Bold = true;
                        worksheet.Cell(fila, 1).Value = "# SUCURSAL";
                        worksheet.Cell(fila, 2).Value = "REGION";
                        worksheet.Cell(fila, 3).Value = "CONTRATO";
                        worksheet.Cell(fila, 4).Value = "TITULAR";
                        worksheet.Cell(fila, 5).Value = "CÉDULA";
                        fila++;

                        foreach (var item in lstCopagos.Where(x => x.NumeroEmpresa == emp._empresa_numero))
                        {
                            worksheet.Cell(fila, 1).Value = item.NumeroSucursal.ToString();
                            worksheet.Cell(fila, 2).Value = item.Region.ToString();
                            worksheet.Cell(fila, 3).Value = item.NumeroContrato.ToString();
                            worksheet.Cell(fila, 4).Value = item.NombreTitular;
                            worksheet.Cell(fila, 5).Value = item.IdentificacionTitular;

                            fila++;
                            TotalBloqueados++;
                        }
                        worksheet.Columns().AdjustToContents();

                        // Línea con información resumen de la data
                        worksheet.Row(fila).Style.Font.Bold = true;
                        worksheet.Cell(fila, 2).Value = "TOTAL BLOQUEADOS";
                        worksheet.Cell(fila, 5).Value = TotalBloqueados.ToString();
                        fila++;
                        fila++;

                        // Cargar en el excel la información de los contratos desbloqueados
                        foreach (var item in lstDesbloqueados)
                        {
                            worksheet.Cell(fila, 1).Value = item.NumeroSucursal.ToString();
                            worksheet.Cell(fila, 2).Value = item.Region.ToString();
                            worksheet.Cell(fila, 3).Value = item.NumeroContrato.ToString();
                            worksheet.Cell(fila, 4).Value = item.NombreTitular;
                            worksheet.Cell(fila, 5).Value = item.IdentificacionTitular;

                            fila++;
                            TotalDesbloqueados++;
                        }
                        worksheet.Columns().AdjustToContents();

                        // Línea con información resumen de la data
                        worksheet.Row(fila).Style.Font.Bold = true;
                        worksheet.Cell(fila, 2).Value = "TOTAL DESBLOQUEADOS";
                        worksheet.Cell(fila, 5).Value = TotalDesbloqueados.ToString();

                        Dictionary<string, byte[]> lstAdjuntos = new Dictionary<string, byte[]>();
                        using (var memoryStream = new MemoryStream())
                        {
                            workbook.SaveAs(memoryStream);
                            //memoryStream.WriteTo(Response.OutputStream);
                            //Convert.ToBase64String(memoryStream.ToArray());
                            lstAdjuntos.Add("BloqueadosXCopagos.xlsx", memoryStream.ToArray());
                        }

                        using (PortalContratante model = new PortalContratante())
                        {

                            // Obtener los correos de los admins de la empresa actual
                            var lstAdmins = model.UsuarioAdmin_VTA.Where(x => x.IdEmpresa == emp._empresa_numero);
                            foreach (var item in lstAdmins)
                            {
                                if (model.SEG_PermisoUsuario.Count(x => x.IDUsuario == item.Id && x.IDPermiso == 14) == 0)
                                    continue;

                                // Envió el mail de notificación a los administradores
                                Dictionary<string, string> tokens = new Dictionary<string, string>();
                                tokens.Add("Cliente", item.NombreApellido);
                                tokens.Add("fechahasta", DateTime.Today.ToLongDateString());

                                //string PathTemplates = System.Configuration.ConfigurationManager.AppSettings["PathTemplates"];
                                //string ContenidoMail = SW.Common.Utils.GenerarContenido(PathTemplates + "T10_NotificacionCopagosContratante.htm", tokens);
                                //SW.Common.Utils.SendMail(item.Email, "", ContenidoMail, "Salud SA - Notificación de Copagos Pendientes", lstAdjuntos);

                                SW.Common.Utils.SendMail(item.Email, "", SW.Common.TipoNotificacionEnum.NotificacionBloqueadosXCopagosContratante, tokens, lstAdjuntos);
                            }
                        }
                        #endregion
                    }
                }

            }

            using (PortalContratante model = new PortalContratante())
            {
                foreach (var item in lstCopagos)
                {
                    if (model.CORP_CopagoPendiente.Count(x => x.NumeroContrato == item.NumeroContrato && x.Region == item.Region
                         && x.CodigoProducto == item.CodigoProducto && x.Estado == 1 && x.TipoPendiente == 1) == 0)
                        model.CORP_CopagoPendiente.Add(item);
                }
                model.SaveChanges();
            }

        }

        private static byte[] GenerarReporteFacturacion(Sigmep._cl01_empresasRow emp)
        {
            string SeguridadesUsername = ConfigurationManager.AppSettings["SeguridadesUsername"];
            string SeguridadesPassword = ConfigurationManager.AppSettings["SeguridadesPassword"];
            string SeguridadesGrantType = ConfigurationManager.AppSettings["SeguridadesGrantType"];
            string SeguridadesClientID = ConfigurationManager.AppSettings["SeguridadesClientID"];
            string address_token = ConfigurationManager.AppSettings["AddressToken"];
            string AddressFacturacion = ConfigurationManager.AppSettings["AddressFacturacion"];


            cl02_empresa_sucursalesTableAdapter cl02ta = new cl02_empresa_sucursalesTableAdapter();
            // GENERACION REPORTE FACTURACIÓN
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


            FacturaEmpresaDataTable dt = new FacturaEmpresaDataTable();

            foreach (var sucursal in cl02ta.GetDataByEmpresa(emp._empresa_numero))
            {
                string qparams = "?empresaNumero=" + emp._empresa_numero.ToString() +
                    "&sucursalEmpresa=" + sucursal._sucursal_empresa +
                    "&fechaDesde=" + DateTime.Today.AddYears(-2).ToString("yyyy-MM-dd") + // verifica deudas pendientes de hasta 2 años atrás
                    "&fechaHasta=" + DateTime.Today.ToString("yyyy-MM-dd") +
                    "&codigoEstadoFactura=36";


                var client_liq = new RestClient(AddressFacturacion + qparams);

                var request_liq = new RestRequest(Method.POST);
                request_liq.AddHeader("Content-Type", "application/json");
                request_liq.AddHeader("Authorization", "bearer " + respuesta_token.access_token);
                request_liq.AddHeader("CodigoAplicacion", "3");
                request_liq.AddHeader("DispositivoNavegador", "Chrome");
                request_liq.AddHeader("DireccionIP", "1.1.1.1");
                request_liq.AddHeader("SistemaOperativo", "Windows");
                request_liq.AddHeader("CodigoPlataforma", "7");

                request_liq.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

                IRestResponse response_liq = client_liq.Execute(request_liq);
                //var respuesta_liq = new JavaScriptSerializer().Deserialize<object>(response_liq.Content);

                if (response_liq.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // response_liq.Content
                    var msg = Newtonsoft.Json.JsonConvert.DeserializeObject<Msg>(response_liq.Content);
                    if (msg.Estado == "OK")
                    {
                        var dti = Newtonsoft.Json.JsonConvert.DeserializeObject<FacturaEmpresaDataTable>(msg.Datos.ToString());
                        foreach (var it in dti)
                        {
                            it.FechaCaptura = it.FechaCaptura.Replace("00:00:00", "");
                            it.FechaFacturacion = it.FechaFacturacion.Replace("00:00:00", "");
                            it.FechaFinFactura = it.FechaFinFactura.Replace("00:00:00", "");
                            it.FechaInicioFactura = it.FechaInicioFactura.Replace("00:00:00", "");
                        }

                        dt.Merge(dti);
                    }
                }
            }

            // si no hay datos para presentar, no se genera el adjunto
            if (dt.Rows.Count == 0)
                return null;

            // Generaciòn del PDF

            LocalReport report = new LocalReport();
            report.ReportPath = AppDomain.CurrentDomain.BaseDirectory + "ReporteFacturacion.rdlc";
            //report.DataSources.Clear();
            //report.SetParameters(new ReportParameter[] { parameter });
            report.DataSources.Clear();
            report.DataSources.Add(new ReportDataSource("DataSet1", (DataTable)dt));

            report.Refresh();


            string[] streamids;
            string minetype;
            string encod;
            string fextension;
            string deviceInfo =
              "<DeviceInfo>" +
              "  <OutputFormat>EMF</OutputFormat>" +
              "  <PageWidth>11in</PageWidth>" +
              "  <PageHeight>8.5in</PageHeight>" +
              "  <MarginTop>0.20in</MarginTop>" +
              "  <MarginLeft>0.20in</MarginLeft>" +
              "  <MarginRight>0.20in</MarginRight>" +
              "  <MarginBottom>0.20in</MarginBottom>" +
              "</DeviceInfo>";
            Warning[] warnings;
            byte[] rpbybe = report.Render("EXCELOPENXML", deviceInfo, out minetype, out encod, out fextension, out streamids,
               out warnings);

            return rpbybe;
        }
    }
}
