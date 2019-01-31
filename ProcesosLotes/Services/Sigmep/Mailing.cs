using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace SW.Salud.Services.Sigmep
{
    public partial class Logic
    {
        [OperationContract]
        public void GuardarMail(int EmpresaID, String Subject, String Body, List<Archivo> AttachedFiles, string From, string Username, string region)
        {
            Result eres = new Result();
            eres.Tipo = "Buzon";
            eres.Cedula = string.Empty;
            eres.Nombres = string.Empty;
            eres.Fecha = DateTime.Now;

            DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresata = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
            DataAccess.SaludCorporativoEntities context = new DataAccess.SaludCorporativoEntities();
            DataAccess.DataModelTableAdapters.MailingTA mailingta = new DataAccess.DataModelTableAdapters.MailingTA();
            DataAccess.DataModelTableAdapters.MailingAttachmentTA mailingattachmentta = new DataAccess.DataModelTableAdapters.MailingAttachmentTA();

            //Buscar info de la empresa
            DataAccess.Sigmep._cl01_empresasRow cl01 = empresata.GetDataByEmpresaNumero(EmpresaID).FirstOrDefault();
            if (string.IsNullOrEmpty(From))
                From = cl01.Is_email_rrhhNull() ? cl01.Is_email_brokerNull() ? null : cl01._email_broker : cl01._email_rrhh;

            StringBuilder body = new StringBuilder();
            string to = string.Empty;
            if (string.IsNullOrEmpty(region))
            {
                foreach (DataAccess.PostalBox p in context.PostalBox)
                {
                    to += (p.Email + ";");
                }
            }
            else
            {
                DataAccess.PostalBox p = context.PostalBox.FirstOrDefault(pt => pt.Region.ToLower() == region.ToLower());
                if (p != null)
                    to = p.Email;
                else
                {
                    foreach (DataAccess.PostalBox pr in context.PostalBox)
                    {
                        to += (pr.Email + ";");
                    }
                }
            }
            //body.AppendLine("Hemos recibido un nuevo requerimiento de la empresa " + (cl01 == null ? string.Empty : cl01._razon_social));
            //body.AppendLine("Los requerimientos recibidos son : " + Subject);
            //body.AppendLine("El detalle de los requerimientos es el siguiente:");
            body.Append(Body);


            #region guardar mensaje


            long mailid = mailingta.Ins(From, to, (cl01 == null ? string.Empty : cl01._razon_social) + " / " + Subject, body.ToString(), 666, DateTime.Now, null, 13)[0].MailingID;

            #endregion

            #region guardar adjuntos del mensaje
            //Documentos adjuntos de la poliza madre.
            foreach (Archivo dr in AttachedFiles)
            {
                mailingattachmentta.Ins(mailid,
                                        dr.Name,
                                        dr.Content,
                                        dr.ContentType);
            }

            ////15. AÑADIR EL PDF A LA TABLA
            //DataModel.endorsementDataTable endoso = endormentta.GetDataByApplicationId(ApplicationID);
            //if (endoso.Rows.Count > 0)
            //    foreach (DataModel.endorsementRow row in endoso)
            //        attachta.Ins(mail[0].MailingID, ApplicationID.ToString() + ".pdf", row.endorsementdocument, "application/pdf");

            #endregion

            //Guardar registro de Log
            Logging.Log(EmpresaID, Username, new List<object>() { EmpresaID, Subject, Body, AttachedFiles, From }, new List<Result>() { eres }, 1, "Buzon");

        }

        [OperationContract]
        public void EnviarMailUsuarioFinal(List<Services.Inclusion> inclusiones, Persona Persona)
        {
            //guardar Transaccion
            Guid transaccion = Logging.Log(inclusiones.First().EmpresaID, inclusiones.First().Usuario, new List<object>() { inclusiones, Persona }, new List<Result>() { }, -69, "Usuario Final");

            //String Body
            StringBuilder body = new StringBuilder();
            body.AppendLine("<table><tr><td>");
            body.AppendLine("Estimado " + Persona.Nombres + " " + Persona.Apellidos + ".</td></tr>");
            body.AppendLine("<tr><td>Bienvenido a Salud S.A.</td></tr>");
            body.AppendLine("<tr><td>Por favor actualice su información para disfrutar los beneficios de su producto en la siguiente link: <a href=\"http://10.10.76.12/MovimientosCorporativos/dependientes.aspx?id=" + transaccion.ToString() + "\">Actualizar Información</a></td></tr>");
            body.AppendLine("</table>");
            //Generar Mail
            GuardarMailUsuario(inclusiones.First().EmpresaID, "Bienvenido a Salud",body.ToString(), string.Empty, Persona.emailempresa, "auto", "auto");
        }


        public void GuardarMailUsuario(int EmpresaID, String Subject, String Body, string From, string To, string Username, string region)
        {
            DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresata = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
            DataAccess.SaludCorporativoEntities context = new DataAccess.SaludCorporativoEntities();
            DataAccess.DataModelTableAdapters.MailingTA mailingta = new DataAccess.DataModelTableAdapters.MailingTA();
            DataAccess.DataModelTableAdapters.MailingAttachmentTA mailingattachmentta = new DataAccess.DataModelTableAdapters.MailingAttachmentTA();

            //Buscar info de la empresa
            DataAccess.Sigmep._cl01_empresasRow cl01 = empresata.GetDataByEmpresaNumero(EmpresaID).FirstOrDefault();
            if (string.IsNullOrEmpty(From))
                From = cl01.Is_email_rrhhNull() ? cl01.Is_email_brokerNull() ? null : cl01._email_broker : cl01._email_rrhh;

            StringBuilder body = new StringBuilder();
            body.Append(Body);


            #region guardar mensaje
            long mailid = mailingta.Ins(From, To, (cl01 == null ? string.Empty : cl01._razon_social) + " / " + Subject, body.ToString(), 666, DateTime.Now, null, 13)[0].MailingID;
            #endregion

            #region guardar adjuntos del mensaje
          
            #endregion
        }

        [OperationContract]
        public void GuardarMailReporte(int EmpresaID, String Subject, String Body, string From, string To, string Username)
        {
            DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresata = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
            DataAccess.SaludCorporativoEntities context = new DataAccess.SaludCorporativoEntities();
            DataAccess.DataModelTableAdapters.MailingTA mailingta = new DataAccess.DataModelTableAdapters.MailingTA();
            

            //Buscar info de la empresa
            DataAccess.Sigmep._cl01_empresasRow cl01 = empresata.GetDataByEmpresaNumero(EmpresaID).FirstOrDefault();
            if (string.IsNullOrEmpty(From))
                To = cl01.Is_email_rrhhNull() ? cl01.Is_email_brokerNull() ? null : cl01._email_broker : cl01._email_rrhh;

            StringBuilder body = new StringBuilder();
            body.Append(Body);


            #region guardar mensaje
            long mailid = mailingta.Ins(From, To, (cl01 == null ? string.Empty : cl01._razon_social) + " / " + Subject, body.ToString(), 666, DateTime.Now, null, 13)[0].MailingID;
            #endregion
        }
    }
}
