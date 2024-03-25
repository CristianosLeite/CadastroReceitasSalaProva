using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Win32;

#pragma warning disable IDE1006

namespace CadastroReceitasSalaProva
{
    public class Recipe
    {
        public string Tag { get; set; }
        public string T_Value_5 { get; set; }
        public string T_Value_12 { get; set; }
        public string Description { get; set; }

        public Recipe()
        {
            Tag = "";
            T_Value_5 = "";
            T_Value_12 = "";
            Description = "";
        }
    }

    public partial class CreateRecipe : UserControl
    {
        private readonly ObservableCollection<Recipe> _parameters = new();
        public ObservableCollection<string> _recipeList = new();

        readonly Database db = new(DatabaseConfig.ConnectionString);

        public CreateRecipe()
        {
            InitializeComponent();

            LoadRecipes();

            _parameters.CollectionChanged += (sender, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    ParametersDataGrid.ItemsSource = _parameters;
            };
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedRecipe = (string)RecipeComboBox.SelectedItem;

            if (selectedRecipe == "Nova Receita")
            {
                HandleNewRecipeSelection();
            }
            else
            {
                HandleExistingRecipeSelection(selectedRecipe);
            }

            if (RecipeComboBox.SelectedIndex != -1)
            {
                SaveRecipeBtn.Visibility = Visibility.Visible;
            }
        }

        private void HandleNewRecipeSelection()
        {
            // Clear existing parameters when creating a new recipe
            _parameters.Clear();
            RecipeNameTxt.Text = "Nova Receita";
            RecipeComboBox.SelectedIndex = 0;
            UploadRecipe.Visibility = Visibility.Visible;
            RecipeNameTxt.Visibility = Visibility.Visible;
            DeleteRecipe.Visibility = Visibility.Hidden;
        }

        private void HandleExistingRecipeSelection(string selectedRecipe)
        {
            // Load recipe list only if ItemsSource is null
            RecipeComboBox.ItemsSource ??= db.LoadRecipeList();

            LoadRecipe(selectedRecipe);

            RecipeNameTxt.Text = selectedRecipe;
            UploadRecipe.Visibility = Visibility.Hidden;
            RecipeNameTxt.Visibility = Visibility.Hidden;
            DeleteRecipe.Visibility = Visibility.Visible;
        }

