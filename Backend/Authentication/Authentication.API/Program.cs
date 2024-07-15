using Authentication.DAL.Contexts;
using Authentication.DAL.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Authentication.API.Infrastructure;
using Authentication.DL.Repositories;
using Authentication.DL.Services;
using Microsoft.EntityFrameworkCore;
using Authentication.API.AsyncDataService;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddTransient<IUsersRepository, UsersRepository>();
builder.Services.AddTransient<IAuthService, AuthService>();

builder.Services.AddSingleton<IMessageBusClient, MessageBusClient>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


var connectionString = builder.Configuration.GetConnectionString("AccountConnectionString")
                    ?? throw new InvalidOperationException("Connection string 'AccountConnectionString' not found.");
if(builder.Environment.IsProduction())
    builder.Services.AddDbContext<IdentityContext>(options => options.UseSqlServer(connectionString
    , b => b.MigrationsAssembly("Authentication.API")));
else
    builder.Services.AddDbContext<IdentityContext>(options => options.UseSqlite(connectionString
    , b => b.MigrationsAssembly("Authentication.API")));



builder.Services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<IdentityContext>();

builder.Services.Configure<IdentityOptions>(opts =>
{
    opts.Password.RequiredLength = 6;
    opts.Password.RequireNonAlphanumeric = true;
    opts.Password.RequireLowercase = true;
    opts.Password.RequireUppercase = true;
    opts.Password.RequireDigit = true;
    opts.User.RequireUniqueEmail = true;
    opts.User.AllowedUserNameCharacters = "1234567890qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM_";
});

//Jwt configuration starts here
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opts =>
    {
        opts.SaveToken = true;
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = new SymmetricSecurityKey
            (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
        };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();

var app = builder.Build();

//app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseDefaultFiles();

PrepDb.PrepMemberRoles(app);

app.Run();
