using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace ConAuntenticacion.Models
{
    // =====================================================================
    // Consultas sobre la base AerolineaVIP_Identity (usuarios y roles).
    // Reserva.UsuarioId guarda el Id de AspNetUsers: como está en OTRA base
    // de datos no existe una FK física, por eso la aplicación lo valida aquí.
    // =====================================================================
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
    }
}
