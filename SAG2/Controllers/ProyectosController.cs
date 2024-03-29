﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SAG2.Models;
using SAG2.Classes;

namespace SAG2.Controllers
{ 
    public class ProyectosController : Controller
    {
        private SAG2DB db = new SAG2DB();
        private Util utils = new Util();

        //
        // GET: /Proyectos/

        public ViewResult Index(string q = "")
        {
            Usuario usuario = (Usuario)Session["Usuario"];
            List<Proyecto> proyecto = new List<Proyecto>();

            if (usuario.esAdministrador)
            {
                proyecto = db.Proyecto.OrderBy(p => p.Nombre).ToList();
            }
            else if (usuario.esSupervisor)
            {
                proyecto = db.Rol.Where(r => r.PersonaID == usuario.PersonaID).Select(r => r.Proyecto).Where(r => r.Eliminado == null).Distinct().OrderBy(r => r.Nombre).ToList();
            }
            else
            {
                proyecto = db.Rol.Where(r => r.PersonaID == usuario.PersonaID).Select(r => r.Proyecto).Where(r => r.Eliminado == null).Where(r => r.Cerrado == null).Distinct().OrderBy(r => r.Nombre).ToList();
            }

            return View(proyecto.ToList());
        }

        //
        // GET: /Proyectos/Details/5

        public ViewResult Details(int id)
        {
            Proyecto proyecto = db.Proyecto.Find(id);
            return View(proyecto);
        }

        //
        // GET: /Proyectos/Create

        public ActionResult Create()
        {
            Usuario usuario = (Usuario)Session["Usuario"];

            // Si el usuario no es administrador, solo puede editar sus Proyectos (No crear)
            if (!usuario.esAdministrador)
            {
                Proyecto proyecto = (Proyecto)Session["Proyecto"];
                return RedirectToAction("Edit", new { id = proyecto.ID });
            }

            ViewBag.PersonaID = new SelectList(db.Persona.OrderBy(p => p.Nombres).ThenBy(p => p.ApellidoParterno).ThenBy(p => p.ApellidoMaterno), "ID", "NombreLista");
            ViewBag.SistemaAsistencialID = new SelectList(db.SistemaAsistencial, "ID", "NombreLista");
            ViewBag.TipoProyectoID = new SelectList(db.TipoProyecto, "ID", "NombreLista");
            ViewBag.RegionID = new SelectList(db.Region.OrderBy(a => a.Nombre), "ID", "Nombre");
            ViewBag.BancoID = new SelectList(db.Banco, "ID", "Nombre");
            return View();
        } 

        //
        // POST: /Proyectos/Create

