﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SAG2.Models
{
    public class Cuenta
    {
        [ScaffoldColumn(false)]
        public int ID { get; set; }
        public int CuentaID { get; set; }
        public int Orden { get; set; }
        [StringLength(100)]
        public string Nombre { get; set; }
        [StringLength(10)]
        public string Codigo { get; set; }
        
        // Codigos de enlace a SenaInfo
            // Ingresos
            public int? SenameTipo { get; set; }
            public int? SenameDetalle { get; set; }
            // Egresos
            public int? SenameObjetivo { get; set; }
            public int? SenameUso { get; set; }

        [StringLength(10)]
        public string CodigoTranstecnia { get; set; }
        [StringLength(1)]
        public string Tipo { get; set; }
        public string Descripcion { get; set; }

        public virtual Cuenta Padre { get; set; }
        public virtual List<Cuenta> Hijos { get; set; }
        public virtual List<Movimiento> Ingresos { get; set; }
        public virtual List<DetalleEgreso> Egresos { get; set; }
        //public virtual List<Movimiento> Egresos { get; set; }

        public virtual string NombreLista 
        {
            get 
            {
                return Codigo + " " + Nombre;
            }
        }
    }
}