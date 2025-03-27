using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Newtonsoft.Json.Linq;
using System.Windows.Threading;

namespace WeatherApp
{
    public partial class MainWindow : Window
    {
        private static readonly string apiKey = "YOUR_API_KEY";//works if you pay subscription to openweathermap.org
        private static readonly string apiUrl = "https://api.openweathermap.org/data/2.5/weather?q={0}&appid=" + apiKey + "&units=metric";
        private static readonly string forecastUrl = "https://api.openweathermap.org/data/2.5/forecast?q={0}&appid=" + apiKey + "&units=metric";
        private DispatcherTimer refreshTimer;
        
        public MainWindow()
        {
            InitializeComponent();
            GetWeather("New York"); // Default location
            GetForecast("New York");
            
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromMinutes(5);
            refreshTimer.Tick += (s, e) => {
                GetWeather(CurrentCity.Text);
                GetForecast(CurrentCity.Text);
            };
        }

        private async void GetWeather(string city)
        {
            try
            {
                using HttpClient client = new HttpClient();
                string response = await client.GetStringAsync(string.Format(apiUrl, city));
                JObject weatherData = JObject.Parse(response);
                
                string temperature = weatherData["main"]["temp"].ToString() + "°C";
                string condition = weatherData["weather"][0]["main"].ToString();
                string iconCode = weatherData["weather"][0]["icon"].ToString();
                
                WeatherCondition.Text = condition;
                Temperature.Text = temperature;
                CurrentCity.Text = city;
                WeatherIcon.Source = new BitmapImage(new Uri($"https://openweathermap.org/img/wn/{iconCode}@2x.png"));
                
                ApplyWeatherAnimation(condition);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching weather: " + ex.Message);
            }
        }

        private async void GetForecast(string city)
        {
            try
            {
                using HttpClient client = new HttpClient();
                string response = await client.GetStringAsync(string.Format(forecastUrl, city));
                JObject forecastData = JObject.Parse(response);
                
                string forecastText = "";
                for (int i = 0; i < 5; i++) // Get 5 time slots (usually every 3 hours)
                {
                    var entry = forecastData["list"][i];
                    string time = entry["dt_txt"].ToString();
                    string temp = entry["main"]["temp"].ToString() + "°C";
                    string weather = entry["weather"][0]["main"].ToString();
                    forecastText += $"{time}: {weather}, {temp}\n";
                }
                
                ForecastBlock.Text = forecastText;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching forecast: " + ex.Message);
            }
        }

        private void ApplyWeatherAnimation(string condition)
        {
            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                From = 0.5,
                To = 1,
                Duration = TimeSpan.FromSeconds(1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            
            if (condition.Contains("Rain"))
                WeatherBackground.Fill = new SolidColorBrush(Colors.Gray);
            else if (condition.Contains("Clear"))
                WeatherBackground.Fill = new SolidColorBrush(Colors.LightSkyBlue);
            else
                WeatherBackground.Fill = new SolidColorBrush(Colors.LightGray);
            
            WeatherIcon.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            GetWeather(CityInput.Text);
            GetForecast(CityInput.Text);
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
}
