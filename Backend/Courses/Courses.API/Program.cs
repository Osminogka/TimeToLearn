using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Courses.DAL.Context;
using Courses.DAL.Models;
using Courses.DL.Repositories;
using Courses.DL.Grpc;
using Courses.DL.Services;
using Grpc.Net.Client;
using UserService;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddSingleton(_ =>
    GrpcChannel.ForAddress(builder.Configuration["GrpcUsersApi"]!));
builder.Services.AddSingleton(sp =>
    new GrpcUsers.GrpcUsersClient(sp.GetRequiredService<GrpcChannel>()));
builder.Services.AddScoped<IUserInfoClient, UserInfoClient>();

builder.Services.AddTransient<IBaseRepository<Course>, BaseRepository<Course>>();
builder.Services.AddTransient<IBaseRepository<Lesson>, BaseRepository<Lesson>>();

builder.Services.AddTransient<IMarkdownService, MarkdownService>();
builder.Services.AddTransient<ICourseService, CourseService>();
builder.Services.AddTransient<ILessonService, LessonService>();


var connectionString = builder.Configuration.GetConnectionString("DataConnectionString")
                    ?? throw new InvalidOperationException("Connection string 'DataConnectionString' not found.");

builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(connectionString
        , b => b.MigrationsAssembly("Courses.API")));

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
