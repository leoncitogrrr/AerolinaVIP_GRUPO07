using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Principal;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace ConAuntenticacion.Models
{
    // Consultas sobre la base AerolineaVIP_Identity (usuarios y roles).
    // Reserva.UsuarioId guarda el Id de AspNetUsers: como está en OTRA base
    // de datos no existe una FK física, por eso la aplicación lo valida aquí.
    public static class ServicioUsuarios
    {
        // Devuelve el Id del usuario si existe y tiene el rol Cliente; si no, null
        public static string BuscarIdCliente(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return null;
            }
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                UserManager<ApplicationUser> userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
                ApplicationUser user = userManager.FindByName(userName.Trim());
                if (user == null)
                {
                    return null;
                }
                return userManager.IsInRole(user.Id, RolesSistema.Cliente) ? user.Id : null;
            }
        }

        // Id -> "usuario (Nombres Apellidos)" para mostrar en listas y comprobantes
        public static Dictionary<string, string> NombresUsuarios()
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                return db.Users.ToList().ToDictionary(
                    u => u.Id,
                    u => u.UserName + (string.IsNullOrEmpty(u.Nombres) ? "" : " (" + u.Nombres + " " + u.Apellidos + ")"));
            }
        }

        public static List<UsuarioItemViewModel> ListarUsuarios(string filtro)
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                Dictionary<string, string> roles = db.Roles.ToList().ToDictionary(r => r.Id, r => r.Name);
                IQueryable<ApplicationUser> consulta = db.Users.Include(u => u.Roles);
                if (!string.IsNullOrWhiteSpace(filtro))
                {
                    string f = filtro.Trim();
                    consulta = consulta.Where(u => u.UserName.Contains(f) || u.Email.Contains(f) || u.Nombres.Contains(f) || u.Apellidos.Contains(f));
                }
                return consulta.OrderBy(u => u.UserName).ToList().Select(u => new UsuarioItemViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    NombreCompleto = (u.Nombres + " " + u.Apellidos).Trim(),
                    Roles = string.Join(", ", u.Roles.Select(ur => roles.ContainsKey(ur.RoleId) ? roles[ur.RoleId] : ur.RoleId))
                }).ToList();
            }
        }

        // Solo el SuperUsuario llama a este método (ver AdminController): deja al usuario con UN rol
        public static void AsignarRol(string usuarioId, string rol)
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                UserManager<ApplicationUser> userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
                string[] actuales = userManager.GetRoles(usuarioId).ToArray();
                if (actuales.Length > 0)
                {
                    userManager.RemoveFromRoles(usuarioId, actuales);
                }
                userManager.AddToRole(usuarioId, rol);
            }
        }

        // CRUD DE USUARIOS (pantalla 20 · MOD-01 y MOD-10)

        // Roles que puede asignar quien está conectado:
        // SuperUsuario -> todos; Administrador -> solo Cliente y Counter
        public static List<string> RolesAsignables(IPrincipal actor)
        {
            if (actor != null && actor.IsInRole(RolesSistema.SuperUsuario))
            {
                return new List<string> { RolesSistema.Cliente, RolesSistema.Counter, RolesSistema.Administrador, RolesSistema.SuperUsuario };
            }
            if (actor != null && actor.IsInRole(RolesSistema.Administrador))
            {
                return new List<string> { RolesSistema.Cliente, RolesSistema.Counter };
            }
            return new List<string>();
        }

        public static string RolDe(string usuarioId)
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                UserManager<ApplicationUser> userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
                return userManager.GetRoles(usuarioId).FirstOrDefault();
            }
        }

        // El Administrador solo administra cuentas de Cliente y Counter; el SuperUsuario, todas
        public static bool PuedeAdministrar(IPrincipal actor, string usuarioId)
        {
            string rol = RolDe(usuarioId);
            return rol == null || RolesAsignables(actor).Contains(rol);
        }

        public static UsuarioFormViewModel Obtener(string usuarioId)
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                ApplicationUser u = db.Users.FirstOrDefault(x => x.Id == usuarioId);
                if (u == null)
                {
                    return null;
                }
                UsuarioFormViewModel m = new UsuarioFormViewModel();
                m.Id = u.Id;
                m.UserName = u.UserName;
                m.Nombres = u.Nombres;
                m.Apellidos = u.Apellidos;
                m.Email = u.Email;
                m.Rol = RolDe(u.Id);
                return m;
            }
        }

        public static bool ExisteUserName(string userName, string excluirId)
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                string n = (userName ?? "").Trim();
                return db.Users.Any(u => u.UserName == n && u.Id != excluirId);
            }
        }

        public static bool ExisteEmail(string email, string excluirId)
        {
            using (ApplicationDbContext db = new ApplicationDbContext())
            {
                string e = (email ?? "").Trim();
                return db.Users.Any(u => u.Email == e && u.Id != excluirId);
            }
        }

        // Crea la cuenta con la política de contraseñas de IdentityConfig y le asigna su rol
        public static void Crear(ApplicationUserManager userManager, UsuarioFormViewModel m)
        {
            ApplicationUser user = new ApplicationUser
            {
                UserName = m.UserName.Trim(),
                Email = m.Email.Trim(),
                Nombres = m.Nombres.Trim(),
                Apellidos = m.Apellidos.Trim(),
                EmailConfirmed = true
            };
            IdentityResult resultado = userManager.Create(user, m.Password);
            if (!resultado.Succeeded)
            {
                throw new ReglaNegocioException(string.Join(" ", resultado.Errors));
            }
            userManager.AddToRole(user.Id, m.Rol);
        }

        public static void Actualizar(ApplicationUserManager userManager, UsuarioFormViewModel m, bool cambiarRol)
        {
            ApplicationUser user = userManager.FindById(m.Id);
            if (user == null)
            {
                throw new ReglaNegocioException("El usuario no existe.");
            }
            user.Nombres = m.Nombres.Trim();
            user.Apellidos = m.Apellidos.Trim();
            user.Email = m.Email.Trim();
            IdentityResult resultado = userManager.Update(user);
            if (!resultado.Succeeded)
            {
                throw new ReglaNegocioException(string.Join(" ", resultado.Errors));
            }
            if (cambiarRol)
            {
                string[] actuales = userManager.GetRoles(user.Id).ToArray();
                if (!actuales.Contains(m.Rol))
                {
                    if (actuales.Length > 0)
                    {
                        userManager.RemoveFromRoles(user.Id, actuales);
                    }
                    userManager.AddToRole(user.Id, m.Rol);
                }
            }
        }

        // No se elimina una cuenta propia ni una que tenga reservas (se perdería la titularidad)
        public static string EvaluarEliminacion(string usuarioId, string actorId)
        {
            if (usuarioId == actorId)
            {
                return "No puede eliminar su propia cuenta.";
            }
            using (AerolineaVIPEntities dominio = new AerolineaVIPEntities())
            {
                if (dominio.Reserva.Any(r => r.UsuarioId == usuarioId))
                {
                    return "El usuario tiene reservas registradas; no se puede eliminar para conservar el historial.";
                }
            }
            return null;
        }

        public static void Eliminar(ApplicationUserManager userManager, string usuarioId, string actorId)
        {
            string error = EvaluarEliminacion(usuarioId, actorId);
            if (error != null)
            {
                throw new ReglaNegocioException(error);
            }
            ApplicationUser user = userManager.FindById(usuarioId);
            if (user == null)
            {
                throw new ReglaNegocioException("El usuario no existe.");
            }
            IdentityResult resultado = userManager.Delete(user);
            if (!resultado.Succeeded)
            {
                throw new ReglaNegocioException(string.Join(" ", resultado.Errors));
            }
        }
    }
}