        [HttpPost]
        public ActionResult Create(Proyecto proyecto)
        {
            Usuario usuario = (Usuario)Session["Usuario"];
            if (!usuario.Administrador.Equals("S"))
            {
                Proyecto tmp = (Proyecto)Session["Proyecto"];
                return RedirectToAction("Edit", new { id = tmp.ID });
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // Ingreso dirección
                    Direccion direccion = proyecto.Direccion;
                    direccion.Mostrar = 1;
                    direccion.ComunaID = Int32.Parse(Request.Form["ComunaID"].ToString());
                    db.Direccion.Add(direccion);
                    db.SaveChanges();

                    // Ingreso convenio
                    Convenio convenio = proyecto.Convenio;
                    if (Request.Form["FechaInicio"] != null && Request.Form["FechaTermino"] != null && !Request.Form["FechaTermino"].ToString().Equals("") && !Request.Form["FechaInicio"].ToString().Equals(""))
                    {
                        convenio.FechaInicio = DateTime.Parse(Request.Form["FechaInicio"].ToString());
                        convenio.FechaTermino = DateTime.Parse(Request.Form["FechaTermino"].ToString());
                    }
                    else
                    {
                        convenio.FechaInicio = null;
                        convenio.FechaTermino = null;
                    }

                    // Se define periodo de inicio del proyecto
                    int? periodo_inicio = Int32.Parse(Request.Form["PeriodoInicio"].ToString());
                    int? mes_inicio = Int32.Parse(Request.Form["MesInicio"].ToString());

                    convenio.Periodo = periodo_inicio;
                    convenio.Mes = mes_inicio;

                    db.Convenio.Add(convenio);
                    db.SaveChanges();

                    db.Proyecto.Add(proyecto);
                    db.SaveChanges();

                    convenio.ProyectoID = proyecto.ID;
                    db.Entry(convenio).State = EntityState.Modified;
                    db.SaveChanges();

                    if (Request.Form["Numero"] != null && Request.Form["BancoID"] != null && !Request.Form["Numero"].ToString().Equals("") && !Request.Form["BancoID"].ToString().Equals(""))
                    {
                        Direccion direccionCtaCte = new Direccion();
                        direccionCtaCte.Mostrar = 1;
                        direccionCtaCte.ComunaID = Int32.Parse(Request.Form["ComunaID"].ToString());
                        db.Direccion.Add(direccionCtaCte);
                        db.SaveChanges();

                        // Se ingresa cuenta corriente
                        CuentaCorriente cuentaCorriente = new CuentaCorriente();
                        cuentaCorriente.ProyectoID = proyecto.ID;
                        cuentaCorriente.BancoID = Int32.Parse(Request.Form["BancoID"].ToString());
                        cuentaCorriente.Numero = Request.Form["Numero"].ToString();
                        cuentaCorriente.Desactiva = 0;
                        cuentaCorriente.LineaCredito = 0;
                        cuentaCorriente.DireccionID = direccionCtaCte.ID;
                        db.CuentaCorriente.Add(cuentaCorriente);
                        db.SaveChanges();

                        proyecto.MesInicio = mes_inicio;
                        proyecto.PeriodoInicio = periodo_inicio;

                        db.Entry(proyecto).State = EntityState.Modified;
                        db.SaveChanges();

                        // Se define saldo inicial para periodo
                        Saldo saldo = new Saldo();
                        saldo.CuentaCorrienteID = cuentaCorriente.ID;
                        saldo.Mes = (int)mes_inicio;
                        saldo.Periodo = (int)periodo_inicio;

                        if (Request.Form["SaldoInicial"] != null && !Request.Form["SaldoInicial"].ToString().Equals("") && !Request.Form["SaldoInicial"].ToString().Equals("0"))
                        {
                            saldo.SaldoInicialCartola = Int32.Parse(Request.Form["SaldoInicial"].ToString());
                            saldo.SaldoFinal = saldo.SaldoInicialCartola;
                            //saldo.SaldoFinalCartola = saldo.SaldoInicialCartola;
                        }
                        else
                        { 
                            saldo.SaldoInicialCartola = 0;
                            saldo.SaldoFinal = 0;
                           // saldo.SaldoFinalCartola = 0;
                        }

                        db.Saldo.Add(saldo);
                        db.SaveChanges();
                    }

                    // Si se selecciona Supervisor, le asignamos Rol y verificamos que tenga cuenta de Usuario activa.
                    if (Request.Form["PersonaID"] != null && !Request.Form["PersonaID"].ToString().Equals(""))
                    {
                        int tipoRolID = 4;
                        int personaID = Int32.Parse(Request.Form["PersonaID"].ToString());

                        try
                        {
                            Usuario usuario_tmp = db.Usuario.Where(u => u.PersonaID == personaID).Single();
                        }
                        catch (Exception)
                        {
                            // El usuario no tiene cuenta y por lo tanto se le crea una de nivel supervisor
                            Persona persona = db.Persona.Find(personaID);
                            string nombre = (persona.Nombres.Substring(0, 1) + "" + persona.ApellidoParterno).Replace(" ", "").ToLower().ToString();

                            Usuario tmp = new Usuario();
                            tmp.PersonaID = personaID;
                            tmp.Nombre = nombre;
                            tmp.Estado = "A";
                            tmp.Administrador = "N";
                            tmp.Supervisor = "S";
                            tmp.Password = utils.md5(persona.Rut.Trim());

                            db.Usuario.Add(tmp);
                            db.SaveChanges();
                        }

                        try
                        {
                            Rol rol_tmp = db.Rol.Where(r => r.PersonaID == personaID).Where(r => r.TipoRolID == tipoRolID).Where(r => r.ProyectoID == proyecto.ID).Single();
                        }
                        catch (Exception)
                        {
                            // El usuario no tiene rol, se le asigna rol supervisor sobre el proyecto
                            Rol tmp = new Rol();
                            tmp.PersonaID = personaID;
                            tmp.ProyectoID = proyecto.ID;
                            tmp.TipoRolID = tipoRolID;

                            db.Rol.Add(tmp);
                            db.SaveChanges();
                        }
                    }

                    return RedirectToAction("Create");  
                }
            }
            catch (Exception e)
            {
                ViewBag.Mensaje = utils.mensajeError(e.Message + " " + e.StackTrace);
                if (e.InnerException != null)
                {
                    ViewBag.Mensaje = utils.mensajeError(e.InnerException.Message + " " + e.StackTrace);
                    if (e.InnerException.InnerException != null)
                    {
                        ViewBag.Mensaje = utils.mensajeError(e.InnerException.InnerException.Message + " " + e.StackTrace);
                    }
                }
            }

