using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Users.API.AsyncDataService;
using Users.API.EventProcessing;
using Users.DAL.Context;
using Users.DAL.Models;
using Users.DL.Repositories;
using Users.DL.Services;

var builder = WebApplication.CreateBuilder(args);

//Logger
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

//Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

//Repositories
builder.Services.AddTransient<IBaseRepository<University>, BaseRepository<University>>();
builder.Services.AddTransient<IBaseRepository<BaseUser>, BaseRepository<BaseUser>>();
builder.Services.AddTransient<IBaseRepository<Student>, BaseRepository<Student>>();
builder.Services.AddTransient<IBaseRepository<Teacher>, BaseRepository<Teacher>>();
builder.Services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();

//Services
builder.Services.AddTransient<IBaseUserService, BaseUserService>();
builder.Services.AddTransient<IUniversityService, UniversityService>();
builder.Services.AddTransient<IStudentService, StudentService>();
builder.Services.AddTransient<ITeacherService, TeacherService>();
builder.Services.AddTransient<IBaseUserService, BaseUserService>();
builder.Services.AddSingleton<IEventProcessor, EventProcessor>();

//Background tasks
builder.Services.AddHostedService<MessageBusSubscriber>();

//Databases
var connectionString = builder.Configuration.GetConnectionString("DataConnectionString")
                    ?? throw new InvalidOperationException("Connection string 'DataConnectionString' not found.");
if (builder.Environment.IsProduction())
    builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(connectionString
    , b => b.MigrationsAssembly("Users.API")));
else
    builder.Services.AddDbContext<DataContext>(options => options.UseSqlite(connectionString
    , b => b.MigrationsAssembly("Users.API")));

//Authentication
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

app.UseAuthorization();

app.MapControllers();

app.UseDefaultFiles();

var scope = app.Services.CreateAsyncScope();
scope.ServiceProvider.GetRequiredService<DataContext>().Database.Migrate();

app.Run();
