using Avalonia.Controls;
using Avalonia.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;
namespace FactorCalculator;

public partial class MainWindow : Window
{

    MainViewModel vm = new();
    public MainWindow()
    {
        //vm.Results.CollectionChanged += Results_CollectionChanged;
        DataContext = vm;
        Task.Run(Update);
        InitializeComponent();
    }

    private void Update()
    {
        try
        {
            var manager = new UpdateManager(new GithubSource("https://github.com/DVSProductions/Factor-Calculator", null, false));
            var newVersion = manager.CheckForUpdates();
            if (newVersion == null)
                return; // no update available
            vm.Updating = true;
            // download new version
            manager.DownloadUpdates(newVersion,(x)=>Dispatcher.UIThread.Post(() =>
            {
                vm.HasDownloadState = true;
                vm.UpdateProgress = x;
            }));

            // install new version and restart app
            manager.ApplyUpdatesAndRestart(newVersion);
        }
        catch { }
    }

    //private void Results_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
    //	ResultsViewer.ScrollToEnd();
    //}

    private void ListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
            vm.VisualizeResultCommand.Execute(e.AddedItems[0]);
    }
}