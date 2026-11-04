using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using ConAuntenticacion.Models;

namespace ConAuntenticacion.Controllers
{
    // Pantalla 20 (MOD-01 / MOD-10): CRUD de usuarios.
    // Administrador: cuentas de Cliente y Counter. SuperUsuario: todas, y es el único que elimina.
    [Authorize(Roles = RolesSistema.Administracion)]
    public class UsuariosController : Controller
    {
        private ApplicationUserManager UserManager
        {
            get { return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
        }

        // GET: Usuarios
        public ActionResult Index(string buscar)
        {
            ViewBag.Buscar = buscar;
            return View(ServicioUsuarios.ListarUsuarios(buscar));
        }

        // GET: Usuarios/Details/id
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UsuarioFormViewModel usuario = ServicioUsuarios.Obtener(id);
            if (usuario == null)
            {
                return HttpNotFound();
            }
            return View(usuario);
        }

        // GET: Usuarios/Create
        public ActionResult Create()
        {
            CargarRoles();
            return View(new UsuarioFormViewModel { Rol = RolesSistema.Cliente });
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UsuarioFormViewModel modelo)
        {
            modelo.Id = null;
            if (ModelState.IsValid)
            {
                try
                {
                    ServicioUsuarios.Crear(UserManager, modelo);
                    TempData["Mensaje"] = "Usuario " + modelo.UserName + " creado con el rol " + modelo.Rol + ".";
                    return RedirectToAction("Index");
                }
                catch (ReglaNegocioException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            CargarRoles();
            return View(modelo);
        }

        // GET: Usuarios/Edit/id
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UsuarioFormViewModel usuario = ServicioUsuarios.Obtener(id);
            if (usuario == null)
            {
                return HttpNotFound();
            }
            if (!ServicioUsuarios.PuedeAdministrar(User, id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "No tiene permiso para modificar esta cuenta.");
            }
            CargarRoles();
            return View(usuario);
        }

        // POST: Usuarios/Edit/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UsuarioFormViewModel modelo)
        {
            // En la edición la contraseña no se cambia (para eso está la recuperación de acceso)
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");
            ModelState.Remove("UserName");

            if (ModelState.IsValid)
            {
                try
                {
                    ServicioUsuarios.Actualizar(UserManager, modelo, modelo.Id != User.Identity.GetUserId());
                    TempData["Mensaje"] = "Datos del usuario actualizados.";
                    return RedirectToAction("Index");
                }
                catch (ReglaNegocioException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            UsuarioFormViewModel actual = ServicioUsuarios.Obtener(modelo.Id);
            modelo.UserName = actual != null ? actual.UserName : modelo.UserName;
            CargarRoles();
            return View(modelo);
        }

        // GET: Usuarios/Delete/id
        [Authorize(Roles = RolesSistema.SuperUsuario)]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UsuarioFormViewModel usuario = ServicioUsuarios.Obtener(id);
            if (usuario == null)
            {
                return HttpNotFound();
            }
            ViewBag.MotivoNoEliminar = ServicioUsuarios.EvaluarEliminacion(id, User.Identity.GetUserId());
            return View(usuario);
        }

        // POST: Usuarios/Delete/id
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesSistema.SuperUsuario)]
        public ActionResult DeleteConfirmed(string id)
        {
            try
            {
                ServicioUsuarios.Eliminar(UserManager, id, User.Identity.GetUserId());
                TempData["Mensaje"] = "Usuario eliminado.";
            }
            catch (ReglaNegocioException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        private void CargarRoles()
        {
            ViewBag.Roles = new SelectList(ServicioUsuarios.RolesAsignables(User));
        }
    }
}