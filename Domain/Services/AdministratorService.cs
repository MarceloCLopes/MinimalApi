using MinimalApp.Domain.DTOs;
using MinimalApp.Domain.Entities;
using MinimalApp.Domain.Interfaces;
using MinimalApp.Infra.Db;

namespace MinimalApp.Domain.Services
{
    public class AdministratorService : IAdministratorService
    {
        private readonly DBContext _context;

        public AdministratorService(DBContext context)
        {
            _context = context;
        }

        public Administrator? BuscarPorId(int id)
        {
            return _context.Administrators.Where(v => v.Id == id).FirstOrDefault();
        }

        public Administrator? Incluir(Administrator administrator)
        {
            _context.Administrators.Add(administrator);
            _context.SaveChanges();

            return administrator;
        }

        public Administrator? Login(LoginDTO loginDTO)
        {
            var adm = _context.Administrators.Where(a => a.Email == loginDTO.Email && a.Senha == loginDTO.Senha).FirstOrDefault();
            return adm;
        }

        public List<Administrator> Todos(int? pagina)
        {
            var query = _context.Administrators.AsQueryable();

            int itensPorPagina = 10;

            if (pagina != null)
                query = query.Skip(((int)pagina - 1) * itensPorPagina).Take(itensPorPagina);
            return [.. query];
        }
    }
}