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
using System.Windows.Navigation;
using NetVips;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Image = NetVips.Image;

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

           fetch_listview_data(mangaDirectory);
        }

        private void fetch_listview_data(string mangaDirectory)
        {
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
            if (lv_mangadata.SelectedItem != null)
            {
                tbox_Title.Text = lv_mangadata.SelectedItem.ToString();
            }
            
        }

        private async void convert_click(object sender, RoutedEventArgs e)
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
            bool a = (check_img1.IsChecked == true);
            bool b = (check_img2.IsChecked == true);

            string directory_images = $"{Properties.Settings.Default.manga_directory}\\{tbox_Title.Text}";

            //check folder size
            long bfsize = FolderSizeCalculator.GetDirectorySizeParallel(directory_images);
            reportSizeBefore.Content = FolderSizeCalculator.FormatSize(bfsize);
            /// Get all image file paths from the input directory
            var imageFiles = Directory.GetFiles(directory_images, "*.*", SearchOption.AllDirectories)
                                .Where(f =>
                                    (a || !b) && (f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".tiff")) ||  // Default to a's file types if a is true or both a and b are false
                                    (b && !a && f.EndsWith(".webp"))                                                   // If b is true and a is false, use webp
                                )
                                .ToList();

            // Natural sort using CompareInfo
            imageFiles.Sort(NaturalStringComparer);

            await ProcessImages(imageFiles, resize_width, quality,parallel,resize_status,del_stat);

            //string message = String.Join(Environment.NewLine, imageFiles);
            //MessageBox.Show($"Resize Status : {resize_status}\nResize Width : {resize_width}\nQuality : {quality}\nDelete Status : {del_stat}\nParallel : {parallel}\nPng,jpg :{img1_status}\nwebp: {img2_status}\nImages :{message}");
            long afsize = FolderSizeCalculator.GetDirectorySizeParallel(directory_images);
            reportSizeAfter.Content = FolderSizeCalculator.FormatSize(afsize);

            double percentDrop = FolderSizeCalculator.CalculatePercentChange(bfsize, afsize);
            // Remove the minus sign and format the percentage
            int roundedPercent = (int)Math.Ceiling(Math.Abs(percentDrop));
            string displayPercent = $"{roundedPercent}%";
            // Change the label's color based on the value
            if (percentDrop < 0)
            {
                reportPercent.Content = displayPercent;
                reportPercent.Foreground = new SolidColorBrush(Colors.Green); // Green for reduction
            }
            else
            {
                reportPercent.Content = displayPercent;
                reportPercent.Foreground = new SolidColorBrush(Colors.Red); // Red for increase
            }

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
                    int result = System.String.Compare(xParts[i], yParts[i], StringComparison.OrdinalIgnoreCase);
                    if (result != 0)
                        return result;
                }
            }

            // If all compared parts are equal, the shorter string should come first
            return xParts.Length.CompareTo(yParts.Length);
        }

        private void clear_btn_click(object sender, RoutedEventArgs e)
        {
            tbox_search.Clear();
        }

        private void btn_config_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow settingWindow = new SettingWindow();
            settingWindow.ShowDialog();

            // If the user cancels, exit the application
            if (settingWindow.DialogResult == true)
            {
                fetch_listview_data(Properties.Settings.Default.manga_directory);
                tbox_Title.Text = "";
                tbox_search.Focus();
            }
            
        }

        // Add line to RichTextBox
        public void AddTextToRichTextBox(string text)
        {
            // Get the existing Paragraph or create a new one if it's the first time
            Paragraph paragraph;
            if (display_status.Document.Blocks.FirstBlock is Paragraph existingParagraph)
            {
                paragraph = existingParagraph;
            }
            else
            {
                paragraph = new Paragraph();
                display_status.Document.Blocks.Add(paragraph);
            }

            // Append text followed by a newline
            paragraph.Inlines.Add(new Run(text));
            paragraph.Inlines.Add(new LineBreak()); // Adds a new line after the text
        }

        public void Display_log_convert(bool convert_status,string chapter, string images, bool resize, int resize_width, bool delete_status)
        {
            Paragraph paragraph;

            // Get the existing Paragraph or create a new one if it's the first time
            if (display_status.Document.Blocks.FirstBlock is Paragraph existingParagraph)
            {
                paragraph = existingParagraph;
            }
            else
            {
                paragraph = new Paragraph();
                display_status.Document.Blocks.Add(paragraph);
            }

            //checking status Convert
            if (convert_status) {
                paragraph.Inlines.Add(new Bold(new Run("Converted")
                {
                    Foreground = Brushes.Green,
                }));
            }
            else
            {
                paragraph.Inlines.Add(new Bold(new Run("Failed")
                {
                    Foreground = Brushes.Red,
                }));
            }

            // Add a semicolon
            paragraph.Inlines.Add(new Run(" : "));


            // Append Chapter
            paragraph.Inlines.Add(new Run($"Chapter {chapter} - "));

            // Append Images
            paragraph.Inlines.Add(new Run($"Images "));
            paragraph.Inlines.Add(new Bold(new Run(images)));

            if (convert_status)
            {
                // Add resize
                if (resize) {
                    paragraph.Inlines.Add(new Run(" - "));
                    paragraph.Inlines.Add(new Bold(new Run("Resize : ")
                    {
                        Foreground = Brushes.Blue,
                    }));
                    paragraph.Inlines.Add(new Run($"{resize_width}"));
                }

                if (delete_status) {
                    paragraph.Inlines.Add(new Run(" - "));
                    paragraph.Inlines.Add(new Bold(new Run("Deleted")
                    {
                        Foreground = Brushes.Red,
                    }));
                }
            }

            // Add a line break after the text
            paragraph.Inlines.Add(new LineBreak());
            display_status.ScrollToEnd();
        }

        private void btn_reset_Click(object sender, RoutedEventArgs e)
        {
            reportSizeBefore.Content = "";
            reportSizeAfter.Content = "";
            reportPercent.Content = "";
            tbox_search.Clear();
            tbox_Title.Clear();
            quality_slider.Value = 60;
            resize_slider.Value = 1000;
            slider_parallel.Value = 5;
            lv_mangadata.SelectedItem = null;
            radio_del_yes.IsChecked = true;
            resize_bold.Text = "";
            resize_btn.Visibility = Visibility.Hidden;
            check_img1.IsChecked = true;
            check_img2.IsChecked = false;
            display_status.Document.Blocks.Clear();
            progress_convert.Value = 0;
            tbox_search.Focus();


        }

        public async Task ProcessImages(List<string> imagePaths,int newWidth ,int quality,int parallel,bool resize_status, bool del_status)
        {
            int totalImages = imagePaths.Count; // Total number of images
            int processedCount = 0; // Counter for processed images
                                    // Parallel processing of images
            await Task.Run(() => // Run the processing in a background task
            {
                // Parallel processing of images
                Parallel.ForEach(imagePaths, new ParallelOptions { MaxDegreeOfParallelism = parallel }, imagePath =>
                {
                    try
                    {
                        // Open the images with NetVips
                        using (var image = Image.NewFromFile(imagePath))
                        {
                            Image convertImage = image;

                            int finalWidth = image.Width; // Keep track of the final width
                            int finalHeight = image.Height; // Keep track of the final height
                            bool finalStatusResize = false;

                            // Landscape: Limit width to 1980 if greater
                            if (image.Width > image.Height)
                            {
                                if (image.Width > 1980)
                                {
                                    finalWidth = 1980;
                                    double aspectRatio = (double)image.Height / image.Width;
                                    finalHeight = (int)Math.Ceiling(finalWidth * aspectRatio); // Maintain aspect ratio
                                }
                            }
                            // Portrait: Only resize if width is greater than newWidth
                            else
                            {
                                if (image.Width > newWidth)
                                {
                                    finalWidth = newWidth;
                                    double aspectRatio = (double)image.Height / image.Width;
                                    finalHeight = (int)Math.Ceiling(finalWidth * aspectRatio); // Maintain aspect ratio
                                }
                            }

                            // Resize the image if needed
                            if (finalWidth < image.Width) {

                                convertImage = image.Resize((double)finalWidth / image.Width, Enums.Kernel.Mitchell, vscale: (double)finalHeight / image.Height);
                                finalStatusResize = true;
                            }
                             

                            // Define the output path for webp
                            string outputPath = Path.ChangeExtension(imagePath, ".webp");

                            // Set WebP compression options similar to cwebp
                            var options = new VOption
                            {
                                { "Q", quality },           // Quality 60
                                { "lossless", false },      // Lossy compression
                            };

                            // Save the image as Webp with quality
                            convertImage.WriteToFile(outputPath, options);

                            // Delete the old image file
                            if (File.Exists(imagePath) && del_status)
                            {
                                File.Delete(imagePath); // Delete the original image file
                            }

                            //await Task.Delay(500);

                            // Update processed count
                            int currentProcessed = Interlocked.Increment(ref processedCount); // Atomically increment the count

                            // Log progress for each image (for completeness)
                            Dispatcher.Invoke(() =>
                            {
                                var res = ExtractPathComponents(imagePath);
                                Display_log_convert(true, res.Chapter, res.ImageFile, finalStatusResize, finalWidth, del_status);
                            });

                            // Only update the progress bar periodically for performance
                            if (currentProcessed % 5 == 0 || currentProcessed == totalImages)
                            {
                                double progress = (double)currentProcessed / totalImages * 100;
                                Dispatcher.Invoke(() =>
                                {
                                    progress_convert.Value = progress;
                                });
                            }

                        }
                    }
                    catch
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // Your UI updating code here
                            var res = ExtractPathComponents(imagePath);
                            Display_log_convert(false, res.Chapter, res.ImageFile, false, newWidth, false);
                        }));

                    }
                });
            });
        }

        public static (string Title, string Chapter, string ImageFile) ExtractPathComponents(string filePath)
        {
            // Split the path into its components
            var parts = filePath.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            // Assuming the structure is fixed: 
            // [Drive Letter] [Folder1] [Folder2] ... [Title] [Chapter] [ImageFile]
            // Title is all parts between the second last folder and last folder
            string title = string.Join(" ", parts[4..^1]); // From index 4 to the second last index
            string chapter = parts[^2]; // Second last part is the chapter number
            string imageFile = parts[^1]; // Last part is the image file name

            return (title, chapter, imageFile);
        }

        // print report manga before
        private void ReportManga(string mangaTitle)
        {
            Paragraph paragraph = display_status.Document.Blocks.FirstBlock as Paragraph ?? new Paragraph();

            if (paragraph.Parent == null) // Check if it's a new paragraph that hasn't been added
            {
                display_status.Document.Blocks.Add(paragraph);
            }

            paragraph.Inlines.Add(new Run(new string('=', 20)));
            paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Bold(new Run(mangaTitle.ToUpper())));
            paragraph.Inlines.Add(new Run(new string('=', 20)));
            paragraph.Inlines.Add(new LineBreak());

        }
    } // mainwindows
} // namespace