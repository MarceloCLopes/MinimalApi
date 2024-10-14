using MinimalApp.Domain.DTOs;
using MinimalApp.Domain.Entities;

namespace MinimalApp.Domain.Interfaces
{
    public interface IAdministratorService
    {
        Administrator? Login(LoginDTO loginDTO);
        Administrator? Incluir(Administrator administrator);
        Administrator? BuscarPorId(int id);
        List<Administrator> Todos(int? pagina);
    }
}