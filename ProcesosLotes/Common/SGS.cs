using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;

/// <summary>

/// SiteGlobalSettings is used to "cache" settings that are used globally and frequently

/// and are stored in the applications settings section of the web.config file.

/// This was the web.config file does not have to be read from disk each time a 

/// "Site Wide Setting' needs to be accessed. 

/// When changes are made to the web.config file, the appdomain will be reset so the 

/// static constructor will be run when the application restarts and the new values will 
/// be retrieved. 

/// </summary>

public static class SGS
{
    static public decimal ModuleID { get; set; }
    static public bool ProductionEnviroment { get; set; }
    static public string GISServerImage { get; set; }
    //static public string EnsuranceConection { get; set; }
    //static public string QBECargoConection { get; set; }
    //static public string EmailFrom { get; set; }
    static public string ErrorLogPath { get; set; }
    //static public string WebSite { get; set; }
    //static public int StartNumber { get; set; }
    //static public bool ConsultarBaseRSC { get; set; }
    //static public int QBEBillingDefaultDay { get; set; }
    //static public int QBEBillingDefaultDate { get; set; }
    //static public int QBEBillingDefaultNextMonthDay { get; set; }
    //public const string Custodia = "Control de Riesgos, Inspección en Puerto, Candado Satelital.";
    //public const string CustodiaMessage = "El monto asegurado requiere control de riesgos, inspección en puerto, candado satelital";
    //public const string TipoMercaderiasMessage = "Usted no tiene asignada el tipo de mercadería seleccionada, por favor seleccione otro tipo de mercadería o comuníquese con el Administrador. ";

    static private Dictionary<string, string> _connectionStrings { get; set; }
    static public string GetConnectionString(string name)
    {
        //consulta en memoria por el connectionString
        KeyValuePair<string,string> connectionString = _connectionStrings.FirstOrDefault(p => p.Key.Equals(name));

        if (connectionString.Equals(default(KeyValuePair<string, string>)))
        {
            //Consultar en web.config
            string value = ConfigurationManager.ConnectionStrings[name].ConnectionString;
            _connectionStrings.Add(name, value);
            return value;
        }
        else
            return connectionString.Value;
    }
    ///*Ciudadaes
    // * Variable en memoria que mantendra bajo demanda las ciudades del sistema
    // */
    //static private List<CIUDAD> _ciudades { get; set; }

    ////static private DataModel.clasifDataTable Clasificadores { get; set; }
    //static public string GetCiudad(string CiudadID)
    //{
    //    //Consulto en memoria por el clasificador
    //    CIUDAD row = _ciudades.FirstOrDefault(p => p.id == CiudadID);

    //    if (row == null)
    //    {
    //        //Consulto a base de datos
    //        QBEEntities model = new QBEEntities();
    //        row = model.CIUDAD.FirstOrDefault(p => p.id == CiudadID);
    //        model.Dispose();
    //        if (row == null)
    //            return "N/D";
    //        else
    //        {
    //            _ciudades.Add(row);
    //            return row.nombre;
    //        }
    //    }
    //    else
    //        return row.nombre;
    //}

    ////Moneda
    //static private List<DataModel.CountryRow> _paises { get; set; }

    //static public DataModel.CountryRow GetCountry(int countryid)
    //{
    //    DataModel.CountryRow row = _paises.FirstOrDefault(p => p.CountryID == countryid);
    //    if (row == null)
    //    {
    //        //consultar base de datos
    //        CountryTA cta = new CountryTA();
    //        DataModel.CountryRow crow = cta.GetDataByCountryID(countryid).FirstOrDefault();
    //        if (crow == null)
    //            return null;
    //        else
    //            _paises.Add(crow);
    //        return crow;
    //    }
    //    else
    //        return row;
    //}

    static SGS()
    {
        #region decimal values
        decimal dnumber;
        if (decimal.TryParse(ConfigurationManager.AppSettings.Get("ModuleID"), out dnumber))
            ModuleID = dnumber;
        else
            ModuleID = 0;
        #endregion

        #region bool values
        bool bvalue = false;
        if (bool.TryParse(ConfigurationManager.AppSettings.Get("ProductionEnviroment"), out bvalue))
            ProductionEnviroment = bvalue;
        else
            ProductionEnviroment = false;
        #endregion

        #region Dictionary values
        _connectionStrings = new Dictionary<string, string>();
        #endregion

        #region String values
        GISServerImage = ConfigurationManager.AppSettings.Get("GISServerImage");
        ErrorLogPath = ConfigurationManager.AppSettings.Get("ErrorLogPath");
        #endregion
    }


}

