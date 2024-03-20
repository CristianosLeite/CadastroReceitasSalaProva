using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace CadastroReceitasSalaProva
{
    public class PartNumber
    {
        public string Partnumber { get; set; }
        public string Description { get; set; }
        public bool IsSelected { get; set; }

        public PartNumber(string partnumber, string description)
        {
            Partnumber = partnumber;
            Description = description;
            IsSelected = false;
        }
    }

    public partial class CreatePartnumber : Window
    {
        readonly Database db =
            new(DatabaseConfig.ConnectionString);
        private readonly string Recipe;

        public CreatePartnumber(string recipe)
        {
            InitializeComponent();

            dgPartnumber.ItemsSource = new List<PartNumber>();
            Recipe = recipe;
        }

        private void NovoBtnClick(object sender, RoutedEventArgs e)
        {
            //Add a new line to the data grid
            PartNumber partNumberParameter = new("", "");
            ((List<PartNumber>)dgPartnumber.ItemsSource).Add(partNumberParameter);
            dgPartnumber.Items.Refresh();
        }

        private void BtnSalvarClick(object sender, RoutedEventArgs e)
        {
            try
            {
                db.SavePartnumber((List<PartNumber>)dgPartnumber.ItemsSource);
                MessageBox.Show("Partnumber salvo com sucesso!");
                Hide();
                AssociatePartnumber partnumber = new(Recipe);
                partnumber.ShowDialog();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnExcluirClick(object sender, RoutedEventArgs e) { }
    }
}
