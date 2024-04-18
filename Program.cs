using Master.Data;
using Master.Domain;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string allowAllOrigins = "AllowAll";

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
.AddDbContext<AppDbContext>(opts =>
{
    opts
        .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.Scan(scan => scan.FromAssemblies(AppDomain.CurrentDomain.Load("Master"))
    .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service")))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

builder.Services.AddCors(options =>
{
    options.AddPolicy(allowAllOrigins, policy =>
    {

        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandler>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseCors(allowAllOrigins);

app.Run();
