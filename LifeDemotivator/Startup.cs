using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(LifeDemotivator.Startup))]
namespace LifeDemotivator
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
            ConfigureAuth(app);
        }
    }
}
