using Godot;
using IFeelDumpQuiz;
using IFeelDumpQuiz.Repositories;
using IFeelDumpQuiz.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class QuestionMenu : Control
{
    private enum SaveDialogAction
    {
        None,
        ExportQuestions,
        ExportTemplate
    }

    private enum MediaTargetSlot
    {
        None,
        Slot1,
        Slot2
    }

    private ItemList _categoryList = null!;
    private LineEdit _txtSearch = null!;
    private VBoxContainer _questionRows = null!;
    private Label _questionCountLabel = null!;
    private Label _statusLabel = null!;
    private Control _editorOverlay = null!;
    private Label _popupTitle = null!;
    private Label _popupStatus = null!;
    private TabContainer _popupTabs = null!;
    private LineEdit _txtCategory = null!;
    private OptionButton _correctSelect = null!;
    private TextEdit _txtQuestion = null!;
    private LineEdit _txtAnswerA = null!;
    private LineEdit _txtAnswerB = null!;
    private LineEdit _txtAnswerC = null!;
    private LineEdit _txtAnswerD = null!;
    private TextEdit _txtExplanation = null!;
    private OptionButton _mediaType1 = null!;
    private OptionButton _mediaTiming1 = null!;
    private LineEdit _mediaPath1 = null!;
    private Label _mediaInfo1 = null!;
    private OptionButton _mediaType2 = null!;
    private OptionButton _mediaTiming2 = null!;
    private LineEdit _mediaPath2 = null!;
    private Label _mediaInfo2 = null!;
    private FileDialog _importDialog = null!;
    private FileDialog _saveDialog = null!;
    private FileDialog _mediaDialog = null!;

    private SaveDialogAction _pendingSaveAction;
    private MediaTargetSlot _pendingMediaSlot;
    private readonly QuestionDataRepository _questionRepository = new();
    private List<QuestionData> _allQuestions = new();
    private QuestionData? _editingQuestion;
    private QuestionData? _originalQuestionSnapshot;
    private string _selectedCategory = "Alle";

    public override void _Ready()
    {
        _categoryList = GetNode<ItemList>("RootMargin/RootVBox/MainContent/CategoryPanel/CategoryMargin/CategoryVBox/CategoryList");
        _txtSearch = GetNode<LineEdit>("RootMargin/RootVBox/MainContent/QuestionPanel/QuestionMargin/QuestionVBox/SearchRow/TxtSearch");
        _questionRows = GetNode<VBoxContainer>("RootMargin/RootVBox/MainContent/QuestionPanel/QuestionMargin/QuestionVBox/QuestionListScroll/QuestionRows");
        _questionCountLabel = GetNode<Label>("RootMargin/RootVBox/MainContent/QuestionPanel/QuestionMargin/QuestionVBox/ActionRow/LblQuestionCount");
        _statusLabel = GetNode<Label>("RootMargin/RootVBox/MainContent/QuestionPanel/QuestionMargin/QuestionVBox/StatusLabel");
        _editorOverlay = GetNode<Control>("EditorOverlay");
        _popupTitle = GetNode<Label>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupHeader/LblPopupTitle");
        _popupStatus = GetNode<Label>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupFooter/PopupStatus");
        _popupTabs = GetNode<TabContainer>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs");
        _txtCategory = GetNode<LineEdit>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Allgemein/ScrollAllgemein/AllgemeinMargin/AllgemeinVBox/MetaGrid/TxtCategory");
        _correctSelect = GetNode<OptionButton>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Allgemein/ScrollAllgemein/AllgemeinMargin/AllgemeinVBox/MetaGrid/CorrectSelect");
        _txtQuestion = GetNode<TextEdit>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Allgemein/ScrollAllgemein/AllgemeinMargin/AllgemeinVBox/TxtQuestion");
        _txtAnswerA = GetNode<LineEdit>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Allgemein/ScrollAllgemein/AllgemeinMargin/AllgemeinVBox/AnswersGrid/TxtAnswerA");
        _txtAnswerB = GetNode<LineEdit>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Allgemein/ScrollAllgemein/AllgemeinMargin/AllgemeinVBox/AnswersGrid/TxtAnswerB");
        _txtAnswerC = GetNode<LineEdit>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Allgemein/ScrollAllgemein/AllgemeinMargin/AllgemeinVBox/AnswersGrid/TxtAnswerC");
        _txtAnswerD = GetNode<LineEdit>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Allgemein/ScrollAllgemein/AllgemeinMargin/AllgemeinVBox/AnswersGrid/TxtAnswerD");
        _txtExplanation = GetNode<TextEdit>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Erklaerung/ScrollErklaerung/ErklaerungMargin/ErklaerungVBox/TxtExplanation");
        _mediaType1 = GetNode<OptionButton>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Medien/ScrollMedien/MedienMargin/MedienVBox/MediaSlot1/MediaSlot1Margin/MediaSlot1VBox/Media1Grid/MediaType1");
        _mediaTiming1 = GetNode<OptionButton>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Medien/ScrollMedien/MedienMargin/MedienVBox/MediaSlot1/MediaSlot1Margin/MediaSlot1VBox/Media1Grid/MediaTiming1");
        _mediaPath1 = GetNode<LineEdit>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Medien/ScrollMedien/MedienMargin/MedienVBox/MediaSlot1/MediaSlot1Margin/MediaSlot1VBox/MediaPath1");
        _mediaInfo1 = GetNode<Label>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Medien/ScrollMedien/MedienMargin/MedienVBox/MediaSlot1/MediaSlot1Margin/MediaSlot1VBox/MediaInfo1");
        _mediaType2 = GetNode<OptionButton>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Medien/ScrollMedien/MedienMargin/MedienVBox/MediaSlot2/MediaSlot2Margin/MediaSlot2VBox/Media2Grid/MediaType2");
        _mediaTiming2 = GetNode<OptionButton>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Medien/ScrollMedien/MedienMargin/MedienVBox/MediaSlot2/MediaSlot2Margin/MediaSlot2VBox/Media2Grid/MediaTiming2");
        _mediaPath2 = GetNode<LineEdit>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Medien/ScrollMedien/MedienMargin/MedienVBox/MediaSlot2/MediaSlot2Margin/MediaSlot2VBox/MediaPath2");
        _mediaInfo2 = GetNode<Label>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Medien/ScrollMedien/MedienMargin/MedienVBox/MediaSlot2/MediaSlot2Margin/MediaSlot2VBox/MediaInfo2");
        _importDialog = GetNode<FileDialog>("ImportDialog");
        _saveDialog = GetNode<FileDialog>("SaveDialog");
        _mediaDialog = GetNode<FileDialog>("MediaDialog");

        ConfigureSelectors();
        WireEvents();
        ReloadQuestions();
    }

    private void ConfigureSelectors()
    {
        _correctSelect.Clear();
        _correctSelect.AddItem("A", 0);
        _correctSelect.AddItem("B", 1);
        _correctSelect.AddItem("C", 2);
        _correctSelect.AddItem("D", 3);

        ConfigureMediaSelectors(_mediaType1, _mediaTiming1);
        ConfigureMediaSelectors(_mediaType2, _mediaTiming2);
    }

    private static void ConfigureMediaSelectors(OptionButton type, OptionButton timing)
    {
        type.Clear();
        type.AddItem("Kein Medium", 0);
        type.AddItem("Bild", 1);
        type.AddItem("Audio", 2);

        timing.Clear();
        timing.AddItem("Vor der Frage", 0);
        timing.AddItem("Waehrend der Frage", 1);
        timing.AddItem("Bei der Aufloesung", 2);
    }

    private void WireEvents()
    {
        GetNode<Button>("RootMargin/RootVBox/TopBar/TopBarMargin/TopBarHBox/BtnImport").Pressed += OnImportCsvPressed;
        GetNode<Button>("RootMargin/RootVBox/TopBar/TopBarMargin/TopBarHBox/BtnExport").Pressed += OnExportCsvPressed;
        GetNode<Button>("RootMargin/RootVBox/TopBar/TopBarMargin/TopBarHBox/BtnTemplate").Pressed += OnTemplateCsvPressed;
        GetNode<Button>("RootMargin/RootVBox/TopBar/TopBarMargin/TopBarHBox/BtnBack").Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        GetNode<Button>("RootMargin/RootVBox/MainContent/QuestionPanel/QuestionMargin/QuestionVBox/SearchRow/BtnClearSearch").Pressed += () =>
        {
            _txtSearch.Text = string.Empty;
            RebuildQuestionRows();
        };
        _txtSearch.TextChanged += _ => RebuildQuestionRows();
        GetNode<Button>("RootMargin/RootVBox/MainContent/QuestionPanel/QuestionMargin/QuestionVBox/ActionRow/BtnNewQuestion").Pressed += OpenNewQuestion;
        _categoryList.ItemSelected += OnCategorySelected;

        GetNode<Button>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupHeader/BtnClosePopup").Pressed += CloseEditor;
        GetNode<Button>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupFooter/BtnSave").Pressed += SaveCurrentQuestion;
        GetNode<Button>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupFooter/BtnDuplicate").Pressed += DuplicateCurrentQuestion;
        GetNode<Button>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupFooter/BtnDelete").Pressed += DeleteCurrentQuestion;
        GetNode<Button>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupFooter/BtnReset").Pressed += ResetCurrentQuestion;

        GetNode<Button>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Medien/ScrollMedien/MedienMargin/MedienVBox/MediaSlot1/MediaSlot1Margin/MediaSlot1VBox/MediaButtons1/BtnChooseMedia1").Pressed += () => OpenMediaDialog(MediaTargetSlot.Slot1);
        GetNode<Button>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Medien/ScrollMedien/MedienMargin/MedienVBox/MediaSlot1/MediaSlot1Margin/MediaSlot1VBox/MediaButtons1/BtnClearMedia1").Pressed += () => ClearMedia(MediaTargetSlot.Slot1);
        GetNode<Button>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Medien/ScrollMedien/MedienMargin/MedienVBox/MediaSlot2/MediaSlot2Margin/MediaSlot2VBox/MediaButtons2/BtnChooseMedia2").Pressed += () => OpenMediaDialog(MediaTargetSlot.Slot2);
        GetNode<Button>("EditorOverlay/PopupCenter/PopupPanel/PopupMargin/PopupVBox/PopupTabs/Medien/ScrollMedien/MedienMargin/MedienVBox/MediaSlot2/MediaSlot2Margin/MediaSlot2VBox/MediaButtons2/BtnClearMedia2").Pressed += () => ClearMedia(MediaTargetSlot.Slot2);

        _importDialog.FileSelected += OnImportFileSelected;
        _saveDialog.FileSelected += OnSaveFileSelected;
        _mediaDialog.FileSelected += OnMediaFileSelected;
    }

    private void ReloadQuestions()
    {
        _allQuestions = _questionRepository.LoadActiveQuestions();
        if (_allQuestions.Count == 0)
        {
            _selectedCategory = "Alle";
        }

        RebuildCategories();
        RebuildQuestionRows();
        _statusLabel.Text = _allQuestions.Count == 0
            ? "Aktuell sind keine Fragen gespeichert."
            : $"{_allQuestions.Count} Frage(n) geladen.";
    }

    private void RebuildCategories()
    {
        _categoryList.Clear();
        AddCategoryItem("Alle", _allQuestions.Count);

        foreach (var group in _allQuestions
                     .GroupBy(question => string.IsNullOrWhiteSpace(question.Category) ? "Ohne Kategorie" : question.Category)
                     .OrderBy(group => group.Key))
        {
            AddCategoryItem(group.Key, group.Count());
        }

        var selectIndex = 0;
        for (var i = 0; i < _categoryList.ItemCount; i++)
        {
            var metadata = _categoryList.GetItemMetadata(i);
            var category = metadata.VariantType == Variant.Type.Nil ? string.Empty : metadata.ToString();
            if (string.Equals(category, _selectedCategory, StringComparison.OrdinalIgnoreCase))
            {
                selectIndex = i;
                break;
            }
        }

        _categoryList.Select(selectIndex);
    }

    private void AddCategoryItem(string category, int count)
    {
        _categoryList.AddItem($"{category} ({count})");
        _categoryList.SetItemMetadata(_categoryList.ItemCount - 1, category);
    }

    private void OnCategorySelected(long index)
    {
        var metadata = _categoryList.GetItemMetadata((int)index);
        _selectedCategory = metadata.VariantType == Variant.Type.Nil ? "Alle" : metadata.ToString();
        RebuildQuestionRows();
    }

    private void RebuildQuestionRows()
    {
        foreach (var child in _questionRows.GetChildren())
        {
            child.QueueFree();
        }

        var filtered = GetFilteredQuestions();
        _questionCountLabel.Text = $"{filtered.Count} Fragen";

        if (filtered.Count == 0)
        {
            var empty = new Label
            {
                Text = "Keine Fragen fuer den aktuellen Filter gefunden.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            _questionRows.AddChild(empty);
            return;
        }

        foreach (var question in filtered)
        {
            _questionRows.AddChild(BuildQuestionRow(question));
        }
    }

    private List<QuestionData> GetFilteredQuestions()
    {
        IEnumerable<QuestionData> query = _allQuestions;

        if (!string.IsNullOrWhiteSpace(_selectedCategory) && !string.Equals(_selectedCategory, "Alle", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(question => string.Equals(question.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        var search = _txtSearch.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(question => ContainsIgnoreCase(question.Text, search)
                                            || ContainsIgnoreCase(question.Category, search)
                                            || ContainsIgnoreCase(question.Explanation, search));
        }

        return query.OrderBy(question => question.Category).ThenBy(question => question.Text).ToList();
    }

    private static bool ContainsIgnoreCase(string value, string search)
    {
        return value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
    }

    private Control BuildQuestionRow(QuestionData question)
    {
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var row = new HBoxContainer();
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddThemeConstantOverride("separation", 10);
        margin.AddChild(row);

        var textVBox = new VBoxContainer();
        textVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(textVBox);

        var title = new Label();
        title.Text = Truncate(question.Text, 90);
        title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        textVBox.AddChild(title);

        var meta = new Label();
        meta.Text = BuildQuestionMeta(question);
        meta.Modulate = new Color(0.77f, 0.82f, 0.95f, 0.9f);
        meta.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        textVBox.AddChild(meta);

        var editButton = new Button();
        editButton.Text = "Bearbeiten";
        editButton.CustomMinimumSize = new Vector2(130, 38);
        editButton.Pressed += () => OpenEditQuestion(question.Id);
        row.AddChild(editButton);

        return panel;
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength].TrimEnd() + "...";
    }

    private static string BuildQuestionMeta(QuestionData question)
    {
        var parts = new List<string> { question.Category };
        if (!string.IsNullOrWhiteSpace(question.Explanation))
        {
            parts.Add("Erklaerung");
        }

        if (question.Media.Any())
        {
            var imageCount = question.Media.Count(media => string.Equals(media.MediaType, "image", StringComparison.OrdinalIgnoreCase));
            var audioCount = question.Media.Count(media => string.Equals(media.MediaType, "audio", StringComparison.OrdinalIgnoreCase));
            if (imageCount > 0)
            {
                parts.Add($"Bild {imageCount}");
            }

            if (audioCount > 0)
            {
                parts.Add($"Audio {audioCount}");
            }
        }
        else
        {
            parts.Add("Keine Medien");
        }

        return string.Join(" • ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private void OpenNewQuestion()
    {
        _editingQuestion = new QuestionData
        {
            Id = GetNextQuestionId(),
            Category = _selectedCategory == "Alle" ? string.Empty : _selectedCategory,
            Answers = new List<string> { string.Empty, string.Empty, string.Empty, string.Empty },
            Media = new List<QuestionMediaData>()
        };
        _originalQuestionSnapshot = CloneQuestion(_editingQuestion);
        PopulateEditor(_editingQuestion);
        _popupTitle.Text = "Neue Frage";
        _popupStatus.Text = string.Empty;
        _popupTabs.CurrentTab = 0;
        _editorOverlay.Visible = true;
    }

    private void OpenEditQuestion(int questionId)
    {
        var question = _allQuestions.FirstOrDefault(item => item.Id == questionId);
        if (question == null)
        {
            _statusLabel.Text = "Frage konnte nicht gefunden werden.";
            return;
        }

        _editingQuestion = CloneQuestion(question);
        _originalQuestionSnapshot = CloneQuestion(question);
        PopulateEditor(_editingQuestion);
        _popupTitle.Text = "Frage bearbeiten";
        _popupStatus.Text = string.Empty;
        _popupTabs.CurrentTab = 0;
        _editorOverlay.Visible = true;
    }

    private void PopulateEditor(QuestionData question)
    {
        _txtCategory.Text = question.Category;
        _correctSelect.Select(Math.Clamp(question.CorrectIndex, 0, 3));
        _txtQuestion.Text = question.Text;
        _txtAnswerA.Text = question.Answers.ElementAtOrDefault(0) ?? string.Empty;
        _txtAnswerB.Text = question.Answers.ElementAtOrDefault(1) ?? string.Empty;
        _txtAnswerC.Text = question.Answers.ElementAtOrDefault(2) ?? string.Empty;
        _txtAnswerD.Text = question.Answers.ElementAtOrDefault(3) ?? string.Empty;
        _txtExplanation.Text = question.Explanation;
        PopulateMediaSlot(MediaTargetSlot.Slot1, question.Media.ElementAtOrDefault(0));
        PopulateMediaSlot(MediaTargetSlot.Slot2, question.Media.ElementAtOrDefault(1));
    }

    private void PopulateMediaSlot(MediaTargetSlot slot, QuestionMediaData? media)
    {
        var type = GetMediaTypeControl(slot);
        var timing = GetMediaTimingControl(slot);
        var path = GetMediaPathControl(slot);
        var info = GetMediaInfoControl(slot);

        if (media == null)
        {
            type.Select(0);
            timing.Select(0);
            path.Text = string.Empty;
            info.Text = "Noch kein Medium hinterlegt.";
            return;
        }

        type.Select(media.MediaType.Equals("audio", StringComparison.OrdinalIgnoreCase) ? 2 : media.MediaType.Equals("image", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        timing.Select(media.Timing switch
        {
            "during_question" => 1,
            "on_reveal" => 2,
            _ => 0
        });
        path.Text = media.StoredPath;
        info.Text = $"Gespeichert: {media.OriginalFileName}";
    }

    private void SaveCurrentQuestion()
    {
        if (_editingQuestion == null)
        {
            return;
        }

        var validationError = ValidateEditor();
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            _popupStatus.Text = validationError;
            return;
        }

        CollectEditorValues(_editingQuestion);

        var index = _allQuestions.FindIndex(question => question.Id == _editingQuestion.Id);
        if (index >= 0)
        {
            _allQuestions[index] = CloneQuestion(_editingQuestion);
        }
        else
        {
            _allQuestions.Add(CloneQuestion(_editingQuestion));
        }

        ReassignQuestionIds();
        if (!_questionRepository.ReplaceQuestions(_allQuestions, out var message))
        {
            _popupStatus.Text = message;
            return;
        }

        _popupStatus.Text = message;
        _statusLabel.Text = message;
            _editingQuestion = _allQuestions.FirstOrDefault(question => question.Id == _editingQuestion.Id);
            _originalQuestionSnapshot = _editingQuestion == null ? null : CloneQuestion(_editingQuestion);
            ReloadQuestions();
            CloseEditor();
    }

    private void DuplicateCurrentQuestion()
    {
        if (_editingQuestion == null)
        {
            return;
        }

        var validationError = ValidateEditor();
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            _popupStatus.Text = validationError;
            return;
        }

        CollectEditorValues(_editingQuestion);
        var duplicate = CloneQuestion(_editingQuestion);
        duplicate.Id = GetNextQuestionId();
        duplicate.Text += " (Kopie)";
        duplicate.Media = duplicate.Media.Select(media => CloneMedia(media)).ToList();
        _allQuestions.Add(duplicate);

        ReassignQuestionIds();
        if (!_questionRepository.ReplaceQuestions(_allQuestions, out var message))
        {
            _popupStatus.Text = message;
            return;
        }

        _statusLabel.Text = message;
        ReloadQuestions();
        OpenEditQuestion(duplicate.Id);
    }

    private void DeleteCurrentQuestion()
    {
        if (_editingQuestion == null)
        {
            return;
        }

        _allQuestions.RemoveAll(question => question.Id == _editingQuestion.Id);
        ReassignQuestionIds();
        if (!_questionRepository.ReplaceQuestions(_allQuestions, out var message))
        {
            _popupStatus.Text = message;
            return;
        }

        _statusLabel.Text = message;
        ReloadQuestions();
        CloseEditor();
    }

    private void ResetCurrentQuestion()
    {
        if (_originalQuestionSnapshot == null)
        {
            return;
        }

        _editingQuestion = CloneQuestion(_originalQuestionSnapshot);
        PopulateEditor(_editingQuestion);
        _popupStatus.Text = "Aenderungen verworfen.";
    }

    private void CloseEditor()
    {
        _editorOverlay.Visible = false;
        _editingQuestion = null;
        _originalQuestionSnapshot = null;
        _popupStatus.Text = string.Empty;
    }

    private string ValidateEditor()
    {
        if (string.IsNullOrWhiteSpace(_txtCategory.Text))
        {
            return "Bitte eine Kategorie eingeben.";
        }

        if (string.IsNullOrWhiteSpace(_txtQuestion.Text))
        {
            return "Bitte eine Frage eingeben.";
        }

        if (string.IsNullOrWhiteSpace(_txtAnswerA.Text) || string.IsNullOrWhiteSpace(_txtAnswerB.Text) || string.IsNullOrWhiteSpace(_txtAnswerC.Text) || string.IsNullOrWhiteSpace(_txtAnswerD.Text))
        {
            return "Bitte alle vier Antworten fuellen.";
        }

        if (!ValidateMediaSlot(MediaTargetSlot.Slot1, out var mediaError1))
        {
            return mediaError1;
        }

        if (!ValidateMediaSlot(MediaTargetSlot.Slot2, out var mediaError2))
        {
            return mediaError2;
        }

        return string.Empty;
    }

    private bool ValidateMediaSlot(MediaTargetSlot slot, out string message)
    {
        message = string.Empty;
        var type = GetMediaTypeControl(slot).Selected;
        var path = GetMediaPathControl(slot).Text?.Trim() ?? string.Empty;
        if (type == 0)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            message = slot == MediaTargetSlot.Slot1 ? "Bitte fuer Medium 1 eine Datei auswaehlen." : "Bitte fuer Medium 2 eine Datei auswaehlen.";
            return false;
        }

        return true;
    }

    private void CollectEditorValues(QuestionData question)
    {
        question.Category = _txtCategory.Text.Trim();
        question.Text = _txtQuestion.Text.Trim();
        question.Answers = new List<string>
        {
            _txtAnswerA.Text.Trim(),
            _txtAnswerB.Text.Trim(),
            _txtAnswerC.Text.Trim(),
            _txtAnswerD.Text.Trim()
        };
        question.CorrectIndex = _correctSelect.Selected;
        question.Explanation = _txtExplanation.Text.Trim();
        question.Media = BuildMediaList(question.Id);
    }

    private List<QuestionMediaData> BuildMediaList(int questionId)
    {
        var result = new List<QuestionMediaData>();
        AddMediaFromSlot(MediaTargetSlot.Slot1, questionId, result, 1);
        AddMediaFromSlot(MediaTargetSlot.Slot2, questionId, result, 2);
        return result;
    }

    private void AddMediaFromSlot(MediaTargetSlot slot, int questionId, List<QuestionMediaData> target, int mediaId)
    {
        var typeControl = GetMediaTypeControl(slot);
        var pathControl = GetMediaPathControl(slot);
        var storedPath = pathControl.Text?.Trim() ?? string.Empty;
        if (typeControl.Selected == 0 || string.IsNullOrWhiteSpace(storedPath))
        {
            return;
        }

        target.Add(new QuestionMediaData
        {
            Id = mediaId,
            QuestionId = questionId,
            MediaType = typeControl.Selected == 2 ? "audio" : "image",
            Timing = GetTimingValue(GetMediaTimingControl(slot).Selected),
            StoredPath = storedPath,
            OriginalFileName = Path.GetFileName(storedPath)
        });
    }

    private static string GetTimingValue(int selectedIndex)
    {
        return selectedIndex switch
        {
            1 => "during_question",
            2 => "on_reveal",
            _ => "before_question"
        };
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
        if (CsvImportExportService.TryImportQuestions(path, out var importedCount, out var message))
        {
            ReloadQuestions();
            _statusLabel.Text = $"{message} ({importedCount} importiert)";
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
                var category = string.Equals(_selectedCategory, "Alle", StringComparison.OrdinalIgnoreCase) ? null : _selectedCategory;
                result = CsvImportExportService.ExportQuestions(path, category, out message);
                break;
            case SaveDialogAction.ExportTemplate:
                result = CsvImportExportService.ExportTemplate(path, out message);
                break;
            default:
                return;
        }

        _pendingSaveAction = SaveDialogAction.None;
        _statusLabel.Text = message;
    }

    private void OpenMediaDialog(MediaTargetSlot slot)
    {
        _pendingMediaSlot = slot;
        _mediaDialog.PopupCenteredRatio(0.75f);
    }

    private void OnMediaFileSelected(string sourcePath)
    {
        if (_pendingMediaSlot == MediaTargetSlot.None)
        {
            return;
        }

        try
        {
            var questionId = _editingQuestion?.Id ?? GetNextQuestionId();
            var slotName = _pendingMediaSlot == MediaTargetSlot.Slot1 ? "media_1" : "media_2";
            var storedPath = MediaStorageService.ImportQuestionMedia(questionId, sourcePath, slotName);
            GetMediaPathControl(_pendingMediaSlot).Text = storedPath;
            GetMediaInfoControl(_pendingMediaSlot).Text = $"Intern gespeichert: {storedPath}";
            _popupStatus.Text = "Mediendatei wurde importiert.";
        }
        catch (Exception ex)
        {
            _popupStatus.Text = $"Mediendatei konnte nicht importiert werden: {ex.Message}";
        }
        finally
        {
            _pendingMediaSlot = MediaTargetSlot.None;
        }
    }

    private void ClearMedia(MediaTargetSlot slot)
    {
        var storedPath = GetMediaPathControl(slot).Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(storedPath))
        {
            MediaStorageService.DeleteStoredMedia(storedPath);
        }

        GetMediaTypeControl(slot).Select(0);
        GetMediaTimingControl(slot).Select(0);
        GetMediaPathControl(slot).Text = string.Empty;
        GetMediaInfoControl(slot).Text = "Noch kein Medium hinterlegt.";
    }

    private OptionButton GetMediaTypeControl(MediaTargetSlot slot)
    {
        return slot == MediaTargetSlot.Slot1 ? _mediaType1 : _mediaType2;
    }

    private OptionButton GetMediaTimingControl(MediaTargetSlot slot)
    {
        return slot == MediaTargetSlot.Slot1 ? _mediaTiming1 : _mediaTiming2;
    }

    private LineEdit GetMediaPathControl(MediaTargetSlot slot)
    {
        return slot == MediaTargetSlot.Slot1 ? _mediaPath1 : _mediaPath2;
    }

    private Label GetMediaInfoControl(MediaTargetSlot slot)
    {
        return slot == MediaTargetSlot.Slot1 ? _mediaInfo1 : _mediaInfo2;
    }

    private int GetNextQuestionId()
    {
        return _allQuestions.Count == 0 ? 1 : _allQuestions.Max(question => question.Id) + 1;
    }

    private void ReassignQuestionIds()
    {
        var ordered = _allQuestions.OrderBy(question => question.Category).ThenBy(question => question.Text).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].Id = i + 1;
            foreach (var media in ordered[i].Media)
            {
                media.QuestionId = ordered[i].Id;
            }
        }

        _allQuestions = ordered;
    }

    private static QuestionData CloneQuestion(QuestionData source)
    {
        return new QuestionData
        {
            Id = source.Id,
            Category = source.Category,
            Text = source.Text,
            Answers = new List<string>(source.Answers),
            CorrectIndex = source.CorrectIndex,
            Explanation = source.Explanation,
            Media = source.Media.Select(CloneMedia).ToList()
        };
    }

    private static QuestionMediaData CloneMedia(QuestionMediaData source)
    {
        return new QuestionMediaData
        {
            Id = source.Id,
            QuestionId = source.QuestionId,
            MediaType = source.MediaType,
            Timing = source.Timing,
            StoredPath = source.StoredPath,
            OriginalFileName = source.OriginalFileName
        };
    }
}
