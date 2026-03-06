using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace WeatherApp
{
    public partial class MainWindow : Window
    {
        private static readonly string apiKey = "44d5686adca5804b4770bb25a76260c8";
        private static readonly string apiUrl = "https://api.openweathermap.org/data/2.5/weather?q={0}&appid=" + apiKey + "&units=metric";
        private static readonly string forecastUrl = "https://api.openweathermap.org/data/2.5/forecast?q={0}&appid=" + apiKey + "&units=metric";

        private DispatcherTimer refreshTimer;

        public MainWindow()
        {
            InitializeComponent();

            // Set up Auto-Refresh Timer
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromMinutes(5);
            refreshTimer.Tick += (s, e) => { RefreshData(CurrentCity.Text); };

            // Default location on load
            RefreshData("Regina");
        }

        private async void RefreshData(string city)
        {
            if (string.IsNullOrWhiteSpace(city)) return;

            LoadingText.Visibility = Visibility.Visible;
            await GetWeather(city);
            await GetForecast(city);
            LoadingText.Visibility = Visibility.Collapsed;
        }

        private async Task GetWeather(string city)
        {
            try
            {
                using HttpClient client = new HttpClient();
                string response = await client.GetStringAsync(string.Format(apiUrl, city));
                JObject weatherData = JObject.Parse(response);

                string temperature = Math.Round((double)weatherData["main"]["temp"]) + "°C";
                string condition = weatherData["weather"][0]["main"].ToString();
                string iconCode = weatherData["weather"][0]["icon"].ToString();

                WeatherCondition.Text = condition;
                Temperature.Text = temperature;
                CurrentCity.Text = weatherData["name"].ToString(); // Uses official city name from API

                WeatherIcon.Source = new BitmapImage(new Uri($"https://openweathermap.org/img/wn/{iconCode}@2x.png"));

                ApplyWeatherAnimation(condition);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching weather. Please check the city name or your API key.");
            }
        }

        private async Task GetForecast(string city)
        {
            try
            {
                using HttpClient client = new HttpClient();
                string response = await client.GetStringAsync(string.Format(forecastUrl, city));
                JObject forecastData = JObject.Parse(response);

                List<ForecastItem> forecastList = new List<ForecastItem>();

                // Get next 6 intervals (18 hours)
                for (int i = 0; i < 6; i++)
                {
                    var entry = forecastData["list"][i];
                    DateTime date = DateTime.Parse(entry["dt_txt"].ToString());
                    string iconCode = entry["weather"][0]["icon"].ToString();

                    forecastList.Add(new ForecastItem
                    {
                        Time = date.ToString("ddd h:mm tt"), // e.g., "Fri 2:00 PM"
                        Temp = Math.Round((double)entry["main"]["temp"]) + "°C",
                        Condition = entry["weather"][0]["main"].ToString(),
                        IconUrl = $"https://openweathermap.org/img/wn/{iconCode}.png"
                    });
                }

                // Bind data to the UI List
                ForecastList.ItemsSource = forecastList;
            }
            catch (Exception)
            {
                // Silently fail forecast if it errors, weather might still work
            }
        }

        private void ApplyWeatherAnimation(string condition)
        {
            // 1. Icon Pulse Animation
            DoubleAnimation pulseAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            WeatherIcon.BeginAnimation(OpacityProperty, pulseAnimation);

            // 2. Smooth Background Color Transition
            Color targetColor = Colors.WhiteSmoke; // Default fallback

            if (condition.Contains("Rain") || condition.Contains("Drizzle")) targetColor = Colors.LightSteelBlue;
            else if (condition.Contains("Clear")) targetColor = Colors.LightSkyBlue;
            else if (condition.Contains("Clouds")) targetColor = Colors.LightGray;
            else if (condition.Contains("Snow")) targetColor = Colors.AliceBlue;
            else if (condition.Contains("Thunderstorm")) targetColor = Colors.SlateGray;

            SolidColorBrush animatedBrush = new SolidColorBrush(((SolidColorBrush)WeatherBackground.Fill).Color);
            WeatherBackground.Fill = animatedBrush;

            ColorAnimation colorAnim = new ColorAnimation(targetColor, TimeSpan.FromSeconds(2));
            animatedBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
        }

        // --- Event Handlers ---

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshData(CityInput.Text);
        }

        private void CityInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RefreshData(CityInput.Text);
            }
        }

        private void AutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            refreshTimer.Start();
        }

        private void AutoRefresh_Unchecked(object sender, RoutedEventArgs e)
        {
            refreshTimer.Stop();
        }
    }

    // --- Data Model for the Forecast List ---
    public class ForecastItem
    {
        public string Time { get; set; }
        public string Temp { get; set; }
        public string IconUrl { get; set; }
        public string Condition { get; set; }
    }
}
