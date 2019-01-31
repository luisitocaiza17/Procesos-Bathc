using SW.Salud.DataAccess.SigmepTableAdapters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW.Common
{
    public static class ArchivosHelper
    {
        public static byte[] DescargaPublicidadLista(int IDEmpresa, int IDSucursal)
        {
            string Dominio = ConfigurationManager.AppSettings["DescargaArchivosPortal_Dominio"];
            string Usuario = ConfigurationManager.AppSettings["DescargaArchivosPortal_Usuario"];
            string Password = ConfigurationManager.AppSettings["DescargaArchivosPortal_Password"];
            string Path = ConfigurationManager.AppSettings["DescargaArchivosPortal_Path"];

            cl02_empresa_sucursalesTableAdapter sucursalta = new cl02_empresa_sucursalesTableAdapter();
            var sucursales = sucursalta.GetDataByEmpresaSucursal(IDEmpresa, IDSucursal);
            var sucursal = sucursales.FirstOrDefault();

            byte[] fileContent = null;

            // ImpersonationHelper.Impersonate(Dominio, Usuario, Password, delegate
            using (new NetworkConnection(Path, new System.Net.NetworkCredential(Usuario, Password, Dominio)))
            {
                // Si no existe la carpeta de la empresa
                if (!System.IO.Directory.Exists(Path + @"\" + IDEmpresa.ToString()))
                {
                    fileContent = null;
                }

                //si existe el archivo por alias
                if (sucursal != null && System.IO.File.Exists(Path + @"\" + IDEmpresa.ToString() + @"\PUBLICIDAD_" + sucursal._sucursal_alias.Trim().Replace(" ","") + ".pdf"))
                {
                    fileContent = System.IO.File.ReadAllBytes(Path + @"\" + IDEmpresa.ToString() + @"\PUBLICIDAD_" + sucursal._sucursal_alias.Trim().Replace(" ", "") + ".pdf");
                }

                // si existe el archivo por numero
                if (System.IO.File.Exists(Path + @"\" + IDEmpresa.ToString() + @"\PUBLICIDAD_" + IDSucursal.ToString() + ".pdf"))
                {
                    fileContent = System.IO.File.ReadAllBytes(Path + @"\" + IDEmpresa.ToString() + @"\PUBLICIDAD_" + IDSucursal.ToString() + ".pdf");
                }

                return fileContent;
            }
        }

        public static byte[] DescargaVentaCruzada(int IDEmpresa)
        {
            string Dominio = ConfigurationManager.AppSettings["DescargaArchivosPortal_Dominio"];
            string Usuario = ConfigurationManager.AppSettings["DescargaArchivosPortal_Usuario"];
            string Password = ConfigurationManager.AppSettings["DescargaArchivosPortal_Password"];
            string Path = ConfigurationManager.AppSettings["DescargaArchivosPortal_Path"];

            byte[] fileContent = null;

            // ImpersonationHelper.Impersonate(Dominio, Usuario, Password, delegate
            using (new NetworkConnection(Path, new System.Net.NetworkCredential(Usuario, Password, Dominio)))
            {
                // Si no existe la carpeta de la empresa
                if (!System.IO.Directory.Exists(Path + @"\" + IDEmpresa.ToString()))
                {
                    fileContent = null;
                }

                //existe un archivo de productos
                if (System.IO.File.Exists(Path + @"\" + IDEmpresa.ToString() + @"\PRODUCTOS.pdf"))
                {
                    fileContent = System.IO.File.ReadAllBytes(Path + @"\" + IDEmpresa.ToString() + @"\PRODUCTOS.pdf");
                }

                return fileContent;
            }
        }
        public static byte[] DescargaDocumentoLista(int IDEmpresa, int IDSucursal)
        {
            string Dominio = ConfigurationManager.AppSettings["DescargaArchivosPortal_Dominio"];
            string Usuario = ConfigurationManager.AppSettings["DescargaArchivosPortal_Usuario"];
            string Password = ConfigurationManager.AppSettings["DescargaArchivosPortal_Password"];
            string Path = ConfigurationManager.AppSettings["DescargaArchivosPortal_Path"];

            cl02_empresa_sucursalesTableAdapter sucursalta = new cl02_empresa_sucursalesTableAdapter();
            var sucursales = sucursalta.GetDataByEmpresaSucursal(IDEmpresa, IDSucursal);
            var sucursal = sucursales.FirstOrDefault();

            byte[] fileContent = null;

            // ImpersonationHelper.Impersonate(Dominio, Usuario, Password, delegate
            using (new NetworkConnection(Path, new System.Net.NetworkCredential(Usuario, Password, Dominio)))
            {
                // Si no existe la carpeta de la empresa
                if (!System.IO.Directory.Exists(Path + @"\" + IDEmpresa.ToString()))
                {
                    fileContent = null;
                }

                //si existe el archivo por alias
                if (sucursal != null && System.IO.File.Exists(Path + @"\" + IDEmpresa.ToString() + @"\" + sucursal._sucursal_alias + ".pdf"))
                {
                    fileContent = System.IO.File.ReadAllBytes(Path + @"\" + IDEmpresa.ToString() + @"\" + sucursal._sucursal_alias + ".pdf");
                }

                // si existe el archivo por numero
                if (System.IO.File.Exists(Path + @"\" + IDEmpresa.ToString() + @"\" + IDSucursal.ToString() + ".pdf"))
                {
                    fileContent = System.IO.File.ReadAllBytes(Path + @"\" + IDEmpresa.ToString() + @"\" + IDSucursal.ToString() + ".pdf");
                }

                return fileContent;
            }
        }
    }
}
