using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System;
using System.IO;
using System.Linq;
using Avalonia.Markup.Xaml;
using Muine.App.ViewModels;
using Muine.App.Views;
using Muine.Core.Services;

namespace Muine.App;

public partial class App : Application
{
    private static MusicDatabaseService? _databaseService;
    private static MusicBrainzService? _musicBrainzService;
    private static MetadataService? _metadataService;
    private static ManagedLibraryService? _managedLibraryService;
    private static LibraryConfigurationService? _libraryConfigService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            
            // Initialize shared services
            InitializeServices();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void InitializeServices()
    {
        try
        {
            var databasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Muine",
                "music.db");
            
            _databaseService = new MusicDatabaseService(databasePath);
            _musicBrainzService = new MusicBrainzService();
            _metadataService = new MetadataService();
            _libraryConfigService = new LibraryConfigurationService();
            
            var libraryConfig = _libraryConfigService.LoadConfiguration();
            _managedLibraryService = new ManagedLibraryService(libraryConfig, _metadataService, _databaseService);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Failed to initialize services: {ex.Message}");
        }
    }

    public static MusicBrainzSearchViewModel CreateMusicBrainzSearchViewModel()
    {
        return new MusicBrainzSearchViewModel(
            _musicBrainzService ?? new MusicBrainzService(),
            _metadataService ?? new MetadataService(),
            _databaseService ?? throw new InvalidOperationException("Database service not initialized"),
            _managedLibraryService
        );
    }

    public static MetadataReviewViewModel CreateMetadataReviewViewModel()
    {
        return new MetadataReviewViewModel(
            _databaseService ?? throw new InvalidOperationException("Database service not initialized"),
            _musicBrainzService ?? new MusicBrainzService(),
            _metadataService ?? new MetadataService(),
            _managedLibraryService
        );
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}