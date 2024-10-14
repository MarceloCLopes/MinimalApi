using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinimalApp.Domain.ModelViews
{
    public class AdministratorLogado
    {
        public string Email { get; set; } = default!;
        public string Perfil { get; set; } = default!;
        public string Token { get; set; } = default!;
    }
}