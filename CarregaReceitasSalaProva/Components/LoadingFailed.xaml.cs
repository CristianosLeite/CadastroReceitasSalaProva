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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CarregaReceitasSalaProva.Interfaces;

namespace CarregaReceitasSalaProva
{
    /// <summary>
    /// Interação lógica para LoadingFailed.xam
    /// </summary>
    public partial class LoadingFailed : UserControl
    {
        public LoadingFailed()
        {
            InitializeComponent();

            StatusMessageService.StatusMessageReceived += message =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Message.Content = message;
                });
            };
        }
    }
}
