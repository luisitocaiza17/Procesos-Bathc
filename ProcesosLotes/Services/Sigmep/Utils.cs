using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SW.Salud.Services.Sigmep
{
    public static class Utils
    {
        public static string ObtenerEstadoCivil(string estadocode)
        {
            if (!string.IsNullOrEmpty(estadocode))
            {
                if (estadocode.Equals("5"))
                    return "UNION DE HECHO";
                else if (estadocode.Equals("3"))
                    return "DIVORCIADO";
                else if (estadocode.Equals("2"))
                    return "CASADO";
                else if (estadocode.Equals("1"))
                    return "SOLTERO";
                else if (estadocode.Equals("4"))
                    return "VIUDO";
                else
                    return estadocode;
            }
            else
                return string.Empty;
        }
    }
}
