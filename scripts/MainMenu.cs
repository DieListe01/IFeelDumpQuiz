using Godot;
using IFeelDumpQuiz;
using IFeelDumpQuiz.Services;
using System.IO;
using System.Threading.Tasks;

public partial class MainMenu : Control
{
	private const string EditorUpdateTestVersion = "0.2.3";

	private UpdateService _updateService = null!;
	private Label _updateStatus = null!;
	private Button _btnUpdateTest = null!;
	private Button _btnUpdateDownloadTest = null!;
	private Button _btnUpdateExtractTest = null!;
	private AudioStreamPlayer _startupAudioPlayer = null!;
	private ConfirmationDialog _updateDialog = null!;
	private AcceptDialog _infoDialog = null!;
	private UpdateInfo? _pendingUpdate;
	private const string StartupSoundPath = "res://audio/startup.mp3";

	public override void _Ready()
	{
		_updateService = new UpdateService();
		_updateStatus = GetNode<Label>("RootMargin/Center/MenuPanel/MainVBox/UpdateBanner/UpdateStatus");
		_btnUpdateTest = GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/BtnUpdateTest");
		_btnUpdateDownloadTest = GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/BtnUpdateDownloadTest");
		_btnUpdateExtractTest = GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/BtnUpdateExtractTest");
		_startupAudioPlayer = GetNode<AudioStreamPlayer>("StartupAudioPlayer");
		GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/Buttons/BtnLocalGame").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/GameSetup.tscn");
		GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/Buttons/BtnQuestionMenu").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/QuestionMenu.tscn");
		GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/Buttons/BtnHistory").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/HistoryScene.tscn");
		GetNode<Button>("RootMargin/Center/MenuPanel/MainVBox/Buttons/BtnExit").Pressed += () => GetTree().Quit();
		_btnUpdateTest.Pressed += () => _ = RunEditorUpdateTestAsync();
		_btnUpdateDownloadTest.Pressed += () => _ = RunEditorDownloadTestAsync();
		_btnUpdateExtractTest.Pressed += () => _ = RunEditorExtractTestAsync();

		CreateDialogs();
		PlayStartupSoundIfAvailable();
		if (!AppMetadata.IsPackagedBuild || OS.HasFeature("editor"))
		{
			_updateStatus.Text = $"Version {AppMetadata.Version} - Entwicklungsmodus";
			_btnUpdateTest.Visible = true;
			_btnUpdateDownloadTest.Visible = true;
			_btnUpdateExtractTest.Visible = true;
			return;
		}

		_btnUpdateTest.Visible = false;
		_btnUpdateDownloadTest.Visible = false;
		_btnUpdateExtractTest.Visible = false;
		_updateStatus.Text = $"Version {AppMetadata.Version} - pruefe Updates ...";
		_ = CheckForUpdatesAsync();
	}

	private void PlayStartupSoundIfAvailable()
	{
		if (!ResourceLoader.Exists(StartupSoundPath))
		{
			return;
		}

		var stream = GD.Load<AudioStream>(StartupSoundPath);
		if (stream == null)
		{
			return;
		}

		_startupAudioPlayer.Stream = stream;
		_startupAudioPlayer.Play();
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
		await CheckForUpdatesAsync(null, false);
	}

	private async Task RunEditorUpdateTestAsync()
	{
		_updateStatus.Text = $"Editor-Test: pruefe GitHub gegen {EditorUpdateTestVersion} ...";
		await CheckForUpdatesAsync(EditorUpdateTestVersion, true);
	}

	private async Task RunEditorDownloadTestAsync()
	{
		try
		{
			_updateStatus.Text = $"Editor-Test: suche Release fuer {EditorUpdateTestVersion} ...";
			var update = await _updateService.CheckForUpdateAsync(EditorUpdateTestVersion);
			if (update == null)
			{
				_updateStatus.Text = $"Editor-Test: kein Download-Test moeglich, kein neueres Release gefunden.";
				return;
			}

			_updateStatus.Text = $"Editor-Test: lade {update.Version} herunter ...";
			var zipPath = await _updateService.DownloadUpdateAsync(update);
			_updateStatus.Text = $"Editor-Test: Download erfolgreich ({update.Version})";
			_infoDialog.Title = "Download-Test erfolgreich";
			_infoDialog.DialogText = $"Das Release-ZIP wurde erfolgreich heruntergeladen:\n\n{zipPath}";
			_infoDialog.PopupCentered();
		}
		catch
		{
			_updateStatus.Text = "Editor-Test: Download fehlgeschlagen";
			_infoDialog.Title = "Download-Test fehlgeschlagen";
			_infoDialog.DialogText = "Das Release-ZIP konnte nicht heruntergeladen werden.";
			_infoDialog.PopupCentered();
		}
	}

	private async Task RunEditorExtractTestAsync()
	{
		try
		{
			_updateStatus.Text = $"Editor-Test: suche Release fuer {EditorUpdateTestVersion} ...";
			var update = await _updateService.CheckForUpdateAsync(EditorUpdateTestVersion);
			if (update == null)
			{
				_updateStatus.Text = "Editor-Test: kein Entpack-Test moeglich, kein neueres Release gefunden.";
				return;
			}

			_updateStatus.Text = $"Editor-Test: lade {update.Version} herunter ...";
			var zipPath = await _updateService.DownloadUpdateAsync(update);
			_updateStatus.Text = $"Editor-Test: entpacke {update.Version} ...";
			var extractPath = _updateService.ExtractUpdateToTestDirectory(zipPath, update.Version);
			var exePath = Path.Combine(extractPath, AppMetadata.WindowsExecutableName);
			var versionPath = Path.Combine(extractPath, "VERSION");

			if (!File.Exists(exePath) || !File.Exists(versionPath))
			{
				throw new IOException("Entpacktes Update enthaelt nicht alle erwarteten Dateien.");
			}

			_updateStatus.Text = $"Editor-Test: Entpacken erfolgreich ({update.Version})";
			_infoDialog.Title = "Entpack-Test erfolgreich";
			_infoDialog.DialogText = $"Das Release-ZIP wurde erfolgreich entpackt nach:\n\n{extractPath}\n\nGefunden:\n- {AppMetadata.WindowsExecutableName}\n- VERSION";
			_infoDialog.PopupCentered();
		}
		catch
		{
			_updateStatus.Text = "Editor-Test: Entpacken fehlgeschlagen";
			_infoDialog.Title = "Entpack-Test fehlgeschlagen";
			_infoDialog.DialogText = "Das Release-ZIP konnte nicht sauber entpackt oder verifiziert werden.";
			_infoDialog.PopupCentered();
		}
	}

	private async Task CheckForUpdatesAsync(string? currentVersionOverride, bool isEditorTest)
	{
		try
		{
			var update = await _updateService.CheckForUpdateAsync(currentVersionOverride);
			if (update == null)
			{
				_updateStatus.Text = isEditorTest
					? $"Editor-Test: kein neueres Release gefunden (Vergleich mit {currentVersionOverride})."
					: $"Version {AppMetadata.Version} - aktuell";
				return;
			}

			_pendingUpdate = update;
			_updateStatus.Text = isEditorTest
				? $"Editor-Test: Release {update.Version} gefunden"
				: $"Update verfuegbar: {update.Version}";
			var notes = string.IsNullOrWhiteSpace(update.Notes)
				? "Keine Release-Notizen verfuegbar."
				: update.Notes.Replace("\r", string.Empty).Trim();
			if (notes.Length > 420)
			{
				notes = notes[..420] + "...";
			}

			_updateDialog.Title = isEditorTest
				? $"Editor-Test: Update {update.Version} gefunden"
				: $"Update {update.Version} verfuegbar";
			_updateDialog.DialogText = isEditorTest
				? $"GitHub meldet fuer die Testversion {currentVersionOverride} ein neueres Release.\n\nWichtige Hinweise:\n{notes}\n\nJetzt herunterladen und installieren?"
				: $"Eine neue Version steht bereit.\n\nWichtige Hinweise:\n{notes}\n\nJetzt herunterladen und installieren?";
			_updateDialog.PopupCentered();
		}
		catch
		{
			_updateStatus.Text = isEditorTest
				? "Editor-Test: Updatepruefung fehlgeschlagen"
				: $"Version {AppMetadata.Version} - Updatepruefung fehlgeschlagen";
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
