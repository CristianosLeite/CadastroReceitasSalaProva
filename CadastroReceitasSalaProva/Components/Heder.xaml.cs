﻿using System;
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

namespace CadastroReceitasSalaProva
{
    /// <summary>
    /// Interação lógica para Heder.xam
    /// </summary>
    public partial class Heder : UserControl
    {
        public Heder()
        {
            InitializeComponent();
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Window.GetWindow(this)?.DragMove();
            }
        }
    }
}
