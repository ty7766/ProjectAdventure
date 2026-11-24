using System;
using System.Collections.Generic;
using System.Net.Mail;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Android 빌드용 인게임 고객 문의 모달 (POST /v1/support/tickets).
/// 프리팹 수정 없이 프로젝트 폰트와 GlobalUICanvas 스타일에 맞춰 런타임에 UI를 구성한다.
/// </summary>
public class UI_SupportModal : MonoBehaviour
{
    private static readonly (string Key, string Label)[] Categories =
    {
        ("account", "계정"),
        ("purchase", "결제"),
        ("bug", "오류"),
        ("sanction", "제재"),
        ("other", "기타"),
    };

    private static readonly Color32 CardBg = new Color32(0xFF, 0xF8, 0xEC, 0xFF);
    private static readonly Color32 InputBg = new Color32(0xF5, 0xEC, 0xDC, 0xFF);
    private static readonly Color32 TextDark = new Color32(0x5A, 0x3A, 0x22, 0xFF);
    private static readonly Color32 TextMuted = new Color32(0x8A, 0x6F, 0x56, 0xFF);
    private static readonly Color32 PrimaryFace = new Color32(0xF7, 0xA9, 0x4B, 0xFF);
    private static readonly Color32 SecondaryFace = new Color32(0xF1, 0xE2, 0xC4, 0xFF);
    private static readonly Color32 ErrorColor = new Color32(0xC6, 0x28, 0x28, 0xFF);

    private static UI_SupportModal s_instance;

    private TMP_FontAsset _font;
    private GameObject _formRoot;
    private GameObject _doneRoot;
    private readonly List<Button> _categoryButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> _categoryLabels = new List<TextMeshProUGUI>();
    private TMP_InputField _emailInput;
    private TMP_InputField _bodyInput;
    private TextMeshProUGUI _counterText;
    private TextMeshProUGUI _errorText;
    private Button _submitButton;
    private TextMeshProUGUI _submitLabel;
    private TextMeshProUGUI _doneRefText;
    private Button _doneThreadButton;

    private int _selectedCategory = 4; // other
    private bool _busy;
    private string _threadUrl;

    public static void Show(TMP_FontAsset fontAsset)
    {
        if (s_instance == null)
        {
            var go = new GameObject("UI_SupportModal");
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<UI_SupportModal>();
            s_instance.BuildUi(fontAsset != null ? fontAsset : TMP_Settings.defaultFontAsset);
        }
        s_instance.Open();
    }