        private void LoadRecipe(string recipeName)
        {
            // Fetch parameters for the selected recipe from the database
            try
            {
                var recipe = db.LoadRecipe(recipeName).CreateDataReader();
                while (recipe.Read())
                {
                    _parameters.Add(
                        new Recipe
                        {
                            Tag = recipe.IsDBNull(0) ? "" : recipe.GetString(0),
                            T_Value_5 = recipe.IsDBNull(1) ? "" : recipe.GetString(1),
                            T_Value_12 = recipe.IsDBNull(2) ? "" : recipe.GetString(2),
                            Description = recipe.IsDBNull(2) ? "" : recipe.GetString(3)
                        }
                    );
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private static bool ValidateRecipeName(string recipeName)
        {
            return !string.IsNullOrEmpty(recipeName) && recipeName != "Nova Receita";
        }

        private void ReturnInitialState()
        {
            _parameters.Clear();
            RecipeComboBox.SelectedIndex = -1;
            UploadRecipe.Visibility = Visibility.Hidden;
            RecipeNameTxt.Visibility = Visibility.Hidden;
            DeleteRecipe.Visibility = Visibility.Hidden;
            SaveRecipeBtn.Visibility = Visibility.Hidden;
        }

        private void SaveRecipeBtn_Click(object sender, RoutedEventArgs e)
        {
            string recipeName = RecipeNameTxt.Text;

            // Validate recipeName before using it
            if (!ValidateRecipeName(recipeName))
            {
                MessageBox.Show("Por favor, entre com um nome válido.");
                return;
            }

            // Check if recipe already exists
            if (RecipeExists(recipeName))
            {
                // Recipe already exists, ask for confirmation before overwriting
                MessageBoxResult result = MessageBox.Show(
                    $"A receita '{recipeName}' já existe. Deseja sobrescrevê-la?",
                    "Confirmação",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            db.DeleteRecipe(recipeName);

            // Validate parameters before saving
            if (!ParametersAreValid(_parameters))
            {
                return;
            }

            // Save the recipe to the database
            db.SaveRecipe(recipeName, _parameters);

            MessageBox.Show($"Receita '{recipeName}' cadastrada com sucesso!");

            ReturnInitialState();
            LoadRecipes();
        }

        private bool RecipeExists(string recipeName)
        {
            using var reader = db.LoadRecipe(recipeName);

            return reader.Rows.Count > 0;
        }

        private static bool ParametersAreValid(ObservableCollection<Recipe> parameters)
        {
            bool validation = parameters.All(param =>
                !string.IsNullOrEmpty(param.Tag)
                && !string.IsNullOrEmpty(param.T_Value_5)
                && !string.IsNullOrEmpty(param.T_Value_12)
                && !string.IsNullOrEmpty(param.Description)
            );

            if (!validation)
            {
                MessageBox.Show("Por favor, preencha todos os campos antes de salvar a receita.");
            }

            return validation;
        }

        private void LoadRecipes()
        {
            // Empty the recipe list when a new recipe is selected
            _parameters.Clear();
            RecipeNameTxt.Text = "Nova Receita";
            RecipeComboBox.ItemsSource = null;
            Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle, null);

            // Load recipe list only if ItemsSource is null
            _recipeList = db.LoadRecipeList();

            // Add "New Recipe" at the top
            _recipeList.Insert(0, "Nova Receita");

            RecipeComboBox.ItemsSource = _recipeList;
        }

        private static string GetCellValue(Cell cell, WorkbookPart workbookPart)
        {
            if (
                cell.CellValue == null
                || cell.DataType == null
                || cell.DataType.Value != CellValues.SharedString
            )
                return cell.CellValue!.Text;
            else
            {
                SharedStringTablePart sharedStringPart = workbookPart
                    .GetPartsOfType<SharedStringTablePart>()
                    .First();
                SharedStringItem item = sharedStringPart
                    .SharedStringTable.Elements<SharedStringItem>()
                    .ElementAt(int.Parse(cell.CellValue.Text));

                return item.Text!.Text;
            }
        }

        private void LoadRecipeFromExcel(string filePath)
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Open(filePath, false);

            if (doc.WorkbookPart == null)
            {
                MessageBox.Show("O arquivo selecionado não é uma planilha válida.");
                return;
            }

            try
            {
                WorkbookPart workbookPart = doc.WorkbookPart;
                WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                if (sheetData == null)
                {
                    MessageBox.Show("A planilha está vazia.");
                    return;
                }

                _parameters.Clear();

                foreach (Row r in sheetData.Elements<Row>())
                {
                    // Skip the first row (header) and add the rest to the list.
                    if (r.RowIndex! != 1)
                    {
                        string Tag = GetCellValue(r.Elements<Cell>().ElementAt(0), workbookPart);
                        string T_Value_5 =
                            r.Elements<Cell>().ElementAt(1) != null
                                ? GetCellValue(r.Elements<Cell>().ElementAt(1), workbookPart)
                                : "";
                        string T_Value_12 =
                            r.Elements<Cell>().ElementAt(2) != null
                                ? GetCellValue(r.Elements<Cell>().ElementAt(2), workbookPart)
                                : "";
                        string Description =
                            r.Elements<Cell>().ElementAt(3) != null
                                ? GetCellValue(r.Elements<Cell>().ElementAt(3), workbookPart)
                                : "";

                        _parameters.Add(
                            new Recipe
                            {
                                Tag = Tag,
                                T_Value_5 = T_Value_5,
                                T_Value_12 = T_Value_12,
                                Description = Description
                            }
                        );
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Não foi possível carregar a receita. Verifique se todos os campos foram preechidos corretamente.",
                    "Parâmetro inválido."
                );
            }
        }

        private void UploadRecipeBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog =
                new()
                {
                    Filter = "Excel Files|*.xls;*.xlsx;*.xlsm",
                    Title = "Select a Recipe File"
                };

            if (openFileDialog.ShowDialog() == true)
            {
                // Upload recipe from excel file
                string filePath = openFileDialog.FileName;
                LoadRecipeFromExcel(filePath);
                RecipeNameTxt.Text = System.IO.Path.GetFileNameWithoutExtension(filePath);
            }
        }

        private void DeleteRecipeBtn(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Tem certeza que deseja excluir a receita?",
                "Confirmação",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                string recipeName = RecipeNameTxt.Text;
                db.DeleteRecipe(recipeName);
                MessageBox.Show($"Receita '{recipeName}' excluída com sucesso!");
                LoadRecipes();
                ReturnInitialState();
            }

            return;
        }
    }
}
