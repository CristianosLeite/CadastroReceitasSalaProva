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
    public partial class CreateRecipeMainWindow : Window
    {
        public CreateRecipeMainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            RenderPages.Children.Clear();
            RenderPages.Children.Add(new CreateRecipe());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Header.Children.Add(new Heder());
            RenderPages.Children.Clear();
            RenderPages.Children.Add(new CreateRecipe());
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            RenderPages.Children.Clear();
            RenderPages.Children.Add(new CreatePartnumber());
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            RenderPages.Children.Clear();
            RenderPages.Children.Add(new AssociatePartnumber());
        }
    }
}