            ViewBag.PersonaID = new SelectList(db.Persona.OrderBy(p => p.Nombres).ThenBy(p => p.ApellidoParterno).ThenBy(p => p.ApellidoMaterno), "ID", "NombreLista");
            ViewBag.SistemaAsistencialID = new SelectList(db.SistemaAsistencial, "ID", "NombreLista", proyecto.SistemaAsistencialID);
            ViewBag.TipoProyectoID = new SelectList(db.TipoProyecto, "ID", "NombreLista", proyecto.TipoProyectoID);
            ViewBag.RegionID = new SelectList(db.Region.OrderBy(a => a.Nombre), "ID", "Nombre");
            ViewBag.BancoID = new SelectList(db.Banco, "ID", "Nombre");
            return View(proyecto);
        }
        
        //
        // GET: /Proyectos/Edit/5
 
        public ActionResult Edit(int id)
        {
            if (Session["Mensaje"] != null)
            {
                ViewBag.Mensaje = Session["Mensaje"].ToString();
                Session.Remove("Mensaje");
            }

            Proyecto proyecto = db.Proyecto.Find(id);
            
            if (proyecto.estaCerrado)
            {
                ViewBag.Mensaje = utils.mensajeAdvertencia("Este Proyecto está cerrado, no es posible modificarlo.");
                ViewBag.Script = utils.desabilitarFormulario();
            }

            ViewBag.SistemaAsistencialID = new SelectList(db.SistemaAsistencial, "ID", "NombreLista", proyecto.SistemaAsistencialID);
            ViewBag.TipoProyectoID = new SelectList(db.TipoProyecto, "ID", "NombreLista", proyecto.TipoProyectoID);
            ViewBag.RegionID = new SelectList(db.Region.OrderBy(a => a.Nombre), "ID", "Nombre", proyecto.Direccion.Comuna.RegionID);
            return View(proyecto);
        }

        //
        // POST: /Proyectos/Edit/5

