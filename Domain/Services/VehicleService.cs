using MinimalApp.Domain.Entities;
using MinimalApp.Domain.Interfaces;
using MinimalApp.Infra.Db;

namespace MinimalApp.Domain.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly DBContext _context;

        public VehicleService(DBContext context)
        {
            _context = context;
        }

        public void Apagar(Vehicle vehicle)
        {
            _context.Vehicles.Remove(vehicle);
            _context.SaveChanges();
        }

        public void Atualizar(Vehicle vehicle)
        {
            _context.Vehicles.Update(vehicle);
            _context.SaveChanges();
        }

        public Vehicle? BuscarPorId(int id)
        {
            return _context.Vehicles.Where(v => v.Id == id).FirstOrDefault();
        }

        public void Incluir(Vehicle vehicle)
        {
            _context.Vehicles.Add(vehicle);
            _context.SaveChanges();
        }

        public List<Vehicle> Todos(int? pagina = 1, string? nome = null, string? marca = null)
        {
            var query = _context.Vehicles.AsQueryable();
            if (!string.IsNullOrEmpty(nome))
            {
                query = query.Where(v => v.Nome.ToLower().Contains(nome));
            }
            int itensPorPagina = 10;
            if (pagina != null)
                query = query.Skip(((int)pagina - 1) * itensPorPagina).Take(itensPorPagina);
            return [.. query];
        }
    }
}