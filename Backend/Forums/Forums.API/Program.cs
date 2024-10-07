using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Forums.DAL.Context;
using System.Text;
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

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.UseDefaultFiles();

app.Run();
