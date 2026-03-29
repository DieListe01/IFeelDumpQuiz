using Godot;
using IFeelDumpQuiz;
using IFeelDumpQuiz.Services;
using System.Threading.Tasks;

public partial class MainMenu : Control
{
    private UpdateService _updateService = null!;
    private Label _updateStatus = null!;
    private ConfirmationDialog _updateDialog = null!;
    private AcceptDialog _infoDialog = null!;
    private UpdateInfo? _pendingUpdate;

    public MainMenu()
    {
        GD.Print("MainMenu constructor");
    }

    public override void _EnterTree()
    {
        GD.Print("MainMenu._EnterTree");
        base._EnterTree();
    }

    public override void _Ready()
    {
        GD.Print("MainMenu._Ready start");
        try
        {
            _updateService = new UpdateService();
            GD.Print("UpdateService created");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to create UpdateService: {ex}");
            // To avoid later null reference, we still assign but it will cause issues if used.
            // For now, we let it be null and check later.
            _updateService = null!;
        }

        try
        {
            _updateStatus = GetNode<Label>("RootMargin/Center/MenuPanel/MainVBox/UpdateBanner/UpdateStatus");
            GD.Print("UpdateStatus node found");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to get UpdateStatus node: {ex}");
        }

        try
        {
            GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/Buttons/BtnLocalGame").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/GameSetup.tscn");
            GD.Print("BtnLocalGame subscribed");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to subscribe BtnLocalGame: {ex}");
        }

        try
        {
            GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/Buttons/BtnQuestionMenu").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/QuestionMenu.tscn");
            GD.Print("BtnQuestionMenu subscribed");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to subscribe BtnQuestionMenu: {ex}");
        }

        try
        {
            GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/Buttons/BtnHistory").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/HistoryScene.tscn");
            GD.Print("BtnHistory subscribed");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to subscribe BtnHistory: {ex}");
        }

        try
        {
            GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/Buttons/BtnExit").Pressed += () => GetTree().Quit();
            GD.Print("BtnExit subscribed");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to subscribe BtnExit: {ex}");
        }

        CreateDialogs();
        if (!AppMetadata.IsPackagedBuild || OS.HasFeature("editor"))
        {
            _updateStatus.Text = $"Version {AppMetadata.Version} - Entwicklungsmodus";
            return;
        }

        _updateStatus.Text = $"Version {AppMetadata.Version} - pruefe Updates ...";
        _ = CheckForUpdatesAsync();
    }

    private void CreateDialogs()
    {
        _updateDialog = new ConfirmationDialog
        {
            Title = "Update verfuegbar",
            DialogText = ""
        };
        _updateDialog.Confirmed += OnInstallUpdateConfirmed;
        AddChild(_updateDialog);

        _infoDialog = new AcceptDialog
        {
            Title = "Update"
        };
        AddChild(_infoDialog);
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var update = await _updateService.CheckForUpdateAsync();
            if (update == null)
            {
                _updateStatus.Text = $"Version {AppMetadata.Version} - aktuell";
                return;
            }

            _pendingUpdate = update;
            _updateStatus.Text = $"Update verfuegbar: {update.Version}";
            var notes = string.IsNullOrWhiteSpace(update.Notes)
                ? "Keine Release-Notizen verfuegbar."
                : update.Notes.Replace("\r", string.Empty).Trim();
            if (notes.Length > 420)
            {
                notes = notes[..420] + "...";
            }

            _updateDialog.Title = $"Update {update.Version} verfuegbar";
            _updateDialog.DialogText = $"Eine neue Version steht bereit.\n\nWichtige Hinweise:\n{notes}\n\nJetzt herunterladen und installieren?";
            _updateDialog.PopupCentered();
        }
        catch
        {
            _updateStatus.Text = $"Version {AppMetadata.Version} - Updatepruefung fehlgeschlagen";
        }
    }

    private async void OnInstallUpdateConfirmed()
    {
        if (_pendingUpdate == null)
        {
            return;
        }

        try
        {
            _updateStatus.Text = $"Lade Update {_pendingUpdate.Version} herunter ...";
            var zipPath = await _updateService.DownloadUpdateAsync(_pendingUpdate);
            _updateStatus.Text = $"Installiere Update {_pendingUpdate.Version} ...";
            _updateService.StartWindowsUpdater(zipPath);
            GetTree().Quit();
        }
        catch
        {
            _infoDialog.DialogText = "Das Update konnte nicht heruntergeladen oder installiert werden.";
            _infoDialog.PopupCentered();
            _updateStatus.Text = $"Version {AppMetadata.Version}";
        }
    }
}
