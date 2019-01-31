using ClosedXML.Excel;
using SW.Salud.DataAccess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace BatchNotificacionCarga
{
    class Program
    {
        static void Main(string[] args)
        {

            // Este proceso batch envía un mail a los administradores con la información del avance del proceso de enrolamiento de sus trabajadores.
            // Debería correr el lunes de cada semana
            int ModoEjecucion = Convert.ToInt32(ConfigurationManager.AppSettings["NC_ModoEjecucion"]);
            int FrecuenciaEjecucion = Convert.ToInt32(ConfigurationManager.AppSettings["NC_FrecuenciaEjecucion"]);

            ////if (ModoEjecucion == 1)
            ////    FrecuenciaEjecucion = 30;

            //DateTime firstOfNextMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
            //DateTime lastOfThisMonth = firstOfNextMonth.AddDays(-1);
            //if (FrecuenciaEjecucion > lastOfThisMonth.Day)
            //    FrecuenciaEjecucion = lastOfThisMonth.Day;

            // Ahora dicen que el proceso debe correr cada lunes
            if (DateTime.Today.DayOfWeek == DayOfWeek.Monday)
            {
                using (PortalContratante model = new PortalContratante())
                {
                    foreach (var emp in model.CORP_Registro.Where(x => x.Estado == (int)EnumEstadoRegistroTemporalMasivo.Incluido).GroupBy(x => x.IdEmpresa).ToList())
                    {
                        int IDEmpresa = emp.Key.Value;

                        int TotalPendientes = model.CORP_Registro.Count(x => x.IdEmpresa == IDEmpresa && x.Estado == (int)EnumEstadoRegistroTemporalMasivo.Incluido
                             && (!x.CompletadoEnrolamiento.HasValue || x.CompletadoEnrolamiento == false));
                        if (TotalPendientes > 0)
                        {
                            //var lstRegistros = model.CORP_Registro.Where(x => x.IdEmpresa == emp.Key && x.Estado == (int)EnumEstadoRegistroTemporalMasivo.Incluido
                            //                    && (!x.CompletadoEnrolamiento.HasValue || x.CompletadoEnrolamiento == false));
                            //var lstRegistros = model.CORP_Registro.Where(x => x.IdEmpresa == IDEmpresa && x.Estado == (int)EnumEstadoRegistroTemporalMasivo.Incluido);

                            // Armar el excel
                            String pathFileTemplate = "";
                            pathFileTemplate = ConfigurationManager.AppSettings["PathTemplates"] + "PLANTILLA_CARGA_ASEGURADOS_CONTRATANTE.xlsx";

                            var workbook = new XLWorkbook(pathFileTemplate);
                            var worksheet = workbook.Worksheet(1);

                            // Generar la fila cabecera
                            int fila = 6;
                            worksheet.Row(fila).Style.Font.Bold = true;
                            //worksheet.Cell(fila, 1).Value = "ID Registro";
                            worksheet.Cell(fila, 1).Value = "Tipo Documento";
                            worksheet.Cell(fila, 2).Value = "Numero de Documento";
                            worksheet.Cell(fila, 3).Value = "Nombres";
                            worksheet.Cell(fila, 4).Value = "Apellidos";
                            worksheet.Cell(fila, 5).Value = "Email";
                            worksheet.Cell(fila, 6).Value = "Código Producto";
                            worksheet.Cell(fila, 7).Value = "Producto";
                            worksheet.Cell(fila, 8).Value = "Cobertura";
                            //worksheet.Cell(fila, 10).Value = "Teléfono Celular";
                            //worksheet.Cell(fila, 11).Value = "Teléfono Domicilio";
                            worksheet.Cell(fila, 9).Value = "Estado Civil";
                            worksheet.Cell(fila, 10).Value = "Fecha Nacimiento";
                            worksheet.Cell(fila, 11).Value = "Enrolamiento";
                            worksheet.Cell(fila, 12).Value = "Bloqueo";

                            for (int i = 1; i <= 12; i++)
                            {
                                worksheet.Cell(fila, i).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                                worksheet.Cell(fila, i).Style.Border.SetOutsideBorderColor(XLColor.Black);
                            }
                            fila++;

                            foreach (var item in emp)
                            {
                                //worksheet.Cell(fila, 1).Value = item.IdRegistro.ToString();
                                worksheet.Cell(fila, 1).Value = item.TipoDocumento == 1 ? "CEDULA" : "PASAPORTE";
                                worksheet.Cell(fila, 2).Value = item.NumeroDocumento;
                                worksheet.Cell(fila, 3).Value = item.Nombres;
                                worksheet.Cell(fila, 4).Value = item.Apellidos;
                                worksheet.Cell(fila, 5).Value = item.Email;
                                worksheet.Cell(fila, 6).Value = item.IdProducto;
                                worksheet.Cell(fila, 7).Value = item.NombreProducto;
                                worksheet.Cell(fila, 8).Value = item.IdCobertura;
                                //worksheet.Cell(fila, 10).Value = item.RC_Celular;
                                //worksheet.Cell(fila, 11).Value = item.RC_TelefonoDomicilio;
                                if (item.RC_EstadoCivil.HasValue)
                                    worksheet.Cell(fila, 9).Value = item.RC_EstadoCivil.ToString();
                                if (item.RC_FechaNacimiento.HasValue)
                                    worksheet.Cell(fila, 10).Value = item.RC_FechaNacimiento.Value.ToShortDateString();
                                worksheet.Cell(fila, 11).Value = item.CompletadoEnrolamiento.HasValue ? (item.CompletadoEnrolamiento.Value ? "ENROLADO" : "PENDIENTE") : "PENDIENTE";
                                worksheet.Cell(fila, 12).Value = item.BloqueadoServicio.HasValue ? (item.BloqueadoServicio.Value ? "BLOQUEADO" : "NO BLOQUEADO") : "NO BLOQUEADO";

                                for (int i = 1; i <= 12; i++)
                                {
                                    worksheet.Cell(fila, i).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                                    worksheet.Cell(fila, i).Style.Border.SetOutsideBorderColor(XLColor.Black);
                                }
                                fila++;
                            }
                            worksheet.Columns().AdjustToContents();

                            // Linea con información resumen de la data
                            worksheet.Row(fila).Style.Font.Bold = true;
                            worksheet.Cell(fila, 1).Value = "TOTAL PENDIENTES DE ENROLAMIENTO";
                            worksheet.Cell(fila, 1).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                            worksheet.Cell(fila, 1).Style.Border.SetOutsideBorderColor(XLColor.Black);
                            worksheet.Cell(fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            worksheet.Range(worksheet.Cell(fila, 1), worksheet.Cell(fila, 3)).Row(fila).Merge();

                            worksheet.Cell(fila, 4).Value = TotalPendientes.ToString();
                            worksheet.Cell(fila, 5).Value = "TOTAL TRABAJADORES ENROLADOS";
                            worksheet.Cell(fila, 5).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                            worksheet.Cell(fila, 5).Style.Border.SetOutsideBorderColor(XLColor.Black);
                            worksheet.Cell(fila, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            worksheet.Range(worksheet.Cell(fila, 5), worksheet.Cell(fila, 7)).Row(fila).Merge();

                            worksheet.Cell(fila, 8).Value = (emp.Count() - TotalPendientes).ToString();
                            worksheet.Cell(fila, 9).Value = "TOTAL TRABAJADORES";
                            worksheet.Cell(fila, 9).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                            worksheet.Cell(fila, 9).Style.Border.SetOutsideBorderColor(XLColor.Black);
                            worksheet.Cell(fila, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            worksheet.Range(worksheet.Cell(fila, 9), worksheet.Cell(fila, 11)).Row(fila).Merge();

                            worksheet.Cell(fila, 12).Value = emp.Count().ToString();

                            using (var memoryStream = new MemoryStream())
                            {
                                workbook.SaveAs(memoryStream);
                                //memoryStream.WriteTo(Response.OutputStream);
                                //Convert.ToBase64String(memoryStream.ToArray());
                                var Attach = memoryStream.ToArray();

                                // Obtener los correos de los admins de la empresa actual
                                String MailTO = "";
                                var lstAdmins = model.UsuarioAdmin_VTA.Where(x => x.IdEmpresa == IDEmpresa).ToList();
                                foreach (var item in lstAdmins)
                                {
                                    if (!String.IsNullOrEmpty(item.Email))
                                        MailTO += item.Email + ";";
                                }

                                // Envió el mail de notificación de avance de enrolamiento a los administradores
                                Dictionary<string, string> tokens = new Dictionary<string, string>();
                                //tokens.Add("TOTALEMPLEADOS", emp.Count().ToString());
                                //tokens.Add("TOTALDEPENDIENTES", TotalPendientes.ToString() + " - "
                                //    + (((Decimal)TotalPendientes / (Decimal)emp.Count()) * 100) + "%");
                                SW.Salud.DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresata = new SW.Salud.DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
                                SW.Salud.DataAccess.Sigmep._cl01_empresasRow cl01 = empresata.GetDataByEmpresaNumero(IDEmpresa).FirstOrDefault();

                                string LinkPortalContratante = ConfigurationManager.AppSettings["LinkPortalContratante"];
                                string Pastel64 = GenerarPastel("Estadística de Enrolamiento", emp.Count() - TotalPendientes, "Enrolados", TotalPendientes, "Pendientes");
                                tokens.Add("NOMBRE", cl01._razon_social);
                                tokens.Add("IMAGESRC", Pastel64);
                                tokens.Add("LINK", LinkPortalContratante);


                                var registro = model.CORP_Registro.Where(r => r.IdEmpresa == IDEmpresa).OrderByDescending(r => r.FechaCreacion).Take(1).First();
                                tokens.Add("FECHA", registro.FechaCreacion.Value.AddDays(30).ToString("dd/MM/yyyy"));

                                //string PathTemplates = System.Configuration.ConfigurationManager.AppSettings["PathTemplates"];
                                //string ContenidoMail = SW.Common.Utils.GenerarContenido(PathTemplates + "T5_ResumenEnrolamientoEmpresa.html", tokens);
                                //SW.Common.Utils.SendMail(MailTO, "", ContenidoMail, "Saludsa - Avance Carga de Información de Enrolamiento", Attach);

                                Dictionary<string, byte[]> adjuntos = new Dictionary<string, byte[]>();
                                adjuntos.Add("ListaEnrolamiento.xlsx", Attach);
                                SW.Common.Utils.SendMail(MailTO, "", SW.Common.TipoNotificacionEnum.ResumenEnrolamientoEmpresa, tokens, adjuntos);
                            }

                        }
                    }
                }
            }
        }

        public static string GenerarPastel(string Titulo, int Valor1, string Etiqueta1, int Valor2, string Etiqueta2)
        {
            //prepare chart control...
            Chart chart = new Chart();
            chart.Width = 600;
            chart.Height = 350;
            //chart.Titles.Add(Titulo);

            //create serie...
            Series serie1 = new Series();
            serie1.Name = "Serie1";
            serie1.Color = Color.FromArgb(112, 255, 200);
            serie1.BorderColor = Color.FromArgb(164, 164, 164);
            serie1.ChartType = SeriesChartType.Pie;
            serie1.BorderDashStyle = ChartDashStyle.Solid;
            serie1.BorderWidth = 1;
            serie1.ShadowColor = Color.FromArgb(128, 128, 128);
            serie1.ShadowOffset = 1;
            serie1.IsValueShownAsLabel = false;
            serie1.Font = new Font("Arial", 14);
            serie1.BackSecondaryColor = Color.FromArgb(0, 102, 153);
            serie1.LabelForeColor = Color.FromArgb(100, 100, 100);
            serie1.IsVisibleInLegend = true;

            int Total = Valor1 + Valor2;

            DataPoint p1 = new DataPoint();
            p1.LegendText = Etiqueta1;
            p1.Label = (((double)Valor1 / (double)Total) * (double)100).ToString("N2") + "%";
            p1.SetValueXY(Etiqueta1, Valor1);
            p1.Font = new Font("Arial", 14);
            p1.Color = Color.FromArgb(220, 107, 47);
            p1.LabelForeColor = Color.White;
            serie1.Points.Add(p1);

            DataPoint p2 = new DataPoint();
            p2.LegendText = Etiqueta2;
            p2.Label = (((double)Valor2 / (double)Total) * (double)100).ToString("N2") + "%";
            p2.SetValueXY(Etiqueta2, Valor2);
            p2.Font = new Font("Arial", 14);
            p2.Color = Color.FromArgb(224, 126, 60);
            p2.LabelForeColor = Color.White;
            serie1.Points.Add(p2);

            chart.Series.Add(serie1);

            //create chartareas...
            ChartArea ca = new ChartArea();

            ca.Name = "ChartArea1";
            ca.BackColor = Color.White;
            ca.BorderColor = Color.FromArgb(26, 59, 105);
            ca.BorderWidth = 0;
            ca.BorderDashStyle = ChartDashStyle.Solid;
            ca.AxisX = new Axis();
            ca.AxisY = new Axis();

            chart.ChartAreas.Add(ca);
            Legend lg = new Legend();
            lg.LegendStyle = LegendStyle.Column;
            lg.Font = new Font("Arial", 14);
            chart.Legends.Add(lg);

            MemoryStream ms = new MemoryStream();
            chart.SaveImage(ms, ChartImageFormat.Png);
            string Base64 = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());

            return Base64;
        }

        public enum EnumModoEjecucion { Normal = 1, Configurado = 2 }

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
