using System.Windows;

namespace BioVic.Desktop.Views;

public partial class SplashScreenWindow : Window
{
    public SplashScreenWindow()
    {
        InitializeComponent();
    }

    public void UpdateStatus(string message)
    {
        Dispatcher.Invoke(() => StatusText.Text = message);
    }
}