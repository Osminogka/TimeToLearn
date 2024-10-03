using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Users.API.AsyncDataService;
using Users.API.EventProcessing;
using Users.API.Grpc;
using Users.DAL.Context;
using Users.DAL.Models;
using Users.DL.Repositories;
using Users.DL.Services;

var builder = WebApplication.CreateBuilder(args);

//Logger
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

//Grpc
builder.Services.AddGrpc();

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

builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(connectionString
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

//Grpc
app.MapGrpcService<GrpcUserInfoService>();
app.MapGet("/protos/userinfo.proto", async context =>
{
    await context.Response.WriteAsync(File.ReadAllText("Protos/userinfo.proto"));
});

//app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.UseDefaultFiles();

var scope = app.Services.CreateAsyncScope();
scope.ServiceProvider.GetRequiredService<DataContext>().Database.Migrate();

app.Run();
