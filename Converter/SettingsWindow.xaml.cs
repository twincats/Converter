using System;
using System.Collections.Generic;
using System.Configuration;
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

namespace Converter
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SettingWindow : Window
    {
        public SettingWindow()
        {
            InitializeComponent();
            // Load existing settings into the TextBoxes
            tx_manga_directory.Text = Properties.Settings.Default.manga_directory;
            tx_manga_directory.Focus();
            tx_manga_directory.SelectAll();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Update the app.config file
            UpdateAppConfig("manga_directory", tx_manga_directory.Text);

            DialogResult = true; // Close the window and return true
            Close();
        }

        private void UpdateAppConfig(string key, string value)
        {
            // Update the setting value
            Properties.Settings.Default.manga_directory = tx_manga_directory.Text;

            // Save the changes
            Properties.Settings.Default.Save(); // This saves the user settings
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Close the window and return false
            Close();
        }
    }
}
