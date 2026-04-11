using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Forums.DAL.Context;
using Forums.DAL.Models;
using Forums.DL.Repositories;
using Forums.DL.Grpc;
using Forums.DL.Services;

var builder = WebApplication.CreateBuilder(args);

//Logger
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

//Automapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

//Grpc
builder.Services.AddScoped<IUserInfoClient, UserInfoClient>();

//Repositories
builder.Services.AddTransient<IBaseRepository<Topic>, BaseRepository<Topic>>();
builder.Services.AddTransient<IBaseRepository<Comment>, BaseRepository<Comment>>();
builder.Services.AddTransient<IBaseRepository<Like>, BaseRepository<Like>>();
builder.Services.AddTransient<IBaseRepository<Dislike>, BaseRepository<Dislike>>();

//Services
builder.Services.AddTransient<ITopicService, TopicService>();
builder.Services.AddTransient<ICommentService, CommentService>();

var connectionString = builder.Configuration.GetConnectionString("DataConnectionString")
                    ?? throw new InvalidOperationException("Connection string 'DataConnectionString' not found.");

builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(connectionString
        , b => b.MigrationsAssembly("Forums.API")));

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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseDefaultFiles();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<DataContext>().Database.Migrate();

app.Run();
