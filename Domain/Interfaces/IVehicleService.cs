using MinimalApp.Domain.Entities;

namespace MinimalApp.Domain.Interfaces
{
    public interface IVehicleService
    {
        List<Vehicle> Todos(int? pagina = 1, string? nome = null, string? marca = null);
        Vehicle? BuscarPorId(int id);
        void Incluir(Vehicle vehicle);
        void Atualizar(Vehicle vehicle);
        void Apagar(Vehicle vehicle);
    }
}