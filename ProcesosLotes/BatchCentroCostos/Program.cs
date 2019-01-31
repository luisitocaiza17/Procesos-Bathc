using ClosedXML.Excel;
using SW.Salud.DataAccess;
using SW.Salud.Services.Sigmep;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchCentroCostos
{
    class Program
    {
        static void Main(string[] args)
        {
            // ESTE PROCESO BATCH DEBE EJECUTARSE PERMANENTEMENTE
            // LOS USUARIOS CARGAN ARCHIVOS PARA QUE SALUD COMPARE ENTRE LAS LISTAS QUE ELLOS DISPONEN Y LA INFORMACIÓN CARGADA EN SALUD
            // EL OBJETIVO ES GENERAR UN ARCHIVO DE REGRESO CON LA INFORMACIÓN ADECUADA
            // SE CONECTARÁ DIRECTAMENTE A PROGRESS A OBTENER LOS DATOS REQUERIDOS

            using (PortalContratante context = new PortalContratante())
            {
                var ArchivosAProcesar = context.CORP_ArchivoCentroCostos.Where(c => c.Estado == 1).ToList(); // estado PROCESANDO

                foreach (CORP_ArchivoCentroCostos item in ArchivosAProcesar)
                {

                    try
                    {
                        // Leo el archivo binario cargado por el usuario
                        // Se asume que es un Excel // validación en pantalla

                        StringBuilder observaciones = new StringBuilder();
                        observaciones.AppendLine("Archivo: " + item.IDArchivoCentroCostos.ToString() + " Inicio:" + DateTime.Now.ToString());
                        Console.WriteLine(DateTime.Now.ToString());

                        #region Verificación del archivo tiene contenido
                        if (item.ContenidoCargado == null)
                        {
                            observaciones.AppendLine("Archivo cargado sin contenido");
                            observaciones.AppendLine("Archivo: " + item.IDArchivoCentroCostos.ToString() + " Fin:" + DateTime.Now.ToString());
                            item.Estado = 3; // ERROR
                            item.MensajeError = observaciones.ToString();
                            context.SaveChanges();
                            continue;
                        }
                        #endregion

                        //Lectura de archivo
                        MemoryStream ms = new MemoryStream(item.ContenidoCargado);
                        XLWorkbook workbook = new XLWorkbook(ms);
                        var sheet = workbook.Worksheet(1); // siempre debe ser la primera hoja

                        bool ErrorFormato = false;

                        StringBuilder Message = new StringBuilder();

                        // valido que el archivo tenga las cabeceras requeridas
                        #region validacion de cabeceras / validaciòn de formato
                        int FirstColumn = 1;
                        int FirstRow = 1;

                        if (sheet.Cell(FirstRow, FirstColumn).GetString() != "CEDULA" && sheet.Cell(FirstRow, FirstColumn).GetString() != "CÉDULA" && sheet.Cell(FirstRow, FirstColumn).GetString() != "CÉDULA/PASAPORTE" && sheet.Cell(FirstRow, FirstColumn).GetString() != "CEDULA/PASAPORTE")
                        {
                            ErrorFormato = true;
                            Message.AppendLine("La tabla debería tener la cabecera CEDULA/PASAPORTE");
                        }
                        if (sheet.Cell(FirstRow, FirstColumn + 1).GetString() != "AFILIACIÓN" && sheet.Cell(FirstRow, FirstColumn + 1).GetString() != "AFILIACION")
                        {
                            ErrorFormato = true;
                            Message.AppendLine("La tabla debería tener la cabecera AFILIACIÓN");
                        }
                        if (sheet.Cell(FirstRow, FirstColumn + 2).GetString() != "NOMBRES")
                        {
                            ErrorFormato = true;
                            Message.AppendLine("La tabla debería tener la cabecera NOMBRES");
                        }
                        if (sheet.Cell(FirstRow, FirstColumn + 3).GetString() != "APELLIDOS")
                        {
                            ErrorFormato = true;
                            Message.AppendLine("La tabla debería tener la cabecera APELLIDOS");
                        }
                        if (sheet.Cell(FirstRow, FirstColumn + 4).GetString() != "TIPO TARIFA")
                        {
                            ErrorFormato = true;
                            Message.AppendLine("La tabla debería tener la cabecera TIPO TARIFA");
                        }
                        if (sheet.Cell(FirstRow, FirstColumn + 5).GetString() != "VALOR TARIFA")
                        {
                            ErrorFormato = true;
                            Message.AppendLine("La tabla debería tener la cabecera VALOR TARIFA");
                        }
                        if (sheet.Cell(FirstRow, FirstColumn + 6).GetString() != "FECHA DE NACIMIENTO")
                        {
                            ErrorFormato = true;
                            Message.AppendLine("La tabla debería tener la cabecera FECHA DE NACIMIENTO");
                        }
                        if (sheet.Cell(FirstRow, FirstColumn + 7).GetString() != "COBERTURA" && sheet.Cell(FirstRow, FirstColumn + 7).GetString() != "MONTO MÁXIMO DE COBERTURA" && sheet.Cell(FirstRow, FirstColumn + 7).GetString() != "MONTO MAXIMO DE COBERTURA")
                        {
                            ErrorFormato = true;
                            Message.AppendLine("La tabla debería tener la cabecera MONTO MÁXIMO DE COBERTURA");
                        }
                        if (ErrorFormato)
                        {
                            observaciones.Append(Message);
                            item.Estado = 3; // ERROR DE FORMATO
                            item.MensajeError = observaciones.ToString();
                            context.SaveChanges();
                            continue;
                        }
                        #endregion

                        var cell = sheet.Cell(FirstRow + 1, FirstColumn);
                        int CurrentRow = FirstRow + 1;
                        List<RegistroCC> registros = new List<RegistroCC>();

                        #region Lectura y corrección de datos de Excel a objetos
                        while (!cell.IsEmpty())
                        {
                            Message = new StringBuilder();
                            Console.WriteLine(DateTime.Now.ToString());
                            RegistroCC registro = new RegistroCC();

                            registro.Row = CurrentRow;

                            // Lleno el objeto con los datos leídos desde el archivo
                            try
                            {
                                registro.Cedula = sheet.Cell(CurrentRow, FirstColumn).GetString();
                            }
                            catch (Exception ex)
                            {
                                registro.MsgCedula = ex.Message + "|";
                            }
                            try
                            {
                                registro.Afiliacion = sheet.Cell(CurrentRow, FirstColumn + 1).GetString();
                                if(registro.Afiliacion == null || registro.Afiliacion.Trim() == "")
                                {
                                    throw new System.ArgumentException("Parametro Relación no puede estar vacío", "original");
                                }
                            }
                            catch (Exception ex)
                            {
                                registro.MsgAfiliacion = ex.Message + "|";
                            }
                            try
                            {
                                registro.Nombres = sheet.Cell(CurrentRow, FirstColumn + 2).GetString();
                            }
                            catch (Exception ex)
                            {
                                registro.MsgNombres = ex.Message + "|";
                            }
                            try
                            {
                                registro.Apellidos = sheet.Cell(CurrentRow, FirstColumn + 3).GetString();
                            }
                            catch (Exception ex)
                            {
                                registro.MsgApellidos = ex.Message + "|";
                            }
                            try
                            {
                                registro.Tarifa = sheet.Cell(CurrentRow, FirstColumn + 4).GetString();
                            }
                            catch (Exception ex)
                            {
                                registro.MsgTarifa = ex.Message + "|";
                            }
                            try
                            {
                                registro.PrecioTarifa = sheet.Cell(CurrentRow, FirstColumn + 5).GetDouble();
                            }
                            catch (Exception ex)
                            {
                                registro.MsgPrecioTarifa = ex.Message + "|";
                            }
                            try
                            {
                                registro.FechaNacimiento = sheet.Cell(CurrentRow, FirstColumn + 6).GetDateTime();
                            }
                            catch (Exception ex)
                            {
                                registro.MsgFechaNacimiento = ex.Message + "|";
                            }
                            try
                            {
                                registro.Cobertura = sheet.Cell(CurrentRow, FirstColumn + 7).GetDouble();
                            }
                            catch (Exception ex)
                            {
                                registro.MsgCobertura = ex.Message + "|";
                            }


                            // Validaciones de estructura (datos esperados en columnas determinadas)

                            LlenarAfiliacion(registro);
                            if (registro.AfiliacionProcesada == AfiliacionEnum.INDETERMINADA)
                            {
                                registro.MsgAfiliacion += "INDETERMINADA: No se reconoce si es AFILIADO o DEPENDIENTE|";
                            }

                            if(registro.AfiliacionProcesada == AfiliacionEnum.VACIA)
                            {
                                throw new System.ArgumentException("No se puede procesar los parametros de la columna relación, no puden estar vacíos", "original");
                            }

                            LlenarTarifa(registro);
                            if (registro.TarifaProcesada == TarifasEnum.INDETERMINADA)
                            {
                                registro.MsgTarifa += "INDETERMINADA: No se reconoce la tarifa, debe ser AT, A1 o AF|";
                            }

                            registro.CedulaProcesada = registro.Cedula != "" ? InfoEnum.LLENO : InfoEnum.VACIA;
                            registro.ColorCedula = registro.Cedula != "" ? ResColorEnum.Verde : ResColorEnum.Amarillo;

                            registro.NombresProcesada = registro.Nombres != "" ? InfoEnum.LLENO : InfoEnum.VACIA;
                            registro.ColorNombres = registro.Nombres != "" ? ResColorEnum.Verde : ResColorEnum.Amarillo;

                            registro.ApellidosProcesada = registro.Apellidos != "" ? InfoEnum.LLENO : InfoEnum.VACIA;
                            registro.ColorApellidos = registro.Apellidos != "" ? ResColorEnum.Verde : ResColorEnum.Amarillo;

                            registro.PrecioTarifaProcesada = registro.PrecioTarifa > 0 ? InfoEnum.LLENO : InfoEnum.VACIA;
                            registro.ColorPrecioTarifa = registro.PrecioTarifa > 0 ? ResColorEnum.Verde : ResColorEnum.Amarillo;

                            registro.FechaNacimientoProcesada = registro.FechaNacimiento != default(DateTime) ? InfoEnum.LLENO : InfoEnum.VACIA;
                            registro.ColorFechaNacimiento = registro.FechaNacimiento != default(DateTime) ? ResColorEnum.Verde : ResColorEnum.Amarillo;

                            registro.CoberturaProcesada = registro.Cobertura > 0 ? InfoEnum.LLENO : InfoEnum.VACIA;
                            registro.ColorCobertura = registro.Cobertura > 0 ? ResColorEnum.Verde : ResColorEnum.Amarillo;


                            // Corrección de cédulas
                            if (registro.Cedula.Length == 9 && Validate_Identification("0" + registro.Cedula) == "")
                                registro.Cedula = "0" + registro.Cedula;

                            if (registro.Cedula.Length == 11 && Validate_Identification(registro.Cedula.Replace("'", "")) == "")
                                registro.Cedula = registro.Cedula.Replace("'", "");

                            registros.Add(registro);

                            CurrentRow++;
                            cell = sheet.Cell(CurrentRow, FirstColumn);
                        }
                        #endregion

                        #region Lógica de Comparación

                        // si es que hay referido algún dependiente, entonces también compara dependientes, sino solamente los titulares
                        bool RevisaDependientes = registros.Count(r => r.AfiliacionProcesada == AfiliacionEnum.DEPENDIENTE) > 0;

                        // Lógica de comparación
                        var logic = new Logic();

                        // Diferencia de Dependientes
                        List<SW.Salud.DataAccess.SigmepPortalCorp.BeneficiariosRow> difDependientes = new List<SW.Salud.DataAccess.SigmepPortalCorp.BeneficiariosRow>();

                        // Traigo toda la data de SIGMEP que hace el cruce de sucursal, beneficiario, persona, para toda la empresa
                        SW.Salud.DataAccess.SigmepPortalCorp.BeneficiariosDataTable dtData = logic.ObtenerTitularesYBeneficiariosPorEmpresa(item.IDEmpresa);

                        // Hago la lógica de comparación
                        foreach (RegistroCC registro in registros.Where(r => r.AfiliacionProcesada == AfiliacionEnum.TITULAR).OrderBy(r => r.Row))
                        {
                            registro.Analizado = true;

                            // no puede comparar si no tiene la cédula llena
                            if (registro.CedulaProcesada == InfoEnum.VACIA)
                            {
                                registro.MsgCedula += "NO ENCONTRADO|";

                                registro.AfiliacionProcesada = AfiliacionEnum.VACIA;
                                registro.ApellidosProcesada = InfoEnum.VACIA;
                                registro.CoberturaProcesada = InfoEnum.VACIA;
                                registro.FechaNacimientoProcesada = InfoEnum.VACIA;
                                registro.NombresProcesada = InfoEnum.VACIA;
                                registro.PrecioTarifaProcesada = InfoEnum.VACIA;
                                registro.TarifaProcesada = TarifasEnum.VACIA;

                                registro.ColorAfiliacion = ResColorEnum.Rojo;
                                registro.ColorApellidos = ResColorEnum.Rojo;
                                registro.ColorCedula = ResColorEnum.Rojo;
                                registro.ColorCobertura = ResColorEnum.Rojo;
                                registro.ColorFechaNacimiento = ResColorEnum.Rojo;
                                registro.ColorNombres = ResColorEnum.Rojo;
                                registro.ColorPrecioTarifa = ResColorEnum.Rojo;
                                registro.ColorTarifa = ResColorEnum.Rojo;

                                continue;
                            }

                            // no puede comparar si no la  afiliacion llena
                            if (registro.AfiliacionProcesada == AfiliacionEnum.VACIA)
                            {
                                registro.MsgAfiliacion += "VACÍA|";

                                registro.AfiliacionProcesada = AfiliacionEnum.VACIA;
                                registro.ApellidosProcesada = InfoEnum.VACIA;
                                registro.CoberturaProcesada = InfoEnum.VACIA;
                                registro.FechaNacimientoProcesada = InfoEnum.VACIA;
                                registro.NombresProcesada = InfoEnum.VACIA;
                                registro.PrecioTarifaProcesada = InfoEnum.VACIA;
                                registro.TarifaProcesada = TarifasEnum.VACIA;

                                registro.ColorAfiliacion = ResColorEnum.Amarillo;
                                registro.ColorApellidos = ResColorEnum.Amarillo;
                                registro.ColorCedula = ResColorEnum.Amarillo;
                                registro.ColorCobertura = ResColorEnum.Amarillo;
                                registro.ColorFechaNacimiento = ResColorEnum.Amarillo;
                                registro.ColorNombres = ResColorEnum.Amarillo;
                                registro.ColorPrecioTarifa = ResColorEnum.Amarillo;
                                registro.ColorTarifa = ResColorEnum.Amarillo;

                                continue;
                            }

                            // busco al titular en función de su identificación
                            var lstCoincidentes = dtData.Where(d => d._persona_cedula.ToLower().Trim() == registro.Cedula.ToLower().Trim() || d._persona_pasaporte.ToLower().Trim() == registro.Cedula.ToLower().Trim());

                            SW.Salud.DataAccess.SigmepPortalCorp.BeneficiariosRow titularSIGMEP = null;


                            // no encuentra en la lista al titular
                            if (lstCoincidentes.Count() == 0)
                            {
                                registro.MsgCedula += "NO ENCONTRADO|";
                                // no puede seguir con el resto de la comparación si no encuentra al titular

                                registro.AfiliacionProcesada = AfiliacionEnum.VACIA;
                                registro.ApellidosProcesada = InfoEnum.VACIA;
                                registro.CoberturaProcesada = InfoEnum.VACIA;
                                registro.FechaNacimientoProcesada = InfoEnum.VACIA;
                                registro.NombresProcesada = InfoEnum.VACIA;
                                registro.PrecioTarifaProcesada = InfoEnum.VACIA;
                                registro.TarifaProcesada = TarifasEnum.VACIA;

                                registro.ColorAfiliacion = ResColorEnum.Amarillo;
                                registro.ColorApellidos = ResColorEnum.Amarillo;
                                registro.ColorCedula = ResColorEnum.Amarillo;
                                registro.ColorCobertura = ResColorEnum.Amarillo;
                                registro.ColorFechaNacimiento = ResColorEnum.Amarillo;
                                registro.ColorNombres = ResColorEnum.Amarillo;
                                registro.ColorPrecioTarifa = ResColorEnum.Amarillo;
                                registro.ColorTarifa = ResColorEnum.Amarillo;


                                continue;
                            }
                            else if (lstCoincidentes.Count() == 1)
                            {
                                // el titular debe estar en la lista COR y estar marcado como titular
                                titularSIGMEP = lstCoincidentes.FirstOrDefault(d => d._codigo_producto == "COR" && d.titular == true);

                                if (titularSIGMEP != null)
                                {
                                    registro.MsgCedula += "ENCONTRADO|";
                                    registro.ColorCedula = ResColorEnum.Verde;
                                }

                            }
                            else // se encuentra pero está en más de una lista.
                            {
                                // el titular debe estar en la lista COR y estar marcado como titular
                                titularSIGMEP = lstCoincidentes.FirstOrDefault(d => d._codigo_producto == "COR" && d.titular == true);

                                if (titularSIGMEP != null)
                                {
                                    registro.MsgCedula += "ENCONTRADO|";
                                    registro.ColorCedula = ResColorEnum.Verde;
                                }
                            }

                            if (titularSIGMEP == null)
                            {
                                registro.MsgCedula += "NO ENCONTRADO|";

                                registro.AfiliacionProcesada = AfiliacionEnum.VACIA;
                                registro.ApellidosProcesada = InfoEnum.VACIA;
                                registro.CoberturaProcesada = InfoEnum.VACIA;
                                registro.FechaNacimientoProcesada = InfoEnum.VACIA;
                                registro.NombresProcesada = InfoEnum.VACIA;
                                registro.PrecioTarifaProcesada = InfoEnum.VACIA;
                                registro.TarifaProcesada = TarifasEnum.VACIA;

                                registro.ColorAfiliacion = ResColorEnum.Amarillo;
                                registro.ColorApellidos = ResColorEnum.Amarillo;
                                registro.ColorCedula = ResColorEnum.Amarillo;
                                registro.ColorCobertura = ResColorEnum.Amarillo;
                                registro.ColorFechaNacimiento = ResColorEnum.Amarillo;
                                registro.ColorNombres = ResColorEnum.Amarillo;
                                registro.ColorPrecioTarifa = ResColorEnum.Amarillo;
                                registro.ColorTarifa = ResColorEnum.Amarillo;

                                continue;
                            }

                            // ANÁLISIS NOMBRES
                            if (registro.NombresProcesada != InfoEnum.VACIA)
                            {
                                if (titularSIGMEP.Is_persona_nombresNull() == false)
                                {
                                    if (registro.Nombres.ToLower().Trim().Equals(titularSIGMEP._persona_nombres.ToLower().Trim(), StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        registro.MsgNombres += "COINCIDE|";
                                        registro.ColorNombres = ResColorEnum.Verde;
                                    }
                                    else
                                    {
                                        registro.MsgNombres += "NO COINCIDE, ES " + titularSIGMEP._persona_nombres + "|";
                                        registro.ColorNombres = ResColorEnum.Rojo;
                                    }
                                }
                                else
                                {
                                    // Si el dato en sigmep es null, no puedo comparar
                                    registro.MsgNombres += "NO ES POSIBLE REALIZAR LA COMPARACIÓN|";
                                    registro.ColorNombres = ResColorEnum.Amarillo;
                                }
                            }
                            // ANÁLISIS APELLIDOS
                            if (registro.ApellidosProcesada != InfoEnum.VACIA)
                            {
                                if (titularSIGMEP.Is_persona_apellidosNull() == false)
                                {
                                    if (registro.Apellidos.ToLower().Trim().Equals(titularSIGMEP._persona_apellidos.ToLower().Trim(), StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        registro.MsgApellidos += "COINCIDE|";
                                        registro.ColorApellidos = ResColorEnum.Verde;
                                    }
                                    else
                                    {
                                        registro.MsgApellidos += "NO COINCIDE, ES " + titularSIGMEP._persona_apellidos + "|";
                                        registro.ColorApellidos = ResColorEnum.Rojo;
                                    }
                                }
                                else
                                {
                                    // Si el dato en sigmep es null, no puedo comparar
                                    registro.MsgApellidos += "NO ES POSIBLE REALIZAR LA COMPARACIÓN|";
                                    registro.ColorApellidos = ResColorEnum.Amarillo;
                                }
                            }

                            // ANÁLISIS DE COINCIDENCIA DE PLAN
                            if (registro.TarifaProcesada != TarifasEnum.VACIA && registro.TarifaProcesada != TarifasEnum.INDETERMINADA)
                            {
                                if (titularSIGMEP.Is_codigo_planNull() == false)
                                {
                                    if (registro.TarifaProcesada == TarifasEnum.A1 && titularSIGMEP._codigo_plan.Substring(0, 2).ToUpper() == "A1")
                                    {
                                        registro.MsgTarifa += "COINCIDE|";
                                        registro.ColorTarifa = ResColorEnum.Verde;
                                    }
                                    else if (registro.TarifaProcesada == TarifasEnum.AF && titularSIGMEP._codigo_plan.Substring(0, 2).ToUpper() == "AF")
                                    {
                                        registro.MsgTarifa += "COINCIDE|";
                                        registro.ColorTarifa = ResColorEnum.Verde;
                                    }
                                    else if (registro.TarifaProcesada == TarifasEnum.AT && titularSIGMEP._codigo_plan.Substring(0, 2).ToUpper() == "AT")
                                    {
                                        registro.MsgTarifa += "COINCIDE|";
                                        registro.ColorTarifa = ResColorEnum.Verde;
                                    }
                                    else
                                    {
                                        registro.MsgTarifa += "NO COINCIDE, ES " + titularSIGMEP._codigo_plan.Substring(0, 2) + "|";
                                        registro.ColorTarifa = ResColorEnum.Rojo;
                                    }
                                }
                                else
                                {
                                    // Si el dato en sigmep es null, no puedo comparar
                                    registro.MsgTarifa += "NO ES POSIBLE REALIZAR LA COMPARACIÓN|";
                                    registro.ColorTarifa = ResColorEnum.Amarillo;
                                }
                            }


                            if (registro.PrecioTarifaProcesada != InfoEnum.VACIA)
                            {
                                if (titularSIGMEP.Is_precio_baseNull() == false)
                                {
                                    // Si el valor absolutro de la resta entre valores es de menos o igual a 1 dólar, se puede entender que es el mismo
                                    if (Math.Abs((decimal)registro.PrecioTarifa - titularSIGMEP._precio_base) <= 1)
                                    {
                                        registro.MsgPrecioTarifa += "COINCIDE|";
                                        registro.ColorPrecioTarifa = ResColorEnum.Verde;
                                    }
                                    else
                                    {
                                        registro.MsgPrecioTarifa += "NO COINCIDE, es " + titularSIGMEP._precio_base.ToString() + "|";
                                        registro.ColorPrecioTarifa = ResColorEnum.Rojo;
                                    }
                                }
                                else
                                {
                                    // Si el dato en sigmep es null, no puedo comparar
                                    registro.MsgPrecioTarifa += "NO ES POSIBLE REALIZAR LA COMPARACIÓN|";
                                    registro.ColorPrecioTarifa = ResColorEnum.Amarillo;
                                }
                            }

                            if (registro.FechaNacimientoProcesada != InfoEnum.VACIA)
                            {
                                if (titularSIGMEP.Is_persona_fecha_nacimientoNull() == false)
                                {
                                    if (registro.FechaNacimiento.Date == titularSIGMEP._persona_fecha_nacimiento.Date)
                                    {
                                        registro.MsgFechaNacimiento += "COINCIDE|";
                                        registro.ColorFechaNacimiento = ResColorEnum.Verde;
                                    }
                                    else
                                    {
                                        registro.MsgFechaNacimiento += "NO COINCIDE, ES " + titularSIGMEP._persona_fecha_nacimiento.ToString("dd/MM/yyyy") + "|";
                                        registro.ColorFechaNacimiento = ResColorEnum.Rojo;
                                    }
                                }
                                else
                                {
                                    // Si el dato en sigmep es null, no puedo comparar
                                    registro.MsgFechaNacimiento += "NO ES POSIBLE REALIZAR LA COMPARACIÓN|";
                                    registro.ColorFechaNacimiento = ResColorEnum.Amarillo;
                                }
                            }

                            // COMPARACIÓN DE COBERTURA
                            // NOTA: ESTA COMPARACIÓN PUEDE TENER PROBLEMAS PORQUE COMPARA CON EL NOMBRE DE LA LISTA (MISMO PROBLEMA QUE SE TENÍA EN LA CARGA DE ARCHIVOS)

                            if (registro.CoberturaProcesada != InfoEnum.VACIA)
                            {
                                if (titularSIGMEP.Is_sucursal_nombreNull() == false)
                                {
                                    string nomCob = titularSIGMEP._sucursal_nombre.Replace(" ", "").Replace(",", "").Replace(".", "").Replace("$", "");
                                    if (nomCob.Contains(((int)registro.Cobertura).ToString()))
                                    {
                                        registro.MsgCobertura += "COINCIDE|";
                                        registro.ColorCobertura = ResColorEnum.Verde;
                                    }
                                    else
                                    {
                                        registro.MsgCobertura += "NO COINCIDE, ES " + titularSIGMEP._sucursal_nombre + "|";
                                        registro.ColorCobertura = ResColorEnum.Rojo;
                                    }
                                }
                                else
                                {
                                    // Si el dato en sigmep es null, no puedo comparar
                                    registro.MsgCobertura += "NO ES POSIBLE REALIZAR LA COMPARACIÓN|";
                                    registro.ColorCobertura = ResColorEnum.Amarillo;
                                }
                            }

                            if (RevisaDependientes)
                            {
                                List<RegistroCC> dependientes = new List<RegistroCC>();

                                bool EncontroTitular = false;

                                // PROCESAMIENTO DE DEPENDIENTES DEL ARCHIVO CARGADO
                                foreach (var r in registros.OrderBy(r => r.Row))
                                {
                                    // busco el titular
                                    if (EncontroTitular == false && r.Row == registro.Row)
                                    {
                                        EncontroTitular = true;
                                        // desde aquì busca los dependientes contiguos
                                        continue;
                                    }
                                    if (EncontroTitular && r.AfiliacionProcesada == AfiliacionEnum.TITULAR ||
                                        EncontroTitular && r.AfiliacionProcesada == AfiliacionEnum.VACIA ||
                                        EncontroTitular && r.AfiliacionProcesada == AfiliacionEnum.INDETERMINADA)
                                    {
                                        break;
                                    }
                                    // 
                                    if (EncontroTitular && r.AfiliacionProcesada == AfiliacionEnum.DEPENDIENTE)
                                    {
                                        // AGREGO AL DEPENDIENTE
                                        dependientes.Add(r);
                                    }

                                }

                                // PROCESAMIENTO DE DEPENDIENTES DE LA BASE PROGRESS
                                var dependientesSIGMEP = dtData
                                    .Where(r => r._contrato_persona == titularSIGMEP._beneficiario_persona &&
                                    r.titular == false &&
                                    r._codigo_producto == "COR").ToList();

                                // LOGICA DE COMPARACIÓN
                                foreach (RegistroCC depA in dependientes)
                                {
                                    // si no encuentra un dependiente en SIGMEP, que sí está en el archivo
                                    var comp = dependientesSIGMEP.Count(d => d._persona_cedula.ToLower() == depA.Cedula.ToLower() || d._persona_pasaporte.ToLower() == depA.Cedula.ToLower());
                                    if (comp == 0)
                                    {
                                        depA.MsgDependiente += "NO SE ENCUENTRA REGISTRADO|";
                                        //depA.AfiliacionProcesada = AfiliacionEnum.VACIA;
                                        depA.ApellidosProcesada = InfoEnum.VACIA;
                                        depA.CoberturaProcesada = InfoEnum.VACIA;
                                        depA.FechaNacimientoProcesada = InfoEnum.VACIA;
                                        depA.NombresProcesada = InfoEnum.VACIA;
                                        depA.PrecioTarifaProcesada = InfoEnum.VACIA;
                                        depA.TarifaProcesada = TarifasEnum.VACIA;

                                        depA.ColorAfiliacion = ResColorEnum.Amarillo;
                                        depA.ColorApellidos = ResColorEnum.Amarillo;
                                        depA.ColorCedula = ResColorEnum.Amarillo;
                                        depA.ColorCobertura = ResColorEnum.Amarillo;
                                        depA.ColorFechaNacimiento = ResColorEnum.Amarillo;
                                        depA.ColorNombres = ResColorEnum.Amarillo;
                                        depA.ColorPrecioTarifa = ResColorEnum.Amarillo;
                                        depA.ColorTarifa = ResColorEnum.Amarillo;
                                    }
                                    else
                                    {
                                        depA.MsgDependiente += "SÍ SE ENCUENTRA REGISTRADO|";
                                    }
                                }

                                foreach (var depS in dependientesSIGMEP)
                                {
                                    // si no encuentra un dependiente en el archivo, que en SIGME sí está
                                    var comp = dependientes.Count(d => d.Cedula.ToLower() == depS._persona_cedula.ToLower() || d.Cedula.ToLower() == depS._persona_pasaporte.ToLower());
                                    if (comp == 0)
                                    {
                                        difDependientes.Add(depS);
                                    }
                                }

                            }

                        }

                        List<SW.Salud.DataAccess.SigmepPortalCorp.BeneficiariosRow> difTitulares = new List<SW.Salud.DataAccess.SigmepPortalCorp.BeneficiariosRow>();

                        // Comparo todos los titulares de SIGMEP, para ver si vinieron en el archivo
                        foreach (var titularSIGMEP in dtData.Where(r => r.titular == true && r._codigo_producto == "COR"))
                        {
                            // busco si consta entre los titulares leídos del archivo
                            var comp = registros.Count(r => r.AfiliacionProcesada == AfiliacionEnum.TITULAR &&
                            (r.Cedula.ToLower() == titularSIGMEP._persona_cedula.ToLower() ||
                            r.Cedula.ToLower() == titularSIGMEP._persona_pasaporte.ToLower()));

                            if (comp == 0)
                            {
                                difTitulares.Add(titularSIGMEP);
                            }
                        }

                        #endregion

                        #region Escritura archivo Excel con resultado

                        // ESCRIBO EL ARCHIVO
                        int WriteColumn = FirstColumn + 8;

                        // NUEVAS CABECERAS
                        sheet.Cell(1, WriteColumn).SetValue<string>("RESULTADO CEDULA");
                        sheet.Cell(1, WriteColumn).Style.Font.SetBold();
                        sheet.Cell(1, WriteColumn).Style.Fill.SetBackgroundColor(XLColor.LightGreen);

                        //sheet.Cell(1, WriteColumn + 1).SetValue<string>("RESULTADO AFILIACIÓN");
                        //sheet.Cell(1, WriteColumn + 1).Style.Font.SetBold();
                        //sheet.Cell(1, WriteColumn + 1).Style.Fill.SetBackgroundColor(XLColor.LightGreen);

                        sheet.Cell(1, WriteColumn + 1).SetValue<string>("RESULTADO NOMBRES");
                        sheet.Cell(1, WriteColumn + 1).Style.Font.SetBold();
                        sheet.Cell(1, WriteColumn + 1).Style.Fill.SetBackgroundColor(XLColor.LightGreen);

                        sheet.Cell(1, WriteColumn + 2).SetValue<string>("RESULTADO APELLIDOS");
                        sheet.Cell(1, WriteColumn + 2).Style.Font.SetBold();
                        sheet.Cell(1, WriteColumn + 2).Style.Fill.SetBackgroundColor(XLColor.LightGreen);

                        sheet.Cell(1, WriteColumn + 3).SetValue<string>("RESULTADO TIPO TARIFA");
                        sheet.Cell(1, WriteColumn + 3).Style.Font.SetBold();
                        sheet.Cell(1, WriteColumn + 3).Style.Fill.SetBackgroundColor(XLColor.LightGreen);

                        sheet.Cell(1, WriteColumn + 4).SetValue<string>("RESULTADO VALOR TARIFA");
                        sheet.Cell(1, WriteColumn + 4).Style.Font.SetBold();
                        sheet.Cell(1, WriteColumn + 4).Style.Fill.SetBackgroundColor(XLColor.LightGreen);

                        sheet.Cell(1, WriteColumn + 5).SetValue<string>("RESULTADO FECHA DE NACIMIENTO");
                        sheet.Cell(1, WriteColumn + 5).Style.Font.SetBold();
                        sheet.Cell(1, WriteColumn + 5).Style.Fill.SetBackgroundColor(XLColor.LightGreen);

                        sheet.Cell(1, WriteColumn + 6).SetValue<string>("RESULTADO MONTO MÁXIMO COBERTURA COBERTURA");
                        sheet.Cell(1, WriteColumn + 6).Style.Font.SetBold();
                        sheet.Cell(1, WriteColumn + 6).Style.Fill.SetBackgroundColor(XLColor.LightGreen);


                        //if (RevisaDependientes)
                        //{
                        //    sheet.Cell(1, WriteColumn + 8).SetValue<string>("RESULTADO DEPENDIENTE");
                        //    sheet.Cell(1, WriteColumn + 8).Style.Font.SetBold();
                        //    sheet.Cell(1, WriteColumn + 8).Style.Fill.SetBackgroundColor(XLColor.LightGreen);
                        //}

                        // ESCRIBO DETALLE DEL RESULTADO DEL PROCESAMIENTO DEL ARCHIVO
                        foreach (RegistroCC registro in registros.OrderBy(r => r.Row))
                        {
                            sheet.Cell(registro.Row, WriteColumn).SetValue<string>(registro.CedulaProcesada == InfoEnum.VACIA ? "SIN DATO" : registro.MsgCedula.Replace("|", " "));
                            sheet.Cell(registro.Row, WriteColumn).Style.Fill.SetBackgroundColor(registro.ColorCedula == ResColorEnum.Verde ? XLColor.LightGreen : (registro.ColorCedula == ResColorEnum.Amarillo ? XLColor.LightYellow : XLColor.LightSalmon));
                            string Afiliacion = "";
                            if (registro.AfiliacionProcesada == AfiliacionEnum.VACIA) Afiliacion = "SIN DATO";
                            else if (registro.AfiliacionProcesada == AfiliacionEnum.INDETERMINADA) Afiliacion = "DATO NO RECONOCIDO, SE ESPERA TITULAR O DEPENDIENTE";
                            else if (registro.AfiliacionProcesada == AfiliacionEnum.TITULAR) Afiliacion = registro.MsgAfiliacion;
                            else if (registro.AfiliacionProcesada == AfiliacionEnum.DEPENDIENTE) Afiliacion = registro.MsgAfiliacion;
                            if (Afiliacion == "")
                                Afiliacion = "COINCIDE";

                            //sheet.Cell(registro.Row, WriteColumn + 1).SetValue<string>(Afiliacion.Replace("|", " "));
                            //sheet.Cell(registro.Row, WriteColumn + 1).Style.Fill.SetBackgroundColor(registro.ColorAfiliacion == ResColorEnum.Verde ? XLColor.LightGreen : (registro.ColorAfiliacion == ResColorEnum.Amarillo ? XLColor.LightYellow : XLColor.LightSalmon));

                            sheet.Cell(registro.Row, WriteColumn + 1).SetValue<string>(registro.NombresProcesada == InfoEnum.VACIA ? "SIN DATO" : registro.MsgNombres.Replace("|", " "));
                            sheet.Cell(registro.Row, WriteColumn + 1).Style.Fill.SetBackgroundColor(registro.ColorNombres == ResColorEnum.Verde ? XLColor.LightGreen : (registro.ColorNombres == ResColorEnum.Amarillo ? XLColor.LightYellow : XLColor.LightSalmon));

                            sheet.Cell(registro.Row, WriteColumn + 2).SetValue<string>(registro.ApellidosProcesada == InfoEnum.VACIA ? "SIN DATO" : registro.MsgApellidos.Replace("|", " "));
                            sheet.Cell(registro.Row, WriteColumn + 2).Style.Fill.SetBackgroundColor(registro.ColorApellidos == ResColorEnum.Verde ? XLColor.LightGreen : (registro.ColorApellidos == ResColorEnum.Amarillo ? XLColor.LightYellow : XLColor.LightSalmon));

                            string Tarifa = "";
                            if (registro.TarifaProcesada == TarifasEnum.VACIA) { Tarifa = "SIN DATO"; registro.ColorTarifa = ResColorEnum.Amarillo; }
                            else if (registro.TarifaProcesada == TarifasEnum.INDETERMINADA) { Tarifa = "DATO NO RECONOCIDO, SE ESPERA AT, A1, AF"; registro.ColorTarifa = ResColorEnum.Rojo; }
                            else { Tarifa = registro.MsgTarifa; }
                            if (Tarifa == "")
                            {
                                Tarifa = "COINCIDE";
                                registro.ColorTarifa = ResColorEnum.Verde;
                            }
                            else
                            {
                                if(Tarifa== "SIN DATO")
                                    registro.ColorTarifa = ResColorEnum.Amarillo;   
                                else
                                    registro.ColorTarifa = ResColorEnum.Rojo;
                            }

                            sheet.Cell(registro.Row, WriteColumn + 3).SetValue<string>(Tarifa.Replace("|", " "));
                            sheet.Cell(registro.Row, WriteColumn + 3).Style.Fill.SetBackgroundColor(registro.ColorTarifa == ResColorEnum.Verde ? XLColor.LightGreen : (registro.ColorTarifa == ResColorEnum.Amarillo ? XLColor.LightYellow : XLColor.LightSalmon));

                            sheet.Cell(registro.Row, WriteColumn + 4).SetValue<string>(registro.PrecioTarifaProcesada == InfoEnum.VACIA ? "SIN DATO" : registro.MsgPrecioTarifa.Replace("|", " "));
                            sheet.Cell(registro.Row, WriteColumn + 4).Style.Fill.SetBackgroundColor(registro.ColorPrecioTarifa == ResColorEnum.Verde ? XLColor.LightGreen : (registro.ColorPrecioTarifa == ResColorEnum.Amarillo ? XLColor.LightYellow : XLColor.LightSalmon));

                            sheet.Cell(registro.Row, WriteColumn + 5).SetValue<string>(registro.FechaNacimientoProcesada == InfoEnum.VACIA ? "SIN DATO" : registro.MsgFechaNacimiento.Replace("|", " "));
                            sheet.Cell(registro.Row, WriteColumn + 5).Style.Fill.SetBackgroundColor(registro.ColorFechaNacimiento == ResColorEnum.Verde ? XLColor.LightGreen : (registro.ColorFechaNacimiento == ResColorEnum.Amarillo ? XLColor.LightYellow : XLColor.LightSalmon));

                            sheet.Cell(registro.Row, WriteColumn + 6).SetValue<string>(registro.CoberturaProcesada == InfoEnum.VACIA ? "SIN DATO" : registro.MsgCobertura.Replace("|", " "));
                            sheet.Cell(registro.Row, WriteColumn + 6).Style.Fill.SetBackgroundColor(registro.ColorCobertura == ResColorEnum.Verde ? XLColor.LightGreen : (registro.ColorCobertura == ResColorEnum.Amarillo ? XLColor.LightYellow : XLColor.LightSalmon));

                            if (RevisaDependientes)
                            {
                                if (registro.AfiliacionProcesada == AfiliacionEnum.DEPENDIENTE)
                                {
                                    sheet.Cell(registro.Row, WriteColumn).SetValue<string>(registro.MsgDependiente.Replace("|", " "));
                                    // no hay colores diferenciados para dependientes
                                    if (registro.MsgDependiente != null && registro.MsgDependiente.Contains("NO SE ENCUENTRA REGISTRADO"))
                                        sheet.Cell(registro.Row, WriteColumn).Style.Fill.SetBackgroundColor(XLColor.LightYellow);
                                    else
                                        sheet.Cell(registro.Row, WriteColumn).Style.Fill.SetBackgroundColor(XLColor.LightGreen);
                                }
                            }

                        }

                        // CREO UNA NUEVA HOJA PARA LLENAR LOS TITULARES NO ENCONTRADOS
                        var TitularesFaltantes = workbook.AddWorksheet("Colaboradores Faltantes");

                        int RowTF = 1;
                        int ColTF = 1;

                        //Cabeceras
                        TitularesFaltantes.Cell(RowTF, ColTF).SetValue<string>("CEDULA/PASAPORTE");
                        TitularesFaltantes.Cell(RowTF, ColTF).Style.Font.SetBold();

                        TitularesFaltantes.Cell(RowTF, ColTF + 1).SetValue<string>("NOMBRES");
                        TitularesFaltantes.Cell(RowTF, ColTF + 1).Style.Font.SetBold();

                        TitularesFaltantes.Cell(RowTF, ColTF + 2).SetValue<string>("APELLIDOS");
                        TitularesFaltantes.Cell(RowTF, ColTF + 2).Style.Font.SetBold();

                        TitularesFaltantes.Cell(RowTF, ColTF + 3).SetValue<string>("FECHA DE NACIMIENTO");
                        TitularesFaltantes.Cell(RowTF, ColTF + 3).Style.Font.SetBold();

                        TitularesFaltantes.Cell(RowTF, ColTF + 4).SetValue<string>("NUMERO DE LISTA");
                        TitularesFaltantes.Cell(RowTF, ColTF + 4).Style.Font.SetBold();

                        TitularesFaltantes.Cell(RowTF, ColTF + 5).SetValue<string>("VALOR TARIFA");
                        TitularesFaltantes.Cell(RowTF, ColTF + 5).Style.Font.SetBold();

                        TitularesFaltantes.Cell(RowTF, ColTF + 6).SetValue<string>("CÓDIGO COLABORADOR");
                        TitularesFaltantes.Cell(RowTF, ColTF + 6).Style.Font.SetBold();


                        RowTF++;

                        foreach (var titular in difTitulares)
                        {
                            string ident = (titular.Is_persona_cedulaNull() ? "" : titular._persona_cedula) + (titular.Is_persona_pasaporteNull() ? "" : titular._persona_pasaporte);
                            TitularesFaltantes.Cell(RowTF, ColTF).SetValue<string>(ident);
                            TitularesFaltantes.Cell(RowTF, ColTF + 1).SetValue<string>(titular.Is_persona_nombresNull() ? "" : titular._persona_nombres);
                            TitularesFaltantes.Cell(RowTF, ColTF + 2).SetValue<string>(titular.Is_persona_apellidosNull() ? "" : titular._persona_apellidos);
                            TitularesFaltantes.Cell(RowTF, ColTF + 3).SetValue<DateTime?>(titular.Is_persona_fecha_nacimientoNull() ? (DateTime?)null : titular._persona_fecha_nacimiento);
                            TitularesFaltantes.Cell(RowTF, ColTF + 4).SetValue<string>(titular.Is_sucursal_nombreNull() ? "" : titular._sucursal_nombre);
                            TitularesFaltantes.Cell(RowTF, ColTF + 5).SetValue<decimal>(titular.Is_precio_baseNull() ? 0 : titular._precio_base);
                            TitularesFaltantes.Cell(RowTF, ColTF + 6).SetValue<int>(titular.Is_contrato_numeroNull() ? 0 : titular._contrato_numero);

                            RowTF++;
                        }

                        TitularesFaltantes.Columns().AdjustToContents();

                        if (RevisaDependientes)
                        {
                            // CREO UNA NUEVA HOJA PARA LLENAR LOS DEPENDIENTES NO ENCONTRADOS
                            var DependientesFaltantes = workbook.AddWorksheet("Dependientes Faltantes");

                            int RowDF = 1;
                            int ColDF = 1;

                            //Cabeceras
                            DependientesFaltantes.Cell(RowDF, ColDF).SetValue<string>("IDENTIFICACIÓN");
                            DependientesFaltantes.Cell(RowDF, ColDF).Style.Font.SetBold();

                            DependientesFaltantes.Cell(RowDF, ColDF + 1).SetValue<string>("NOMBRES");
                            DependientesFaltantes.Cell(RowDF, ColDF + 1).Style.Font.SetBold();

                            DependientesFaltantes.Cell(RowDF, ColDF + 2).SetValue<string>("APELLIDOS");
                            DependientesFaltantes.Cell(RowDF, ColDF + 2).Style.Font.SetBold();

                            DependientesFaltantes.Cell(RowDF, ColDF + 3).SetValue<string>("FECHA DE NACIMIENTO");
                            DependientesFaltantes.Cell(RowDF, ColDF + 3).Style.Font.SetBold();

                            //DependientesFaltantes.Cell(RowDF, ColDF + 4).SetValue<string>("LISTA");
                            //DependientesFaltantes.Cell(RowDF, ColDF + 4).Style.Font.SetBold();

                            //DependientesFaltantes.Cell(RowDF, ColDF + 5).SetValue<string>("PRECIO");
                            //DependientesFaltantes.Cell(RowDF, ColDF + 5).Style.Font.SetBold();

                            //DependientesFaltantes.Cell(RowDF, ColDF + 6).SetValue<string>("NUM. CONTRATO");
                            //DependientesFaltantes.Cell(RowDF, ColDF + 6).Style.Font.SetBold();


                            RowDF++;

                            foreach (var titular in difDependientes)
                            {
                                string ident = (titular.Is_persona_cedulaNull() ? "" : titular._persona_cedula) + (titular.Is_persona_pasaporteNull() ? "" : titular._persona_pasaporte);
                                DependientesFaltantes.Cell(RowTF, ColTF).SetValue<string>(ident);
                                DependientesFaltantes.Cell(RowTF, ColTF + 1).SetValue<string>(titular.Is_persona_nombresNull() ? "" : titular._persona_nombres);
                                DependientesFaltantes.Cell(RowTF, ColTF + 2).SetValue<string>(titular.Is_persona_apellidosNull() ? "" : titular._persona_apellidos);
                                DependientesFaltantes.Cell(RowTF, ColTF + 3).SetValue<DateTime?>(titular.Is_persona_fecha_nacimientoNull() ? (DateTime?)null : titular._persona_fecha_nacimiento);
                                //DependientesFaltantes.Cell(RowTF, ColTF + 4).SetValue<string>(titular.Is_sucursal_nombreNull() ? "" : titular._sucursal_nombre);
                                //DependientesFaltantes.Cell(RowTF, ColTF + 5).SetValue<decimal>(titular.Is_precio_baseNull() ? 0 : titular._precio_base);
                                //DependientesFaltantes.Cell(RowTF, ColTF + 6).SetValue<int>(titular.Is_contrato_numeroNull() ? 0 : titular._contrato_numero);

                                RowDF++;
                            }

                            DependientesFaltantes.Columns().AdjustToContents();
                        }

                        using (MemoryStream msSave = new MemoryStream())
                        {
                            workbook.SaveAs(msSave);
                            // GRABO EL ARCHIVO PROCESADO DE REGRESO A LA BASE DE DATOS
                            item.ContenidoProcesado = msSave.ToArray();
                        }

                        // CAMBIO EL ESTADO DEL PROCESAMIENTO
                        item.Estado = 2; // ESTADO COMPLETADO
                        context.SaveChanges();

                        #endregion

                        Message.AppendLine("Finaliza " + DateTime.Now.ToString());
                        Console.Write(Message);
                    }
                    catch (Exception ex)
                    {
                        item.Estado = 3; // ESTADO ERROR
                        item.MensajeError = "NO SE PUEDE LEER EL ARCHIVO POR ERRORES EN EL FORMATO. Referencia: " + ex.Message + "\n" + ex.StackTrace;
                        context.SaveChanges();
                    }
                }
            }
        }

        private static void LlenarTarifa(RegistroCC registro)
        {
            string referencia = registro.Tarifa.ToUpper().Trim();
            registro.TarifaProcesada = TarifasEnum.INDETERMINADA;

            if (referencia == "AT") registro.TarifaProcesada = TarifasEnum.AT;
            if (referencia == "AA") registro.TarifaProcesada = TarifasEnum.AT;
            if (referencia == "TITULAR SOLO") registro.TarifaProcesada = TarifasEnum.AT;
            if (referencia == "TITULAR") registro.TarifaProcesada = TarifasEnum.AT;
            if (referencia == "SOLO TITULAR") registro.TarifaProcesada = TarifasEnum.AT;

            if (referencia == "A1") registro.TarifaProcesada = TarifasEnum.A1;
            if (referencia == "A+1") registro.TarifaProcesada = TarifasEnum.A1;
            if (referencia == "TITULAR MÁS UNO") registro.TarifaProcesada = TarifasEnum.A1;
            if (referencia == "TITULAR MAS UNO") registro.TarifaProcesada = TarifasEnum.A1;
            if (referencia == "TITULAR + UNO") registro.TarifaProcesada = TarifasEnum.A1;
            if (referencia == "TITULAR Y UNO") registro.TarifaProcesada = TarifasEnum.A1;
            if (referencia == "AA1") registro.TarifaProcesada = TarifasEnum.A1;

            if (referencia == "AF") registro.TarifaProcesada = TarifasEnum.AF;
            if (referencia == "A+F") registro.TarifaProcesada = TarifasEnum.AF;
            if (referencia == "TITULAR MÁS FAMILIA") registro.TarifaProcesada = TarifasEnum.AF;
            if (referencia == "TITULAR Y FAMILIA") registro.TarifaProcesada = TarifasEnum.AF;
            if (referencia == "TITULAR + FAMILIA") registro.TarifaProcesada = TarifasEnum.AF;
            if (referencia == "TITULAR MAS FAMILIA") registro.TarifaProcesada = TarifasEnum.AF;
            if (referencia == "AAF") registro.TarifaProcesada = TarifasEnum.AF;

            if (referencia == "") registro.TarifaProcesada = TarifasEnum.VACIA;
        }

        private static void LlenarAfiliacion(RegistroCC registro)
        {
            string referencia = registro.Afiliacion.ToUpper().Trim();

            registro.AfiliacionProcesada = AfiliacionEnum.INDETERMINADA;

            // CASOS DE TITULAR
            if (referencia == "TITULAR") registro.AfiliacionProcesada = AfiliacionEnum.TITULAR;
            if (referencia == "AFILIADO") registro.AfiliacionProcesada = AfiliacionEnum.TITULAR;
            if (referencia == "COLABORADOR") registro.AfiliacionProcesada = AfiliacionEnum.TITULAR;
            if (referencia == "T") registro.AfiliacionProcesada = AfiliacionEnum.TITULAR;
            if (referencia == "TIT") registro.AfiliacionProcesada = AfiliacionEnum.TITULAR;
            if (referencia == "TITULA") registro.AfiliacionProcesada = AfiliacionEnum.TITULAR;

            // CASOS DE DEPENDIENTE
            if (referencia == "DEPENDIENTE") registro.AfiliacionProcesada = AfiliacionEnum.DEPENDIENTE;
            if (referencia == "DEP") registro.AfiliacionProcesada = AfiliacionEnum.DEPENDIENTE;
            if (referencia == "D") registro.AfiliacionProcesada = AfiliacionEnum.DEPENDIENTE;
            if (referencia == "BENEFICIARIO") registro.AfiliacionProcesada = AfiliacionEnum.DEPENDIENTE;
            if (referencia == "HIJO") registro.AfiliacionProcesada = AfiliacionEnum.DEPENDIENTE;
            if (referencia == "HIJA") registro.AfiliacionProcesada = AfiliacionEnum.DEPENDIENTE;
            if (referencia == "ESPOSA") registro.AfiliacionProcesada = AfiliacionEnum.DEPENDIENTE;
            if (referencia == "ESPOSO") registro.AfiliacionProcesada = AfiliacionEnum.DEPENDIENTE;
            if (referencia == "BENEFICIARIA") registro.AfiliacionProcesada = AfiliacionEnum.DEPENDIENTE;
            if (referencia == "FAMILIAR") registro.AfiliacionProcesada = AfiliacionEnum.DEPENDIENTE;
            if (referencia == "CARGA") registro.AfiliacionProcesada = AfiliacionEnum.DEPENDIENTE;
            if (referencia == "FAMILIAR") registro.AfiliacionProcesada = AfiliacionEnum.DEPENDIENTE;

            if (referencia == "") registro.AfiliacionProcesada = AfiliacionEnum.VACIA;

        }



        public static String Validate_Identification(String numero)
        {
            TiposIdentificacionValidador tipoIdentificacion = TiposIdentificacionValidador.Error;
            TiposOrigenRUCValidador tipoOrigen = TiposOrigenRUCValidador.Error;
            return Validate_Identification(numero, out tipoIdentificacion, out tipoOrigen);
        }

        public static String Validate_Identification(String numero, out TiposIdentificacionValidador tipoIdentificacion, out TiposOrigenRUCValidador tipoOrigen)
        {
            tipoIdentificacion = TiposIdentificacionValidador.Error;
            tipoOrigen = TiposOrigenRUCValidador.Error;

            /* IdType: 1 para cédula, 2 para RUC */
            int IdType = 0;
            if (string.IsNullOrEmpty(numero))
                IdType = 0;
            if (numero.Length == 10)
                IdType = 1;
            if (numero.Length == 13)
                IdType = 2;
            if (IdType == 0)
            {
                tipoIdentificacion = TiposIdentificacionValidador.Error;
                tipoOrigen = TiposOrigenRUCValidador.Error;
                return "La identificación ingresada es incorrecta";
            }

            tipoIdentificacion = IdType == 1 ? TiposIdentificacionValidador.Cedula : TiposIdentificacionValidador.RUC;

            var suma = 0;
            var residuo = 0;
            var Private = false;
            var Public = false;
            var Natural = false;
            var NumProvincias = 30;
            var Modulo = 11;

            /* Verifico que el campo no contenga letras */
            foreach (var item in numero)
            {
                if (item < '0' || item > '9')
                {
                    tipoIdentificacion = TiposIdentificacionValidador.Error;
                    tipoOrigen = TiposOrigenRUCValidador.Error;
                    return "La identificación no puede contener letras, sólo números";
                }
            }
            //Validación de los dos primeros dígitos (Código de Provincia)
            //if (Convert.ToInt32(numero.Substring(0, 2)) > NumProvincias)
            //{
            //    tipoIdentificacion = TiposIdentificacionValidador.Error;
            //    tipoOrigen = TiposOrigenRUCValidador.Error;
            //    return "El código de la provincia (dos primeros dígitos) es inválido";
            //}

            /* Aqui almacenamos los digitos de la cedula en variables. */
            int d1 = Convert.ToInt32(numero.Substring(0, 1));
            int d2 = Convert.ToInt32(numero.Substring(1, 1));
            int d3 = Convert.ToInt32(numero.Substring(2, 1));
            int d4 = Convert.ToInt32(numero.Substring(3, 1));
            int d5 = Convert.ToInt32(numero.Substring(4, 1));
            int d6 = Convert.ToInt32(numero.Substring(5, 1));
            int d7 = Convert.ToInt32(numero.Substring(6, 1));
            int d8 = Convert.ToInt32(numero.Substring(7, 1));
            int d9 = Convert.ToInt32(numero.Substring(8, 1));
            int d10 = Convert.ToInt32(numero.Substring(9, 1));

            if (d3 == 7 || d3 == 8)
            {
                tipoIdentificacion = TiposIdentificacionValidador.Error;
                tipoOrigen = TiposOrigenRUCValidador.Error;
                return "El tercer dígito ingresado es inválido";
            }

            int p1 = 0, p2 = 0, p3 = 0, p4 = 0, p5 = 0, p6 = 0, p7 = 0, p8 = 0, p9 = 0;
            if (IdType == 1)
            {
                //Validación sólo para cédulas
                /* El tercer digito es menor que 6 (0,1,2,3,4,5) para personas naturales */
                /* Solo para personas naturales (modulo 10) */
                if (d3 < 6)
                {
                    Natural = true;
                    p1 = d1 * 2; if (p1 >= 10) p1 -= 9;
                    p2 = d2 * 1; if (p2 >= 10) p2 -= 9;
                    p3 = d3 * 2; if (p3 >= 10) p3 -= 9;
                    p4 = d4 * 1; if (p4 >= 10) p4 -= 9;
                    p5 = d5 * 2; if (p5 >= 10) p5 -= 9;
                    p6 = d6 * 1; if (p6 >= 10) p6 -= 9;
                    p7 = d7 * 2; if (p7 >= 10) p7 -= 9;
                    p8 = d8 * 1; if (p8 >= 10) p8 -= 9;
                    p9 = d9 * 2; if (p9 >= 10) p9 -= 9;
                    Modulo = 10;
                }
                else
                {
                    tipoIdentificacion = TiposIdentificacionValidador.Error;
                    tipoOrigen = TiposOrigenRUCValidador.Error;
                    return "El tercer dígito ingresado es inválido";
                }

                suma = p1 + p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9;
                residuo = suma % Modulo;

                /* Si residuo=0, dig.ver.=0, caso contrario 10 - residuo*/
                int digitoVerificador;
                if (residuo == 0) digitoVerificador = 0;
                else digitoVerificador = Modulo - residuo;

                /* ahora comparamos el elemento de la posicion 10 con el dig. verificador */
                if (digitoVerificador != d10)
                {
                    tipoIdentificacion = TiposIdentificacionValidador.Error;
                    tipoOrigen = TiposOrigenRUCValidador.Error;
                    return "El número de cédula de la persona natural es incorrecto.";
                }
            }
            else
            {
                //Validación sólo para RUC's
                /* El tercer digito es: */
                /* 9 para sociedades privadas y extranjeros */
                /* 6 para sociedades publicas */
                /* Solo para sociedades publicas (modulo 11) */
                /* Aqui el digito verficador esta en la posicion 9, en las otras 2 en la pos. 10 */
                if (d3 == 6)
                {
                    Public = true;
                    p1 = d1 * 3;
                    p2 = d2 * 2;
                    p3 = d3 * 7;
                    p4 = d4 * 6;
                    p5 = d5 * 5;
                    p6 = d6 * 4;
                    p7 = d7 * 3;
                    p8 = d8 * 2;
                    p9 = 0;
                }

                /* Solo para entidades privadas (modulo 11) */
                if (d3 == 9)
                {
                    Private = true;
                    p1 = d1 * 4;
                    p2 = d2 * 3;
                    p3 = d3 * 2;
                    p4 = d4 * 7;
                    p5 = d5 * 6;
                    p6 = d6 * 5;
                    p7 = d7 * 4;
                    p8 = d8 * 3;
                    p9 = d9 * 2;
                }

                /* El tercer digito es menor que 6 (0,1,2,3,4,5) para personas naturales */
                /* Solo para personas naturales (modulo 10) */
                if (d3 < 6)
                {
                    Natural = true;
                    p1 = d1 * 2; if (p1 >= 10) p1 -= 9;
                    p2 = d2 * 1; if (p2 >= 10) p2 -= 9;
                    p3 = d3 * 2; if (p3 >= 10) p3 -= 9;
                    p4 = d4 * 1; if (p4 >= 10) p4 -= 9;
                    p5 = d5 * 2; if (p5 >= 10) p5 -= 9;
                    p6 = d6 * 1; if (p6 >= 10) p6 -= 9;
                    p7 = d7 * 2; if (p7 >= 10) p7 -= 9;
                    p8 = d8 * 1; if (p8 >= 10) p8 -= 9;
                    p9 = d9 * 2; if (p9 >= 10) p9 -= 9;
                    Modulo = 10;
                }

                suma = p1 + p2 + p3 + p4 + p5 + p6 + p7 + p8 + p9;
                residuo = suma % Modulo;

                /* Si residuo=0, dig.ver.=0, caso contrario 10 - residuo*/
                int digitoVerificador;
                if (residuo == 0) digitoVerificador = 0;
                else digitoVerificador = Modulo - residuo;

                /* ahora comparamos el elemento de la posicion 10 con el dig. verificador */
                if (Public)
                {
                    tipoOrigen = TiposOrigenRUCValidador.Publica;
                    if (digitoVerificador != d9)
                    {
                        tipoIdentificacion = TiposIdentificacionValidador.Error;
                        tipoOrigen = TiposOrigenRUCValidador.Error;
                        return "El RUC de la empresa del sector público es incorrecto.";
                    }

                    /* El ruc de las empresas del sector publico terminan con 0001*/
                    if (numero.Substring(9, 4) == "0000")
                    {
                        tipoIdentificacion = TiposIdentificacionValidador.Error;
                        tipoOrigen = TiposOrigenRUCValidador.Error;
                        return "El RUC de la empresa del sector público debe terminar con una secuencia de 0001";
                    }
                }
                if (Private)
                {
                    tipoOrigen = TiposOrigenRUCValidador.Privada;
                    if (digitoVerificador != d10)
                    {
                        tipoIdentificacion = TiposIdentificacionValidador.Error;
                        tipoOrigen = TiposOrigenRUCValidador.Error;
                        return "El RUC de la empresa del sector privado es incorrecto.";
                    }
                    if (numero.Substring(10, 3) == "000")
                    {
                        tipoIdentificacion = TiposIdentificacionValidador.Error;
                        tipoOrigen = TiposOrigenRUCValidador.Error;
                        return "El RUC de la empresa del sector privado debe terminar con una secuencia de 001";
                    }
                }
                if (Natural)
                {
                    tipoOrigen = TiposOrigenRUCValidador.PersonaNatural;
                    if (digitoVerificador != d10)
                    {
                        tipoIdentificacion = TiposIdentificacionValidador.Error;
                        tipoOrigen = TiposOrigenRUCValidador.Error;
                        return "El RUC de la persona natural es incorrecto.";
                    }
                    if (numero.Substring(10, 3) == "000")
                    {
                        tipoIdentificacion = TiposIdentificacionValidador.Error;
                        tipoOrigen = TiposOrigenRUCValidador.Error;
                        return "El RUC de la persona natural debe terminar con una secuencia de 001";
                    }
                }
            }

            return "";
        }

        public enum TiposIdentificacionValidador { Cedula = 1, RUC = 2, Error = 3 }
        public enum TiposOrigenRUCValidador { PersonaNatural = 1, Privada = 2, Publica = 3, Error = 4 }
    }
}
