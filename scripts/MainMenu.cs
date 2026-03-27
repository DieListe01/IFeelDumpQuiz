using Godot;
using IFeelDumpQuiz;
using IFeelDumpQuiz.Services;
using System.Threading.Tasks;

public partial class MainMenu : Control
{
    private readonly UpdateService _updateService = new();
    private Label _updateStatus = null!;
    private ConfirmationDialog _updateDialog = null!;
    private AcceptDialog _infoDialog = null!;
    private UpdateInfo? _pendingUpdate;

    public override void _Ready()
    {
        _updateStatus = GetNode<Label>("RootMargin/Center/MenuPanel/MainVBox/UpdateBanner/UpdateStatus");
        GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/Buttons/BtnLocalGame").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/GameSetup.tscn");
        GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/Buttons/BtnQuestionMenu").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/QuestionMenu.tscn");
        GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/Buttons/BtnHistory").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/HistoryScene.tscn");
        GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/Buttons/BtnExit").Pressed += () => GetTree().Quit();

        CreateDialogs();
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
