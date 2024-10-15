using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using NetVips;

namespace Converter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isInitializedSlider;
        private CollectionViewSource? _collectionViewSource;

        public MainWindow()
        {
            InitializeComponent();

            string mangaDirectory = Properties.Settings.Default.manga_directory;

            // Check if settings are set
            if (string.IsNullOrEmpty(mangaDirectory))
            {
                // Open settings dialog
                SettingWindow settingWindow = new SettingWindow();
                settingWindow.ShowDialog();

                // If the user cancels, exit the application
                if (settingWindow.DialogResult == false)
                {
                    Application.Current.Shutdown();
                    return;
                }
            }


            resize_bold.Text = "";
            isInitializedSlider = true;
            tbox_search.Focus();

            // Create a list of items
            List<string> _items = Directory.GetDirectories(mangaDirectory)
                                               .Select(dir => Path.GetFileName(dir))
                                               .ToList();
            _collectionViewSource = new CollectionViewSource { Source = _items };
            lv_mangadata.ItemsSource = _collectionViewSource.View;
        }

        private void update_quality_label(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            quality_bold.Text = Math.Round((double)e.NewValue).ToString();
        }

        private void parallel_change(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            parallel_bold.Text = Math.Round((double)e.NewValue).ToString();
        }

        private void resize_click(object sender, RoutedEventArgs e)
        {
            resize_btn.Visibility = Visibility.Hidden;
            resize_bold.Text = Math.Round(resize_slider.Value).ToString();
        }

        private void update_resize_label(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isInitializedSlider)
            {
                resize_bold.Text = Math.Round((double)e.NewValue).ToString();
            }
        }

        private void btn_resize_hide_click(object sender, RoutedEventArgs e)
        {
            resize_bold.Text = "";
            resize_btn.Visibility= Visibility.Visible;
        }

        private void lv_mangadata_selection(object sender, SelectionChangedEventArgs e)
        {
            tbox_Title.Text = lv_mangadata.SelectedItem.ToString();
        }

        private void convert_click(object sender, RoutedEventArgs e)
        {
            if (tbox_Title.Text == "")
            {
                return;
            }

            bool resize_status = (resize_btn.Visibility == Visibility.Hidden);
            int resize_width =(int)Math.Round((double)resize_slider.Value);
            int quality = (int)Math.Round((double)quality_slider.Value);
            bool del_stat = (radio_del_yes.IsChecked == true);
            int parallel = (int)Math.Round((double)slider_parallel.Value);
            bool img1_status = (check_img1.IsChecked == true);
            bool img2_status = (check_img2.IsChecked == true);

            string directory_images = $"{Properties.Settings.Default.manga_directory}\\{tbox_Title.Text}";
            /// Get all image file paths from the input directory
            var imageFiles = Directory.GetFiles(directory_images, "*.*", SearchOption.AllDirectories)
                                      .Where(f => f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".tiff"))
                                      .ToList();
            // Natural sort using CompareInfo
            imageFiles.Sort(NaturalStringComparer);

            string message = String.Join(Environment.NewLine, imageFiles);
            MessageBox.Show($"Resize Status : {resize_status}\nResize Width : {resize_width}\nQuality : {quality}\nDelete Status : {del_stat}\nParallel : {parallel}\nPng,jpg :{img1_status}\nwebp: {img2_status}\nImages :{message}");

        }

        private void tbox_search_change(object sender, TextChangedEventArgs e)
        {
            if(_collectionViewSource != null)
            {
                // Apply filter based on user input
                _collectionViewSource.Filter -= FilterItems;
                _collectionViewSource.Filter += FilterItems;

                // Refresh the view to apply the filter
                _collectionViewSource.View.Refresh();
            }
        }

        private void FilterItems(object sender, FilterEventArgs e)
        {
            if (e.Item is string item)
            {
                // Filter logic: case-insensitive search
                if (!string.IsNullOrEmpty(tbox_search.Text) && !item.ToLower().Contains(tbox_search.Text.ToLower()))
                {
                    e.Accepted = false;
                }
                else
                {
                    e.Accepted = true;
                }
            }
        }

        // Custom natural sorting comparer
        static int NaturalStringComparer(string x, string y)
        {
            // Regex to split the strings into parts: numbers and non-numbers
            var regex = new Regex(@"\d+|\D+");
            var xParts = regex.Matches(x).Cast<Match>().Select(m => m.Value).ToArray();
            var yParts = regex.Matches(y).Cast<Match>().Select(m => m.Value).ToArray();

            int partCount = Math.Min(xParts.Length, yParts.Length);

            for (int i = 0; i < partCount; i++)
            {
                // If both parts are numeric, compare them as integers
                if (int.TryParse(xParts[i], out int xNum) && int.TryParse(yParts[i], out int yNum))
                {
                    int result = xNum.CompareTo(yNum);
                    if (result != 0)
                        return result;
                }
                else
                {
                    // Compare the parts as strings (case-insensitive)
                    int result = String.Compare(xParts[i], yParts[i], StringComparison.OrdinalIgnoreCase);
                    if (result != 0)
                        return result;
                }
            }

            // If all compared parts are equal, the shorter string should come first
            return xParts.Length.CompareTo(yParts.Length);
        }
    }
}