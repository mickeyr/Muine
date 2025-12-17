using Avalonia.Controls;
using Avalonia.Interactivity;
using Muine.App.ViewModels;
using System.Threading.Tasks;

namespace Muine.App.Views;

public partial class AddRadioStationWindow : Window
{
    public bool WasSaved { get; private set; }

    public AddRadioStationWindow()
    {
        InitializeComponent();
    }

    private async void OnExtractMetadataClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddRadioStationViewModel vm)
        {
            await vm.ExtractMetadataAsync();
        }
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AddRadioStationViewModel vm)
        {
            var success = await vm.SaveAsync();
            if (success)
            {
                WasSaved = true;
                Close();
            }
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        WasSaved = false;
        Close();
    }
}
