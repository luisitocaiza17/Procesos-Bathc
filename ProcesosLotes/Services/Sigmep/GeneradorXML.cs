using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SW.Salud.Services.Sigmep
{
    public static class GeneradorXml
    {
        public static XmlDocument CrearDocumentoXML()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode XLog = doc.CreateElement("logCambios");
            doc.AppendChild(XLog);

            XmlNode Fechas = doc.CreateElement("fechas");
            XLog.AppendChild(Fechas);


            XmlNode persona_cedula = doc.CreateElement("persona_cedula");
            XmlNode persona_pasaporte = doc.CreateElement("persona_pasaporte");
            XmlNode persona_tipo_documento = doc.CreateElement("persona_tipo_documento");
            XmlNode persona_numero = doc.CreateElement("persona_numero");
            XmlNode persona_nombres = doc.CreateElement("persona_nombres");
            XmlNode persona_apellido_pater = doc.CreateElement("persona_apellido_pater");
            XmlNode persona_apellido_mater = doc.CreateElement("persona_apellido_mater");
            XmlNode persona_sexo = doc.CreateElement("persona_sexo");
            XmlNode persona_fecha_nacimiento = doc.CreateElement("persona_fecha_nacimiento");
            XmlNode domicilio_ciudad = doc.CreateElement("domicilio_ciudad");
            XmlNode domicilio_principal = doc.CreateElement("domicilio_principal");
            XmlNode domicilio_transversal = doc.CreateElement("domicilio_transversal");
            XmlNode domiciliio_numero = doc.CreateElement("domiciliio_numero");
            XmlNode domicilio_referencia = doc.CreateElement("domicilio_referencia");
            XmlNode domicilio_latitud = doc.CreateElement("domicilio_latitud");
            XmlNode domicilio_longitud = doc.CreateElement("domicilio_longitud");
            XmlNode domicilio_georeferenciada = doc.CreateElement("domicilio_georeferenciada");
            XmlNode domicilio_email = doc.CreateElement("domicilio_email");
            XmlNode domicilio_telefono = doc.CreateElement("domicilio_telefono");
            XmlNode celular = doc.CreateElement("celular");
            XmlNode nom_emp_trabajo = doc.CreateElement("nom_emp_trabajo");
            XmlNode trabajo_email = doc.CreateElement("trabajo_email");
            XmlNode trabajo_telefono = doc.CreateElement("trabajo_telefono");
            XmlNode trabajo_ciudad = doc.CreateElement("trabajo_ciudad");
            XmlNode trabajo_principal = doc.CreateElement("trabajo_principal");
            XmlNode trabajo_transversal = doc.CreateElement("trabajo_transversal");
            XmlNode trabajo_numero_edificio = doc.CreateElement("trabajo_numero_edificio");
            XmlNode trabajo_referencia = doc.CreateElement("trabajo_referencia");
            XmlNode trabajo_latitud = doc.CreateElement("trabajo_latitud");
            XmlNode trabajo_longitud = doc.CreateElement("trabajo_longitud");
            XmlNode trabajo_georeferenciada = doc.CreateElement("trabajo_georeferenciada");
            XmlNode contacto_nombres = doc.CreateElement("contacto_nombres");
            XmlNode contacto_telefono = doc.CreateElement("contacto_telefono");
            XmlNode direccion_correspondencia = doc.CreateElement("direccion_correspondencia");
            XmlNode operadora_celular = doc.CreateElement("operadora_celular");

            XmlNode rango_ingresos_anual = doc.CreateElement("rango_ingresos_anual");
            XmlNode ocupacion = doc.CreateElement("ocupacion");
            XmlNode profesion = doc.CreateElement("profesion");
            XmlNode vehiculo = doc.CreateElement("vehiculo");
            XmlNode codicion_laboral = doc.CreateElement("codicion_laboral");
            XmlNode antiguedad_laboral = doc.CreateElement("antiguedad_laboral");
            XmlNode hobby = doc.CreateElement("hobby");
            XmlNode persona_estado_civil = doc.CreateElement("persona_estado_civil");
            XmlNode domicilio_barrio = doc.CreateElement("domicilio_barrio");
            XmlNode trabajo_barrio = doc.CreateElement("trabajo_barrio");


            Fechas.AppendChild(persona_cedula); persona_cedula.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(persona_pasaporte); persona_pasaporte.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(persona_tipo_documento); persona_tipo_documento.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(persona_numero); persona_numero.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(persona_nombres); persona_nombres.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(persona_apellido_pater); persona_apellido_pater.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(persona_apellido_mater); persona_apellido_mater.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(persona_sexo); persona_sexo.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(persona_fecha_nacimiento); persona_fecha_nacimiento.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(domicilio_ciudad); domicilio_ciudad.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(domicilio_principal); domicilio_principal.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(domicilio_transversal); domicilio_transversal.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(domiciliio_numero); domiciliio_numero.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(domicilio_referencia); domicilio_referencia.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(domicilio_latitud); domicilio_latitud.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(domicilio_longitud); domicilio_longitud.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(domicilio_georeferenciada); domicilio_georeferenciada.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(domicilio_email); domicilio_email.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(domicilio_telefono); domicilio_telefono.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(celular); celular.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(nom_emp_trabajo); nom_emp_trabajo.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(trabajo_email); trabajo_email.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(trabajo_telefono); trabajo_telefono.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(trabajo_ciudad); trabajo_ciudad.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(trabajo_principal); trabajo_principal.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(trabajo_transversal); trabajo_transversal.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(trabajo_numero_edificio); trabajo_numero_edificio.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(trabajo_referencia); trabajo_referencia.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(trabajo_latitud); trabajo_latitud.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(trabajo_longitud); trabajo_longitud.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(trabajo_georeferenciada); trabajo_georeferenciada.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(contacto_nombres); contacto_nombres.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(contacto_telefono); contacto_telefono.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(direccion_correspondencia); direccion_correspondencia.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(operadora_celular); operadora_celular.InnerText = DateTime.Today.ToShortDateString();

            Fechas.AppendChild(rango_ingresos_anual); rango_ingresos_anual.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(ocupacion); ocupacion.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(profesion); profesion.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(vehiculo); vehiculo.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(codicion_laboral); codicion_laboral.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(antiguedad_laboral); antiguedad_laboral.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(hobby); hobby.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(persona_estado_civil); persona_estado_civil.InnerText = DateTime.Today.ToShortDateString();

            Fechas.AppendChild(domicilio_barrio); domicilio_barrio.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(trabajo_barrio); trabajo_barrio.InnerText = DateTime.Today.ToShortDateString();




            return doc;


        }

        public static XmlDocument CrearDocumentoXML(DateTime fecha)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode XLog = doc.CreateElement("logCambios");
            doc.AppendChild(XLog);

            XmlNode Fechas = doc.CreateElement("fechas");
            XLog.AppendChild(Fechas);


            XmlNode persona_cedula = doc.CreateElement("persona_cedula");
            XmlNode persona_pasaporte = doc.CreateElement("persona_pasaporte");
            XmlNode persona_tipo_documento = doc.CreateElement("persona_tipo_documento");
            XmlNode persona_numero = doc.CreateElement("persona_numero");
            XmlNode persona_nombres = doc.CreateElement("persona_nombres");
            XmlNode persona_apellido_pater = doc.CreateElement("persona_apellido_pater");
            XmlNode persona_apellido_mater = doc.CreateElement("persona_apellido_mater");
            XmlNode persona_sexo = doc.CreateElement("persona_sexo");
            XmlNode persona_fecha_nacimiento = doc.CreateElement("persona_fecha_nacimiento");
            XmlNode domicilio_ciudad = doc.CreateElement("domicilio_ciudad");
            XmlNode domicilio_principal = doc.CreateElement("domicilio_principal");
            XmlNode domicilio_transversal = doc.CreateElement("domicilio_transversal");
            XmlNode domiciliio_numero = doc.CreateElement("domiciliio_numero");
            XmlNode domicilio_referencia = doc.CreateElement("domicilio_referencia");
            XmlNode domicilio_latitud = doc.CreateElement("domicilio_latitud");
            XmlNode domicilio_longitud = doc.CreateElement("domicilio_longitud");
            XmlNode domicilio_georeferenciada = doc.CreateElement("domicilio_georeferenciada");
            XmlNode domicilio_email = doc.CreateElement("domicilio_email");
            XmlNode domicilio_telefono = doc.CreateElement("domicilio_telefono");
            XmlNode celular = doc.CreateElement("celular");
            XmlNode nom_emp_trabajo = doc.CreateElement("nom_emp_trabajo");
            XmlNode trabajo_email = doc.CreateElement("trabajo_email");
            XmlNode trabajo_telefono = doc.CreateElement("trabajo_telefono");
            XmlNode trabajo_ciudad = doc.CreateElement("trabajo_ciudad");
            XmlNode trabajo_principal = doc.CreateElement("trabajo_principal");
            XmlNode trabajo_transversal = doc.CreateElement("trabajo_transversal");
            XmlNode trabajo_numero_edificio = doc.CreateElement("trabajo_numero_edificio");
            XmlNode trabajo_referencia = doc.CreateElement("trabajo_referencia");
            XmlNode trabajo_latitud = doc.CreateElement("trabajo_latitud");
            XmlNode trabajo_longitud = doc.CreateElement("trabajo_longitud");
            XmlNode trabajo_georeferenciada = doc.CreateElement("trabajo_georeferenciada");
            XmlNode contacto_nombres = doc.CreateElement("contacto_nombres");
            XmlNode contacto_telefono = doc.CreateElement("contacto_telefono");
            XmlNode direccion_correspondencia = doc.CreateElement("direccion_correspondencia");
            XmlNode operadora_celular = doc.CreateElement("operadora_celular");

            XmlNode rango_ingresos_anual = doc.CreateElement("rango_ingresos_anual");
            XmlNode ocupacion = doc.CreateElement("ocupacion");
            XmlNode profesion = doc.CreateElement("profesion");
            XmlNode vehiculo = doc.CreateElement("vehiculo");
            XmlNode codicion_laboral = doc.CreateElement("codicion_laboral");
            XmlNode antiguedad_laboral = doc.CreateElement("antiguedad_laboral");
            XmlNode hobby = doc.CreateElement("hobby");
            XmlNode persona_estado_civil = doc.CreateElement("persona_estado_civil");
            XmlNode domicilio_barrio = doc.CreateElement("domicilio_barrio");
            XmlNode trabajo_barrio = doc.CreateElement("trabajo_barrio");


            Fechas.AppendChild(persona_cedula); persona_cedula.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(persona_pasaporte); persona_pasaporte.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(persona_tipo_documento); persona_tipo_documento.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(persona_numero); persona_numero.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(persona_nombres); persona_nombres.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(persona_apellido_pater); persona_apellido_pater.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(persona_apellido_mater); persona_apellido_mater.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(persona_sexo); persona_sexo.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(persona_fecha_nacimiento); persona_fecha_nacimiento.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(domicilio_ciudad); domicilio_ciudad.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(domicilio_principal); domicilio_principal.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(domicilio_transversal); domicilio_transversal.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(domiciliio_numero); domiciliio_numero.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(domicilio_referencia); domicilio_referencia.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(domicilio_latitud); domicilio_latitud.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(domicilio_longitud); domicilio_longitud.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(domicilio_georeferenciada); domicilio_georeferenciada.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(domicilio_email); domicilio_email.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(domicilio_telefono); domicilio_telefono.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(celular); celular.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(nom_emp_trabajo); nom_emp_trabajo.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(trabajo_email); trabajo_email.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(trabajo_telefono); trabajo_telefono.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(trabajo_ciudad); trabajo_ciudad.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(trabajo_principal); trabajo_principal.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(trabajo_transversal); trabajo_transversal.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(trabajo_numero_edificio); trabajo_numero_edificio.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(trabajo_referencia); trabajo_referencia.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(trabajo_latitud); trabajo_latitud.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(trabajo_longitud); trabajo_longitud.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(trabajo_georeferenciada); trabajo_georeferenciada.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(contacto_nombres); contacto_nombres.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(contacto_telefono); contacto_telefono.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(direccion_correspondencia); direccion_correspondencia.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(operadora_celular); operadora_celular.InnerText = fecha.ToShortDateString();

            Fechas.AppendChild(rango_ingresos_anual); rango_ingresos_anual.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(ocupacion); ocupacion.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(profesion); profesion.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(vehiculo); vehiculo.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(codicion_laboral); codicion_laboral.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(antiguedad_laboral); antiguedad_laboral.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(hobby); hobby.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(persona_estado_civil); persona_estado_civil.InnerText = fecha.ToShortDateString();
            Fechas.AppendChild(domicilio_barrio); domicilio_barrio.InnerText = DateTime.Today.ToShortDateString();
            Fechas.AppendChild(trabajo_barrio); trabajo_barrio.InnerText = DateTime.Today.ToShortDateString();


            return doc;


        }


    }


}
