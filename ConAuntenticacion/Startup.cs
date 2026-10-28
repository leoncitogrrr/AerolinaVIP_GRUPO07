using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ConAuntenticacion.Startup))]
namespace ConAuntenticacion
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
