using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalApp.Domain.DTOs;
using MinimalApp.Domain.Entities;
using MinimalApp.Domain.Enuns;
using MinimalApp.Domain.Interfaces;
using MinimalApp.Domain.ModelViews;
using MinimalApp.Domain.Services;
using MinimalApp.Infra.Db;

#region Builder
var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();
if (string.IsNullOrEmpty(key)) key = "123456";

builder.Services.AddAuthentication(options =>
{
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidateLifetime = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
    ValidateIssuer = false,
    ValidateAudience = false,
  };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministratorService, AdministratorService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
  options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
  {
    Name = "Authorization",
    Type = SecuritySchemeType.Http,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = ParameterLocation.Header,
    Description = "Insira o seu token JWT aqui",
  });

  options.AddSecurityRequirement(new OpenApiSecurityRequirement
  {
    {
    new OpenApiSecurityScheme{
      Reference = new OpenApiReference{
        Type = ReferenceType.SecurityScheme,
        Id = "Bearer"
      }
    },
    Array.Empty<string>()
  }
  });
});

builder.Services.AddDbContext<DBContext>(options =>
{
  options.UseMySql(
      builder.Configuration.GetConnectionString("Mysql"),
      ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("Mysql"))
    );
});

var app = builder.Build();
#endregion

#region Home
app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
#endregion

#region Administradores
string GerarTokenJwt(Administrator administrator)
{
  if (string.IsNullOrEmpty(key)) return string.Empty;
  var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
  var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

  var claims = new[]
  {
    new Claim("Email", administrator.Email),
    new Claim("Perfil", administrator.Perfil),
    new Claim(ClaimTypes.Role, administrator.Perfil)
  };

  var token = new JwtSecurityToken(
      claims: claims,
      expires: DateTime.Now.AddDays(1),
      signingCredentials: credentials
  );

  return new JwtSecurityTokenHandler().WriteToken(token);
}

app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministratorService administratorService) =>
{
  var adm = administratorService.Login(loginDTO);
  if (adm != null)
  {
    string token = GerarTokenJwt(adm);
    return Results.Ok(new AdministratorLogado
    {
      Email = adm.Email,
      Perfil = adm.Perfil,
      Token = token
    });
  }
  else
    return Results.Unauthorized();
}).AllowAnonymous().WithTags("Administradores");

app.MapPost("/administradores", ([FromBody] AdministratorDTO administratorDTO, IAdministratorService administratorService) =>
{
  var validacao = new ErrorValidation
  {
    Mensagens = []
  };

  if (string.IsNullOrEmpty(administratorDTO.Email))
    validacao.Mensagens.Add("Campo Obrigatório");

  if (string.IsNullOrEmpty(administratorDTO.Senha))
    validacao.Mensagens.Add("Campo Obrigatório");

  if (administratorDTO.Perfil.ToString() == null)
    validacao.Mensagens.Add("Campo Obrigatório");

  if (validacao.Mensagens.Count > 0)
    return Results.BadRequest(validacao);

  var administrator = new Administrator
  {
    Email = administratorDTO.Email,
    Senha = administratorDTO.Email,
    Perfil = administratorDTO.Perfil.ToString() ?? Perfil.Editor.ToString(),
  };
  administratorService.Incluir(administrator);

  return Results.Created($"/administrador/{administrator.Id}", new AdministratorModelView
  {
    Id = administrator.Id,
    Email = administrator.Email,
    Perfil = administrator.Perfil,
  });
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
.WithTags("Administradores");

app.MapGet("/administradores", ([FromQuery] int? pagina, IAdministratorService administratorService) =>
{
  var adms = new List<AdministratorModelView>();
  var administradores = administratorService.Todos(pagina);
  foreach (var adm in administradores)
  {
    adms.Add(new AdministratorModelView
    {
      Id = adm.Id,
      Email = adm.Email,
      Perfil = adm.Perfil,
    });
  }
  return Results.Ok(adms);
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
.WithTags("Administradores");

app.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministratorService administratorService) =>
{
  var administrador = administratorService.BuscarPorId(id);

  if (administrador == null) return Results.NotFound();

  return Results.Ok(new AdministratorModelView
  {
    Id = administrador.Id,
    Email = administrador.Email,
    Perfil = administrador.Perfil,
  });
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
.WithTags("Administradores");

#endregion

#region Veiculos
ErrorValidation validaDTO(VehicleDTO vehicleDTO)
{
  var validacao = new ErrorValidation
  {
    Mensagens = []
  };

  if (string.IsNullOrEmpty(vehicleDTO.Nome))
    validacao.Mensagens.Add("O nome do veículo é obrigatório");

  if (string.IsNullOrEmpty(vehicleDTO.Marca))
    validacao.Mensagens.Add("A marca do veículo é obrigatória");

  if (vehicleDTO.Ano < 1950)
    validacao.Mensagens.Add("Veículo muito antigos, aceita somente anos seperiores a 1950");

  return validacao;
}

app.MapPost("/veiculos", ([FromBody] VehicleDTO vehicleDTO, IVehicleService vehicleService) =>
{
  var validacao = validaDTO(vehicleDTO);
  if (validacao.Mensagens.Count > 0)
    return Results.BadRequest(validacao);

  var veiculo = new Vehicle
  {
    Nome = vehicleDTO.Nome,
    Marca = vehicleDTO.Marca,
    Ano = vehicleDTO.Ano,
  };
  vehicleService.Incluir(veiculo);

  return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm, Editor" })
.WithTags("Veiculos");

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVehicleService vehicleService) =>
{
  var veiculos = vehicleService.Todos(pagina);

  return Results.Ok(veiculos);
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm, Editor" })
.WithTags("Veiculos");

app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
{
  var veiculo = vehicleService.BuscarPorId(id);

  if (veiculo == null) return Results.NotFound();

  return Results.Ok(veiculo);
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm, Editor" })
.WithTags("Veiculos");

app.MapPut("/veiculos/{id}", ([FromRoute] int id, VehicleDTO vehicleDTO, IVehicleService vehicleService) =>
{
  var veiculo = vehicleService.BuscarPorId(id);
  if (veiculo == null) return Results.NotFound();

  var validacao = validaDTO(vehicleDTO);
  if (validacao.Mensagens.Count > 0)
    return Results.BadRequest(validacao);

  veiculo.Nome = vehicleDTO.Nome;
  veiculo.Marca = vehicleDTO.Marca;
  veiculo.Ano = vehicleDTO.Ano;

  vehicleService.Atualizar(veiculo);

  return Results.Ok(veiculo);
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
.WithTags("Veiculos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
{
  var veiculo = vehicleService.BuscarPorId(id);
  if (veiculo == null) return Results.NotFound();

  vehicleService.Apagar(veiculo);

  return Results.NoContent();
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute { Roles = "Adm" })
.WithTags("Veiculos");
#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
#endregion
