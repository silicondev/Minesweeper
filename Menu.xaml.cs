using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Minesweeper
{
    /// <summary>
    /// Interaction logic for Menu.xaml
    /// </summary>
    public partial class Menu : Window
    {
        public Menu()
        {
            InitializeComponent();

            rdbDifficultyEasy.Click += OnSwitch;
            rdbDifficultyMedium.Click += OnSwitch;
            rdbDifficultyHard.Click += OnSwitch;
        }

        public int Difficulty { get; private set; } = 2;

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void OnSwitch(object sender, RoutedEventArgs e)
        {
            var rdb = sender as RadioButton;
            if (rdb == null) return;

            switch (rdb.Content)
            {
                case "EASY":
                    Difficulty = 1;
                    break;
                case "MEDIUM":
                    Difficulty = 2;
                    break;
                case "HARD":
                    Difficulty = 3;
                    break;
            }
        }
    }
}
