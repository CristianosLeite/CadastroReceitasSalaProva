using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CheckBox = System.Windows.Controls.CheckBox;

namespace CadastroReceitasSalaProva
{
    public partial class AssociatePartnumber : Window
    {
        readonly Database db =
            new(DatabaseConfig.ConnectionString);
        
        private readonly string Recipe;

        public ObservableCollection<PartNumber> PartnumberList { get; set; } = new();

        public AssociatePartnumber(string recipe)
        {
            Recipe = recipe;

            LoadPartnumberList();
            InitializeComponent();
        }

        public void LoadPartnumberList()
        {
            PartnumberList = db.LoadPartnumberList();

            foreach (PartNumber partnumber in PartnumberList)
            {
                ptnList?.Items.Add(partnumber.Partnumber + ' ' + partnumber.Description);
            }

            DataContext = this;
        }

        private void BtnCreateClick(object sender, RoutedEventArgs e)
        {
            CreatePartnumber createPartnumber = new(Recipe);
            Hide();
            createPartnumber.ShowDialog();
        }

        private void BtnAssociateClick(object sender, RoutedEventArgs e)
        {
            foreach (PartNumber item in PartnumberList)
            {
                if (item.IsSelected)
                {
                    string partnumber = item.Partnumber.ToString()!.Split(' ')[0];

                    if (db.InsertPartnumberIndex(Recipe, partnumber) != 0)
                        return;
                }
            }

            MessageBox.Show("Partnumber associado com sucesso!");
            Close();
        }

        public void ChangeSelection(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            PartNumber selectedPartNumber = (PartNumber)checkBox.DataContext;
            int index = PartnumberList.IndexOf(selectedPartNumber);

            PartnumberList[index].IsSelected = checkBox.IsChecked switch
            {
                true => true,
                _ => false,
            };
        }
    }
}
