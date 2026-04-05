using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Core.API.AsyncDataService;
using Core.API.EventProcessing;
using Core.API.Grpc;
using Core.DAL.Context;
using Core.DAL.Models;
using Core.DL.Repositories;
using Core.DL.Services;

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
builder.Services.AddTransient<IBaseRepository<Teacher>, BaseRepository<Teacher>>();
builder.Services.AddTransient<IBaseRepository<EntryRequest>, BaseRepository<EntryRequest>>();
builder.Services.AddTransient<IBaseRepository<StudentEnrollment>, BaseRepository<StudentEnrollment>>();
builder.Services.AddTransient<IBaseRepository<TeacherEnrollment>, BaseRepository<TeacherEnrollment>>();

//Services
builder.Services.AddTransient<IBaseUserService, BaseUserService>();
builder.Services.AddTransient<IUniversityService, UniversityService>();
builder.Services.AddTransient<IStudentService, StudentService>();
builder.Services.AddTransient<ITeacherService, TeacherService>();
builder.Services.AddTransient<IDirectorService, DirectorService>();
builder.Services.AddTransient<IGeneralInfoService, GeneralUserInfoService>();
builder.Services.AddSingleton<IEventProcessor, EventProcessor>();

//Background tasks
builder.Services.AddHostedService<MessageBusSubscriber>();

//Databases
var connectionString = builder.Configuration.GetConnectionString("DataConnectionString")
                    ?? throw new InvalidOperationException("Connection string 'DataConnectionString' not found.");

builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(connectionString
    , b => b.MigrationsAssembly("Core.API")));

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
        opts.RequireHttpsMetadata = false;

        var metadataAddress = builder.Configuration["OpenId:MetadataAddress"];
        if (!string.IsNullOrWhiteSpace(metadataAddress))
            opts.MetadataAddress = metadataAddress;

        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["OpenId:Issuer"],
            ValidateAudience = false,
            ValidateLifetime = true,
        };

        // Fallback public key used when MetadataAddress is unavailable
        var publicKeyB64 = builder.Configuration["OpenId:PublicKey"];
        if (!string.IsNullOrWhiteSpace(publicKeyB64))
        {
            var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKeyB64), out _);
            opts.TokenValidationParameters.IssuerSigningKey = new RsaSecurityKey(rsa) { KeyId = "ttl-rsa-key-1" };
        }
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