        [HttpPost]
        public ActionResult Edit(Proyecto proyecto)
        {
            int periodo = (int)Session["Periodo"];
            int mes = (int)Session["Mes"];

            if (ModelState.IsValid)
            {
                // Modificación dirección
                Direccion Direccion = db.Direccion.Find(Int32.Parse(Request.Form["DireccionID"].ToString())); ;
                Direccion.Calle = proyecto.Direccion.Calle;
                Direccion.Numero = proyecto.Direccion.Numero;
                Direccion.Depto = proyecto.Direccion.Depto;
                Direccion.ComunaID = Int32.Parse(Request.Form["ComunaID"].ToString());
                db.Entry(Direccion).State = EntityState.Modified;
                db.SaveChanges();
                proyecto.Direccion = Direccion;
                
                int convenioID = 0;

                // Modificación convenio
                //Convenio Convenio = db.Convenio.Find(Int32.Parse(Request.Form["ConvenioID"].ToString()));
                try
                {
                    Convenio Convenio = db.Convenio.Where(c => c.ProyectoID == proyecto.ID && c.Periodo == periodo && c.Mes == mes).Single();
                    convenioID = Convenio.ID;
                    Convenio.ResEx = proyecto.Convenio.ResEx;
                    Convenio.NroPlazas = proyecto.Convenio.NroPlazas;
                    Convenio.Comentarios = proyecto.Convenio.Comentarios;

                    if (Request.Form["FechaInicio"] != null && Request.Form["FechaTermino"] != null && !Request.Form["FechaTermino"].ToString().Equals("") && !Request.Form["FechaInicio"].ToString().Equals(""))
                    {
                        Convenio.FechaInicio = DateTime.Parse(Request.Form["FechaInicio"].ToString());
                        Convenio.FechaTermino = DateTime.Parse(Request.Form["FechaTermino"].ToString());
                    }
                    else
                    {
                        Convenio.FechaInicio = null;
                        Convenio.FechaTermino = null;
                    }

                    db.Entry(Convenio).State = EntityState.Modified;
                    db.SaveChanges();
                    //proyecto.Convenio = Convenio;
                }
                catch (Exception)
                {
                    //Convenio Convenio = db.Convenio.Where(c => c.ProyectoID == proyecto.ID && c.Periodo == periodo && c.Mes == mes).Single();
                    Convenio Convenio = new Convenio();
                    Convenio.ResEx = proyecto.Convenio.ResEx;
                    Convenio.NroPlazas = proyecto.Convenio.NroPlazas;
                    Convenio.Comentarios = proyecto.Convenio.Comentarios;
                    Convenio.ProyectoID = proyecto.ID;
                    Convenio.Periodo = periodo;
                    Convenio.Mes = mes;

                    if (Request.Form["FechaInicio"] != null && Request.Form["FechaTermino"] != null && !Request.Form["FechaTermino"].ToString().Equals("") && !Request.Form["FechaInicio"].ToString().Equals(""))
                    {
                        Convenio.FechaInicio = DateTime.Parse(Request.Form["FechaInicio"].ToString());
                        Convenio.FechaTermino = DateTime.Parse(Request.Form["FechaTermino"].ToString());
                    }
                    else
                    {
                        Convenio.FechaInicio = null;
                        Convenio.FechaTermino = null;
                    }

                    db.Convenio.Add(Convenio);
                    db.SaveChanges();

                    convenioID = Convenio.ID;
                }

                proyecto.Convenio = null;
                proyecto.ConvenioID = convenioID;
                
                db.Entry(proyecto).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Create");
            }
            ViewBag.SistemaAsistencialID = new SelectList(db.SistemaAsistencial, "ID", "NombreLista", proyecto.SistemaAsistencialID);
            ViewBag.TipoProyectoID = new SelectList(db.TipoProyecto, "ID", "NombreLista", proyecto.TipoProyectoID);
            ViewBag.RegionID = new SelectList(db.Region.OrderBy(a => a.Nombre), "ID", "Nombre", proyecto.Direccion.Comuna.RegionID);
            return View(proyecto);
        }

        [HttpGet, ActionName("Eliminar")]
        public ActionResult DeleteConfirmed(int id)
        {
            if (db.Contrato.Where(c => c.ProyectoID == id).Where(c => c.Activo == "S").Count() > 0)
            {
                Session.Add("Mensaje", utils.mensajeError("Existen contratos vigentes para este Proyecto, no es posible eliminarlo."));
                return RedirectToAction("Edit", new { @id = id }); 
            }

            Proyecto proyecto = db.Proyecto.Find(id);
            proyecto.Eliminado = "S";
            db.Entry(proyecto).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Create");
        }

        [HttpGet, ActionName("Cerrar")]
        public ActionResult CerrarProyecto(int id)
        {
            if (db.Contrato.Where(c => c.ProyectoID == id).Where(c => c.Activo == "S").Count() > 0)
            {
                Session.Add("Mensaje", utils.mensajeError("Existen contratos vigentes para este Proyecto, no es posible cerrarlo."));
                return RedirectToAction("Edit", new { @id = id });
            }

            Proyecto proyecto = db.Proyecto.Find(id);
            proyecto.Cerrado = "S";
            db.Entry(proyecto).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Create");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}