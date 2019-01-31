using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SW.Salud.DataAccess.SigmepTableAdapters;
using SW.Salud.DataAccess.SigmepPortalCorpTableAdapters;
using System.Configuration;
using SW.Salud.DataAccess;
using ClosedXML.Excel;
using System.IO;

namespace BatchFaltaPagoFacturas
{
    class Program
    {
        static void Main(string[] args)
        {
            cl01_empresasTableAdapter cl01ta = new cl01_empresasTableAdapter();
            cl02_empresa_sucursalesTableAdapter cl02ta = new cl02_empresa_sucursalesTableAdapter();
            FacturacionCorpMoraTA fcta = new FacturacionCorpMoraTA();

            List<CORP_CopagoPendiente> lstSucursalesABloquear = new List<CORP_CopagoPendiente>();
            foreach (var emp in cl01ta.GetDataPortalCorp())
            {
                //Decimal TotalPendiente = 0, TotalPagado = 0, TotalSaldo = 0;
                //int TotalDias = 0;
                var lstFacturas = fcta.GetDataByEmpresa(emp._empresa_numero);

                // Determinar el período de notificación
                int diasEsperaBloquearSucursal = int.Parse(ConfigurationManager.AppSettings["DiasEsperaBloquearSucursal"]);
                if (!emp.Is_periodo_pagoNull() && emp._periodo_pago > 0)
                    diasEsperaBloquearSucursal = emp._periodo_pago;

                if (lstFacturas.Count() > 0)
                {
                    foreach (var item in lstFacturas)
                    {
                        if (item.IsFECHAEMISIONNull() == false && DateTime.Today.AddDays(-(diasEsperaBloquearSucursal)) > item.FECHAEMISION)
                        {
                            if(!item.bloqueado)
                            {
                                // Bloquear la sucursal pasadas 48 horas de la notificación
                                cl02ta.BloquearSucursalXNumero(emp._empresa_numero, item.NUMEROSUCURSAL);

                                // Agregar al listado de sucursales bloqueadas a grabar en SQL
                                lstSucursalesABloquear.Add(new CORP_CopagoPendiente()
                                {
                                    CodigoProducto = item._codigo_producto,
                                    Estado = 1, // 1: Bloqueado - 2: Desbloqueado
                                    FechaBloqueo = DateTime.Now,
                                    NumeroContrato = 0,
                                    NumeroEmpresa = emp._empresa_numero,
                                    NumeroSucursal = item.NUMEROSUCURSAL,
                                    Region = item.NUMEROFACTURA,
                                    NombreSucursal = item.NOMBRESUCURSAL,
                                    TipoPendiente = 2 // Tipo: 2 - Facturas
                                });
                            }
                        }
                    }

                    // Determinar si las sucursales bloqueadas en SQL deben desbloquearse en el sigmep
                    List<CORP_CopagoPendiente> lstDesbloqueados = new List<CORP_CopagoPendiente>();
                    using (PortalContratante model = new PortalContratante())
                    {
                        var lstBloqueados = model.CORP_CopagoPendiente.Where(x => x.NumeroEmpresa == emp._empresa_numero && x.Estado == 1
                                            && x.TipoPendiente == 2);
                        foreach (var factura in lstBloqueados)
                        {
                            if (lstFacturas.Count(x => x.NUMEROSUCURSAL == factura.NumeroSucursal) == 0)
                            {
                                // Desbloquear la sucursal si no lo esta todavía
                                var sucursal = lstFacturas.FirstOrDefault(x => x.NUMEROSUCURSAL == factura.NumeroSucursal);
                                if (sucursal.bloqueado)
                                {
                                    cl02ta.DesBloquearSucursalXNumero(emp._empresa_numero, factura.NumeroSucursal);

                                    // Actualizar la información en SQL
                                    factura.Estado = 2;
                                    factura.FechaDesbloqueo = DateTime.Now;

                                    lstDesbloqueados.Add(factura);
                                }
                            }
                        }

                        model.SaveChanges();
                    }

                    #region Envio de Notificaciones
                    // Armar el excel
                    String pathFileTemplate = "";
                    pathFileTemplate = ConfigurationManager.AppSettings["PathTemplates"] + "PLANTILLA_BLOQUEADOSXFALTAPAGO.xlsx";

                    var workbook = new XLWorkbook(pathFileTemplate);
                    var worksheet = workbook.Worksheet(1);

                    // Llenar los datos de la cabecera
                    worksheet.Cell(7, 3).Value = emp._razon_social.ToUpper();
                    worksheet.Cell(8, 3).Value = DateTime.Today.ToShortDateString();

                    // Generar la fila cabecera
                    int fila = 10;
                    worksheet.Row(fila).Style.Font.Bold = true;
                    worksheet.Cell(fila, 1).Value = "# SUCURSAL";
                    worksheet.Cell(fila, 2).Value = "NOMBRE SUCURSAL";
                    worksheet.Cell(fila, 3).Value = "PRODUCTO";
                    fila++;

                    foreach (var item in lstSucursalesABloquear.Where(x => x.NumeroEmpresa == emp._empresa_numero))
                    {
                        worksheet.Cell(fila, 1).Value = item.NumeroSucursal.ToString();
                        worksheet.Cell(fila, 2).Value = item.NombreSucursal.ToString();
                        worksheet.Cell(fila, 3).Value = item.CodigoProducto.ToString();

                        fila++;
                    }
                    worksheet.Columns().AdjustToContents();

                    // Línea con información resumen de la data
                    worksheet.Row(fila).Style.Font.Bold = true;
                    worksheet.Cell(fila, 1).Value = "TOTAL BLOQUEADOS";
                    worksheet.Cell(fila, 3).Value = lstSucursalesABloquear.Count(x => x.NumeroEmpresa == emp._empresa_numero).ToString();
                    fila++;
                    fila++;

                    // Cargar en el excel la información de los contratos desbloqueados
                    foreach (var item in lstDesbloqueados)
                    {
                        worksheet.Cell(fila, 1).Value = item.NumeroSucursal.ToString();
                        worksheet.Cell(fila, 2).Value = item.NombreSucursal.ToString();
                        worksheet.Cell(fila, 3).Value = item.CodigoProducto.ToString();

                        fila++;
                    }
                    worksheet.Columns().AdjustToContents();

                    // Línea con información resumen de la data
                    worksheet.Row(fila).Style.Font.Bold = true;
                    worksheet.Cell(fila, 1).Value = "TOTAL DESBLOQUEADOS";
                    worksheet.Cell(fila, 3).Value = lstDesbloqueados.Count().ToString();

                    Dictionary<string, byte[]> lstAdjuntos = new Dictionary<string, byte[]>();
                    using (var memoryStream = new MemoryStream())
                    {
                        workbook.SaveAs(memoryStream);
                        //memoryStream.WriteTo(Response.OutputStream);
                        //Convert.ToBase64String(memoryStream.ToArray());
                        lstAdjuntos.Add("BloqueadosXFacturasImpagas.xlsx", memoryStream.ToArray());
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

            using (PortalContratante model = new PortalContratante())
            {
                foreach (var item in lstSucursalesABloquear)
                {
                    if (model.CORP_CopagoPendiente.Count(x => x.NumeroEmpresa == item.NumeroEmpresa && x.NumeroSucursal == item.NumeroSucursal 
                        && x.Estado == 1 && x.TipoPendiente == 2) == 0)
                        model.CORP_CopagoPendiente.Add(item);
                }
                model.SaveChanges();
            }

        }
    }
}
