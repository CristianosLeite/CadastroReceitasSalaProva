
using CarregaReceitasSalaProva.Interfaces;
using Sharp7.Rx.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CarregaReceitasSalaProva
{
    /// <summary>
    /// Lógica interna para LoadRecipeMainWindow.xaml
    /// </summary>
    public partial class LoadRecipeMainWindow : Window
    {
        public static System.Timers.Timer Set(System.Action action, int interval)
        {
            var timer = new System.Timers.Timer(interval);
            timer.Elapsed += (s, e) => {
                timer.Enabled = false;
                action();
                timer.Enabled = true;
            };
            timer.Enabled = true;
            return timer;
        }

        public LoadRecipeMainWindow()
        {
            InitializeComponent();

            LoadingRecipe loadingRecipe = new();
            RenderPages.Children.Clear();
            RenderPages.Children.Add(loadingRecipe);

            StatusMessageService.StatusMessageReceived += message =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (message == "Receita carregada com sucesso!")
                    {
                        System.Timers.Timer timer = Set(() =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                Close();
                            });
                        }, 2000);
                    }

                    else if (message == "Falha ao carregar receita")
                    {
                        RenderPages.Children.Clear();
                        RenderPages.Children.Add(new LoadingFailed());
                    }
                });
            };
        }
    }
}
