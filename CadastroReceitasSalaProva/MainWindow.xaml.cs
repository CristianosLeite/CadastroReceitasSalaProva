﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public class RecipeParameter
    {
        public string Tag { get; set; }
        public string T_Value_5 { get; set; }
        public string T_Value_12 { get; set; }
        public string Description { get; set; }

        public RecipeParameter()
        {
            Tag = "";
            T_Value_5 = "";
            T_Value_12 = "";
            Description = "";
        }
    }

    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<RecipeParameter> _parameters = new();

        readonly Database db =
            new(DatabaseConfig.ConnectionString);

        public MainWindow()
        {
            InitializeComponent();

            // Load recipe list only if ItemsSource is null
            RecipeComboBox.ItemsSource ??= db.LoadRecipeList();

            ParametersDataGrid.ItemsSource = _parameters;
        }

        private void RecipeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
        }

        private void HandleExistingRecipeSelection(string selectedRecipe)
        {
            // Load recipe list only if ItemsSource is null
            RecipeComboBox.ItemsSource ??= db.LoadRecipeList();

            LoadRecipe(selectedRecipe);

            RecipeNameTxt.Text = selectedRecipe;
            UploadRecipe.Visibility = Visibility.Hidden;
            RecipeNameTxt.Visibility = Visibility.Hidden;
        }

        private void LoadRecipe(string recipeName)
        {
            // Fetch parameters for the selected recipe from the database
            try
            {
                using var reader = db.LoadRecipe(recipeName).CreateDataReader();
                while (reader.Read())
                {
                    _parameters.Add(
                        new RecipeParameter
                        {
                            Tag = reader.IsDBNull(0) ? "" : reader.GetString(0),
                            T_Value_5 = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            T_Value_12 = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Description = reader.IsDBNull(2) ? "" : reader.GetString(3)
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

            // Associate AssociatePartnumber with recipe
            AssociatePartnumber(recipeName);

            MessageBox.Show($"Receita '{recipeName}' cadastrada com sucesso!");

            Close();
        }

        private bool RecipeExists(string recipeName)
        {
            using var reader = db.LoadRecipe(recipeName);

            return reader.Rows.Count > 0;
        }

        private static bool ParametersAreValid(ObservableCollection<RecipeParameter> parameters)
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
            RecipeComboBox.ItemsSource ??= db.LoadRecipeList();
        }

        private void ListBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            LoadRecipes();
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
                            new RecipeParameter
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

        private static void CreatePartnumber(string recipeName)
        {
            CreatePartnumber createPartnumber = new(recipeName);
            createPartnumber.ShowDialog();
        }

        public void AssociatePartnumber(string recipeName)
        {
            using var connection = db.GetConnection();
            connection.Open();

            if (db.IsPartnumberEmpty())
            {
                CreatePartnumber(recipeName);
                return;
            }

            AssociatePartnumber associatePartnumber = new(recipeName);
            associatePartnumber.ShowDialog();
        }
    }
}
