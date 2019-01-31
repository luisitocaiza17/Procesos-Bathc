using ClosedXML.Excel;
using FuzzyString;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SW.Salud.Services;
using Newtonsoft.Json;
using SW.Salud.DataAccess;
using System.Xml.Linq;
using Ionic.Zip;
using System.Configuration;

namespace BatchLecturaArchivos
{
    class Program
    {
        static void Main(string[] args)
        {
            // Este proceso batch lee los archivos cargados en el portal contratante (archivos de inclusión masiva)
            // corre las validaciones correspondientes
            // luego los graba en la base de datos sql para su posterior visualizaciòn
            try
            {
                using (PortalContratante context = new PortalContratante())
                {
                    //ESTADOS ARCHIVO
                    //1 - PROCESANDO
                    //2 - EN REVISIÓN
                    //3 - GENERANDO INCLUSIONES
                    //4 - COMPLETADO
                    //5 - ERROR

                    var itemsProcesar = context.CORP_FileMasivos.Where(f => f.EstadoProceso == 1 /* f.FileMasivosID==1047*/).OrderBy(f => f.FechaCreacion).ToList();
                    StringBuilder observaciones = new StringBuilder();
                    Console.WriteLine(DateTime.Now.ToString());
                    foreach (var item in itemsProcesar)
                    {
                        observaciones = new StringBuilder();
                        observaciones.AppendLine("Archivo: " + item.FileMasivosID.ToString() + " Inicio:" + DateTime.Now.ToString());
                        Console.WriteLine(DateTime.Now.ToString());
                        //Nuevo modelo de manejo de archivos
                        //Se define un nuevo campo modo que determina que modelo de archivo es el que se carga
                        //según eso se procedará con el proceso de verificación del archivo
                        // 1 o null - ARCHIVO DEFECTO ENROLAMIENTO
                        // 2 - ARCHIVO COMPLETO NO NECESITA ENROLAMIENTO
                        // 3 - ARCHIVO CON INFORMACION PARCIAL NECESITA ENROLAMIENTO
                        #region Verificación del archivo tiene contenido
                        if (item.FileContent == null)
                        {
                            observaciones.AppendLine("Archivo cargado sin contenido");
                            observaciones.AppendLine("Archivo: " + item.FileMasivosID.ToString() + " Fin:" + DateTime.Now.ToString());
                            item.EstadoProceso = 5; // ERROR
                            item.Observaciones = observaciones.ToString();
                            item.PorcentajeAvance = 0;
                            context.SaveChanges();
                            continue;
                        }
                        #endregion
                        #region Procesamiento del archivo
                        else
                        {
                            MemoryStream ms = new MemoryStream(item.FileContent);
                            XLWorkbook workbook = null;
                            //closedxml tiene ciertos problemas de carga del archivo excel
                            //con ciertos elementos que lanzan excepciones que se las debe controlar
                            //para el correcto funcionamiento del proceso
                            #region Validaciones carga archivo
                            try
                            {
                                //Lectura de archivo
                                workbook = new XLWorkbook(ms, XLEventTracking.Disabled);
                            }
                            catch (DocumentFormat.OpenXml.Packaging.OpenXmlPackageException)
                            {
                                observaciones.AppendLine("Problemas en los hiperlinks del archivo cargado");
                                observaciones.AppendLine("Archivo: " + item.FileMasivosID.ToString() + " Fin:" + DateTime.Now.ToString());
                                item.EstadoProceso = 5; // ERROR
                                item.Observaciones = observaciones.ToString();
                                item.PorcentajeAvance = 0;
                                context.SaveChanges();
                                continue;
                                //Clean(ms, FixIt);
                                //workbook = new XLWorkbook(ms, XLEventTracking.Disabled);
                            }
                            catch (UriFormatException)
                            {
                                observaciones.AppendLine("Problemas en los hiperlinks del archivo cargado");
                                observaciones.AppendLine("Archivo: " + item.FileMasivosID.ToString() + " Fin:" + DateTime.Now.ToString());
                                item.EstadoProceso = 5; // ERROR
                                item.Observaciones = observaciones.ToString();
                                item.PorcentajeAvance = 0;
                                context.SaveChanges();
                                continue;
                                //Clean(ms, FixIt);
                                //workbook = new XLWorkbook(ms, XLEventTracking.Disabled);
                            }
                            #endregion  
                            var sheet = workbook.Worksheet(1);
                            bool ErrorFormato = false;
                            StringBuilder Message = new StringBuilder();

                            int FirstColumn = 1;
                            int FirstRow = 1;



                            // busco el inicio y fin del cuadro de valores, seg{un la cédula y pasaporte, esperando que esas sean siempre las primeras columnas de la tabla de datos
                            #region validacion de cabeceras

                            var celdaCedulaInicio = sheet.Search("CEDULA", CompareOptions.OrdinalIgnoreCase);
                            if (celdaCedulaInicio != null && celdaCedulaInicio.Count() > 0)
                            {
                                FirstColumn = celdaCedulaInicio.First().Address.ColumnNumber;
                                FirstRow = celdaCedulaInicio.First().Address.RowNumber;
                            }
                            else
                            {
                                celdaCedulaInicio = sheet.Search("CÉDULA", CompareOptions.OrdinalIgnoreCase);
                                if (celdaCedulaInicio != null && celdaCedulaInicio.Count() > 0)
                                {
                                    FirstColumn = celdaCedulaInicio.First().Address.ColumnNumber;
                                    FirstRow = celdaCedulaInicio.First().Address.RowNumber;
                                }
                                else
                                {
                                    var celdaPasaporteInicio = sheet.Search("PASAPORTE", CompareOptions.OrdinalIgnoreCase);
                                    if (celdaPasaporteInicio != null && celdaPasaporteInicio.Count() > 0)
                                    {
                                        FirstColumn = celdaPasaporteInicio.First().Address.ColumnNumber;
                                        FirstRow = celdaPasaporteInicio.First().Address.RowNumber;
                                    }
                                    else
                                    {
                                        ErrorFormato = true;
                                        Message.AppendLine("La tabla no tiene valores correctos, se busca el valor de tipo como CEDULA O PASAPORTE, y no se encuentra");
                                    }
                                }
                            }
                            if (FirstRow == 1)
                            {
                                ErrorFormato = true;
                                Message.AppendLine("No se encuentra el valor de cabecera inicial esperado: Se espera que se encuentre en primer lugar la cabecera de TIPO");
                            }
                            if (sheet.Cell(1, FirstColumn).GetString() != "TIPO")
                            {
                                ErrorFormato = true;
                                Message.AppendLine("La tabla debería tener la cabecera TIPO");
                            }
                            if (sheet.Cell(1, FirstColumn + 1).GetString() != "DOCUMENTO")
                            {
                                ErrorFormato = true;
                                Message.AppendLine("La tabla debería tener la cabecera DOCUMENTO");
                            }
                            if (sheet.Cell(1, FirstColumn + 2).GetString() != "NOMBRES")
                            {
                                ErrorFormato = true;
                                Message.AppendLine("La tabla debería tener la cabecera NOMBRES");
                            }
                            if (sheet.Cell(1, FirstColumn + 3).GetString() != "APELLIDOS")
                            {
                                ErrorFormato = true;
                                Message.AppendLine("La tabla debería tener la cabecera APELLIDOS");
                            }
                            if (sheet.Cell(1, FirstColumn + 4).GetString() != "EMAIL")
                            {
                                ErrorFormato = true;
                                Message.AppendLine("La tabla debería tener la cabecera EMAIL");
                            }
                            if (sheet.Cell(1, FirstColumn + 5).GetString() != "PRODUCTO (Alias)"
                                && sheet.Cell(1, FirstColumn + 5).GetString() != "PRODUCTO (Monto máximo)"
                                && sheet.Cell(1, FirstColumn + 5).GetString() != "PRODUCTO (Nombre de lista)")
                            {
                                ErrorFormato = true;
                                Message.AppendLine("La tabla debería tener la cabecera PRODUCTO (Alias) o PRODUCTO (Monto máximo) o PRODUCTO (Nombre de lista)");
                            }
                            if (sheet.Cell(1, FirstColumn + 6).GetString() != "ATENCION AFILIADO")
                            {
                                ErrorFormato = true;
                                Message.AppendLine("La tabla debería tener la cabecera ATENCION AFILIADO");
                            }
                            if (!string.IsNullOrEmpty(item.Tipo) && item.Tipo.ToLower().Equals("masivo"))
                            {
                                if (sheet.Cell(1, FirstColumn + 7).GetString() != "FECHA INCLUSIÓN")
                                {
                                    ErrorFormato = true;
                                    Message.AppendLine("La tabla debería tener la cabecera FECHA INCLUSIÓN");
                                }
                            }
                            if (ErrorFormato)
                            {
                                observaciones.Append(Message);
                                item.EstadoProceso = 5; // ERROR DE FORMATO
                                item.Observaciones = observaciones.ToString();
                                item.PorcentajeAvance = 0;
                                context.SaveChanges();
                                continue;
                            }
                            #endregion

                            //Consultas a sigmep
                            SW.Salud.Services.Sigmep.Logic sigmep = new SW.Salud.Services.Sigmep.Logic();
                            var listas = sigmep.ObtenerListas(item.IDEmpresa).AsEnumerable();
                            List<Cobertura> coberturatodas = new List<Cobertura>();
                            foreach (var lista in listas)
                            {
                                coberturatodas.AddRange(sigmep.ObtenerCoberturas(item.IDEmpresa, lista._sucursal_empresa));
                            }

                            List<CORP_Registro> lst = new List<CORP_Registro>();

                            var cell = sheet.Cell(FirstRow, FirstColumn);
                            var cell2 = sheet.Cell(FirstRow, FirstColumn + 1);
                            var cell3 = sheet.Cell(FirstRow, FirstColumn + 2);
                            var row1 = sheet.Cell(FirstRow, FirstColumn);
                            var row2 = sheet.Cell(FirstRow + 2, FirstColumn);
                            var row3 = sheet.Cell(FirstRow + 3, FirstColumn);

                            int CurrentRow = FirstRow;
                            int Total = 0;
                            while ((!cell.IsEmpty() || !cell2.IsEmpty() || !cell3.IsEmpty()) &&
                                (!row1.IsEmpty() || !row2.IsEmpty() || !row3.IsEmpty()))
                            {
                                Total++;
                                CurrentRow++;

                                cell = sheet.Cell(CurrentRow, FirstColumn);
                                cell2 = sheet.Cell(CurrentRow, FirstColumn + 1);
                                cell3 = sheet.Cell(CurrentRow, FirstColumn + 2);
                                row1 = sheet.Cell(CurrentRow, FirstColumn);
                                row2 = sheet.Cell(CurrentRow + 1, FirstColumn);
                                row3 = sheet.Cell(CurrentRow + 2, FirstColumn);
                            }

                            cell = sheet.Cell(FirstRow, FirstColumn);
                            cell2 = sheet.Cell(FirstRow, FirstColumn + 1);
                            cell3 = sheet.Cell(FirstRow, FirstColumn + 2);
                            row1 = sheet.Cell(FirstRow, FirstColumn);
                            row2 = sheet.Cell(FirstRow + 2, FirstColumn);
                            row3 = sheet.Cell(FirstRow + 3, FirstColumn);


                            CurrentRow = FirstRow;
                            #region procesamiento de registro
                            int j = 1;
                            while ((!cell.IsEmpty() || !cell2.IsEmpty() || !cell3.IsEmpty()) &&
                                (!row1.IsEmpty() || !row2.IsEmpty() || !row3.IsEmpty()))
                            {
                                j++;
                                Message = new StringBuilder();
                                Console.WriteLine(DateTime.Now.ToString());
                                CORP_Registro registro = new CORP_Registro();
                                RegistroCivil.Persona validado = null;
                                SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow ListaEncontrada = null;
                                List<Inclusion> inclusiones = null;
                                Persona personainclusion = null;
                                string CoberturaPrefijo = string.Empty;



                                try
                                {
                                    #region carga datos iniciales
                                    registro.IdArchivo = item.FileMasivosID;
                                    registro.IdRegistro = 0;
                                    registro.IdUsuario = item.IDUsuario;
                                    registro.IdEmpresa = item.IDEmpresa;
                                    registro.FechaCreacion = DateTime.Now;
                                    if (!string.IsNullOrEmpty(item.Tipo) && item.Tipo.ToLower().Equals("masivo"))
                                        registro.TipoMovimiento = 2; // Inclusion Masiva
                                    else
                                        registro.TipoMovimiento = 1; // Inclusion
                                    registro.TipoDocumento = CedulaPasaporte(getString(sheet.Cell(CurrentRow, FirstColumn)));//.GetString());
                                    registro.NumeroDocumento = getString(sheet.Cell(CurrentRow, FirstColumn + 1)).Replace("-", "").Replace(" ", ""); //.GetString();
                                    registro.Nombres = getString(sheet.Cell(CurrentRow, FirstColumn + 2)); //sheet.Cell(CurrentRow, FirstColumn + 2).GetString();
                                    registro.Apellidos = getString(sheet.Cell(CurrentRow, FirstColumn + 3)); //.GetString();
                                    registro.Datos = "";
                                    registro.Email = getString(sheet.Cell(CurrentRow, FirstColumn + 4)); //.GetString();
                                    registro.Estado = 1; // Empieza como leìdo correcto
                                                         //ESTADOS REGISTRO TEMPORAL
                                                         //1 - LEÍDO CORRECTO
                                                         //2 - LEÍDO CON ERROR
                                                         //3 - CORREGIDO
                                                         //4 - DESCARTADO
                                                         //5 - APROBADO
                                                         //6 - INCLUIDO
                                                         //7 - ERROR EN INCLUSION
                                    #endregion
                                    #region Calculo de lista/cobertur
                                    // lectura de producto // la celda debería tener formato de numero
                                    //var Valor = (int)sheet.Cell(CurrentRow, FirstColumn + 5).GetDouble();
                                    var Valor = sheet.Cell(CurrentRow, FirstColumn + 5).GetString().Replace(" ", "").Replace(".", "").Replace(",", "").ToUpperInvariant();
                                    // debería buscar la lista en función del valor de cobertura y poner
                                    //ListaEncontrada = listas.FirstOrDefault(l => l._sucursal_nombre.Trim().Replace(" ", "").Replace(".", "").Replace(",", "").Contains(Valor.ToString()));
                                    ListaEncontrada = listas.FirstOrDefault(l => l._sucursal_alias.Trim().Replace(" ", "").Replace(".", "").Replace(",", "").ToUpperInvariant().Equals(Valor.ToString()));
                                    if (ListaEncontrada == null)
                                    {
                                        Message.AppendLine("Producto seleccionado no existe para la  empresa.");
                                        registro.IdProducto = "";
                                        registro.NombreProducto = "";
                                        registro.IdCobertura = "";
                                        registro.Estado = 2; // Leìdo con error
                                    }
                                    else
                                    {
                                        registro.IdProducto = ListaEncontrada._sucursal_empresa.ToString();
                                        registro.NombreProducto = ListaEncontrada._sucursal_alias;

                                        // busca las coberturas solamente si encontró la lista
                                        // saca las coberturas de dicha lista y busca el plan, si encuentra lo liga, sino naranjas
                                        CoberturaPrefijo = ParseCobertura(sheet.Cell(CurrentRow, FirstColumn + 6).GetString());

                                        if (string.IsNullOrEmpty(CoberturaPrefijo))
                                        {
                                            Message.AppendLine("Cobertura registrada no coincide, por favor verificar plantilla.");
                                            registro.IdCobertura = "";
                                            registro.Estado = 2; // Leìdo con error
                                        }
                                        else
                                        {
                                            if (CoberturaPrefijo.Equals("AA"))
                                                CoberturaPrefijo = "AT";
                                            var coberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == item.IDEmpresa && p.CodigoSucursal == ListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith(CoberturaPrefijo));
                                            if (coberturaEncontrada == null)
                                            {
                                                Message.AppendLine("Cobertura seleccionada no existe para el producto seleccionado.");
                                                registro.IdCobertura = "";
                                                registro.Estado = 2; // Leìdo con error
                                            }
                                            else
                                            {
                                                registro.IdCobertura = coberturaEncontrada.CodigoPlan;
                                            }
                                        }
                                    }
                                    #endregion


                                }
                                catch (Exception ex)
                                {
                                    registro.Estado = 2; // Leìdo con error
                                    registro.Resultado = "Error de procesamiento de lectura: " + ex.Message;
                                }


                                // VALIDACIONES
                                #region Validaciones
                                if (registro.NumeroDocumento != "")
                                {
                                    #region Validaciones iniciales
                                    //a.Cédulas con 10 dígitos
                                    if (registro.TipoDocumento == 1 && registro.NumeroDocumento.Length != 9 && registro.NumeroDocumento.Length != 10)
                                    {
                                        Message.AppendLine("Cédula ingresada no tiene 10 dígitos");
                                        //registro.Resultado = "Cédula no tiene 10 dígitos";
                                        registro.Estado = 2; // Leìdo con error
                                    }
                                    //b.Si es cèdula y viene con 9 dígitos, incluir el 0 adelante (CORRECCION)
                                    if (registro.TipoDocumento == 1 && registro.NumeroDocumento.Length == 9)
                                    {
                                        //Message.AppendLine("La cédula tiene 9 dígitos se aumento al inicio el valor 0");
                                        registro.NumeroDocumento = "0" + registro.NumeroDocumento;
                                        //registro.Estado = 2;
                                    }
                                    // Si empieza con apóstrofe
                                    if (registro.TipoDocumento == 1 && registro.NumeroDocumento.Length == 11 && registro.NumeroDocumento.StartsWith("'"))
                                    {
                                        //Message.AppendLine("La cédula tiene 9 dígitos se aumento al inicio el valor 0");
                                        registro.NumeroDocumento = registro.NumeroDocumento.Replace("'", "");
                                        //registro.Estado = 2;
                                    }
                                    // cumple con el formato de cédula válido
                                    TiposIdentificacionValidador tipoIdentificacion;
                                    TiposOrigenRUCValidador tipoOrigen;

                                    string res = Validate_Identification(registro.NumeroDocumento, out tipoIdentificacion, out tipoOrigen);
                                    if (res == "" && registro.TipoDocumento != 1)
                                    {
                                        registro.TipoDocumento = 1; // si ponen pasaporte a una cèdula ecuatoriana vàlida, se corrije
                                    }
                                    if (registro.TipoDocumento == 1 && (res != "" || tipoIdentificacion == TiposIdentificacionValidador.Error))
                                    {
                                        Message.AppendLine(res);
                                        //registro.Resultado = res;
                                        registro.Estado = 2;
                                    }
                                    if (registro.TipoDocumento == 1 && tipoIdentificacion == TiposIdentificacionValidador.RUC)
                                    {
                                        Message.AppendLine("El registro ingresado es un RUC, no una cédula");
                                        //registro.Resultado = "El registro ingresado es un RUC, no una cédula";
                                        registro.Estado = 2;
                                    }
                                    #endregion

                                    //c.Validar con registro civil
                                    if (registro.TipoDocumento == 1)
                                    {
                                        RegistroCivil.ServiciosClienteClient client = new RegistroCivil.ServiciosClienteClient();
                                        validado = client.ObtenerPersonaPorNumeroIdentificacion("C", registro.NumeroDocumento);

                                        if (validado == null)
                                        {
                                            Message.AppendLine("Error en la identificación,  no es encuentra información en el registro civil, favor revisar identificación y datos personales");
                                            //registro.Resultado = Message.ToString();
                                            registro.Estado = 2;
                                        }
                                        else
                                        {
                                            registro.RC_Celular = validado.Celular;
                                            registro.RC_CondicionCedulado = validado.Condicion_Cedulado;
                                            registro.RC_EmailPersonal = validado.Email_Personal;
                                            registro.RC_EmailTrabajo = validado.Email_Trabajo;
                                            registro.RC_EstadoCivil = validado.Estado_Civil;
                                            registro.RC_FechaNacimiento = validado.Fecha_Nacimiento;
                                            registro.RC_Genero = validado.Genero;
                                            registro.RC_TelefonoDomicilio = validado.Telefono_Domicilio;
                                            registro.RC_TelefonoTrabajo = validado.Telefono_Trabajo;

                                            //d.Comparar con nombres y apellidos de registro civil
                                            // COMPARACIÓN DIFUSA
                                            string saved = registro.Nombres.ToUpper().Trim();

                                            List<FuzzyStringComparisonOptions> options = new List<FuzzyStringComparisonOptions>();

                                            options.Add(FuzzyStringComparisonOptions.UseJaccardDistance);
                                            //options.Add(FuzzyStringComparisonOptions.UseNormalizedLevenshteinDistance);
                                            options.Add(FuzzyStringComparisonOptions.UseOverlapCoefficient);
                                            options.Add(FuzzyStringComparisonOptions.UseLongestCommonSubsequence);
                                            //options.Add(FuzzyStringComparisonOptions.CaseSensitive);
                                            //options.Add(FuzzyStringComparisonOptions.UseLevenshteinDistance);

                                            string NombreRC = ((validado.Primer_Nombre == null ? "" : validado.Primer_Nombre.ToUpper().Trim()) + " " + (validado.Segundo_Nombre == null ? "" : validado.Segundo_Nombre.ToUpper().Trim())).Trim();
                                            string ApellidosRC = ((validado.Primer_Apellido == null ? "" : validado.Primer_Apellido.ToUpper().Trim()) + " " + (validado.Segundo_Apellido == null ? "" : validado.Segundo_Apellido.ToUpper().Trim())).Trim();

                                            if (!saved.ApproximatelyEquals(NombreRC, options, FuzzyStringComparisonTolerance.Strong))
                                            {
                                                Message.AppendLine("Nombres no son iguales a los del registro civil, favor revisar los datos en la columna de nombres.");
                                                //registro.Resultado = "Nombres no se aproximan a los consultados con el Registro Civil";
                                                registro.Estado = 2;
                                            }

                                            string saved2 = registro.Apellidos.ToUpper().Trim();
                                            if (!saved2.ApproximatelyEquals(ApellidosRC, options, FuzzyStringComparisonTolerance.Strong))
                                            {
                                                Message.AppendLine("Apellidos no son iguales a los del registro civil, favor revisar los datos en la columna de apellidos.");
                                                //registro.Resultado = "Apellidos no se aproximan a los consultados con el Registro Civil, favor revisar los datos en la columna de apellido";
                                                registro.Estado = 2;
                                            }

                                            // tomo finalmente los nombres del registro civil por ser mas confiables
                                            registro.Nombres = NombreRC;
                                            registro.Apellidos = ApellidosRC;
                                        }
                                    }
                                }
                                //e.Validar que todos los campos estén llenos, sin datos en blanco
                                if (registro.Apellidos == "")
                                {
                                    Message.AppendLine("No tiene datos en el apellido.");
                                    //registro.Resultado = "No tiene datos en el apellido";
                                    registro.Estado = 2;
                                }
                                if (registro.Email == "")
                                {
                                    Message.AppendLine("No tiene datos en el email.");
                                    //registro.Resultado = "No tiene datos en el Email";
                                    registro.Estado = 2;
                                }
                                if (registro.IdArchivo == 0)
                                {
                                    registro.IdArchivo = item.FileMasivosID;
                                }
                                if (registro.IdCobertura == "")
                                {
                                    Message.AppendLine("Cobertura seleccionada no existe.");
                                    //registro.Resultado = "No tiene registrada la Atención a Afiliado";
                                    registro.Estado = 2;
                                }
                                if (registro.IdEmpresa == 0)
                                {
                                    registro.IdEmpresa = item.IDEmpresa;
                                }
                                registro.IdRegistro = 0;
                                if (registro.IdUsuario == 0)
                                {
                                    registro.IdUsuario = item.IDUsuario;
                                }
                                if (registro.IdProducto == "")
                                {
                                    Message.AppendLine("Producto seleccionado no existe.");
                                    //registro.Resultado = "El producto no tiene coincidencias con los registrados para la empresa";
                                    registro.Estado = 2;
                                }
                                if (registro.Nombres == "")
                                {
                                    Message.AppendLine("No tiene datos en los nombres.");
                                    //registro.Resultado = "No tiene registrados los Nombres";
                                    registro.Estado = 2;
                                }
                                if (registro.NumeroDocumento == "")
                                {
                                    Message.AppendLine("No tiene datos en la identificación.");
                                    //registro.Resultado = "No tiene registrada la Identificación";
                                    registro.Estado = 2;
                                }
                                registro.Observaciones = ""; // Luego se ejecutan las validaciones
                                if (registro.TipoDocumento == 0)
                                {
                                    Message.AppendLine("No tiene datos el el tipo de documento.");
                                    //registro.Resultado = "No tiene registrado el Tipo de Documento";
                                    registro.Estado = 2;
                                }
                                //registro.TipoMovimiento = 1; // Inclusion

                                //f.Evitar cedulas repetidas dentro del archivo
                                if (lst.Count(g => g.NumeroDocumento == registro.NumeroDocumento) > 0)
                                {
                                    lst.Where(p => p.NumeroDocumento == registro.NumeroDocumento).ToList()
                                        .ForEach(t =>
                                        {
                                            t.Estado = 2;
                                            StringBuilder dup = new StringBuilder();
                                            dup.AppendLine(t.Resultado);
                                            if (!t.Resultado.Contains("El número de documento se encuentra duplicado en el archivo, por favor editar el registro a incluirse."))
                                                dup.AppendLine("El número de documento se encuentra duplicado en el archivo, por favor editar el registro a incluirse.");
                                            t.Resultado = dup.ToString();
                                        });
                                    Message.AppendLine("El número de documento se encuentra duplicado.");
                                    //registro.Resultado = "Este registro se encuentra repetido";
                                    registro.Estado = 2;
                                }
                                //validacion formato email
                                string email = Validate_Mail(string.IsNullOrEmpty(registro.Email) ? string.Empty : registro.Email);
                                if (!string.IsNullOrEmpty(email))
                                {
                                    Message.AppendLine(email);
                                }
                                //todo desactivar
                                //f.Evitar correos repetidos dentro del archivo
                                if (lst.Count(g => g.Email == registro.Email) > 0)
                                {
                                    Message.AppendLine("El correo electrónico se encuentra repetido en el archivo.");
                                    //registro.Resultado = "Este registro se encuentra repetido";
                                    registro.Estado = 2;
                                }


                                #endregion
                                #region Validaciones fecha Inclusion
                                DateTime? fechainclusion = null;
                                if (!string.IsNullOrEmpty(item.Tipo) && item.Tipo.ToLower().Equals("masivo"))
                                {
                                    try
                                    {
                                        fechainclusion = sheet.Cell(CurrentRow, FirstColumn + 7).GetDateTime();
                                        if (ListaEncontrada != null)
                                        {
                                            if (!ListaEncontrada.Is_fecha_inicio_sucursalNull())
                                            {
                                                if (fechainclusion < ListaEncontrada._fecha_inicio_sucursal)
                                                {
                                                    Message.AppendLine("La fecha ingresada es menor a la fecha de inicio de vigencia de la cobertura.");
                                                    registro.Estado = 2;
                                                }
                                                int DiasRetractivos = int.Parse(ConfigurationManager.AppSettings["DiasRetroactivo"]);
                                                if (fechainclusion < DateTime.Today.AddDays(-DiasRetractivos))
                                                {
                                                    Message.AppendLine("La fecha ingresada es menor a la fecha mínima retroactiva.");
                                                    registro.Estado = 2;
                                                }
                                            }
                                        }
                                        registro.FechaInclusion = fechainclusion;
                                    }
                                    catch
                                    {
                                        Message.AppendLine("El valor ingresado no corresponde a una fecha");
                                        registro.Estado = 2;
                                    }
                                }
                                #endregion
                                #region Generacion de Objeto de Datos para inclusion
                                inclusiones = new List<Inclusion>();
                                if (!string.IsNullOrEmpty(item.Tipo) && item.Tipo.ToLower().Equals("masivo"))
                                {
                                    inclusiones.Add(new Inclusion()
                                    {
                                        EmpresaID = registro.IdEmpresa.HasValue ? registro.IdEmpresa.Value : 0,
                                        SucursalID = string.IsNullOrEmpty(registro.IdProducto) ? 0 : Convert.ToInt32(registro.IdProducto),
                                        Usuario = string.Empty,
                                        PlanID = registro.IdCobertura,
                                        FechaInclusion = registro.FechaInclusion ?? DateTime.Today, //ListaEncontrada == null ? DateTime.Today : ListaEncontrada.Is_fecha_inicio_sucursalNull() ? DateTime.Today : ListaEncontrada._fecha_inicio_sucursal,
                                        NombreSucursal = ListaEncontrada == null ? string.Empty : ListaEncontrada.Is_sucursal_nombreNull() ? string.Empty : ListaEncontrada._sucursal_nombre,
                                        CompletadoEnrolamiento = false
                                    });
                                }
                                else
                                {
                                    inclusiones.Add(new Inclusion()
                                    {
                                        EmpresaID = registro.IdEmpresa.HasValue ? registro.IdEmpresa.Value : 0,
                                        SucursalID = string.IsNullOrEmpty(registro.IdProducto) ? 0 : Convert.ToInt32(registro.IdProducto),
                                        Usuario = string.Empty,
                                        PlanID = registro.IdCobertura,
                                        FechaInclusion = ListaEncontrada == null ? DateTime.Today : ListaEncontrada.Is_fecha_inicio_sucursalNull() ? DateTime.Today : ListaEncontrada._fecha_inicio_sucursal,
                                        NombreSucursal = ListaEncontrada == null ? string.Empty : ListaEncontrada.Is_sucursal_nombreNull() ? string.Empty : ListaEncontrada._sucursal_nombre,
                                        CompletadoEnrolamiento = false
                                    });
                                }
                                //lectura de sublistas
                                if (ListaEncontrada != null)
                                {
                                    string sublistas = ListaEncontrada.Is_sucursal_configuracionNull() ? string.Empty : ListaEncontrada._sucursal_configuracion;
                                    if (!string.IsNullOrEmpty(sublistas))
                                    {
                                        List<SubSucursal> listasadicionales = JsonConvert.DeserializeObject<List<SubSucursal>>(sublistas);
                                        SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow SubListaEncontrada = null;
                                        Cobertura subcoberturaEncontrada = null;
                                        foreach (var li in listasadicionales)
                                        {
                                            if (!li.opcional)
                                            {
                                                SubListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == li.id);
                                                //calculo de coberturas
                                                if (SubListaEncontrada != null)
                                                {
                                                    if (li.plan.Equals("AF"))
                                                    {
                                                        subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == item.IDEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith(CoberturaPrefijo));
                                                    }
                                                    else if (li.plan.Equals("A1"))
                                                    {
                                                        if (CoberturaPrefijo.Equals("AF"))
                                                            subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == item.IDEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith("A1"));
                                                        else
                                                            subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == item.IDEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith(CoberturaPrefijo));
                                                    }
                                                    else if (li.plan.Equals("AT"))
                                                    {
                                                        if (CoberturaPrefijo.Equals("AF") || CoberturaPrefijo.Equals("A1"))
                                                            subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == item.IDEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith("AT"));
                                                        else
                                                            subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == item.IDEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith(CoberturaPrefijo));
                                                    }
                                                    if (subcoberturaEncontrada != null)
                                                    {
                                                        if (!string.IsNullOrEmpty(item.Tipo) && item.Tipo.ToLower().Equals("masivo"))
                                                        {
                                                            inclusiones.Add(new Inclusion()
                                                            {
                                                                EmpresaID = registro.IdEmpresa.HasValue ? registro.IdEmpresa.Value : 0,
                                                                SucursalID = li.id,
                                                                Usuario = string.Empty,
                                                                PlanID = subcoberturaEncontrada.CodigoPlan,
                                                                FechaInclusion = registro.FechaInclusion ?? DateTime.Today, //ListaEncontrada == null ? DateTime.Today : ListaEncontrada.Is_fecha_inicio_sucursalNull() ? DateTime.Today : ListaEncontrada._fecha_inicio_sucursal,
                                                                NombreSucursal = SubListaEncontrada.Is_sucursal_nombreNull() ? string.Empty : SubListaEncontrada._sucursal_nombre,
                                                                CompletadoEnrolamiento = false
                                                            });
                                                        }
                                                        else
                                                        {
                                                            inclusiones.Add(new Inclusion()
                                                            {
                                                                EmpresaID = registro.IdEmpresa.HasValue ? registro.IdEmpresa.Value : 0,
                                                                SucursalID = li.id,
                                                                Usuario = string.Empty,
                                                                PlanID = subcoberturaEncontrada.CodigoPlan,
                                                                FechaInclusion = SubListaEncontrada.Is_fecha_inicio_sucursalNull() ? DateTime.Today : SubListaEncontrada._fecha_inicio_sucursal,
                                                                NombreSucursal = SubListaEncontrada.Is_sucursal_nombreNull() ? string.Empty : SubListaEncontrada._sucursal_nombre,
                                                                CompletadoEnrolamiento = false
                                                            });
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (!string.IsNullOrEmpty(item.Tipo) && item.Tipo.ToLower().Equals("masivo"))
                                                        {
                                                            inclusiones.Add(new Inclusion()
                                                            {
                                                                EmpresaID = registro.IdEmpresa.HasValue ? registro.IdEmpresa.Value : 0,
                                                                SucursalID = li.id,
                                                                Usuario = string.Empty,
                                                                PlanID = "0",
                                                                FechaInclusion = registro.FechaInclusion ?? DateTime.Today, //ListaEncontrada == null ? DateTime.Today : ListaEncontrada.Is_fecha_inicio_sucursalNull() ? DateTime.Today : ListaEncontrada._fecha_inicio_sucursal,
                                                                NombreSucursal = SubListaEncontrada.Is_sucursal_nombreNull() ? string.Empty : SubListaEncontrada._sucursal_nombre,
                                                                CompletadoEnrolamiento = false
                                                            });
                                                        }
                                                        else
                                                        {
                                                            inclusiones.Add(new Inclusion()
                                                            {
                                                                EmpresaID = registro.IdEmpresa.HasValue ? registro.IdEmpresa.Value : 0,
                                                                SucursalID = li.id,
                                                                Usuario = string.Empty,
                                                                PlanID = "0",
                                                                FechaInclusion = SubListaEncontrada.Is_fecha_inicio_sucursalNull() ? DateTime.Today : SubListaEncontrada._fecha_inicio_sucursal,
                                                                NombreSucursal = SubListaEncontrada.Is_sucursal_nombreNull() ? string.Empty : SubListaEncontrada._sucursal_nombre,
                                                                CompletadoEnrolamiento = false
                                                            });
                                                        }
                                                        Message.AppendLine("La atención afiliado del beneficio adicional seleccionado esta inactiva, por favor contactese con su asesor");
                                                        //registro.Resultado = "Este registro se encuentra repetido";
                                                        registro.Estado = 2;
                                                    }
                                                }
                                                else
                                                {
                                                    if (!string.IsNullOrEmpty(item.Tipo) && item.Tipo.ToLower().Equals("masivo"))
                                                    {
                                                        inclusiones.Add(new Inclusion()
                                                        {
                                                            EmpresaID = registro.IdEmpresa.HasValue ? registro.IdEmpresa.Value : 0,
                                                            SucursalID = li.id,
                                                            Usuario = string.Empty,
                                                            PlanID = "0",
                                                            FechaInclusion = registro.FechaInclusion ?? DateTime.Today, //ListaEncontrada == null ? DateTime.Today : ListaEncontrada.Is_fecha_inicio_sucursalNull() ? DateTime.Today : ListaEncontrada._fecha_inicio_sucursal,
                                                            NombreSucursal = string.Empty,//ListaEncontrada.Is_sucursal_nombreNull() ? string.Empty : ListaEncontrada._sucursal_nombre,
                                                            CompletadoEnrolamiento = false
                                                        });
                                                    }
                                                    else
                                                    {
                                                        inclusiones.Add(new Inclusion()
                                                        {
                                                            EmpresaID = registro.IdEmpresa.HasValue ? registro.IdEmpresa.Value : 0,
                                                            SucursalID = li.id,
                                                            Usuario = string.Empty,
                                                            PlanID = "0",
                                                            FechaInclusion = DateTime.Today, //ListaEncontrada.Is_fecha_inicio_sucursalNull() ? DateTime.Today : ListaEncontrada._fecha_inicio_sucursal,
                                                            NombreSucursal = string.Empty,//ListaEncontrada.Is_sucursal_nombreNull() ? string.Empty : ListaEncontrada._sucursal_nombre,
                                                            CompletadoEnrolamiento = false
                                                        });
                                                    }
                                                    Message.AppendLine("El beneficio adicional seleccionado se encuentra inactivo, por favor contactese con su asesor");
                                                    //registro.Resultado = "Este registro se encuentra repetido";
                                                    registro.Estado = 2;
                                                }
                                            }
                                        }
                                    }
                                }
                                #endregion
                                #region Generacion de Objeto de Datos para inclusion
                                //En el caso que sea pasaporte y al consultar al registro civil no hay datos se llena directe el objeto
                                if (registro.TipoDocumento == 1)
                                {
                                    if (validado != null)
                                    {
                                        personainclusion = new Persona()
                                        {
                                            Apellido1 = validado.Primer_Apellido,
                                            Apellido2 = validado.Segundo_Apellido,
                                            Nombre1 = validado.Primer_Nombre,
                                            Nombre2 = validado.Segundo_Nombre,
                                            EstadoCivil = validado.Estado_Civil.ToString(),
                                            Genero = validado.Genero.ToString(),
                                            Nombres = validado.Primer_Nombre + " " + validado.Segundo_Nombre,
                                            Apellidos = validado.Primer_Apellido + " " + validado.Segundo_Apellido,
                                            Cedula = validado.Identificacion,
                                            FechaNacimiento = validado.Fecha_Nacimiento,
                                            TipoDocumento = validado.Tipo_Identificacion.ToString(),
                                            ///////////////////////////////////
                                            emailempresa = registro.Email
                                        };
                                    }
                                    else { personainclusion = new Persona(); }
                                }
                                else
                                {
                                    string nombre1 = string.Empty;
                                    string nombre2 = string.Empty;
                                    string apellido1 = string.Empty;
                                    string apellido2 = string.Empty;
                                    string[] parts = registro.Apellidos.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Count() == 1)
                                    {
                                        apellido1 = parts[0];
                                    }
                                    else if (parts.Count() == 2)
                                    {
                                        apellido1 = parts[0];
                                        apellido2 = parts[1];
                                    }
                                    else if (parts.Count() > 2)
                                    {
                                        apellido1 = parts[0];
                                        for (int i = 1; i < parts.Count(); i++)
                                        {
                                            apellido2 += parts[i] + ' ';
                                        };
                                        apellido2 = apellido2.TrimEnd();
                                    }
                                    parts = registro.Nombres.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (parts.Count() == 1)
                                    {
                                        nombre1 = parts[0];
                                    }
                                    else if (parts.Count() == 2)
                                    {
                                        nombre1 = parts[0];
                                        nombre2 = parts[1];
                                    }
                                    else if (parts.Count() > 2)
                                    {
                                        nombre1 = parts[0];
                                        for (int i = 1; i < parts.Count(); i++)
                                        {
                                            nombre2 += parts[i] + ' ';
                                        };
                                        nombre2 = nombre2.TrimEnd();
                                    }
                                    personainclusion = new Persona()
                                    {
                                        //en el caso de pasaporte toca dividir 
                                        Apellido1 = apellido1,//validado.Primer_Apellido,
                                        Apellido2 = apellido2,//validado.Segundo_Apellido,
                                        Nombre1 = nombre1,//validado.Primer_Nombre,
                                        Nombre2 = nombre2,//validado.Segundo_Nombre,
                                        //EstadoCivil = validado.Estado_Civil.ToString(),
                                        //Genero = validado.Genero.ToString(),
                                        Nombres = registro.Nombres,//validado.Primer_Nombre + " " + validado.Segundo_Nombre,
                                        Apellidos = registro.Apellidos,//validado.Primer_Apellido + " " + validado.Segundo_Apellido,
                                        Cedula = registro.NumeroDocumento,//validado.Identificacion,
                                        //FechaNacimiento = //validado.Fecha_Nacimiento,
                                        TipoDocumento = registro.TipoDocumento.ToString(),//validado.Tipo_Identificacion.ToString(),
                                        ///////////////////////////////////
                                        emailempresa = registro.Email
                                    };
                                }
                                #endregion
                                #region Guardar datos para inclusion
                                List<object> datosingreso = new List<object>() { inclusiones, personainclusion };
                                registro.Datos = JsonConvert.SerializeObject(datosingreso);
                                #endregion
                                lst.Add(registro);
                                //Message.AppendLine("Finaliza " + DateTime.Now.ToString());
                                registro.Resultado = Message.ToString();
                                CurrentRow++;
                                cell = sheet.Cell(CurrentRow, FirstColumn);
                                cell2 = sheet.Cell(CurrentRow, FirstColumn + 1);
                                cell3 = sheet.Cell(CurrentRow, FirstColumn + 2);
                                row1 = sheet.Cell(CurrentRow, FirstColumn);
                                row2 = sheet.Cell(CurrentRow + 1, FirstColumn);
                                row3 = sheet.Cell(CurrentRow + 2, FirstColumn);

