using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SW.Common
{
    public class Utils
    {
        #region EMAIL
        public static string GenerarContenido(string TemplatePath, Dictionary<string, string> ParamValues)
        {
            // Envío al correo electrónico
            string EmailContent = System.IO.File.ReadAllText(TemplatePath);

            List<string> DetectedParams = new List<string>();
            //System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\[\[[a-zA-Z0-9\.]*\]\]", System.Text.RegularExpressions.RegexOptions.Compiled);
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\[\[[a-zA-Z0-9\.]*\]\]", System.Text.RegularExpressions.RegexOptions.Compiled);
            foreach (System.Text.RegularExpressions.Match match in regex.Matches(EmailContent))
            {
                DetectedParams.Add(match.Value);
            }

            foreach (string DetectedParam in DetectedParams)
            {
                if (ParamValues.ContainsKey(DetectedParam.Replace("[", "").Replace("]", "")))
                    EmailContent = EmailContent.Replace(DetectedParam, ParamValues[DetectedParam.Replace("[", "").Replace("]", "")]);
            }
            return EmailContent;
        }

        //public static void SendMail(string Email, string CopyTo, string Body, string Subject)
        //{
        //    string MailFrom = System.Configuration.ConfigurationManager.AppSettings["MailFrom"];
        //    string DisplayName = System.Configuration.ConfigurationManager.AppSettings["DisplayName"];
        //    SendMail(MailFrom, DisplayName, Email, CopyTo, Body, Subject);
        //}

        //public static void SendMail(string Email, string CopyTo, string Body, string Subject, byte[] attach)
        //{
        //    string MailFrom = System.Configuration.ConfigurationManager.AppSettings["MailFrom"];
        //    string DisplayName = System.Configuration.ConfigurationManager.AppSettings["DisplayName"];
        //    SendMail(MailFrom, DisplayName, Email, CopyTo, Body, Subject, attach);
        //}

        //public static void SendMail(string Email, string CopyTo, string Body, string Subject, Dictionary<string, byte[]> MultipleAttach)
        //{
        //    string MailFrom = System.Configuration.ConfigurationManager.AppSettings["MailFrom"];
        //    string DisplayName = System.Configuration.ConfigurationManager.AppSettings["DisplayName"];
        //    System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
        //    if (DisplayName == "")
        //        message.From = new System.Net.Mail.MailAddress(MailFrom);
        //    else
        //        message.From = new System.Net.Mail.MailAddress(MailFrom, DisplayName);

        //    string[] parts = Email.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        //    foreach (string part in parts)
        //        message.To.Add(new System.Net.Mail.MailAddress(part.Trim()));

        //    if (!string.IsNullOrEmpty(CopyTo))
        //    {
        //        string[] partscopy = CopyTo.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        //        foreach (string part in partscopy)
        //            message.CC.Add(new System.Net.Mail.MailAddress(part.Trim()));
        //    }
        //    message.IsBodyHtml = true;
        //    foreach (var att in MultipleAttach)
        //    {
        //        // Agrego los adjuntos enviados
        //        if (att.Value != null)
        //            message.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(att.Value), att.Key));
        //    }
        //    message.Subject = Subject;
        //    message.Body = Body;
        //    message.Priority = System.Net.Mail.MailPriority.Normal;

        //    string Host = System.Configuration.ConfigurationManager.AppSettings["HostSMTP"];
        //    int Port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["PortSMTP"]);
        //    string Username = System.Configuration.ConfigurationManager.AppSettings["CredentialsUsuarioSMTP"];
        //    string Password = System.Configuration.ConfigurationManager.AppSettings["CredentialsPassSMTP"];
        //    bool EnableSSL = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["SMTPEnableSSL"]);

        //    System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient();

        //    if (!string.IsNullOrEmpty(Username))
        //        client.Credentials = new System.Net.NetworkCredential(Username, Password);

        //    client.Host = Host;
        //    client.Port = Port;
        //    client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
        //    client.EnableSsl = EnableSSL;
        //    client.Send(message);
        //}

        //public static void SendMail(string From, string DisplayName, string Email, string CopyTo, string Body, string Subject, byte[] attach)
        //{
        //    System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
        //    if (DisplayName == "")
        //        message.From = new System.Net.Mail.MailAddress(From);
        //    else
        //        message.From = new System.Net.Mail.MailAddress(From, DisplayName);

        //    string[] parts = Email.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        //    foreach (string part in parts)
        //        message.To.Add(new System.Net.Mail.MailAddress(part.Trim()));

        //    if (!string.IsNullOrEmpty(CopyTo))
        //    {
        //        string[] partscopy = CopyTo.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        //        foreach (string part in partscopy)
        //            message.CC.Add(new System.Net.Mail.MailAddress(part.Trim()));
        //    }
        //    message.IsBodyHtml = true;
        //    //if (attach != null)
        //    //    message.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(attach), "FORMULARIOS_VINCULACION_COMERCIAL_CLIENTES.pdf"));
        //    message.Subject = Subject;
        //    message.Body = Body;
        //    message.Priority = System.Net.Mail.MailPriority.Normal;

        //    string Host = System.Configuration.ConfigurationManager.AppSettings["HostSMTP"];
        //    int Port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["PortSMTP"]);
        //    string Username = System.Configuration.ConfigurationManager.AppSettings["CredentialsUsuarioSMTP"];
        //    string Password = System.Configuration.ConfigurationManager.AppSettings["CredentialsPassSMTP"];
        //    bool EnableSSL = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["SMTPEnableSSL"]);

        //    System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient();

        //    if (!string.IsNullOrEmpty(Username))
        //        client.Credentials = new System.Net.NetworkCredential(Username, Password);

        //    client.Host = Host;
        //    client.Port = Port;
        //    client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
        //    client.EnableSsl = EnableSSL;
        //    client.Send(message);
        //}

        //public static void SendMail(string From, string DisplayName, string Email, string CopyTo, string Body, string Subject)
        //{
        //    System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
        //    if (DisplayName == "")
        //        message.From = new System.Net.Mail.MailAddress(From);
        //    else
        //        message.From = new System.Net.Mail.MailAddress(From, DisplayName);

        //    string[] parts = Email.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        //    foreach (string part in parts)
        //        message.To.Add(new System.Net.Mail.MailAddress(part.Trim()));

        //    if (!string.IsNullOrEmpty(CopyTo))
        //    {
        //        string[] partscopy = CopyTo.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        //        foreach (string part in partscopy)
        //            message.Bcc.Add(new System.Net.Mail.MailAddress(part.Trim()));
        //    }

        //    message.IsBodyHtml = true;
        //    message.Subject = Subject;
        //    message.Body = Body;
        //    message.Priority = System.Net.Mail.MailPriority.Normal;

        //    string Host = System.Configuration.ConfigurationManager.AppSettings["HostSMTP"];
        //    int Port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["PortSMTP"]);
        //    string Username = System.Configuration.ConfigurationManager.AppSettings["CredentialsUsuarioSMTP"];
        //    string Password = System.Configuration.ConfigurationManager.AppSettings["CredentialsPassSMTP"];
        //    bool EnableSSL = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["SMTPEnableSSL"]);

        //    System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient();

        //    if (!string.IsNullOrEmpty(Username))
        //        client.Credentials = new System.Net.NetworkCredential(Username, Password);

        //    client.Host = Host;
        //    client.Port = Port;
        //    client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
        //    client.EnableSsl = EnableSSL;
        //    client.Send(message);
        //}

        public static bool SendMail(string Email, string CopyTo, TipoNotificacionEnum TipoNotificacion, Dictionary<string, string> Campos, Dictionary<string, byte[]> Adjuntos)
        {
            string MailFrom = System.Configuration.ConfigurationManager.AppSettings["MailFrom"];
            string DisplayName = System.Configuration.ConfigurationManager.AppSettings["DisplayName"];
            TipoNotificacionDetalle tnd = GenerarDetalle(TipoNotificacion);

            bool UseEmailService = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["UseEmailService"]);

            if (UseEmailService)
            {
                if (!Campos.ContainsKey("subject")) Campos.Add("subject", tnd.Asunto);

                // Llenado del objeto de correo electr{onico de AT Mailing
                CorreoElectronico correo = new CorreoElectronico();

                //lectura de adjuntos
                foreach (var archivo in Adjuntos)
                {
                    if (correo.Adjuntos == null)
                        correo.Adjuntos = new List<Adjunto>();
                    if (archivo.Value != null)
                        correo.Adjuntos.Add(new Adjunto() { Nombre = archivo.Key, Contenido = Convert.ToBase64String(archivo.Value) });
                }
                correo.Asunto = tnd.Asunto;

                foreach (var campo in Campos)
                {
                    if (correo.Campos == null)
                        correo.Campos = new List<Campo>();
                    if (campo.Value != null)
                        correo.Campos.Add(new Campo() { Nombre = campo.Key, Valor = campo.Value });
                }

                correo.Contrato = "";
                correo.Cuerpo = ""; // no manda cuerpo porque está enviando plantilla
                correo.EmailOrigen = MailFrom;


                correo.EmailsCopia = new List<BuzonCorreoElectronico>(); // FALTA

                string[] parts = Email.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    if (correo.EmailsDestino == null)
                        correo.EmailsDestino = new List<BuzonCorreoElectronico>();
                    if (!string.IsNullOrEmpty(part.Trim()) && IsValidEmail(part.Trim()) && correo.EmailsDestino.Count(e => e.Direccion == part.Trim()) == 0)
                        correo.EmailsDestino.Add(new BuzonCorreoElectronico() { Nombre = part.Trim(), Direccion = part.Trim() });

                }

                if (!string.IsNullOrEmpty(CopyTo.Trim()))
                {
                    string[] CCparts = CopyTo.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string part in CCparts)
                    {
                        if (correo.EmailsCopia == null)
                            correo.EmailsCopia = new List<BuzonCorreoElectronico>();
                        if (!string.IsNullOrEmpty(part.Trim()) && IsValidEmail(part.Trim()) && correo.EmailsDestino.Count(e => e.Direccion == part.Trim()) == 0)
                            correo.EmailsCopia.Add(new BuzonCorreoElectronico() { Nombre = part.Trim(), Direccion = part.Trim() });
                    }
                }
                try
                {
                    string copy = ConfigurationManager.AppSettings["UsarCopia"];
                    if (copy.ToUpper() == "S")
                        correo.EmailsCopia.Add(new BuzonCorreoElectronico() { Nombre = "Tester", Direccion = "pruebas@smartwork.com.ec" });
                }
                catch (Exception)
                {
                }
                correo.IdAplicacion = "13"; // ejemplo enviado 13
                correo.IdPlantilla = tnd.IdPlantilla;
                correo.IdTransaccion = ""; // ejemplo enviado vacío
                correo.NombreOrigen = DisplayName;
                correo.NumeroIdentificacion = ""; // ejemplo enviado vacío
                correo.TiempoEspera = ""; // ejemplo enviado vacío


                // ENVÍO
                //Generar token 
                RestHelper.GenerarToken();

                //Enviar Email
                //Generacion del cliente a ejecutarse
                var address = ConfigurationManager.AppSettings["EmailServiceAdress"];
                address = address + "/EnviarAdjuntosPlantilla";
                var client = new RestClient(address);

                //Generacion del pedido a ejecutarse
                var request = RestHelper.GenerarPedido(Method.POST);

                //generacion de valores para el body
                JavaScriptSerializer jss = new JavaScriptSerializer();
                jss.MaxJsonLength = int.MaxValue;
                var cor = jss.Serialize(correo);
                request.AddParameter("mensaje", cor, ParameterType.RequestBody);

                //Ejecucion de pedido
                IRestResponse response = client.Execute(request);

                //reenvio del pedido
                var respuesta = jss.Deserialize<CorreoRespuesta>(response.Content);
                if (respuesta.Estado == "Error")
                    throw new Exception("Problema con envío de notificación: " + respuesta.Mensajes.Aggregate((a, b) => a + ", " + b));

                if (respuesta.Datos.Enviado)
                    return true;
            }
            else
            {
                string path = ConfigurationManager.AppSettings["PathTemplates"];
                string Body = GenerarContenido(path + tnd.NombreArchivoPlantilla, Campos);
                System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
                if (DisplayName == "")
                    message.From = new System.Net.Mail.MailAddress(MailFrom);
                else
                    message.From = new System.Net.Mail.MailAddress(MailFrom, DisplayName);

                string[] parts = Email.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                    if (!string.IsNullOrEmpty(part.Trim()) && IsValidEmail(part.Trim()) && message.To.Count(m => m.Address == part.Trim()) == 0)
                        message.To.Add(new System.Net.Mail.MailAddress(part.Trim()));

                if (!string.IsNullOrEmpty(CopyTo))
                {
                    string[] partscopy = CopyTo.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string part in partscopy)
                        if (!string.IsNullOrEmpty(part.Trim()) && IsValidEmail(part.Trim()) && message.To.Count(m => m.Address == part.Trim()) == 0)
                            message.CC.Add(new System.Net.Mail.MailAddress(part.Trim()));
                }
                try
                {
                    string copy = ConfigurationManager.AppSettings["UsarCopia"];
                    if (copy.ToUpper() == "S")
                        message.Bcc.Add(new System.Net.Mail.MailAddress("pruebas@smartwork.com.ec"));
                }
                catch (Exception)
                {
                }
                message.IsBodyHtml = true;
                foreach (var att in Adjuntos)
                {
                    // Agrego los adjuntos enviados
                    if (att.Value != null)
                        message.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(att.Value), att.Key));
                }
                message.Subject = tnd.Asunto;
                message.Body = Body;
                message.Priority = System.Net.Mail.MailPriority.Normal;

                string Host = System.Configuration.ConfigurationManager.AppSettings["HostSMTP"];
                int Port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["PortSMTP"]);
                string Username = System.Configuration.ConfigurationManager.AppSettings["CredentialsUsuarioSMTP"];
                string Password = System.Configuration.ConfigurationManager.AppSettings["CredentialsPassSMTP"];
                bool EnableSSL = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["SMTPEnableSSL"]);

                System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient();

                if (!string.IsNullOrEmpty(Username))
                    client.Credentials = new System.Net.NetworkCredential(Username, Password);

                client.Host = Host;
                client.Port = Port;
                client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                client.EnableSsl = EnableSSL;
                client.Send(message);
                return true;
            }
            return false;
        }

        public static bool SendMail(string Email, string CopyTo, string Asunto, string Body, TipoNotificacionEnum TipoNotificacion,
            Dictionary<string, byte[]> Adjuntos)
        {
            string MailFrom = ConfigurationManager.AppSettings["MailFrom"];
            string DisplayName = ConfigurationManager.AppSettings["DisplayName"];

            bool UseEmailService = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["UseEmailService"]);
            if (UseEmailService)
            {
                // Llenado del objeto de correo electrónico de AT Mailing
                CorreoElectronico correo = new CorreoElectronico();

                //lectura de adjuntos
                foreach (var archivo in Adjuntos)
                {
                    if (correo.Adjuntos == null)
                        correo.Adjuntos = new List<Adjunto>();
                    if (archivo.Value != null)
                        correo.Adjuntos.Add(new Adjunto() { Nombre = archivo.Key, Contenido = Convert.ToBase64String(archivo.Value) });
                }
                correo.Asunto = Asunto;

                //foreach (var campo in Campos)
                //{
                //    if (correo.Campos == null)
                //        correo.Campos = new List<Campo>();
                //    if (campo.Value != null)
                //        correo.Campos.Add(new Campo() { Nombre = campo.Key, Valor = campo.Value });
                //}
                correo.Campos = new List<Campo>();

                correo.Contrato = "";
                correo.Cuerpo = Body;
                correo.EmailOrigen = MailFrom;

                correo.EmailsCopia = new List<BuzonCorreoElectronico>(); // FALTA

                string[] parts = Email.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    if (correo.EmailsDestino == null)
                        correo.EmailsDestino = new List<BuzonCorreoElectronico>();
                    if (!string.IsNullOrEmpty(part.Trim()) && IsValidEmail(part.Trim()) && correo.EmailsDestino.Count(e => e.Direccion == part.Trim()) == 0)
                        correo.EmailsDestino.Add(new BuzonCorreoElectronico() { Nombre = part.Trim(), Direccion = part.Trim() });
                }

                if (!string.IsNullOrEmpty(CopyTo.Trim()))
                {
                    string[] CCparts = CopyTo.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string part in CCparts)
                    {
                        if (correo.EmailsCopia == null)
                            correo.EmailsCopia = new List<BuzonCorreoElectronico>();
                        if (!string.IsNullOrEmpty(part.Trim()) && IsValidEmail(part.Trim()) && correo.EmailsDestino.Count(e => e.Direccion == part.Trim()) == 0)
                            correo.EmailsCopia.Add(new BuzonCorreoElectronico() { Nombre = part.Trim(), Direccion = part.Trim() });
                    }
                }
                try
                {
                    string copy = ConfigurationManager.AppSettings["UsarCopia"];
                    if (copy.ToUpper() == "S")
                        correo.EmailsCopia.Add(new BuzonCorreoElectronico() { Nombre = "Tester", Direccion = "pruebas@smartwork.com.ec" });
                }
                catch (Exception ex)
                {
                }

                correo.IdAplicacion = "13"; // ejemplo enviado 13
                correo.IdPlantilla = "";
                correo.IdTransaccion = ""; // ejemplo enviado vacío
                correo.NombreOrigen = DisplayName;
                correo.NumeroIdentificacion = ""; // ejemplo enviado vacío
                correo.TiempoEspera = ""; // ejemplo enviado vacío

                // ENVÍO
                //Generar token 
                RestHelper.GenerarToken();

                //Enviar Email
                //Generacion del cliente a ejecutarse
                var address = ConfigurationManager.AppSettings["EmailServiceAdress"];
                address = address + "/EnviarAdjuntosPlantilla";
                var client = new RestClient(address);

                //Generacion del pedido a ejecutarse
                var request = RestHelper.GenerarPedido(Method.POST);

                //generacion de valores para el body
                JavaScriptSerializer jss = new JavaScriptSerializer();
                jss.MaxJsonLength = int.MaxValue;
                var cor = jss.Serialize(correo);
                request.AddParameter("mensaje", cor, ParameterType.RequestBody);

                //Ejecucion de pedido
                IRestResponse response = client.Execute(request);

                //reenvio del pedido
                var respuesta = jss.Deserialize<CorreoRespuesta>(response.Content);
                if (respuesta.Estado == "Error")
                    throw new Exception("Problema con envío de notificación: " + respuesta.Mensajes.Aggregate((a, b) => a + ", " + b));

                if (respuesta.Datos.Enviado)
                    return true;
            }
            else
            {
                //Generar token 
                RestHelper.GenerarToken();

                // Envió normal de correos
                System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
                if (DisplayName == "")
                    message.From = new System.Net.Mail.MailAddress(MailFrom);
                else
                    message.From = new System.Net.Mail.MailAddress(MailFrom, DisplayName);

                string[] parts = Email.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                    if (!string.IsNullOrEmpty(part.Trim()) && IsValidEmail(part.Trim()) && message.To.Count(m => m.Address == part.Trim()) == 0)
                        message.To.Add(new System.Net.Mail.MailAddress(part.Trim()));

                if (!string.IsNullOrEmpty(CopyTo))
                {
                    string[] partscopy = CopyTo.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string part in partscopy)
                        if (!string.IsNullOrEmpty(part.Trim()) && IsValidEmail(part.Trim()) && message.To.Count(m => m.Address == part.Trim()) == 0)
                            message.CC.Add(new System.Net.Mail.MailAddress(part.Trim()));
                }
                try
                {
                    string copy = ConfigurationManager.AppSettings["UsarCopia"];
                    if (copy.ToUpper() == "S")
                        message.Bcc.Add(new System.Net.Mail.MailAddress("pruebas@smartwork.com.ec"));
                }
                catch (Exception ex)
                {

                }

                message.IsBodyHtml = true;
                foreach (var att in Adjuntos)
                {
                    // Agrego los adjuntos enviados
                    if (att.Value != null)
                        message.Attachments.Add(new System.Net.Mail.Attachment(new MemoryStream(att.Value), att.Key));
                }
                message.Subject = Asunto;
                message.Body = Body;
                message.Priority = System.Net.Mail.MailPriority.Normal;

                string Host = System.Configuration.ConfigurationManager.AppSettings["HostSMTP"];
                int Port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["PortSMTP"]);
                string Username = System.Configuration.ConfigurationManager.AppSettings["CredentialsUsuarioSMTP"];
                string Password = System.Configuration.ConfigurationManager.AppSettings["CredentialsPassSMTP"];
                bool EnableSSL = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["SMTPEnableSSL"]);

                System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient();

                if (!string.IsNullOrEmpty(Username))
                    client.Credentials = new System.Net.NetworkCredential(Username, Password);

                try
                {
                    client.Host = Host;
                    client.Port = Port;
                    client.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    client.EnableSsl = EnableSSL;
                    client.Send(message);
                }
                catch (Exception ex)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static TipoNotificacionDetalle GenerarDetalle(TipoNotificacionEnum TipoNotificacion)
        {
            TipoNotificacionDetalle tnd = new TipoNotificacionDetalle();
            switch (TipoNotificacion)
            {

                //case TipoNotificacionEnum.BienvenidaCreacionPortal:
                //    tnd.NombreArchivoPlantilla = "T1_BienvenidaCreacionPortal.html";
                //    tnd.Asunto = "Saludsa - SmartPlan - Bienvenido";
                //    tnd.IdPlantilla = "";
                //    break;
                case TipoNotificacionEnum.BienvenidaCreacionPortalGeneral:
                    tnd.NombreArchivoPlantilla = "T1_BienvenidaCreacionPortalGeneral.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Bienvenido";
                    tnd.IdPlantilla = "1533847061902";
                    break;
                case TipoNotificacionEnum.BienvenidaCreacionPortalCarga:
                    tnd.NombreArchivoPlantilla = "T1_BienvenidaCreacionPortalCarga.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Bienvenido";
                    tnd.IdPlantilla = "1533841800650";
                    break;
                case TipoNotificacionEnum.RecuperacionClave:
                    tnd.NombreArchivoPlantilla = "T2_RecuperacionClave.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Notificación de Recuperación de Clave";
                    tnd.IdPlantilla = "1533847250327";
                    break;
                case TipoNotificacionEnum.BienvenidaTitular:
                    tnd.NombreArchivoPlantilla = "T3_BienvenidaTitular.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Carga Información Enrolamiento";
                    tnd.IdPlantilla = "1533847405530";
                    break;
                case TipoNotificacionEnum.BienvenidaTitularOpcional:
                    tnd.NombreArchivoPlantilla = "T3_BienvenidaTitularOpcional.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Carga Información Enrolamiento";
                    tnd.IdPlantilla = "";
                    break;
                case TipoNotificacionEnum.BienvenidaTitularExiste:
                    tnd.NombreArchivoPlantilla = "T3_BienvenidaTitularExiste.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Carga Información Enrolamiento";
                    tnd.IdPlantilla = "1533847610247";
                    break;
                case TipoNotificacionEnum.BienvenidaTitularExisteOpcional:
                    tnd.NombreArchivoPlantilla = "T3_BienvenidaTitularExisteOpcional.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Carga Información Enrolamiento";
                    tnd.IdPlantilla = "";
                    break;
                case TipoNotificacionEnum.RecordatorioEnrolamiento:
                    tnd.NombreArchivoPlantilla = "T4_RecordatorioEnrolamiento.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Recordatorio Carga Información de Enrolamiento";
                    tnd.IdPlantilla = "1533847772620";
                    break;
                case TipoNotificacionEnum.ResumenEnrolamientoEmpresa:
                    tnd.NombreArchivoPlantilla = "T5_ResumenEnrolamientoEmpresa.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Avance Carga de Información de Enrolamiento";
                    tnd.IdPlantilla = "1533847916974";
                    break;
                //case TipoNotificacionEnum.NotificacionBloqueoEmpresa:
                //    tnd.NombreArchivoPlantilla = "T6_NotificacionBloqueoEmpresa.html";
                //    tnd.Asunto = "Saludsa - SmartPlan - Finalización de Período de Registro Empresa";
                //    tnd.IdPlantilla = "";
                //    break;
                //case TipoNotificacionEnum.NotificacionBloqueoTitular:
                //    tnd.NombreArchivoPlantilla = "T7_NotificacionBloqueoTitular.html";
                //    tnd.Asunto = "Saludsa - SmartPlan - Finalización de Período de Registro";
                //    tnd.IdPlantilla = "";
                //    break;

                case TipoNotificacionEnum.NotificacionCopagosContratante:
                    tnd.NombreArchivoPlantilla = "T10_NotificacionCopagosContratante.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Notificación de Copagos Pendientes";
                    tnd.IdPlantilla = "1533848290640";
                    break;

                case TipoNotificacionEnum.NotificacionCopagosUsuario:
                    tnd.NombreArchivoPlantilla = "T11_NotificacionCopagosUsuario.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Notificación de Liquidación";
                    tnd.IdPlantilla = "1533848442826";
                    break;

                case TipoNotificacionEnum.CambioTarifaInclusionBeneficiario:
                    tnd.NombreArchivoPlantilla = "T12_CambioTarifaInclusionBeneficiario.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Cambio Tarifa Inclusión Beneficiario";
                    tnd.IdPlantilla = "1533848646087";
                    break;
                case TipoNotificacionEnum.CambioTarifaExclusionBeneficiario:
                    tnd.NombreArchivoPlantilla = "T13_CambioTarifaExclusionBeneficiario.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Cambio Tarifa Exclusión Beneficiario";
                    tnd.IdPlantilla = "1533848764866";
                    break;
                case TipoNotificacionEnum.CambioProducto:
                    tnd.NombreArchivoPlantilla = "T14_CambioProducto.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Cambio de Producto";
                    tnd.IdPlantilla = "1533849006137";
                    break;
                case TipoNotificacionEnum.NotificacionMaternidad:
                    tnd.NombreArchivoPlantilla = "T15_NotificacionMaternidad.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Notificación Maternidad";
                    tnd.IdPlantilla = "1533849143702";
                    break;
                case TipoNotificacionEnum.NotificacionExclusionTitular:
                    tnd.NombreArchivoPlantilla = "T16_ExclusionTitular.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Notificación Exclusión Titular";
                    tnd.IdPlantilla = "1534284605245";
                    break;
                case TipoNotificacionEnum.NotificacionExclusionBeneficiario:
                    tnd.NombreArchivoPlantilla = "T16_ExclusionBeneficiario.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Notificación Exclusión Beneficiario";
                    tnd.IdPlantilla = "1534282420157";
                    break;
                case TipoNotificacionEnum.NotificacionExclusionServicio:
                    tnd.NombreArchivoPlantilla = "T16_ExclusionServicio.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Notificación Exclusión Servicio";
                    tnd.IdPlantilla = "";
                    break;
                case TipoNotificacionEnum.NotificacionFinMaternidadEnrolamiento:
                    tnd.NombreArchivoPlantilla = "T17_NotificacionFinMaternidadEnrolamiento.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Recordatorio Enrolamiento Recién Nacido";
                    tnd.IdPlantilla = "1534285048720";
                    break;
                // TODO: Falta registrar la plantilla para que nos envien el ID
                case TipoNotificacionEnum.NotificacionRevisionPrefactura:
                    tnd.NombreArchivoPlantilla = "T18_NotificacionRevisionPrefactura.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Notificación de Pre-factura";
                    tnd.IdPlantilla = "0000000000000";
                    break;
                // TODO: Falta registrar la plantilla para que nos envien el ID
                case TipoNotificacionEnum.NotificacionBloqueadosXCopagosContratante:
                    tnd.NombreArchivoPlantilla = "T19_NotificacionBloqueoXCopagos.html";
                    tnd.Asunto = "Saludsa - SmartPlan - Notificación Bloqueados - Desbloqueados por Copagos";
                    tnd.IdPlantilla = "0000000000000";
                    break;
            }

            return tnd;
        }
        #endregion

    }

    public enum TipoNotificacionEnum
    {
        //Bienvenida con carga masiva
        //Correo de bienvenida al contratante el mismo debe direccionarse únicamente al perfil que tiene la opción de cargar el listado de suscripción por primera vez. 
        BienvenidaCreacionPortalCarga,

        //Bienvenida sin carga masiva
        //Correo de bienvenida sin carga masiva se envía al contratante únicamente a los perfiles que NO tiene la opción de cargar el listado de suscripción por primera vez. 
        BienvenidaCreacionPortalGeneral,

        //Activa tu SmartPlan
        //Una vez cargados los colaboradores les debe llegar este comunicado para la actualización de datos e inclusión de dependientes.
        BienvenidaTitular,
        BienvenidaTitularOpcional,
        BienvenidaTitularExiste,
        BienvenidaTitularExisteOpcional,

        //Recordatorio activa tu SmartPlan
        //Correo recordatorio a los colaboradores que aún no han activado su producto.
        RecordatorioEnrolamiento,

        //Cambio de tarifa Inclusión beneficiario
        //El correo se envía al colaborador que ha solicitado una inclusión de beneficiario es decir el cambio de tarifa de menor a mayor es decir del tipo de tarifa AA a A1 o de A1 a AF o AA a AF.
        //T12_CambioTarifaInclusionBeneficiario.html
        // variables: NOMBREUSUARIO, LINK
        CambioTarifaInclusionBeneficiario,

        //Cambio de tarifa Exclusión beneficiario
        //El correo se envía al colaborador que ha solicitado una exclusión de un beneficiario y haya realizado el cambio de tarifa de mayor a menor es decir del tipo de tarifa AF a A1 o de A1 a AA o AF a AA.
        //T13_CambioTarifaExclusionBeneficiario.html
        // variables: NOMBREUSUARIO, LINK
        CambioTarifaExclusionBeneficiario,

        //Cambio de producto
        //El correo se envía al colaborador cuando le realizaron un cambio de lista = producto.
        //T14_CambioProducto.html
        // variables: NOMBREUSUARIO, LINK
        CambioProducto,

        //Emisión copago
        //El correo se envía al colaborador que presenta un copago, debe extraerse la siguiente información [Fecha Incurrencia]
        //        [#copago] [Nombre Colaborador] [Nombre beneficiario] [Valor]  
        //En el campo XX se trae los días ingresados en armonix del tiempo que tiene el cliente para la cancelación de los copagos.
        //Se adjunta la liquidación y el copago generado.
        NotificacionCopagosUsuario,

        //Copago pendientes de pago
        //El correo se envía al contratante
        //El envío de esta pendientera de copagos es cada lunes, en ese caso se tiene que extraer la información que corresponda a la semana que se generaron:
        //Notificamos que se han generado los siguientes copagos correspondientes al periodo del lunes XX(número) de XXXX(mes) al domingo XX(número) de XXXX(mes) del XXXX(año).
        //En el campo XX se trae los días ingresados en armonix del tiempo que tiene el cliente para la cancelación de los copagos.
        //En el texto detallado a continuación se debe colocar el correo del ejecutivo responsable ingresado en Armonix: "Favor enviar la notificación al correo xxxxxxxxx@xxxx.com
        NotificacionCopagosContratante,

        //Estadística de activación
        //El correo se envía al contratante
        //Se debe adjuntar archivo de avance de carga de colaboradores activos/pendientes
        //En el espacio debe reflejarse el pastel de avance.
        ResumenEnrolamientoEmpresa,

        //Factura
        //El correo se envía al contratante
        //Se debe direccionar al momento de dar click en ingresar al menú de facturación.
        //En el XX se debe detallar a que mes corresponden las facturas
        //Adjuntar facturas
        // PENDIENTE FASE 2
        // ENVIARÍA CUANDO SE COMPLETE EL PROCESO DE FACTURACIÓN, AHORA CONTROLADO EN SIGMEP

        //Facturas pendientes
        //El correo se envía al contratante
        //En el XX se debe detallar a que mes corresponden las facturas
        //En el campo XX se trae los días ingresados en armonix del tiempo que tiene el cliente para la cancelación de los copagos.
        //En el texto detallado a continuación se debe colocar el correo del ejecutivo responsable ingresado en Armonix: "Favor enviar la notificación al correo xxxxxxxxx@xxxx.com
        //ACTUALMENTE LAS FACTURAS PENDIENTES SE ENVÍAN JUNTO CON LA NOTIFICACIÓN DE COPAGOS CONTRATANTE (BATCH COPAGOS PENDIENTES)

        //Notificación Maternidad
        //Se envía al colaborador
        //Se envía de acuerdo a lo acordado en flujo.
        //T15_NotificacionMaternidad.html
        // variables: NOMBREUSUARIO, LINK
        NotificacionMaternidad,

        //Próxima facturación
        //Se envía al contratante
        // PENDIENTE FASE 2
        // ENVIARÍA CONFORME LA CONFIGURACIÓN DE LA EMPRESA, SU FECHA DE FACTURACIÓN
        NotificacionRevisionPrefactura,

        //Servicio inhabilitado
        //Se envía al contratante
        //En el texto detallado a continuación se debe colocar el correo del ejecutivo responsable ingresado en Armonix: "enviar la notificación al correo xxxxxxxxx@xxxx.com
        // PENDIENTE FASE 2
        // ENVIARÍA EN FUNCIÓN DE UN BATCH QUE PERMITA INHABILITAR AUTOMÁTICAMENTE A LOS CLIENTES POR FALTA DE PAGO (AHORA ES UN PROCESO MANUAL)

        //Notificación exclusión beneficiario
        //Se envía al colaborador
        //En el campo XX se coloca el mes en cual el beneficiario va a ser excluido.
        // ESTA NOTIFICACIÒN DEBERÍA ENVIARSE CUANDO SE EXCLUYA UN DEPENDIENTE POR PASAR DEL LIMITE DE EDAD (25 AÑOS, ~70 AÑOS)
        // ESTE PROCESO NO PUEDE SER HECHO PORQUE ESTAS EXCLUSIONES AUTOMÀTICAS LAS HACE SIGMEP

        //Recuperación contraseña
        //Al usuario que requiera cambiar contraseña
        RecuperacionClave,

        //Exclusion de titular
        NotificacionExclusionTitular,
        NotificacionExclusionBeneficiario,
        NotificacionExclusionServicio,

        // AHORA YA NO USADOS:

        //BienvenidaCreacionPortal, T1
        //NotificacionBloqueoTitular, T6
        //NotificacionBloqueoEmpresa, T7
        //PrefacturacionCompleta, T8
        //CambioCobertura, T9 (cambió por el nuevo modelo de notificación en movimientos)


        // NO HAY PLANTILLA PARA:

        // EXCLUSIÓN DE TITULAR
        // EXCLUSIÓN DE SERVICIOS ADICIONALES
        // EXCLUSIÓN DE BENEFICIARIO SIN SALTO DE TARIFA
        // MATERNIDAD, ENVÍO BATCH A LAS 40 SEMANAS DE LA FECHA FUM
        NotificacionFinMaternidadEnrolamiento,

        //Bloqueo - Desbloqueo por Copagos Pendientes 
        //El correo se envía al contratante
        //El envío de esta notificación del listado de contratos bloqueados - desbloqueados por copagos pendientes - pagados debe ser diario.
        //En el texto detallado a continuación se debe colocar el correo del ejecutivo responsable ingresado en Armonix: "Favor enviar la notificación al correo xxxxxxxxx@xxxx.com
        NotificacionBloqueadosXCopagosContratante,

        // Envió de correos con asunto y body ya armados.
        // Estos mails se envian al servicio de AT Mailing
        NotificacionGeneralArmonix,
    }

    // MODELO JSON SERVICIO
    // {
    //"Campos": [
    //    {
    //      "Nombre": "string",
    //                    "Valor": "string"
    //    }
    //  ],
    //  "IdPlantilla": "string",
    //  "Adjuntos": [
    //    {
    //      "Nombre": "string",
    //      "Contenido": "string",
    //      "RutaArchivo": "string"
    //    }
    //  ],
    //  "Cuerpo": "string",
    //  "Asunto": "string",
    //  "IdAplicacion": "string",
    //  "IdTransaccion": "string",
    //  "NumeroIdentificacion": "string",
    //  "Contrato": "string",
    //  "NombreOrigen": "string",
    //  "EmailOrigen": "string",
    //  "EmailsDestino": [
    //    {
    //      "Nombre": "string",
    //      "Direccion": "string"
    //    }
    //  ],
    //  "EmailsCopia": [
    //    {
    //      "Nombre": "string",
    //      "Direccion": "string"
    //    }
    //  ],
    //  "TiempoEspera": "string"
    //}

    public class CorreoElectronico
    {
        public List<Campo> Campos;
        public string IdPlantilla;
        public List<Adjunto> Adjuntos;
        public string Cuerpo;
        public string Asunto;
        public string IdAplicacion;
        public string IdTransaccion;
        public string NumeroIdentificacion;
        public string Contrato;
        public string NombreOrigen;
        public string EmailOrigen;
        public List<BuzonCorreoElectronico> EmailsDestino;
        public List<BuzonCorreoElectronico> EmailsCopia;
        public string TiempoEspera;

    }

    public class Campo
    {
        public string Nombre;
        public string Valor;
    }



    public class Adjunto
    {
        public string Nombre;
        public string Contenido;
        public string RutaArchivo;
    }

    public class BuzonCorreoElectronico
    {
        public string Nombre;
        public string Direccion;
    }

    public class TipoNotificacionDetalle
    {
        public string Asunto;
        public string IdPlantilla;
        public string NombreArchivoPlantilla;
    }

    // MODELO JSON MENSAJE RESPUESTA
    //{
    //  "Estado": "string",
    //  "Datos": {
    //    "IdRequerimiento": "string",
    //    "Enviado": true,
    //    "Mensajes": [
    //      "string"
    //    ]
    //},
    //  "Mensajes": [
    //    "string"
    //  ]
    //}
    public class CorreoRespuesta
    {
        public string Estado;
        public CorreoRespuestaDatos Datos;
        public string[] Mensajes;
    }

    public class CorreoRespuestaDatos
    {
        public string IdRequerimiento;
        public bool Enviado;
        public string[] Mensajes;
    }
}
