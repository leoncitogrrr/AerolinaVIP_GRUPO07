using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using ConAuntenticacion.Models;

namespace ConAuntenticacion
{
    public class MvcApplication : System.Web.HttpApplication
    {
        // Se ejecuta UNA sola vez, cuando la aplicación arranca
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // ****************** CODIGO INSERTADO ******************
            // Inicialización de ASP.NET Identity por código:
            // roles, superusuario, administrador, usuarios iniciales y asignación de roles
            try
            {
                // Contexto de Identity: usa la conexión "DefaultConnection" (AerolineaVIP_Identity)
                ApplicationDbContext db = new ApplicationDbContext();

                CrearRoles(db);            // 1) Roles del sistema
                CrearSuperUsuario(db);     // 2) Usuario con acceso a todo
                CrearAdministrador(db);    // 3) Usuario con CRUD limitado
                CrearCounter(db);          // 4) Empleado de counter
                CrearClienteInicial(db);   // 5) Cliente de demostración
                AsignarRoles(db);          // 6) Se asigna cada rol a su usuario

                // Se libera la conexión
                db.Dispose();
            }
            catch (Exception ex)
            {
                // Si algo falla no se cae el sitio: el error queda registrado en un archivo
                RegistrarError("Error al inicializar Identity: " + ex.ToString());
            }
            // ******************************************************
        }

        // Crea los roles si todavía no existen
        private void CrearRoles(ApplicationDbContext db)
        {
            // RoleManager administra los roles de Identity
            var rolemanager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));

            // Lista de roles del sistema (según los perfiles de los wireframes)
            string[] roles = { "SuperUsuario", "Administrador", "Counter", "Cliente" };

            foreach (string rol in roles)
            {
                // Solo se crea si no existe, así no se duplica al reiniciar
                if (!rolemanager.RoleExists(rol))
                {
                    rolemanager.Create(new IdentityRole(rol));
                }
            }
        }

        // Método reutilizable: crea un usuario si no existe
        private void CrearUsuario(ApplicationDbContext db, string userName, string email, string nombres, string apellidos, string clave)
        {
            // UserManager administra los usuarios de Identity
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

            // Se busca por nombre de usuario
            var user = usermanager.FindByName(userName);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    Nombres = nombres,
                    Apellidos = apellidos
                };

                // Identity genera el hash de la contraseña (por eso se crea por código y no con SQL)
                var resultado = usermanager.Create(user, clave);

                // Si Identity rechaza la creación (por ejemplo, contraseña débil) se registra el motivo
                if (!resultado.Succeeded)
                {
                    RegistrarError(userName + " no se creó: " + string.Join(" | ", resultado.Errors));
                }
            }
        }

        // Superusuario: acceso a todas las funciones permitidas por el sistema
        private void CrearSuperUsuario(ApplicationDbContext db)
        {
            CrearUsuario(db, "superusuario", "superusuario@aerolineavip.com", "Super", "Usuario", "Super2026*");
        }

        // Administrador: acceso limitado a las operaciones CRUD que le correspondan
        private void CrearAdministrador(ApplicationDbContext db)
        {
            CrearUsuario(db, "administrador", "administrador@aerolineavip.com", "Administrador", "General", "Admin2026*");
        }

        // Empleado de counter: asiste a los clientes
        private void CrearCounter(ApplicationDbContext db)
        {
            CrearUsuario(db, "counter", "counter@aerolineavip.com", "Empleado", "Counter", "Counter2026*");
        }

        // Cliente inicial para poder probar el flujo de compra
        private void CrearClienteInicial(ApplicationDbContext db)
        {
            CrearUsuario(db, "cliente1", "cliente1@aerolineavip.com", "Cliente", "Demostracion", "Cliente2026*");
        }

        // Asigna el rol correspondiente a cada usuario inicial
        private void AsignarRoles(ApplicationDbContext db)
        {
            var usermanager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

            AsignarRol(usermanager, "superusuario", "SuperUsuario");
            AsignarRol(usermanager, "administrador", "Administrador");
            AsignarRol(usermanager, "counter", "Counter");
            AsignarRol(usermanager, "cliente1", "Cliente");
        }

        // Asigna un rol a un usuario solo si todavía no lo tiene
        private void AsignarRol(UserManager<ApplicationUser> usermanager, string userName, string rol)
        {
            var user = usermanager.FindByName(userName);

            if (user != null && !usermanager.IsInRole(user.Id, rol))
            {
                usermanager.AddToRole(user.Id, rol);
            }
        }

        // Escribe los errores de arranque en App_Data/error_inicio.txt para poder revisarlos
        private void RegistrarError(string mensaje)
        {
            try
            {
                string ruta = System.IO.Path.Combine(HttpRuntime.AppDomainAppPath, "App_Data", "error_inicio.txt");
                System.IO.File.AppendAllText(ruta, DateTime.Now + " - " + mensaje + Environment.NewLine);
            }
            catch
            {
                // Si no se puede escribir el archivo, no se hace nada para no detener la aplicación
            }
        }
    }
}