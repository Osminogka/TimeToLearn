using Authentication.API.Infrastructure;
using Authentication.DAL.Contexts;
using Authentication.DAL.Models;
using Authentication.DL.Repositories;
using Authentication.DL.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Authentication.API.AsyncDataService;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddTransient<IUsersRepository, UsersRepository>();
builder.Services.AddTransient<IAuthService, AuthService>();

builder.Services.AddSingleton<IRsaKeyService, RsaKeyService>();
builder.Services.AddSingleton<IMessageBusClient, MessageBusClient>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


var connectionString = builder.Configuration.GetConnectionString("AccountConnectionString")
                    ?? throw new InvalidOperationException("Connection string 'AccountConnectionString' not found.");
builder.Services.AddDbContext<IdentityContext>(options => options.UseSqlServer(connectionString
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

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opts =>
    {
        opts.SaveToken = true;

        var sp = builder.Services.BuildServiceProvider();
        var rsaKeyService = sp.GetRequiredService<IRsaKeyService>();

        opts.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = rsaKeyService.PublicKey,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidIssuer = rsaKeyService.Issuer,
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

await PrepDb.PrepMemberRoles(app);

app.Run();
