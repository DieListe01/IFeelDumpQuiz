using Godot;
using IFeelDumpQuiz;
using System.Collections.Generic;
using System.Linq;

public partial class QuestionMenu : Control
{
    private enum SaveDialogAction
    {
        None,
        ExportQuestions,
        ExportTemplate
    }

    private Label _statusLabel = null!;
    private OptionButton _categorySelect = null!;
    private CheckBox _exportCategoryOnly = null!;
    private FileDialog _importDialog = null!;
    private FileDialog _saveDialog = null!;
    private SaveDialogAction _pendingSaveAction;
    private List<QuestionData> _loadedQuestions = new();

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("RootMargin/Center/MainPanel/MainVBox/StatusLabel");
        _categorySelect = GetNode<OptionButton>("RootMargin/Center/MainPanel/MainVBox/FormGrid/CategorySelect");
        _exportCategoryOnly = GetNode<CheckBox>("RootMargin/Center/MainPanel/MainVBox/ExportCategoryOnly");

        GetNode<Button>("RootMargin/Center/MainPanel/MainVBox/ButtonRow/BtnImportCsv").Pressed += OnImportCsvPressed;
        GetNode<Button>("RootMargin/Center/MainPanel/MainVBox/ButtonRow/BtnExportCsv").Pressed += OnExportCsvPressed;
        GetNode<Button>("RootMargin/Center/MainPanel/MainVBox/ButtonRow/BtnTemplateCsv").Pressed += OnTemplateCsvPressed;
        GetNode<Button>("RootMargin/Center/MainPanel/MainVBox/ButtonRow/BtnBack").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");

        CreateFileDialogs();
        ReloadQuestions();
    }

    private void ReloadQuestions()
    {
        _loadedQuestions = QuestionRepository.LoadQuestions();
        _categorySelect.Clear();
        _categorySelect.AddItem("Alle");
        foreach (var category in _loadedQuestions.Select(question => question.Category).Distinct().OrderBy(category => category))
        {
            _categorySelect.AddItem(category);
        }

        _statusLabel.Text = _loadedQuestions.Count == 0
            ? "Aktuell sind keine Fragen im Projekt gespeichert."
            : $"Aktuell sind {_loadedQuestions.Count} Fragen gespeichert.";
    }

    private void OnImportCsvPressed()
    {
        _importDialog.PopupCenteredRatio(0.75f);
    }

    private void OnExportCsvPressed()
    {
        _pendingSaveAction = SaveDialogAction.ExportQuestions;
        _saveDialog.CurrentFile = "questions_export.csv";
        _saveDialog.PopupCenteredRatio(0.75f);
    }

    private void OnTemplateCsvPressed()
    {
        _pendingSaveAction = SaveDialogAction.ExportTemplate;
        _saveDialog.CurrentFile = "questions_template.csv";
        _saveDialog.PopupCenteredRatio(0.75f);
    }

    private void OnImportFileSelected(string path)
    {
        if (QuestionRepository.TryImportQuestions(path, out var importedCount, out var message))
        {
            ReloadQuestions();
            _statusLabel.Text = $"{message} Jetzt sind {importedCount} importierte Fragen verfuegbar.";
            return;
        }

        _statusLabel.Text = message;
    }

    private void OnSaveFileSelected(string path)
    {
        bool result;
        string message;

        switch (_pendingSaveAction)
        {
            case SaveDialogAction.ExportQuestions:
                var selectedCategory = _exportCategoryOnly.ButtonPressed ? _categorySelect.GetItemText(_categorySelect.Selected) : null;
                result = QuestionRepository.ExportQuestions(path, selectedCategory, out message);
                break;
            case SaveDialogAction.ExportTemplate:
                result = QuestionRepository.ExportTemplate(path, out message);
                break;
            default:
                return;
        }

        _pendingSaveAction = SaveDialogAction.None;
        _statusLabel.Text = message;
    }

    private void CreateFileDialogs()
    {
        _importDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Title = "CSV-Datei importieren"
        };
        _importDialog.Filters = new string[] { "*.csv ; CSV-Dateien" };
        _importDialog.FileSelected += OnImportFileSelected;
        AddChild(_importDialog);

        _saveDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Title = "CSV speichern"
        };
        _saveDialog.Filters = new string[] { "*.csv ; CSV-Dateien" };
        _saveDialog.FileSelected += OnSaveFileSelected;
        AddChild(_saveDialog);
    }
}
