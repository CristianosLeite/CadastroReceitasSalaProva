using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CadastroReceitasSalaProva
{
    public static class DatabaseConfig
    {
        public static string ConnectionString { get; } = "Host=localhost;Username=postgres;Password=postgres;Database=sala_prova_db";
    }
}
