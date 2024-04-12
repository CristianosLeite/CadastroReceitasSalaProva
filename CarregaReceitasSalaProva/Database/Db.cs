using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace CarregaReceitasSalaProva
{
    internal class Db(string connectionString)
    {
        private readonly string _connectionString = connectionString;

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public ObservableCollection<string> RecipeToLoad()
        {
            var recipe = new ObservableCollection<string>();
            using (var connection = GetConnection())
            {
                connection.Open();
                using var cmd = new NpgsqlCommand("SELECT reading FROM private.operations WHERE operation = 'Receita carregada' ORDER BY id DESC LIMIT 1", connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    recipe.Add(reader.GetString(0));
                }
            }

            return recipe;
        }

        public ObservableCollection<string> Cycle()
        {
            var cycle = new ObservableCollection<string>();
            using (var connection = GetConnection())
            {
                connection.Open();
                using var cmd = new NpgsqlCommand("SELECT reading FROM private.operations WHERE operation = 'Teste iniciado' ORDER BY id DESC LIMIT 1", connection);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    cycle.Add(reader.GetString(0));
                }
            }

            return cycle;
        }

        public DataTable LoadRecipeFromDatabase(string recipeName)
        {
            DataTable recipe = new();

            using var connection = GetConnection();
            connection.Open();

            using var command = new NpgsqlCommand($"SELECT * FROM \"{recipeName}\";", connection);

            try
            {
                using var reader = command.ExecuteReader();
                recipe.Load(reader);
            }
            catch (PostgresException)
            {
                return recipe;
            }

            return recipe;
        }

        public void CreatePlcTable()
        {
            using var connection = GetConnection();
            connection.Open();

            using var command = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS plc (id SERIAL PRIMARY KEY, ip VARCHAR(15) NOT NULL, rack INT NOT NULL, slot INT NOT NULL);", connection);
            command.ExecuteNonQuery();
        }

        public void InsertPlc(string ip, int rack, int slot)
        {
            using var connection = GetConnection();
            connection.Open();

            using var command = new NpgsqlCommand("INSERT INTO plc (ip, rack, slot) VALUES (@ip, @rack, @slot);", connection);
            command.Parameters.AddWithValue("ip", ip);
            command.Parameters.AddWithValue("rack", rack);
            command.Parameters.AddWithValue("slot", slot);
            command.ExecuteNonQuery();
        }
    }
}
