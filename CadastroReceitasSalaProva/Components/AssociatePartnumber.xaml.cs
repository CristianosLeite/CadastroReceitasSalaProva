using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Collections.Specialized;
using CadastroReceitasSalaProva.Database;

namespace CadastroReceitasSalaProva
{
    public partial class AssociatePartnumber : UserControl
    {
        readonly Db db = new(DatabaseConfig.ConnectionString);

        public string SelectedRecipe { get; set; }
        public string SelectedPartnumber { get; set; }

        private int Index;

        public ObservableCollection<string> _recipeList = new();
        public ObservableCollection<string> AvailablePartnumbers = new();
        public ObservableCollection<string> AssociatedPartnumber = new();

        public AssociatePartnumber()
        {
            InitializeComponent();
            LoadRecipeList();

            SelectedRecipe = "";
            SelectedPartnumber = "";

            _recipeList.CollectionChanged += (sender, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                    recipeList.ItemsSource = _recipeList;
            };
        }

        public void LoadRecipeList()
        {
            _recipeList.Clear();
            _recipeList = db.LoadRecipeList();
            recipeList.ItemsSource ??= _recipeList;

            DataContext = this;
        }

        public void LoadAvailablePartnumbers()
        {
            AvailablePartnumbers = db.LoadAvailablePartnumbers();
            lbAvailablePartnumbers.ItemsSource ??= AvailablePartnumbers;
        }

        public void LoadAssociatedPartnumbers()
        {
            AssociatedPartnumber = db.LoadAssociatedPartnumbers(SelectedRecipe);
            lbAssociatedPartnumbers.ItemsSource ??= AssociatedPartnumber;
        }

        public void SelectedItemChanged(object sender, RoutedEventArgs e)
        {
            Index = lbAvailablePartnumbers.SelectedIndex;
        }

        private void SelectionChanged(object sender, RoutedEventArgs e)
        {
            SelectedRecipe = (string)recipeList.SelectedItem;

            lbAvailablePartnumbers.ClearValue(ItemsControl.ItemsSourceProperty);
            lbAssociatedPartnumbers.ClearValue(ItemsControl.ItemsSourceProperty);
            LoadAvailablePartnumbers();
            LoadAssociatedPartnumbers();
        }

        private void AssociateBtnClick(object sender, RoutedEventArgs e)
        {
            if (Index == -1)
            {
                MessageBox.Show(
                    "Selecione um partnumber para associar",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            db.InsertPartnumberIndex(SelectedRecipe, AvailablePartnumbers[Index]);

            SelectionChanged(sender, e);

            Index = -1;
        }

        private void RemoveAssociation(object sender, RoutedEventArgs e)
        {
            if (lbAssociatedPartnumbers.SelectedIndex == -1)
            {
                MessageBox.Show(
                    "Selecione um partnumber para desassociar",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            db.DeletePartnumberIndex(AssociatedPartnumber[lbAssociatedPartnumbers.SelectedIndex]);

            SelectionChanged(sender, e);

            Index = -1;
        }
    }
}
