using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SW.Salud.Services;

using SW.Salud.DataAccess;
using ClosedXML.Excel;
using System.IO;

namespace BatchBloqueoEnrolamiento
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (PortalContratante model = new PortalContratante())
                {
                    int NumDiasEspera = Convert.ToInt32(ConfigurationManager.AppSettings["DiasEsperaAntesBloqueo"]);
                    DateTime FechaDesde = DateTime.Today.AddDays(-NumDiasEspera);

                    // filtra a los que tienen que notificarse, se agrupa por empresa
                    foreach (var emp in model.CORP_Registro.Where(x => x.FechaCreacion <= FechaDesde
                                                && x.Estado == (int)EnumEstadoRegistroTemporalMasivo.Incluido
                                                && (!x.CompletadoEnrolamiento.HasValue || x.CompletadoEnrolamiento == false)
                                                && (x.BloqueadoServicio.HasValue == false || x.BloqueadoServicio.Value == false)
                                                && x.IdEmpresa == 61134
                                                )
                                                .GroupBy(x => x.IdEmpresa).ToList())
                    {
                        // Obtener los correos de los admins de la empresa actual
                        int IDEmpresa = emp.Key.Value;
                        Dictionary<string, string> lstMailsAdmin = new Dictionary<string, string>();
                        var lstAdmins = model.UsuarioAdmin_VTA.Where(x => x.IdEmpresa == IDEmpresa).ToList();
                        foreach (var item in lstAdmins)
                        {
                            if (!String.IsNullOrEmpty(item.Email) && !lstMailsAdmin.ContainsKey(item.NombreApellido))
                                lstMailsAdmin.Add(item.NombreApellido, item.Email);
                        }

                        var lstBloqueados = new List<int>();
                        List<Persona> lstColaboradores = new List<Persona>();

                        //strListadoColaboradores.AppendLine("<table>");
                        //strListadoColaboradores.AppendLine("    <tr>");
                        //strListadoColaboradores.AppendLine("        <td> Identificación </td>");
                        //strListadoColaboradores.AppendLine("        <td> Nombre </td>");
                        //strListadoColaboradores.AppendLine("        <td> Email </td>");
                        //strListadoColaboradores.AppendLine("        <td> Producto </td>");
                        //strListadoColaboradores.AppendLine("        <td> Plan </td>");
                        //strListadoColaboradores.AppendLine("    </tr>");

                        foreach (var registro in model.CORP_Registro.Where(x => x.FechaCreacion <= FechaDesde
                                                && x.Estado == (int)EnumEstadoRegistroTemporalMasivo.Incluido
                                                && (!x.CompletadoEnrolamiento.HasValue || x.CompletadoEnrolamiento == false)
                                                && (x.BloqueadoServicio.HasValue == false || x.BloqueadoServicio.Value == false)
                                                && x.IdEmpresa == IDEmpresa).ToList())
                        {
                            Console.WriteLine(registro.IdRegistro);
                            if (String.IsNullOrEmpty(registro.Datos)) continue;

                            List<Inclusion> lstInclusiones = new List<Inclusion>();
                            Persona persona = null;

                            List<object> Datos = JsonConvert.DeserializeObject<List<object>>(registro.Datos);
                            if (Datos.Count > 0)
                            {
                                lstInclusiones = JsonConvert.DeserializeObject<List<Inclusion>>(Datos[0].ToString());
                                persona = JsonConvert.DeserializeObject<Persona>(Datos[1].ToString());

                                foreach (var item in lstInclusiones)
                                {
                                    // Bloquear el contrato en SIGMEP
                                    SW.Salud.Services.Sigmep.Logic sigmep = new SW.Salud.Services.Sigmep.Logic();
                                    var result = sigmep.BloquearContratoPorEnrolamiento(item.ContratoNumero, String.IsNullOrEmpty(item.Region) ? "" : item.Region,
                                                    item.TipoProducto);
                                    if (result) // Bloqueo Satisfactorio - Enviar Correo y grabar en SQL
                                    {
                                        // Grabación en la BD de SQL Server
                                        registro.BloqueadoServicio = true;

                                        // solo enviar a COR
                                        if (item.TipoProducto.ToUpper().Equals("COR"))
                                        {
                                            // Enviar notificación al titular y al administrador sobre el bloqueo automático
                                            #region Envío de notificación a los titulares
                                            //String LinkPortalUsuarios = ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                                            Dictionary<string, string> ParamValues = new Dictionary<string, string>();
                                            ParamValues.Add("NOMBRE", registro.Nombres + " " + registro.Apellidos);
                                            ParamValues.Add("USUARIO", registro.NumeroDocumento);
                                            ParamValues.Add("CLAVE", registro.RC_FechaNacimiento.HasValue ? registro.RC_FechaNacimiento.Value.ToString("ddMMyyyy") : registro.NumeroDocumento);

                                            string link = string.Empty;
                                            link += System.Configuration.ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                                            link += "Views/ActivacionUsuario.html?p=";
                                            string data = registro.IdEmpresa.ToString() + "," + registro.IdUsuario + "," + registro.IdRegistro;
                                            link += Base64Encode(data);
                                            ParamValues.Add("LINK", link);

                                            //string PathTemplates = System.Configuration.ConfigurationManager.AppSettings["PathTemplates"];
                                            //string ContenidoMail = SW.Common.Utils.GenerarContenido(PathTemplates + "T7_NotificacionBloqueoTitular.html", ParamValues);
                                            //SW.Common.Utils.SendMail(registro.Email, "", ContenidoMail, "Saludsa - Finalizaciòn de Período de Registro");

                                            //SW.Common.Utils.SendMail(registro.Email, "", SW.Common.TipoNotificacionEnum.NotificacionBloqueoTitular, ParamValues, new Dictionary<string, byte[]>());
                                            #endregion
                                        }
                                        // Agregar al contenido de la lista de bloqueados para mostrar al contratante
                                        if (!lstBloqueados.Contains(registro.IdRegistro))
                                        {
                                            lstBloqueados.Add(registro.IdRegistro);
                                            lstColaboradores.Add(new Persona()
                                            {
                                                Cedula = registro.NumeroDocumento,
                                                Apellidos = registro.Apellidos,
                                                Nombres = registro.Nombres,
                                                email = registro.Email,
                                                TipoCuenta = item.TipoProducto,
                                                NumeroCuenta = item.PlanID
                                            });
                                            //strListadoColaboradores.AppendLine("    <tr>");
                                            //strListadoColaboradores.AppendLine("        <td>");
                                            //strListadoColaboradores.AppendLine("            Identificación: " + registro.NumeroDocumento);
                                            //strListadoColaboradores.AppendLine("        </td>");
                                            //strListadoColaboradores.AppendLine("        <td>");
                                            //strListadoColaboradores.AppendLine("            Nombre: " + registro.Apellidos + " " + registro.Nombres);
                                            //strListadoColaboradores.AppendLine("        </td>");
                                            //strListadoColaboradores.AppendLine("        <td>");
                                            //strListadoColaboradores.AppendLine("            Email: " + registro.Email);
                                            //strListadoColaboradores.AppendLine("        </td>");
                                            //strListadoColaboradores.AppendLine("        <td>");
                                            //strListadoColaboradores.AppendLine("            Producto: " + item.TipoProducto);
                                            //strListadoColaboradores.AppendLine("        </td>");
                                            //strListadoColaboradores.AppendLine("        <td>");
                                            //strListadoColaboradores.AppendLine("            Plan: " + item.PlanID);
                                            //strListadoColaboradores.AppendLine("        </td>");
                                            //strListadoColaboradores.AppendLine("    </tr>");
                                        }
                                    }
                                }

                            }
                        }

                        if (lstBloqueados.Count() > 0)
                            model.SaveChanges();
                        //strListadoColaboradores.AppendLine("</table>");

                        // Enviar notificación consolidada al administrador con el listado de empleados bloqueados
                        #region Envío de notificación a los titulares
                        if (lstBloqueados.Count() > 0)
                        {
                            foreach (var dataAdmin in lstMailsAdmin)
                            {
                                // Armar el Excel
                                String pathFileTemplate = "";
                                pathFileTemplate = ConfigurationManager.AppSettings["PathTemplates"] + "PLANTILLA_NOT_BLOQUEADOS_CONTRATANTE.xlsx";

                                var workbook = new XLWorkbook(pathFileTemplate);
                                var worksheet = workbook.Worksheet(1);
                                worksheet.PageSetup.ShowGridlines = false;

                                // Generar la fila cabecera
                                int fila = 6;
                                worksheet.Row(fila).Style.Font.Bold = true;
                                worksheet.Cell(fila, 1).Value = "IDENTIFICACIÓN";
                                worksheet.Cell(fila, 2).Value = "APELLIDOS";
                                worksheet.Cell(fila, 3).Value = "NOMBRES";
                                worksheet.Cell(fila, 4).Value = "EMAIL";
                                worksheet.Cell(fila, 5).Value = "PRODUCTO";
                                worksheet.Cell(fila, 6).Value = "PLAN";
                                for (int i = 1; i <= 6; i++)
                                {
                                    worksheet.Cell(fila, i).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                                    worksheet.Cell(fila, i).Style.Border.SetOutsideBorderColor(XLColor.Black);
                                }
                                fila++;

                                foreach (var item in lstColaboradores)
                                {
                                    worksheet.Cell(fila, 1).Value = item.Cedula;
                                    worksheet.Cell(fila, 2).Value = item.Apellidos;
                                    worksheet.Cell(fila, 3).Value = item.Nombres;
                                    worksheet.Cell(fila, 4).Value = item.email;
                                    worksheet.Cell(fila, 5).Value = item.TipoCuenta;
                                    worksheet.Cell(fila, 6).Value = item.NumeroCuenta;
                                    for (int i = 1; i <= 6; i++)
                                    {
                                        worksheet.Cell(fila, i).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                                        worksheet.Cell(fila, i).Style.Border.SetOutsideBorderColor(XLColor.Black);
                                    }
                                    fila++;
                                }
                                worksheet.Columns().AdjustToContents();

                                using (var memoryStream = new MemoryStream())
                                {
                                    workbook.SaveAs(memoryStream);
                                    //memoryStream.WriteTo(Response.OutputStream);
                                    //Convert.ToBase64String(memoryStream.ToArray());
                                    var Attach = memoryStream.ToArray();

                                    String LinkPortalContratante = ConfigurationManager.AppSettings["LinkPortalContratante"];
                                    Dictionary<string, string> tokens = new Dictionary<string, string>();
                                    tokens.Add("LINK", LinkPortalContratante);
                                    //tokens.Add("NombreAdministrador", dataAdmin.Key);
                                    //tokens.Add("ListadoColaboradores", strListadoColaboradores.ToString());

                                    //string PathTemplates = System.Configuration.ConfigurationManager.AppSettings["PathTemplates"];
                                    //string ContenidoMail = SW.Common.Utils.GenerarContenido(PathTemplates + "T6_NotificacionBloqueoEmpresa.html", tokens);
                                    //SW.Common.Utils.SendMail(dataAdmin.Value, "", ContenidoMail, "Saludsa - Finalización Período de Registro", Attach);

                                    Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
                                    files.Add("Bloqueados.xlsx", Attach);
                                    //SW.Common.Utils.SendMail(dataAdmin.Value, "", SW.Common.TipoNotificacionEnum.NotificacionBloqueoEmpresa, tokens, files);
                                }
                            }
                        }
                        #endregion
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

        [Serializable()]
        public class Persona
        {
            public string Cedula;
            public string Nombres;
            public string Nombre1;
            public string Nombre2;
            public string Apellidos;
            public string Apellido1;
            public string Apellido2;
            public DateTime FechaNacimiento;
            public string Genero;
            public string CodigoCliente;
            public string TipoDocumento;
            public string EstadoCivil;
            public string BancoCodigo;
            public string Banco;
            public string NumeroCuenta;
            public string TipoCuenta;
            public string email;
            public string emailempresa;
            public string celular;
            public string provincia;
            public string ciudad;
            public string direccion;
            public int PersonaNumero;
        }

        [Serializable()]
        public class Inclusion
        {
            public int EmpresaID;
            public int SucursalID;
            public string NombreSucursal;
            public int ContratoNumero;
            public string Usuario;
            public string PlanID;
            public string Observacion;
            public DateTime FechaInclusion;
            public int PersonaNumero;
            public List<string> Resultados;
            public string Tipo;
            public string Region;
            public string TipoProducto;
            public bool CompletadoEnrolamiento;
        }

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
