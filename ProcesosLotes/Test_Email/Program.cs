using SW.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_Email
{
    class Program
    {
        static void Main(string[] args)
        {
            string EmailTo = "andres.bastidas@smartwork.com.ec"; // jesus.duran@alphaside.com";
            string CC = "";

            // adjunto comun
            //string TXTContent = "0123456789012345678901234567890123456789012345678901234567890123456789\n0123456789012345678901234567890123456789012345678901234567890123456789\n0123456789012345678901234567890123456789012345678901234567890123456789\n0123456789012345678901234567890123456789012345678901234567890123456789\n0123456789012345678901234567890123456789012345678901234567890123456789\n0123456789012345678901234567890123456789012345678901234567890123456789\n0123456789012345678901234567890123456789012345678901234567890123456789\n0123456789012345678901234567890123456789012345678901234567890123456789\n0123456789012345678901234567890123456789012345678901234567890123456789\n0123456789012345678901234567890123456789012345678901234567890123456789\n";
            //Dictionary<string, byte[]> adjuntos = new Dictionary<string, byte[]>();
            //adjuntos.Add("contenido.txt", System.Text.Encoding.UTF8.GetBytes(TXTContent));

            // adjunto publicidad
            var bytes = System.IO.File.ReadAllBytes(@"D:\Desarrollo\SALUD_Corporativo2\PortalContratante\Batch\BatchLecturaArchivos\Test_Email\PUBLICIDAD_EXEQUIAL.pdf");
            Dictionary<string, byte[]> adjuntos = new Dictionary<string, byte[]>();
            adjuntos.Add("EXEQUIAL.pdf", bytes);

            #region BienvenidaTitular

            Dictionary<string, string> p4 = new Dictionary<string, string>();

            p4.Add("NOMBRE", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p4.Add("USUARIO", "1714766134");
            p4.Add("CLAVE", "FDds8932kl.");
            p4.Add("LINK", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.BienvenidaTitular, p4, adjuntos);

            #endregion

            return;


            #region BienvenidaCreacionPortalGeneral

            Dictionary<string, string> p1 = new Dictionary<string, string>();

            p1.Add("NOMBREUSUARIO", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p1.Add("USUARIO", "1714766134");
            p1.Add("CONTRASENIA", "FDds8932kl.");
            p1.Add("LINKPORTAL", "http://www.google.com");
            
            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.BienvenidaCreacionPortalGeneral, p1, adjuntos);

            #endregion

            #region BienvenidaCreacionPortalCarga

            Dictionary<string, string> p2 = new Dictionary<string, string>();

            p2.Add("NOMBREUSUARIO", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p2.Add("USUARIO", "1714766134");
            p2.Add("CONTRASENIA", "FDds8932kl.");
            p2.Add("LINKPORTAL", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.BienvenidaCreacionPortalCarga, p2, adjuntos);


            #endregion

            #region RecuperacionClave

            Dictionary<string, string> p3 = new Dictionary<string, string>();

            p3.Add("NOMBRE", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p3.Add("CLAVE", "FDds8932kl.");
            p3.Add("LINKPORTAL", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.RecuperacionClave, p3, adjuntos);

            #endregion
            
            //#region BienvenidaTitular

            //Dictionary<string, string> p4 = new Dictionary<string, string>();

            //p4.Add("NOMBRE", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            //p4.Add("USUARIO", "1714766134");
            //p4.Add("CLAVE", "FDds8932kl.");
            //p4.Add("LINK", "http://www.google.com");

            //Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.BienvenidaTitular, p4, adjuntos);

            //#endregion

            #region BienvenidaTitularExiste

            Dictionary<string, string> p5 = new Dictionary<string, string>();

            p5.Add("NOMBRE", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p5.Add("LINK", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.BienvenidaTitularExiste, p5, adjuntos);

            #endregion

            #region RecordatorioEnrolamiento

            Dictionary<string, string> p6 = new Dictionary<string, string>();

            p6.Add("NOMBRE", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p6.Add("USUARIO", "1714766134");
            p6.Add("CLAVE", "FDds8932kl.");
            p6.Add("LINK", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.RecordatorioEnrolamiento, p6, adjuntos);

            #endregion

            #region ResumenEnrolamientoEmpresa

            Dictionary<string, string> p7 = new Dictionary<string, string>();

            p7.Add("NOMBRE", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p7.Add("IMAGESRC", "data:image/png;base64,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");
            p7.Add("LINK", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.ResumenEnrolamientoEmpresa, p7, adjuntos);

            #endregion

            #region NotificacionCopagosContratante

            Dictionary<string, string> p8 = new Dictionary<string, string>();

            p8.Add("Cliente", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p8.Add("fechahasta", "12/08/2018");
            p8.Add("TiempoMaxPago", "20");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.NotificacionCopagosContratante, p8, adjuntos);

            #endregion

            #region NotificacionCopagosUsuario

            Dictionary<string, string> p9 = new Dictionary<string, string>();

            p9.Add("Cliente", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p9.Add("FechaIncurrencia", "12/08/2018");
            p9.Add("NumeroCopago", "5648465");
            p9.Add("Colaborador", "FELIX JIMENEZ");
            p9.Add("Valor", "435.30");
            p9.Add("TiempoMaxPago", "20");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.NotificacionCopagosUsuario, p9, adjuntos);

            #endregion

            #region CambioTarifaInclusionBeneficiario

            Dictionary<string, string> p10 = new Dictionary<string, string>();

            p10.Add("NOMBREUSUARIO", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p10.Add("LINK", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.CambioTarifaInclusionBeneficiario, p10, adjuntos);

            #endregion

            #region CambioTarifaExclusionBeneficiario

            Dictionary<string, string> p11 = new Dictionary<string, string>();

            p11.Add("NOMBREUSUARIO", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p11.Add("LINK", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.CambioTarifaExclusionBeneficiario, p11, adjuntos);

            #endregion

            #region CambioProducto

            Dictionary<string, string> p12 = new Dictionary<string, string>();

            p12.Add("NOMBREUSUARIO", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p12.Add("LINK", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.CambioProducto, p12, adjuntos);

            #endregion

            #region NotificacionMaternidad

            Dictionary<string, string> p13 = new Dictionary<string, string>();

            p13.Add("NOMBREUSUARIO", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p13.Add("LINK", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.NotificacionMaternidad, p13, adjuntos);

            #endregion
            
            #region NotificacionExclusionTitular

            Dictionary<string, string> p14 = new Dictionary<string, string>();

            p14.Add("Cliente", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p14.Add("Link", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.NotificacionExclusionTitular, p14, adjuntos);

            #endregion

            #region NotificacionExclusionBeneficiario

            Dictionary<string, string> p15 = new Dictionary<string, string>();

            p15.Add("Cliente", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p15.Add("Beneficiario", "ESTEBAN DAVID BASTIDAS CANDO");
            p15.Add("Link", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.NotificacionExclusionBeneficiario, p15, adjuntos);

            #endregion

            #region NotificacionFinMaternidadEnrolamiento

            Dictionary<string, string> p16 = new Dictionary<string, string>();

            p16.Add("NOMBRE", "ANDRÉS FERNANDO BASTIDAS FUERTES");
            p16.Add("LINK", "http://www.google.com");

            Utils.SendMail(EmailTo, CC, TipoNotificacionEnum.NotificacionFinMaternidadEnrolamiento, p16, adjuntos);

            #endregion

        }
    }
}
