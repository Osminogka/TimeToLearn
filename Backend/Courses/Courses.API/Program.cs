using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Courses.DAL.Context;
using System.Text;
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

