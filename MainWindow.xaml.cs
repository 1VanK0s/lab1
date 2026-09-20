using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private bool _splashActive = true;
        private bool _transitioning = false;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            PreviewKeyDown += MainWindow_PreviewKeyDown;
            MouseDown += MainWindow_MouseDown;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Try to load images from a Resources folder next to executable
            await LoadLogosAsync();

            // Fade in logo
            var fadeLogo = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(2.2)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            LogoImage.BeginAnimation(OpacityProperty, fadeLogo);

            // After logo fades in, show "Press anything to start" with small delay
            await Task.Delay(1200);
            var fadeText = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(1.2)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            StartText.BeginAnimation(OpacityProperty, fadeText);
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TryStartTransition();
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TryStartTransition();
        }

        private void TryStartTransition()
        {
            if (!_splashActive || _transitioning) return;
            _transitioning = true;
            StartLoadingSequence();
        }

        private async void StartLoadingSequence()
        {
            // fade out splash
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
            LogoImage.BeginAnimation(OpacityProperty, fadeOut);
            StartText.BeginAnimation(OpacityProperty, fadeOut);

            await Task.Delay(520);
            SplashGrid.Visibility = Visibility.Collapsed;

            // show loading
            LoadingGrid.Visibility = Visibility.Visible;
            _splashActive = false;

            // simulate loading
            for (int i = 0; i <= 100; i++)
            {
                ProgressBar.Value = i;
                ProgressText.Text = i + "%";
                await Task.Delay(25 + i / 2); // slight acceleration
            }

            // transition to character select
            await Task.Delay(400);
            LoadingGrid.Visibility = Visibility.Collapsed;
            CharacterGrid.Visibility = Visibility.Visible;
            // small reveal animation
            var fadeInChar = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            CharacterGrid.BeginAnimation(OpacityProperty, fadeInChar);
        }

        private async Task LoadLogosAsync()
        {
            try
            {
                // Try to find the project/source folder that contains MainWindow.xaml by walking up from the assembly location.
                string assemblyDir = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
                string resourcesDir = null;

                string probe = assemblyDir;
                for (int i = 0; i < 6 && !string.IsNullOrEmpty(probe); i++)
                {
                    string candidateXaml = Path.Combine(probe, "MainWindow.xaml");
                    if (File.Exists(candidateXaml))
                    {
                        resourcesDir = Path.Combine(probe, "Resources");
                        break;
                    }
                    probe = Path.GetDirectoryName(probe);
                }

                // If not found, fall back to a Resources folder next to the assembly (bin output).
                if (resourcesDir == null)
                {
                    resourcesDir = Path.Combine(assemblyDir, "Resources");
                }

                string[] candidates = new string[0];
                if (Directory.Exists(resourcesDir))
                {
                    candidates = Directory.GetFiles(resourcesDir)
                        .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                }

                // Attempt to use any found images. Prefer a file with 'logo' in name for main logo.
                string main = candidates.FirstOrDefault(f => Path.GetFileName(f).ToLower().Contains("logo")) ?? candidates.FirstOrDefault();
                string small = candidates.FirstOrDefault(f => Path.GetFileName(f).ToLower().Contains("small")) ?? candidates.Skip(1).FirstOrDefault() ?? main;

                if (!string.IsNullOrEmpty(main) && File.Exists(main))
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(main, UriKind.Absolute);
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    LogoImage.Source = img;
                }

                if (!string.IsNullOrEmpty(small) && File.Exists(small))
                {
                    var img2 = new BitmapImage();
                    img2.BeginInit();
                    img2.UriSource = new Uri(small, UriKind.Absolute);
                    img2.CacheOption = BitmapCacheOption.OnLoad;
                    img2.DecodePixelWidth = 64;
                    img2.EndInit();
                    SmallLogo.Source = img2;
                }
                else
                {
                    // Try pack uri fallback (if images added as resources)
                    TryPackUriFallback();
                }
            }
            catch
            {
                TryPackUriFallback();
            }

            await Task.CompletedTask;
        }

        private void TryPackUriFallback()
        {
            try
            {
                // common names
                var packMain = new Uri("pack://application:,,,/Resources/logo.png", UriKind.Absolute);
                LogoImage.Source = new BitmapImage(packMain);
                var packSmall = new Uri("pack://application:,,,/Resources/small.png", UriKind.Absolute);
                SmallLogo.Source = new BitmapImage(packSmall);
            }
            catch
            {
                // ignore - images will be empty
            }
        }
    }
}
