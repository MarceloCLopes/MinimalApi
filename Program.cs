using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalApp.Domain.DTOs;
using MinimalApp.Domain.Entities;
using MinimalApp.Domain.Enuns;
using MinimalApp.Domain.Interfaces;
using MinimalApp.Domain.ModelViews;
using MinimalApp.Domain.Services;
using MinimalApp.Infra.Db;

#region Builder
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAdministratorService, AdministratorService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<DBContext>(options =>
{
  options.UseMySql(
      builder.Configuration.GetConnectionString("mysql"),
      ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql"))
    );
});

var app = builder.Build();
#endregion

#region Home
app.MapGet("/", () => Results.Json(new Home())).WithTags("Home");
#endregion

#region Administradores
app.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministratorService administratorService) =>
{
  if (administratorService.Login(loginDTO) != null)
    return Results.Ok("Login efetuado com sucesso!");
  else
    return Results.Unauthorized();
}).WithTags("Administradores");

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
}).WithTags("Administradores");

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
}).WithTags("Administradores");

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
}).WithTags("Administradores");

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
}).WithTags("Veiculos");

app.MapGet("/veiculos", ([FromQuery] int? pagina, IVehicleService vehicleService) =>
{
  var veiculos = vehicleService.Todos(pagina);

  return Results.Ok(veiculos);
}).WithTags("Veiculos");

app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
{
  var veiculo = vehicleService.BuscarPorId(id);

  if (veiculo == null) return Results.NotFound();

  return Results.Ok(veiculo);
}).WithTags("Veiculos");

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
}).WithTags("Veiculos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVehicleService vehicleService) =>
{
  var veiculo = vehicleService.BuscarPorId(id);
  if (veiculo == null) return Results.NotFound();

  vehicleService.Apagar(veiculo);

  return Results.NoContent();
}).WithTags("Veiculos");
#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.Run();
#endregion
