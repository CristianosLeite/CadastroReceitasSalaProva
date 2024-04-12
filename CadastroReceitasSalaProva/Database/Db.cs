using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls.Primitives;
using CadastroReceitasSalaProva.Interfaces;
using DocumentFormat.OpenXml.Wordprocessing;
using Npgsql;

namespace CadastroReceitasSalaProva
{
    public class Db
    {
        private readonly string _connectionString;

        public Db(string connectionString)
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

            return recipeList;
        }

        public DataTable LoadRecipe(string recipeName)
        {
            // Fetch the recipe from the database
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

        public void SaveRecipe(string recipeName, ObservableCollection<Recipe> parameters)
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
                $"SELECT to_regclass('\"{recipeName}\"')::text;",
                connection
            );

            var exists = checkTableExistsCommand.ExecuteScalar() != DBNull.Value;
            if (exists)
            {
                var deleteCommand = new NpgsqlCommand($"DROP TABLE \"{recipeName}\";", connection);
                deleteCommand.ExecuteNonQuery();
            }
        }

        private void CreatePartnumberIndex()
        {
            CreatePartnumberTable();

            using var connection = GetConnection();
            connection.Open();

            var createIndexCommand = new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS private.partnumber_index (id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1 ), partnumber character varying COLLATE pg_catalog.\"default\" NOT NULL, recipe character varying COLLATE pg_catalog.\"default\" NOT NULL, CONSTRAINT partnumber_index_pkey PRIMARY KEY (id), CONSTRAINT \"UQ_associateted\" UNIQUE (partnumber), CONSTRAINT partnumber_fk FOREIGN KEY (partnumber) REFERENCES private.partnumber (partnumber)) TABLESPACE pg_default; ALTER TABLE IF EXISTS private.partnumber_index OWNER to postgres;",
                connection
            );
            createIndexCommand.ExecuteNonQuery();
        }

        private void CreatePartnumberTable()
        {
            using var connection = GetConnection();
            connection.Open();

            var createTableCommand = new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS private.partnumber (id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1), partnumber character varying COLLATE pg_catalog.\"default\" NOT NULL, description character varying COLLATE pg_catalog.\"default\" NOT NULL, CONSTRAINT partnumber_pkey PRIMARY KEY (id), CONSTRAINT \"UQ_partnumber\" UNIQUE (partnumber)) TABLESPACE pg_default; ALTER TABLE IF EXISTS private.partnumber OWNER to postgres;",
                connection
            );
            createTableCommand.ExecuteNonQuery();
        }

        public int SavePartnumber(List<PartNumber> partnumberList)
        {
            if (partnumberList == null || partnumberList.Count == 0)
            {
                ShowErrorMessage("Recipe Inválido!");
                return 1;
            }

            try
            {
                using var connection = GetConnection();
                connection.Open();

                foreach (var partnumber in partnumberList)
                {
                    InsertOrUpdatePartnumber(partnumber, connection);
                }
            }
            catch (PostgresException)
            {
                ShowErrorMessage("Não foi possível cadastrar o partnumber.");
                return 1;
            }

            return 0;
        }

        private static void InsertOrUpdatePartnumber(
            PartNumber partnumber,
            NpgsqlConnection connection
        )
        {
            using var insertCommand = new NpgsqlCommand(
                "INSERT INTO private.partnumber (partnumber, description) VALUES (@partnumber, @desciption) ON CONFLICT (partnumber) DO UPDATE SET description = @desciption;",
                connection
            );
            insertCommand.Parameters.AddWithValue("@partnumber", partnumber.Partnumber);
            insertCommand.Parameters.AddWithValue("@desciption", partnumber.Description);
            insertCommand.ExecuteNonQuery();
        }

        private static void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public ObservableCollection<PartNumber> LoadPartnumberList()
        {
            CreatePartnumberTable();

            using var connection = GetConnection();
            connection.Open();

            var partnumberList = new ObservableCollection<PartNumber>();

            using var command = new NpgsqlCommand(
                "SELECT partnumber, description FROM private.partnumber;",
                connection
            );
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                partnumberList.Add(new PartNumber(reader.GetString(0), reader.GetString(1)));
            }

            return partnumberList;
        }

        public int DeletePartnumber(string partnumber)
        {
            try
            {
                using var connection = GetConnection();
                connection.Open();

                var deleteCommand = new NpgsqlCommand(
                    "DELETE FROM private.partnumber WHERE partnumber = @partnumber;",
                    connection
                );
                deleteCommand.Parameters.AddWithValue("@partnumber", partnumber);
                deleteCommand.ExecuteNonQuery();
                return 0;
            }
            catch (PostgresException)
            {
                MessageBox.Show(
                    $"Não foi possível deletar o partnumber. Verifique se existe associação à alguma receita.",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return 1;
            }
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
            }
            catch (PostgresException)
            {
                MessageBox.Show(
                    "Recipe já associado a uma receita.",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                return 1;
            }

            return 0;
        }

        public ObservableCollection<string> AssociatedRecipes(string partnumber)
        {
            using var connection = GetConnection();
            connection.Open();

            var partnumberList = new ObservableCollection<string>();
            using var command = new NpgsqlCommand(
                "SELECT recipe FROM private.partnumber_index WHERE partnumber = @partnumber;",
                connection
            );
            command.Parameters.AddWithValue("@partnumber", partnumber);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                partnumberList.Add(reader.GetString(0)!);
            }

            return partnumberList;
        }

        public void DeletePartnumberIndex(string partnumber)
        {
            using var connection = GetConnection();
            connection.Open();

            var deleteCommand = new NpgsqlCommand(
                "DELETE FROM private.partnumber_index WHERE partnumber = @partnumber;",
                connection
            );
            deleteCommand.Parameters.AddWithValue("@partnumber", partnumber);
            deleteCommand.ExecuteNonQuery();
        }

        public ObservableCollection<string> LoadAvailablePartnumbers()
        {
            CreatePartnumberIndex();

            using var connection = GetConnection();
            connection.Open();

            DataTable dt = new();

            ObservableCollection<string> ptnList = new();
            using var command = new NpgsqlCommand(
                "SELECT partnumber FROM private.partnumber WHERE partnumber NOT IN(SELECT partnumber FROM private.partnumber_index);",
                connection
            );
            using var reader = command.ExecuteReader();
            dt.Load(reader);

            foreach (DataRow row in dt.Rows)
            {
                ptnList.Add(item: row[0].ToString()!);
            }

            return ptnList;
        }

        public ObservableCollection<string> LoadAssociatedPartnumbers(string recipe)
        {
            using var connection = GetConnection();
            connection.Open();

            DataTable dt = new();

            ObservableCollection<string> ptnList = new();

            using var command = new NpgsqlCommand(
                "SELECT partnumber FROM private.partnumber_index WHERE recipe = @recipe;",
                connection
            );
            command.Parameters.AddWithValue("@recipe", recipe);
            using var reader = command.ExecuteReader();

            dt.Load(reader);

            foreach (DataRow row in dt.Rows)
            {
                ptnList.Add(item: row[0].ToString()!);
            }

            return ptnList;
        }

        private void CreateLabelIndexTable()
        {
            using var connection = GetConnection();
            connection.Open();

            var createTableCommand = new NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS private.label_index (id bigint NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1), recipe character varying COLLATE pg_catalog.\"default\" NOT NULL, min_empty_5 character varying COLLATE pg_catalog.\"default\", max_empty_5 character varying COLLATE pg_catalog.\"default\", pw_5 character varying COLLATE pg_catalog.\"default\", min_empty_12 character varying COLLATE pg_catalog.\"default\", max_empty_12 character varying COLLATE pg_catalog.\"default\", pw_12 character varying COLLATE pg_catalog.\"default\", CONSTRAINT labels_pkey PRIMARY KEY (id), CONSTRAINT \"UQ_labels\" UNIQUE (recipe)) TABLESPACE pg_default; ALTER TABLE IF EXISTS private.label_index OWNER to postgres;",
                connection
            );
            createTableCommand.ExecuteNonQuery();
        }

        public void SaveLabelIndex(
            LabelIndex lbIndex
        )
        {
            CreateLabelIndexTable();

            using var connection = GetConnection();
            connection.Open();

            var insertCommand = new NpgsqlCommand(
                "INSERT INTO private.label_index (recipe, min_empty_5, max_empty_5, pw_5, min_empty_12, max_empty_12, pw_12) VALUES (@recipe, @min_empty_12, @max_empty_12, @pw_12, @min_empty_12, @max_empty_12, @pw_12) ON CONFLICT (recipe) DO UPDATE SET min_empty_5 = @min_empty_5, max_empty_5 = @max_empty_5, pw_5 = @pw_5, min_empty_12 = @min_empty_12, max_empty_12 = @max_empty_12, pw_12 = @pw_12;",
                connection
            );

            insertCommand.Parameters.AddWithValue("@recipe", lbIndex.Recipe);
            insertCommand.Parameters.AddWithValue("@min_empty_5", lbIndex.MinEmpty5);
            insertCommand.Parameters.AddWithValue("@max_empty_5", lbIndex.MaxEmpty5);
            insertCommand.Parameters.AddWithValue("@pw_5", lbIndex.Pw5);

            insertCommand.Parameters.AddWithValue("@min_empty_12", lbIndex.MinEmpty12);
            insertCommand.Parameters.AddWithValue("@max_empty_12", lbIndex.MaxEmpty12);
            insertCommand.Parameters.AddWithValue("@pw_12", lbIndex.Pw12);
            insertCommand.ExecuteNonQuery();
        }

        public LabelIndex LoadLabelIndex(string recipe)
        {
            CreateLabelIndexTable();

            LabelIndex dt = new();

            using var connection = GetConnection();
            connection.Open();

            using var command = new NpgsqlCommand(
                "SELECT min_empty_5, max_empty_5, pw_5, min_empty_12, max_empty_12, pw_12 FROM private.label_index WHERE recipe = @recipe;",
                connection
            );
            command.Parameters.AddWithValue("@recipe", recipe);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                if (!reader.IsDBNull(0) && !reader.IsDBNull(1) && !reader.IsDBNull(2))
                {
                    dt.MinEmpty5 = reader.GetString(0);
                    dt.MaxEmpty5 = reader.GetString(1);
                    dt.Pw5 = reader.GetString(2);
                }

                if (!reader.IsDBNull(3) && !reader.IsDBNull(4) && !reader.IsDBNull(5))
                {
                    dt.MinEmpty12 = reader.GetString(3);
                    dt.MaxEmpty12 = reader.GetString(4);
                    dt.Pw12 = reader.GetString(5);
                }
            }

            return dt;
        }
    }
}
