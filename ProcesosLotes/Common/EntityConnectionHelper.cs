using System;
using System.Linq;
using System.Configuration;
using System.Data.EntityClient;

namespace SW.Common
{
    public static class EntityConnectionHelper
    {
        public static string CreateConnectionString(Type pType)
        {
            return CreateConnectionString(pType.Name, pType.Assembly.ToString(), pType.Namespace);
        }

        public static string CreateConnectionString(string pModelName)
        {
            return CreateConnectionString(pModelName, null, null);
        }

        public static string CreateConnectionString(string pModelName, string pAssemblyName, string Namespace)
        {
            bool ProductionEnviroment = SGS.ProductionEnviroment;
            EntityConnectionStringBuilder vConnectionStringBuilder = new EntityConnectionStringBuilder();
            vConnectionStringBuilder.Provider = "System.Data.SqlClient";
            string Enviroment = ProductionEnviroment ? 
                string.IsNullOrEmpty(Namespace) ? "Production" : Namespace + "." + pModelName + ".Production" :
                string.IsNullOrEmpty(Namespace) ? "Development" : Namespace + "." + pModelName + ".Development";
            vConnectionStringBuilder.ProviderConnectionString = SGS.GetConnectionString(Enviroment);
            vConnectionStringBuilder.Metadata = CreateMetadata(pModelName, pAssemblyName);
            return vConnectionStringBuilder.ToString();
        }

        public static string CreateMetadata(string pModelName, string pAssemblyName)
        {
            if (String.IsNullOrEmpty(pAssemblyName)) pAssemblyName = "*";
            return String.Format("res://{1}/{0}.csdl|res://{1}/{0}.ssdl|res://{1}/{0}.msl", pModelName, pAssemblyName);
        }
        //public static string ConnectionString()
        //{
        //    bool ProductionEnviroment = SGS.ProductionEnviroment;
        //    return ConfigurationManager.ConnectionStrings[ProductionEnviroment ? "Production" : "Development"].ConnectionString;
        //}
    }
}
