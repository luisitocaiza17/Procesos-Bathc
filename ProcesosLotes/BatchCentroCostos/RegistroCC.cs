using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchCentroCostos
{
    public class RegistroCC
    {
        public int Row { get; set; }
        public string Cedula { get; set; }
        public InfoEnum CedulaProcesada { get; set; }
        public string Afiliacion { get; set; }
        public AfiliacionEnum AfiliacionProcesada { get; set; }
        public string Nombres { get; set; }
        public InfoEnum NombresProcesada { get; set; }
        public string Apellidos { get; set; }
        public InfoEnum ApellidosProcesada { get; set; }
        public string Tarifa { get; set; }
        public TarifasEnum TarifaProcesada { get; set; }
        public double PrecioTarifa { get; set; }
        public InfoEnum PrecioTarifaProcesada { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public InfoEnum FechaNacimientoProcesada { get; set; }
        public double Cobertura { get; set; }
        public InfoEnum CoberturaProcesada { get; set; }

        public string MsgCedula { get; set; }
        public ResColorEnum ColorCedula { get; set; }
        public string MsgAfiliacion { get; set; }
        public ResColorEnum ColorAfiliacion { get; set; }
        public string MsgNombres { get; set; }
        public ResColorEnum ColorNombres { get; set; }
        public string MsgApellidos { get; set; }
        public ResColorEnum ColorApellidos { get; set; }
        public string MsgTarifa { get; set; }
        public ResColorEnum ColorTarifa { get; set; }
        public string MsgPrecioTarifa { get; set; }
        public ResColorEnum ColorPrecioTarifa { get; set; }
        public string MsgFechaNacimiento { get; set; }
        public ResColorEnum ColorFechaNacimiento { get; set; }
        public string MsgCobertura { get; set; }
        public ResColorEnum ColorCobertura { get; set; }
        public string MsgDependiente { get; set; }
        

        public bool Analizado { get; set; }

        public RegistroCC() {
            MsgAfiliacion = "";
            MsgApellidos = "";
            MsgCedula = "";
            MsgCobertura = "";
            MsgDependiente = "";
            MsgFechaNacimiento = "";
            MsgNombres = "";
            MsgPrecioTarifa = "";
            MsgTarifa = "";
            ColorAfiliacion = ResColorEnum.Verde;
            ColorApellidos = ResColorEnum.Verde;
            ColorCedula = ResColorEnum.Verde;
            ColorCobertura = ResColorEnum.Verde;
            ColorFechaNacimiento = ResColorEnum.Verde;
            ColorNombres = ResColorEnum.Verde;
            ColorPrecioTarifa = ResColorEnum.Verde;
            ColorTarifa = ResColorEnum.Verde;
            Analizado = false;
        }
    }
    
    public enum InfoEnum
    {
        LLENO = 1,
        VACIA = 2
    }
    public enum AfiliacionEnum
    {
        TITULAR = 1,
        DEPENDIENTE = 2,
        VACIA = 3,
        INDETERMINADA = 4
    }

    public enum ResColorEnum
    {
        Verde = 1,
        Rojo = 2,
        Amarillo = 3
    }

    public enum TarifasEnum
    {
        AT = 1,
        A1 = 2,
        AF = 3,
        VACIA = 4,
        INDETERMINADA = 5
    }
}
