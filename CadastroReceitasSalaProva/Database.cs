using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Documents;
using Npgsql;

namespace CadastroReceitasSalaProva
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(string connectionString)
        {
            _connectionString = connectionString;
        }

        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        private DataTable FetchRecipesDatabase()
        {
            DataTable recipeList = new();

            using var connection = GetConnection();
            connection.Open();

            using var command = new NpgsqlCommand(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public';",
                connection
            );
            command.ExecuteNonQuery();

            using var reader = command.ExecuteReader();
            recipeList.Load(reader);
            return recipeList;
        }

        public ObservableCollection<string> LoadRecipeList()
        {
            // Fetch existing recipe names from the database
            using var command = FetchRecipesDatabase();

            ObservableCollection<string> recipeList = new();

            foreach (DataRow row in command.Rows)
            {
                recipeList.Add(item: row[0].ToString()!);
            }

            // Add "New Recipe" at the top
            recipeList.Insert(0, "Nova Receita");

            return recipeList;
        }

        public DataTable LoadRecipe(string recipeName)
        {
            // Fetch the recipe from the database
            DataTable recipe = new();

            using var connection = GetConnection();
            connection.Open();

            using var command = new NpgsqlCommand($"SELECT * FROM {recipeName};", connection);

            try
            {
                using var reader = command.ExecuteReader();
                recipe.Load(reader);
            }
            catch (Npgsql.PostgresException)
            {
                return recipe;
            }

            return recipe;
        }

        public void SaveRecipe(string recipeName, ObservableCollection<RecipeParameter> parameters)
        {
            using var connection = GetConnection();
            connection.Open();

            var createTableCommand = new NpgsqlCommand(
                $"CREATE TABLE IF NOT EXISTS \"{recipeName}\" (tag character varying(255) COLLATE pg_catalog.\"default\", t_value_5 character varying(255) COLLATE pg_catalog.\"default\", t_value_12 character varying(255) COLLATE pg_catalog.\"default\", description character varying(255) COLLATE pg_catalog.\"default\") TABLESPACE pg_default; ALTER TABLE IF EXISTS public.receita_padrao OWNER to postgres;",
                connection
            );
            createTableCommand.ExecuteNonQuery();

            foreach (var param in parameters)
            {
                var insertCommand = new NpgsqlCommand(
                    $"INSERT INTO \"{recipeName}\" (Tag, T_Value_5, T_Value_12, Description) VALUES (@Tag, @T_Value_5, @T_Value_12, @Description);",
                    connection
                );
                insertCommand.Parameters.AddWithValue("@Tag", param.Tag);
                insertCommand.Parameters.AddWithValue("@T_Value_5", param.T_Value_5);
                insertCommand.Parameters.AddWithValue("@T_Value_12", param.T_Value_12);
                insertCommand.Parameters.AddWithValue("@Description", param.Description);
                insertCommand.ExecuteNonQuery();
            }
        }

        public void DeleteRecipe(string recipeName)
        {
            using var connection = GetConnection();
            connection.Open();

            var checkTableExistsCommand = new NpgsqlCommand(
                $"SELECT to_regclass('{recipeName}')::text;",
                connection
            );

            var exists = checkTableExistsCommand.ExecuteScalar() != DBNull.Value;
            if (exists)
            {
                var deleteCommand = new NpgsqlCommand($"DELETE FROM \"{recipeName}\";", connection);
                deleteCommand.ExecuteNonQuery();
            }
        }

        private void CreatePartnumberIndex()
        {
            using var connection = GetConnection();
            connection.Open();

            var createIndexCommand = new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS private.partnumber_index (id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ), partnumber character varying COLLATE pg_catalog.\"default\" NOT NULL, recipe character varying COLLATE pg_catalog.\"default\" NOT NULL, CONSTRAINT partnumber_index_pkey PRIMARY KEY (id), CONSTRAINT \"UQ_partnumber\" UNIQUE (partnumber)) TABLESPACE pg_default; ALTER TABLE IF EXISTS private.partnumber_index OWNER to postgres;",
                connection
            );
            createIndexCommand.ExecuteNonQuery();
        }

        public bool IsPartnumberEmpty()
        {
            // Create AssociatePartnumber table if it doesn't exist
            CreatePartnumberIndex();

            //Check if partnuber is not null
            using var connection = GetConnection();
            connection.Open();

            using var command = new NpgsqlCommand(
                $"SELECT id FROM private.partnumber;",
                connection
            );
            using var reader = command.ExecuteReader();

            // If partnumber is empty, return true
            if (!reader.HasRows)
            {
                return true;
            }

            return false;
        }

        public void SavePartnumber(System.Collections.Generic.List<PartNumber> partnumberList)
        {
            using var connection = GetConnection();
            connection.Open();

            foreach (var partnumber in partnumberList)
            {
                var insertCommand = new NpgsqlCommand(
                    "INSERT INTO private.partnumber (desenho_motor, descricao) VALUES (@partnumber, @desciption);",
                    connection
                );
                insertCommand.Parameters.AddWithValue("@partnumber", partnumber.Partnumber);
                insertCommand.Parameters.AddWithValue("@desciption", partnumber.Description);
                insertCommand.ExecuteNonQuery();
            }

            connection.Close();
        }

        public ObservableCollection<PartNumber> LoadPartnumberList()
        {
            using var connection = GetConnection();
            connection.Open();

            var partnumberList = new ObservableCollection<PartNumber>();

            using var command = new NpgsqlCommand(
                "SELECT desenho_motor, descricao FROM private.partnumber;",
                connection
            );
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                partnumberList.Add(new PartNumber(reader.GetString(0), reader.GetString(1)));
            }

            return partnumberList;
        }

        public void DeletePartnumber(string partnumber)
        {
            using var connection = GetConnection();
            connection.Open();

            var deleteCommand = new NpgsqlCommand(
                "DELETE FROM private.partnumber WHERE desenho_motor = @partnumber;",
                connection
            );
            deleteCommand.Parameters.AddWithValue("@partnumber", partnumber);
            deleteCommand.ExecuteNonQuery();
        }

        public int InsertPartnumberIndex(string recipeName, string partnumber)
        {
            CreatePartnumberIndex();

            using var connection = GetConnection();
            connection.Open();

            try
            {
                var insertCommand = new NpgsqlCommand(
                    "INSERT INTO private.partnumber_index (partnumber, recipe) VALUES (@partnumber, @recipename);",
                    connection
                );
                insertCommand.Parameters.AddWithValue("@recipeName", recipeName);
                insertCommand.Parameters.AddWithValue("@partnumber", partnumber);
                insertCommand.ExecuteNonQuery();
                return 0;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("23505"))
                    MessageBox.Show(
                        "Partnumber já associado a uma receita.",
                        "Erro",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );

                _ = new Exception(ex.Message);
                return 1;
            }
        }
    }
}
