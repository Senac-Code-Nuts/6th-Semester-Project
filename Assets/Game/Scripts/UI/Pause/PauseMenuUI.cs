using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PiGame.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        private class PromptRow
        {
            public TMP_Text Label;
            public Image PrimaryIcon;
            public Image SecondaryIcon;
        }

        [SerializeField] private Canvas _targetCanvas;
        [SerializeField] private TMP_FontAsset _font;

        private readonly List<PromptRow> _promptRows = new List<PromptRow>();

        private PauseMenuDefinition _definition;
        private GameObject _root;
        private RectTransform _promptContainer;
        private TMP_Text _titleText;
        private TMP_Text _deviceText;
        private TMP_Text _confirmationText;
        private GameObject _actionsRoot;
        private GameObject _confirmationRoot;
        private Button _resumeButton;
        private Button _exitButton;
        private Button _confirmExitButton;
        private Button _cancelExitButton;
        private Coroutine _selectionRoutine;

        public event Action ResumeRequested;
        public event Action ExitRequested;
        public event Action ExitConfirmed;
        public event Action ExitCanceled;

        public bool IsVisible => _root != null && _root.activeSelf;
        public bool IsConfirmationVisible =>
            _confirmationRoot != null && _confirmationRoot.activeSelf;

        private void Awake()
        {
            BuildView();
            SetVisible(false);
        }

        private void OnDisable()
        {
            StopSelectionRoutine();
        }

        public void Initialize(PauseMenuDefinition definition)
        {
            _definition = definition;
            if (_root == null)
            {
                return;
            }

            RefreshStaticTexts();
            SetInputDevice(PauseInputDevice.KeyboardMouse);
        }

        public void SetVisible(bool isVisible)
        {
            if (_root == null)
            {
                return;
            }

            _root.SetActive(isVisible);
            if (!isVisible)
            {
                StopSelectionRoutine();
                return;
            }

            HideConfirmation();
            QueueSelection(_resumeButton);
        }

        public void SetInputDevice(PauseInputDevice device)
        {
            if (_definition == null || _deviceText == null)
            {
                return;
            }

            _deviceText.text = _definition.GetDeviceTitle(device);
            IReadOnlyList<PauseControlPrompt> prompts = _definition.GetPrompts(device);
            EnsurePromptRowCount(prompts.Count);

            for (int i = 0; i < _promptRows.Count; i++)
            {
                bool isUsed = i < prompts.Count;
                PromptRow row = _promptRows[i];
                row.Label.transform.parent.gameObject.SetActive(isUsed);
                if (!isUsed)
                {
                    continue;
                }

                PauseControlPrompt prompt = prompts[i];
                row.Label.text = prompt.Label;
                ApplyIcon(row.PrimaryIcon, prompt.PrimaryIcon);
                ApplyIcon(row.SecondaryIcon, prompt.SecondaryIcon);
            }
        }

        public void ShowConfirmation()
        {
            if (_confirmationRoot == null)
            {
                return;
            }

            _confirmationRoot.SetActive(true);
            QueueSelection(_cancelExitButton);
        }

        public void HideConfirmation()
        {
            if (_confirmationRoot == null)
            {
                return;
            }

            _confirmationRoot.SetActive(false);
            if (IsVisible)
            {
                QueueSelection(_resumeButton);
            }
        }

        public void SetBusy(bool isBusy)
        {
            SetButtonInteractable(_resumeButton, !isBusy);
            SetButtonInteractable(_exitButton, !isBusy);
            SetButtonInteractable(_confirmExitButton, !isBusy);
            SetButtonInteractable(_cancelExitButton, !isBusy);
        }

        private void BuildView()
        {
            _targetCanvas ??= FindFirstObjectByType<Canvas>();
            if (_targetCanvas == null)
            {
                Debug.LogError("PauseMenuUI precisa de um Canvas na cena.", this);
                enabled = false;
                return;
            }

            _font ??= FindFirstObjectByType<TMP_Text>()?.font;

            _root = CreateUIObject("PauseMenuOverlay", _targetCanvas.transform);
            RectTransform rootRect = _root.GetComponent<RectTransform>();
            Stretch(rootRect);
            Image overlay = _root.AddComponent<Image>();
            overlay.color = new Color(0.015f, 0.012f, 0.055f, 0.92f);
            overlay.raycastTarget = true;
            _root.transform.SetAsLastSibling();

            GameObject panel = CreateUIObject("PausePanel", _root.transform);
            SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(980f, 920f));
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.055f, 0.04f, 0.11f, 0.98f);

            _titleText = CreateText("Title", panel.transform, 48f, TextAlignmentOptions.Center);
            SetRect(_titleText.rectTransform, new Vector2(0f, 390f), new Vector2(880f, 72f));

            _deviceText = CreateText(
                "InputDevice",
                panel.transform,
                22f,
                TextAlignmentOptions.Center);
            _deviceText.color = new Color(0.55f, 0.82f, 1f, 1f);
            SetRect(_deviceText.rectTransform, new Vector2(0f, 330f), new Vector2(760f, 44f));

            GameObject promptContainerObject = CreateUIObject("ControlPrompts", panel.transform);
            _promptContainer = promptContainerObject.GetComponent<RectTransform>();
            SetRect(_promptContainer, new Vector2(0f, 25f), new Vector2(820f, 540f));

            _actionsRoot = CreateUIObject("Actions", panel.transform);
            SetRect(_actionsRoot.GetComponent<RectTransform>(), new Vector2(0f, -365f), new Vector2(820f, 110f));

            _resumeButton = CreateButton("ResumeButton", _actionsRoot.transform, new Vector2(-205f, 0f));
            _exitButton = CreateButton("ExitButton", _actionsRoot.transform, new Vector2(205f, 0f));
            _resumeButton.onClick.AddListener(() => ResumeRequested?.Invoke());
            _exitButton.onClick.AddListener(() => ExitRequested?.Invoke());

            _confirmationRoot = CreateConfirmation(panel.transform);
        }

        private GameObject CreateConfirmation(Transform parent)
        {
            GameObject confirmation = CreateUIObject("ExitConfirmation", parent);
            SetRect(confirmation.GetComponent<RectTransform>(), Vector2.zero, new Vector2(760f, 300f));
            Image background = confirmation.AddComponent<Image>();
            background.color = new Color(0.11f, 0.055f, 0.14f, 1f);

            _confirmationText = CreateText(
                "Message",
                confirmation.transform,
                26f,
                TextAlignmentOptions.Center);
            SetRect(_confirmationText.rectTransform, new Vector2(0f, 72f), new Vector2(680f, 90f));

            _confirmExitButton = CreateButton(
                "ConfirmExitButton",
                confirmation.transform,
                new Vector2(-170f, -72f));
            _cancelExitButton = CreateButton(
                "CancelExitButton",
                confirmation.transform,
                new Vector2(170f, -72f));
            SetButtonLabel(_confirmExitButton, "SIM");
            SetButtonLabel(_cancelExitButton, "NÃO");
            _confirmExitButton.onClick.AddListener(() => ExitConfirmed?.Invoke());
            _cancelExitButton.onClick.AddListener(() => ExitCanceled?.Invoke());
            return confirmation;
        }

        private void RefreshStaticTexts()
        {
            if (_definition == null)
            {
                return;
            }

            _titleText.text = _definition.Title;
            _confirmationText.text = _definition.ExitConfirmationMessage;
            SetButtonLabel(_resumeButton, _definition.ResumeLabel);
            SetButtonLabel(_exitButton, _definition.ExitLabel);
        }

        private void EnsurePromptRowCount(int count)
        {
            while (_promptRows.Count < count)
            {
                int index = _promptRows.Count;
                GameObject rowObject = CreateUIObject($"Prompt{index + 1}", _promptContainer);
                float y = 210f - (index * 82f);
                SetRect(rowObject.GetComponent<RectTransform>(), new Vector2(0f, y), new Vector2(760f, 70f));

                TMP_Text label = CreateText(
                    "Label",
                    rowObject.transform,
                    22f,
                    TextAlignmentOptions.Left);
                SetRect(label.rectTransform, new Vector2(-165f, 0f), new Vector2(400f, 58f));

                Image primaryIcon = CreateIcon("PrimaryIcon", rowObject.transform, new Vector2(190f, 0f));
                Image secondaryIcon = CreateIcon("SecondaryIcon", rowObject.transform, new Vector2(270f, 0f));
                _promptRows.Add(new PromptRow
                {
                    Label = label,
                    PrimaryIcon = primaryIcon,
                    SecondaryIcon = secondaryIcon
                });
            }
        }

        private Button CreateButton(string objectName, Transform parent, Vector2 position)
        {
            GameObject buttonObject = CreateUIObject(objectName, parent);
            SetRect(buttonObject.GetComponent<RectTransform>(), position, new Vector2(330f, 82f));
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.25f, 0.1f, 0.42f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.selectedColor = new Color(1.35f, 1.35f, 1.35f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TMP_Text label = CreateText(
                "Label",
                buttonObject.transform,
                20f,
                TextAlignmentOptions.Center);
            Stretch(label.rectTransform);

            EventTrigger trigger = buttonObject.AddComponent<EventTrigger>();
            EventTrigger.Entry pointerEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            pointerEntry.callback.AddListener(_ =>
            {
                if (EventSystem.current != null && button.interactable)
                {
                    EventSystem.current.SetSelectedGameObject(button.gameObject);
                }
            });
            trigger.triggers.Add(pointerEntry);
            return button;
        }

        private Image CreateIcon(string objectName, Transform parent, Vector2 position)
        {
            GameObject iconObject = CreateUIObject(objectName, parent);
            SetRect(iconObject.GetComponent<RectTransform>(), position, new Vector2(64f, 64f));
            Image icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return icon;
        }

        private TMP_Text CreateText(
            string objectName,
            Transform parent,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUIObject(objectName, parent);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private void QueueSelection(Button button)
        {
            StopSelectionRoutine();
            if (button != null && gameObject.activeInHierarchy)
            {
                _selectionRoutine = StartCoroutine(SelectNextFrame(button));
            }
        }

        private IEnumerator SelectNextFrame(Button button)
        {
            yield return null;
            _selectionRoutine = null;
            if (EventSystem.current != null && button != null && button.interactable)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }

        private void StopSelectionRoutine()
        {
            if (_selectionRoutine == null)
            {
                return;
            }

            StopCoroutine(_selectionRoutine);
            _selectionRoutine = null;
        }

        private static GameObject CreateUIObject(string objectName, Transform parent)
        {
            GameObject uiObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
            uiObject.transform.SetParent(parent, false);
            return uiObject;
        }

        private static void ApplyIcon(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>() : null;
            if (text != null)
            {
                text.text = label;
            }
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private static void SetRect(RectTransform rectTransform, Vector2 position, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }
    }
}