                                item.PorcentajeAvance = (int)((double)j / (double)Total * 100);
                                context.SaveChanges();
                            }
                            #endregion

                            foreach (var registro in lst)
                                context.CORP_Registro.Add(registro);

                            observaciones.AppendLine("Archivo cargado");
                            observaciones.AppendLine("Archivo: " + item.FileMasivosID.ToString() + " Fin:" + DateTime.Now.ToString());
                            item.Observaciones = observaciones.ToString();

                            if (lst.Count(r => r.Estado == 2) > 0)
                                item.EstadoProceso = 2; // si hay algun registro con error, va a estado para que abran la pantalla de log de errores
                            else
                                item.EstadoProceso = 6; // si no hay ninguno con registro de error, puede ir a la aprobación automática
                            item.PorcentajeAvance = 100;
                            context.SaveChanges();
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

        static string FixIt(string old)
        {
            return old.Replace("(", "").Replace(")", "");
        }

        static void Clean(Stream fileName, Func<string, string> fixUri)
        {
            using (ZipFile zip = ZipFile.Read(fileName))
            {
                ZipEntry item = zip["excel / _rels / document.xml.rels"];
                MemoryStream stream = new MemoryStream();
                item.Extract(stream);
                stream.Position = 0;
                XElement doc = XElement.Load(new StreamReader(stream));
                bool changed = false;
                foreach (XElement el in doc.Descendants()
                    .Where(n => n.Attribute("TargetMode") != null && n.Attribute("TargetMode").Value == "External"
                        && !Uri.IsWellFormedUriString(n.Attribute("Target").Value, UriKind.Absolute)))
                {
                    el.Attribute("Target").Value = fixUri(el.Attribute("Target").Value);
                    changed = true;
                }
                if (changed)
                {
                    zip.UpdateEntry(item.FileName, doc.ToString());
                    zip.Save();
                }
            }
        }

        private static string ParseCobertura(string s)
        {
            string v = s.Trim();

            // falta hacer el cruce con la base de datos
            if (v.ToUpper() == "AA / TITULAR") return "AA";
            if (v.ToUpper() == "AT / TITULAR") return "AA";
            if (v.ToUpper() == "AA / AFILIADO") return "AA";
            if (v.ToUpper() == "AT / AFILIADO") return "AA";
            if (v.ToUpper() == "AA / TITULAR AFILIADO") return "AA";
            if (v.ToUpper() == "AT / TITULAR AFILIADO") return "AA";
            if (v.ToUpper() == "AA /TITULAR") return "AA";
            if (v.ToUpper() == "AT /TITULAR") return "AA";
            if (v.ToUpper() == "AA /AFILIADO") return "AA";
            if (v.ToUpper() == "AT /AFILIADO") return "AA";
            if (v.ToUpper() == "AA/TITULAR") return "AA";
            if (v.ToUpper() == "AT/TITULAR") return "AA";
            if (v.ToUpper() == "AA/AFILIADO") return "AA";
            if (v.ToUpper() == "AT/AFILIADO") return "AA";
            if (v.ToUpper() == "AA") return "AA";
            if (v.ToUpper() == "AT") return "AA";
            if (v.ToUpper() == "TITULAR SOLO") return "AA";
            if (v.ToUpper() == "SOLO TITULAR") return "AA";
            if (v.ToUpper() == "AFILIADO SOLO") return "AA";
            if (v.ToUpper() == "SOLO AFILIADO") return "AA";
            if (v.ToUpper() == "AFILIADO") return "AA";
            if (v.ToUpper() == "TITULAR") return "AA";

            if (v.ToUpper() == "A1 / TITULAR AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "A1 / TITULAR AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "A1 / TITULAR AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "A1 / TITULAR AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "A1 / TITULAR AFILIADO MÁS UNO") return "A1";
            if (v.ToUpper() == "A1 / TITULAR AFILIADO MÁS UNO") return "A1";

            if (v.ToUpper() == "A1 /TITULAR AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "A1 /TITULAR AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "A1 /TITULAR AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "A1 /TITULAR AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "A1 /TITULAR AFILIADO MÁS UNO") return "A1";
            if (v.ToUpper() == "A1 /TITULAR AFILIADO MÁS UNO") return "A1";

            if (v.ToUpper() == "A1/TITULAR AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "A1/TITULAR AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "A1/TITULAR AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "A1/TITULAR AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "A1/TITULAR AFILIADO MÁS UNO") return "A1";
            if (v.ToUpper() == "A1/TITULAR AFILIADO MÁS UNO") return "A1";


            if (v.ToUpper() == "A1 / AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "A1 / AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "A1 / AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "A1 / AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "A1 / AFILIADO MÁS UNO") return "A1";
            if (v.ToUpper() == "A1 / AFILIADO MÁS UNO") return "A1";

            if (v.ToUpper() == "A1 /AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "A1 /AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "A1 /AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "A1 /AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "A1 /AFILIADO MÁS UNO") return "A1";
            if (v.ToUpper() == "A1 /AFILIADO MÁS UNO") return "A1";

            if (v.ToUpper() == "A1/AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "A1/AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "A1/AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "A1/AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "A1/AFILIADO MÁS UNO") return "A1";
            if (v.ToUpper() == "A1/AFILIADO MÁS UNO") return "A1";



            if (v.ToUpper() == "A1 / TITULAR MAS UNO") return "A1";
            if (v.ToUpper() == "A1 / TITULAR MAS UNO") return "A1";
            if (v.ToUpper() == "A1 / TITULAR MÀS UNO") return "A1";
            if (v.ToUpper() == "A1 / TITULAR MÀS UNO") return "A1";
            if (v.ToUpper() == "A1 / TITULAR MÁS UNO") return "A1";
            if (v.ToUpper() == "A1 / TITULAR MÁS UNO") return "A1";

            if (v.ToUpper() == "A1 /TITULAR MAS UNO") return "A1";
            if (v.ToUpper() == "A1 /TITULAR MAS UNO") return "A1";
            if (v.ToUpper() == "A1 /TITULAR MÀS UNO") return "A1";
            if (v.ToUpper() == "A1 /TITULAR MÀS UNO") return "A1";
            if (v.ToUpper() == "A1 /TITULAR MÁS UNO") return "A1";
            if (v.ToUpper() == "A1 /TITULAR MÁS UNO") return "A1";

            if (v.ToUpper() == "A1/TITULAR MAS UNO") return "A1";
            if (v.ToUpper() == "A1/TITULAR MAS UNO") return "A1";
            if (v.ToUpper() == "A1/TITULAR MÀS UNO") return "A1";
            if (v.ToUpper() == "A1/TITULAR MÀS UNO") return "A1";
            if (v.ToUpper() == "A1/TITULAR MÁS UNO") return "A1";
            if (v.ToUpper() == "A1/TITULAR MÁS UNO") return "A1";

            if (v.ToUpper() == "A1") return "A1";
            if (v.ToUpper() == "TITULAR AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "TITULAR AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "TITULAR AFILIADO MÁS UNO") return "A1";
            if (v.ToUpper() == "AFILIADO MAS UNO") return "A1";
            if (v.ToUpper() == "AFILIADO MÀS UNO") return "A1";
            if (v.ToUpper() == "AFILIADO MÁS UNO") return "A1";
            if (v.ToUpper() == "TITULAR MAS UNO") return "A1";
            if (v.ToUpper() == "TITULAR MÀS UNO") return "A1";
            if (v.ToUpper() == "TITULAR MÁS UNO") return "A1";
            if (v.ToUpper() == "TITULAR + 1") return "A1";
            if (v.ToUpper() == "TITULAR + UNO") return "A1";
            if (v.ToUpper() == "AFILIADO + 1") return "A1";
            if (v.ToUpper() == "AFILIADO + UNO") return "A1";

            if (v.ToUpper() == "AF / TITULAR AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / TITULAR AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / TITULAR AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / TITULAR AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / TITULAR AFILIADO MÁS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / TITULAR AFILIADO MÁS FAMILIA") return "AF";

            if (v.ToUpper() == "AF / TITULAR MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / TITULAR MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / TITULAR MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / TITULAR MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / TITULAR MÁS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / TITULAR MÁS FAMILIA") return "AF";

            if (v.ToUpper() == "AF / AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / AFILIADO MÁS FAMILIA") return "AF";
            if (v.ToUpper() == "AF / AFILIADO MÁS FAMILIA") return "AF";


            if (v.ToUpper() == "AF /TITULAR AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /TITULAR AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /TITULAR AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /TITULAR AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /TITULAR AFILIADO MÁS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /TITULAR AFILIADO MÁS FAMILIA") return "AF";

            if (v.ToUpper() == "AF /TITULAR MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /TITULAR MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /TITULAR MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /TITULAR MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /TITULAR MÁS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /TITULAR MÁS FAMILIA") return "AF";

            if (v.ToUpper() == "AF /AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /AFILIADO MÁS FAMILIA") return "AF";
            if (v.ToUpper() == "AF /AFILIADO MÁS FAMILIA") return "AF";


            if (v.ToUpper() == "AF/TITULAR AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/TITULAR AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/TITULAR AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/TITULAR AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/TITULAR AFILIADO MÁS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/TITULAR AFILIADO MÁS FAMILIA") return "AF";

            if (v.ToUpper() == "AF/TITULAR MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/TITULAR MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/TITULAR MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/TITULAR MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/TITULAR MÁS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/TITULAR MÁS FAMILIA") return "AF";

            if (v.ToUpper() == "AF/AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/AFILIADO MÁS FAMILIA") return "AF";
            if (v.ToUpper() == "AF/AFILIADO MÁS FAMILIA") return "AF";

            if (v.ToUpper() == "AF") return "AF";
            if (v.ToUpper() == "TITULAR AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "TITULAR AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "TITULAR AFILIADO MÁS FAMILIA") return "AF";
            if (v.ToUpper() == "TITULAR MAS FAMILIA") return "AF";
            if (v.ToUpper() == "TITULAR MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "TITULAR MÁS FAMILIA") return "AF";
            if (v.ToUpper() == "AFILIADO MAS FAMILIA") return "AF";
            if (v.ToUpper() == "AFILIADO MÀS FAMILIA") return "AF";
            if (v.ToUpper() == "AFILIADO MÁS FAMILIA") return "AF";

            if (v.ToUpper().StartsWith("AA")) return "AA";
            if (v.ToUpper().StartsWith("AT")) return "AA";
            if (v.ToUpper().StartsWith("A1")) return "A1";
            if (v.ToUpper().StartsWith("AF")) return "AF";


            return "";
        }

        private static int? CedulaPasaporte(string s)
        {
            string v = s.Trim();
            if (v.ToUpper() == "CEDULA") return 1;
            if (v.ToUpper() == "CÉDULA") return 1;
            if (v.ToUpper() == "C") return 1;
            if (v.ToUpper() == "PASAPORTE") return 2;
            if (v.ToUpper() == "PASPORTE") return 2;
            if (v.ToUpper() == "P") return 2;
            return 0; // si no reconoce, devuelve 0
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

        public static string Validate_Mail(string email)
        {
            try
            {
                ValidacionMail.ServiciosValidacionEmailClient client = new ValidacionMail.ServiciosValidacionEmailClient();
                ValidacionMail.VerificacionEmailChecker check = client.ValidarEmailChecker(email);
                string response = string.Empty;
                if (check != null)
                {
                    if (check.status == "Bad")
                    {
                        response = "La dirección de correo electrónico ingresada no existe. ";
                        if (!string.IsNullOrEmpty(check.emailAddressSuggestion))
                            response += "Se sugiere la siguiente dirección: " + check.emailAddressSuggestion;
                    }
                }
                else
                {
                    response = "La dirección de correo electrónica está vacía.";
                }

                return response;
            }
            catch (Exception ex)
            {
                SW.Common.ExceptionManager.ReportException(ex, SW.Common.ExceptionManager.ExceptionSources.Server);
                return string.Empty;
            }
        }

        public static string getString(IXLCell celda)
        {
            try
            {
                //validacion de celda
                string value = celda.GetString();
                return value;
            }
            catch (Exception ex)
            {
                string value = celda.CachedValue.ToString();
                return value;
            }
        }

    }

    class SubSucursal
    {
        public int id;
        public string cobertura; // nombre de la cobertura EXE,COR,DEN,ONC
        public string plan; //nombre del Plan AT,A1, AF
        public bool opcional;
    }

}
