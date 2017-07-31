using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(mvcSourceCode.Startup))]
namespace mvcSourceCode
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
