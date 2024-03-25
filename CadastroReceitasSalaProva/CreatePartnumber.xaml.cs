using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CadastroReceitasSalaProva
{
    public class PartNumber
    {
        public string Partnumber { get; set; }
        public string Description { get; set; }

        public PartNumber(string partnumber, string description)
        {
            Partnumber = partnumber;
            Description = description;
        }
    }

    public partial class CreatePartnumber : UserControl
    {
        readonly Database db = new(DatabaseConfig.ConnectionString);

        public ObservableCollection<PartNumber> _partnumberList = new();

        public int Index { get; set; }

        public CreatePartnumber()
        {
            InitializeComponent();
            LoadPartnumberList();

            _partnumberList.CollectionChanged += (sender, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    dgPartnumber.Items.Refresh();
            };
        }

        private void NovoBtnClick(object sender, RoutedEventArgs e)
        {
            dgPartnumber.ItemsSource = _partnumberList;
            _partnumberList.Add(new PartNumber("", ""));
        }

        private void BtnSalvarClick(object sender, RoutedEventArgs e)
        {
            //Check if there is any empty field
            if (
                _partnumberList.Any(p =>
                    string.IsNullOrEmpty(p.Partnumber) || string.IsNullOrEmpty(p.Description)
                )
            )
            {
                MessageBox.Show("Preencha todos os campos antes de salvar!");
                return;
            }

            //Chck if there is any duplicated partnumber
            if (_partnumberList.GroupBy(p => p.Partnumber).Any(g => g.Count() > 1))
            {
                MessageBox.Show(
                    "Partnumber duplicado.",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                //Remove duplicated partnumber
                _partnumberList.Remove(
                    _partnumberList.GroupBy(p => p.Partnumber).First(g => g.Count() > 1).Last()
                );
                return;
            }

            //Save partnumber
            if (db.SavePartnumber(_partnumberList.ToList()) != 0)
                return;

            MessageBox.Show("Partnumber salvo com sucesso!");
        }

        public void LoadPartnumberList()
        {
            _partnumberList.Clear();
            _partnumberList = db.LoadPartnumberList();
            dgPartnumber.ItemsSource ??= _partnumberList;

            DataContext = this;
        }

        public void ChangeSelection(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            PartNumber partnumber = (PartNumber)button.DataContext;

            Index = _partnumberList.IndexOf(partnumber);
        }

        private void DeletePartnumber(object sender, RoutedEventArgs e)
        {
            ChangeSelection(sender, e);
            MessageBoxResult result = MessageBox.Show(
                "Deseja realmente excluir este partnumber?",
                "Excluir partnumber",
                MessageBoxButton.YesNo
            );
            if (result == MessageBoxResult.Yes)
            {
                if (db.DeletePartnumber(_partnumberList[Index].Partnumber) != 0)
                    return;

                _partnumberList.RemoveAt(Index);

                MessageBox.Show("Partnumber excluído com sucesso!");
            }

            dgPartnumber.Items.Refresh();
            return;
        }

        private void ListAssociations(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangeSelection(sender, e);
                MessageBox.Show(
                    JsonSerializer.Serialize(
                        db.AssociatedRecipes(_partnumberList[Index].Partnumber)[0].ToString()
                    ),
                    "Receita associada",
                    MessageBoxButton.OK
                );
            }
            catch (Exception)
            {
                MessageBox.Show("Nenhuma receita associada", Name, MessageBoxButton.OK);
            }
        }
    }
}
