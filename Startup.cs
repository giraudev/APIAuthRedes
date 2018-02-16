using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PRLoginRedes.Data;
using PRLoginRedes.Models;
using PRLoginRedes.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace PRLoginRedes
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            #region Utilizando Facebook
            //utilizando o servico do faceboook
            services.AddAuthentication().AddFacebook(facebookoptions =>
            {
                /*o que está dentro do Configuration é o que precisamos colocar no appsettings.json
                verificar como o api do facebook entrega os dados:
                AppId e AppSet*/
                facebookoptions.AppId = Configuration["Authentication:Facebook:AppId"];
                facebookoptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
            });
            #endregion Fim

            #region Utilizando Google
            /*Os services são carregados no arquivo appsettings.json, colocamos os dados das redes que queremos usar como
            autenticação de login dentro de "Authentication */

            //utilizando o servico do Google
            services.AddAuthentication().AddGoogle(googleOptions =>
            {
                /*o que está dentro do Configuration é o que precisamos colocar no appsettings.json
                verificar como a api entrega o login, no caso do google é clienteId e Cliente Secret*/
                googleOptions.ClientId = Configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
            });
            #endregion Fim.

            #region Utilizando Github
            services.AddAuthentication().AddOAuth("Github", githubOptions =>
            {
                githubOptions.ClientId = Configuration["Authentication:GitHub:ClientId"];
                githubOptions.ClientSecret = Configuration["Authentication:GitHub:ClientSecret"];
                githubOptions.CallbackPath = new PathString("/signin-github");
                githubOptions.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                githubOptions.TokenEndpoint = "https://github.com/login/oauth/access_token";
                githubOptions.UserInformationEndpoint = "https://api.github.com/user";
                githubOptions.ClaimsIssuer = "OAuth2-Github";
                githubOptions.SaveTokens = true;
                githubOptions.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                githubOptions.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
                githubOptions.ClaimActions.MapJsonKey("urn:github:name", "name");
                githubOptions.ClaimActions.MapJsonKey(ClaimTypes.Email, "email", ClaimValueTypes.Email);
                githubOptions.ClaimActions.MapJsonKey("urn:github:url", "url");
                githubOptions.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        // Get the GitHub user
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();

                        var user = JObject.Parse(await response.Content.ReadAsStringAsync());

                        context.RunClaimActions(user);
                    }
                };

            });

            #endregion

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