    private void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
    }

    private void Open()
    {
        _selectedCategory = 4;
        _busy = false;
        _threadUrl = "";
        if (_emailInput != null) _emailInput.text = "";
        if (_bodyInput != null) _bodyInput.text = "";
        if (_errorText != null) _errorText.gameObject.SetActive(false);
        if (_formRoot != null) _formRoot.SetActive(true);
        if (_doneRoot != null) _doneRoot.SetActive(false);

        SelectCategory(_selectedCategory);
        Refresh();
        gameObject.SetActive(true);
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void SelectCategory(int index)
    {
        _selectedCategory = index;
        for (int i = 0; i < _categoryButtons.Count; i++)
        {
            bool active = i == index;
            if (_categoryButtons[i].targetGraphic != null)
            {
                _categoryButtons[i].targetGraphic.color = active ? PrimaryFace : SecondaryFace;
            }
            if (_categoryLabels[i] != null)
            {
                _categoryLabels[i].color = active ? Color.white : TextDark;
            }
        }
    }

    private void Refresh()
    {
        int len = _bodyInput != null ? _bodyInput.text.Length : 0;
        if (_counterText != null) _counterText.text = $"{len} / 2000";
        bool canSubmit = !_busy && _bodyInput != null && _bodyInput.text.Trim().Length >= 5;
        if (_submitButton != null) _submitButton.interactable = canSubmit;
        if (_submitLabel != null) _submitLabel.text = _busy ? "전송 중…" : "보내기";
    }

    private void OnSubmit()
    {
        if (_busy) return;
        string body = _bodyInput.text.Trim();
        string email = _emailInput.text.Trim();
        if (body.Length < 5)
        {
            ShowError("문의 내용을 5자 이상 입력해 주세요.");
            return;
        }
        if (email.Length > 0 && !IsValidEmail(email))
        {
            ShowError("이메일 주소 형식을 확인해 주세요.");
            return;
        }

        _busy = true;
        _errorText.gameObject.SetActive(false);
        Refresh();

        LandLinkerServices.SubmitSupportTicket(Categories[_selectedCategory].Key, body, email, (ok, ticketRef, threadUrl, errCode) =>
        {
            _busy = false;
            if (this == null) return;
            Refresh();
            if (ok)
            {
                _threadUrl = threadUrl;
                _doneRefText.text = string.IsNullOrEmpty(ticketRef)
                    ? "문의가 정상적으로 접수되었습니다."
                    : $"문의가 접수되었습니다.\n문의 번호: #{ticketRef}";
                if (_doneThreadButton != null)
                {
                    _doneThreadButton.gameObject.SetActive(!string.IsNullOrEmpty(_threadUrl));
                }
                _formRoot.SetActive(false);
                _doneRoot.SetActive(true);
            }
            else if (errCode == "rate_limited")
            {
                ShowError("잠시 후 다시 시도해 주세요.");
            }
            else if (errCode == "invalid_request")
            {
                ShowError("문의 내용을 5자 이상 2000자 이하로 입력해 주세요.");
            }
            else
            {
                ShowError("문의를 전송하지 못했습니다. 네트워크를 확인한 뒤 다시 시도해 주세요.");
            }
        });
    }

    private void ShowError(string message)
    {
        if (_errorText == null) return;
        _errorText.text = message;
        _errorText.gameObject.SetActive(true);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            return new MailAddress(email).Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private void BuildUi(TMP_FontAsset fontAsset)
    {
        _font = fontAsset != null ? fontAsset : FindAnyFont();

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Dim backdrop
        var backdrop = CreatePanel("Backdrop", transform, new Color(0f, 0f, 0f, 0.65f));
        StretchFull(backdrop.GetComponent<RectTransform>());
        var backdropBtn = backdrop.AddComponent<Button>();
        backdropBtn.transition = Selectable.Transition.None;
        backdropBtn.onClick.AddListener(Close);

        // Card container
        var card = CreatePanel("Card", transform, CardBg);
        var cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(820f, 700f);

        var cardLayout = card.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(36, 36, 30, 30);
        cardLayout.spacing = 14f;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = false;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        // Prevent backdrop click propagation on card
        card.AddComponent<Button>().transition = Selectable.Transition.None;

        var title = CreateText("Title", card.transform, "고객 문의", 32, TextDark, FontStyles.Bold, 44f);
        title.alignment = TextAlignmentOptions.Left;

        // Form container
        _formRoot = new GameObject("Form", typeof(RectTransform));
        _formRoot.transform.SetParent(card.transform, false);
        var formLayout = _formRoot.AddComponent<VerticalLayoutGroup>();
        formLayout.spacing = 12f;
        formLayout.childControlWidth = true;
        formLayout.childControlHeight = false;
        formLayout.childForceExpandWidth = true;
        formLayout.childForceExpandHeight = false;

        // Category row
        var catRow = new GameObject("Categories", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        catRow.transform.SetParent(_formRoot.transform, false);
        catRow.GetComponent<LayoutElement>().preferredHeight = 52f;
        var catHl = catRow.GetComponent<HorizontalLayoutGroup>();
        catHl.spacing = 10f;
        catHl.childControlWidth = true;
        catHl.childControlHeight = true;
        catHl.childForceExpandWidth = true;
        catHl.childForceExpandHeight = true;

        for (int i = 0; i < Categories.Length; i++)
        {
            int idx = i;
            var (btn, lbl) = CreateButton(catRow.transform, Categories[i].Label, SecondaryFace, TextDark, 52f, 22);
            btn.onClick.AddListener(() => SelectCategory(idx));
            _categoryButtons.Add(btn);
            _categoryLabels.Add(lbl);
        }

        // Email input
        _emailInput = CreateInputField(_formRoot.transform, "EmailInput", "답변받을 이메일 (선택)", 56f, false, 254);

        // Body input
        _bodyInput = CreateInputField(_formRoot.transform, "BodyInput", "문의 내용을 입력해 주세요 (5자 이상)", 220f, true, 2000);
        _bodyInput.onValueChanged.AddListener(_ => Refresh());

        // Meta row (device info + char counter)
        var metaRow = new GameObject("MetaRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        metaRow.transform.SetParent(_formRoot.transform, false);
        metaRow.GetComponent<LayoutElement>().preferredHeight = 28f;
        var metaHl = metaRow.GetComponent<HorizontalLayoutGroup>();
        metaHl.childControlWidth = true;
        metaHl.childControlHeight = true;
        metaHl.childForceExpandWidth = true;

        var deviceNotice = CreateText("DeviceNotice", metaRow.transform, "앱 버전과 기기 정보가 함께 전송됩니다.", 18, TextMuted, FontStyles.Normal, 28f);
        deviceNotice.alignment = TextAlignmentOptions.Left;

        _counterText = CreateText("Counter", metaRow.transform, "0 / 2000", 18, TextMuted, FontStyles.Normal, 28f);
        _counterText.alignment = TextAlignmentOptions.Right;

        // Error label
        _errorText = CreateText("Error", _formRoot.transform, "", 20, ErrorColor, FontStyles.Bold, 30f);
        _errorText.gameObject.SetActive(false);

        // Action buttons
        var actionsRow = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        actionsRow.transform.SetParent(_formRoot.transform, false);
        actionsRow.GetComponent<LayoutElement>().preferredHeight = 60f;
        var actHl = actionsRow.GetComponent<HorizontalLayoutGroup>();
        actHl.spacing = 16f;
        actHl.childControlWidth = true;
        actHl.childControlHeight = true;
        actHl.childForceExpandWidth = true;
        actHl.childForceExpandHeight = true;

        var (cancelBtn, _) = CreateButton(actionsRow.transform, "취소", SecondaryFace, TextDark, 60f, 24);
        cancelBtn.onClick.AddListener(Close);

        (_submitButton, _submitLabel) = CreateButton(actionsRow.transform, "보내기", PrimaryFace, Color.white, 60f, 24);
        _submitButton.onClick.AddListener(OnSubmit);

        // Done panel
        _doneRoot = new GameObject("Done", typeof(RectTransform));
        _doneRoot.transform.SetParent(card.transform, false);
        var doneLayout = _doneRoot.AddComponent<VerticalLayoutGroup>();
        doneLayout.spacing = 24f;
        doneLayout.padding = new RectOffset(0, 0, 40, 0);
        doneLayout.childControlWidth = true;
        doneLayout.childControlHeight = false;
        doneLayout.childForceExpandWidth = true;

        _doneRefText = CreateText("DoneMessage", _doneRoot.transform, "", 26, TextDark, FontStyles.Normal, 120f);
        _doneRefText.alignment = TextAlignmentOptions.Center;

        var doneActions = new GameObject("DoneActions", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        doneActions.transform.SetParent(_doneRoot.transform, false);
        doneActions.GetComponent<LayoutElement>().preferredHeight = 60f;
        var doneHl = doneActions.GetComponent<HorizontalLayoutGroup>();
        doneHl.spacing = 16f;
        doneHl.childControlWidth = true;
        doneHl.childControlHeight = true;
        doneHl.childForceExpandWidth = true;
        doneHl.childForceExpandHeight = true;

        (_doneThreadButton, _) = CreateButton(doneActions.transform, "문의 내역 열기", SecondaryFace, TextDark, 60f, 22);
        _doneThreadButton.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(_threadUrl)) Application.OpenURL(_threadUrl);
        });

        var (doneCloseBtn, _) = CreateButton(doneActions.transform, "닫기", PrimaryFace, Color.white, 60f, 24);
        doneCloseBtn.onClick.AddListener(Close);

        _doneRoot.SetActive(false);
    }

    private TMP_FontAsset FindAnyFont()
    {
        var anyTmp = FindFirstObjectByType<TextMeshProUGUI>();
        return anyTmp != null ? anyTmp.font : null;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string content, float size, Color color, FontStyles style, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = height;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private (Button Button, TextMeshProUGUI Label) CreateButton(Transform parent, string labelText, Color bgColor, Color textColor, float height, float fontSize)
    {
        var go = new GameObject("Button_" + labelText, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = height;
        go.GetComponent<Image>().color = bgColor;
        var btn = go.GetComponent<Button>();
        UIButtonFeedback.Ensure(btn);

        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        StretchFull(txtGo.GetComponent<RectTransform>());
        var tmp = txtGo.GetComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.text = labelText;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return (btn, tmp);
    }

    private TMP_InputField CreateInputField(Transform parent, string name, string placeholderText, float height, bool multiline, int charLimit)
    {
        var root = CreatePanel(name, parent, InputBg);
        root.AddComponent<LayoutElement>().preferredHeight = height;
        var input = root.AddComponent<TMP_InputField>();
        input.targetGraphic = root.GetComponent<Image>();
        input.characterLimit = charLimit;
        input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;

        var viewport = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(root.transform, false);
        var vpRect = viewport.GetComponent<RectTransform>();
        StretchFull(vpRect);
        vpRect.offsetMin = new Vector2(16, 12);
        vpRect.offsetMax = new Vector2(-16, -12);

        var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        phGo.transform.SetParent(viewport.transform, false);
        StretchFull(phGo.GetComponent<RectTransform>());
        var phTmp = phGo.GetComponent<TextMeshProUGUI>();
        if (_font != null) phTmp.font = _font;
        phTmp.text = placeholderText;
        phTmp.fontSize = 22f;
        phTmp.color = TextMuted;
        phTmp.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        phTmp.textWrappingMode = TextWrappingModes.Normal;
        phTmp.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(viewport.transform, false);
        StretchFull(textGo.GetComponent<RectTransform>());
        var textTmp = textGo.GetComponent<TextMeshProUGUI>();
        if (_font != null) textTmp.font = _font;
        textTmp.fontSize = 22f;
        textTmp.color = TextDark;
        textTmp.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        textTmp.textWrappingMode = TextWrappingModes.Normal;
        textTmp.raycastTarget = false;

        input.textViewport = vpRect;
        input.textComponent = textTmp;
        input.placeholder = phTmp;
        return input;
    }
}
