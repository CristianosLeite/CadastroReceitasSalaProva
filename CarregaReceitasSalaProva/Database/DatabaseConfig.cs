using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarregaReceitasSalaProva.Database
{
    public class DatabaseConfig
    {
        public DatabaseConfig(string connectionString)
        {
            ConnectionString1 = connectionString;
        }

        public static string ConnectionString { get; } = "Host=localhost;Username=postgres;Password=postgres;Db=sala_prova_db";
        public string ConnectionString1 { get; }
    }
}
