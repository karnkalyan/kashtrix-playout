using System.Windows;
using System.Windows.Input;

namespace Kashtrix.InputEngine;

public partial class FullScreenInputWindow : Window
{
    public FullScreenInputWindow(InputEngineCardModel card)
    {
        InitializeComponent();
        DataContext = card;
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape or Key.F11)
        {
            Close();
            e.Handled = true;
        }
    }

    private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        Close();
        e.Handled = true;
    }
}
