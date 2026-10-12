using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the Overlay GameObject (it must stay active; only the Back button gets hidden).
/// - Back button: visible on every screen except ScreenMain. Goes back to the screen the user came from.
///   BookingStore.Reset() is called only when backing up onto ScreenMain.
/// - Clock: shows the same "now" as ScreenDateController (device clock, or its debug time).
/// </summary>
public class OverlayController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScreenManager screenManager;
    [SerializeField] private ScreenDateController dateController;   // time source (includes its debug override)

    [Header("Back button")]
    [SerializeField] private Button backButton;

    [Header("Clock")]
    [SerializeField] private TMP_Text clockText;                    // HourText

    private readonly Stack<ScreenType> history = new Stack<ScreenType>();
    private bool goingBack;
    private string lastClock;

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
    }

    private void OnEnable()
    {
        if (screenManager != null)
        {
            screenManager.ScreenChanged += OnScreenChanged;
            UpdateBackButton(screenManager.CurrentScreen);
        }

        lastClock = null;
        InvokeRepeating(nameof(UpdateClock), 0f, 1f);
    }

    private void OnDisable()
    {
        if (screenManager != null) screenManager.ScreenChanged -= OnScreenChanged;
        CancelInvoke(nameof(UpdateClock));
    }

    // ---------------------------------------------------------------- navigation

    private void OnScreenChanged(ScreenType from, ScreenType to)
    {
        // Normal forward navigation: remember where we came from
        if (!goingBack && from != to) history.Push(from);

        // Back on the main screen: there is nothing to go back to
        if (to == ScreenType.ScreenMain) history.Clear();

        UpdateBackButton(to);
    }

    private void OnBackClicked()
    {
        if (screenManager == null)
        {
            Debug.LogError("[Overlay] ScreenManager reference is missing.");
            return;
        }

        ScreenType target = history.Count > 0 ? history.Pop() : ScreenType.ScreenMain;

        goingBack = true;
        screenManager.SwitchScreen(target);
        goingBack = false;

        // Only wipe the in-progress booking when backing up onto the main screen
        if (target == ScreenType.ScreenMain)
            BookingStore.Reset();
    }

    private void UpdateBackButton(ScreenType current)
    {
        if (backButton != null)
            backButton.gameObject.SetActive(current != ScreenType.ScreenMain);
    }

    // ---------------------------------------------------------------- clock

    private void UpdateClock()
    {
        if (clockText == null || dateController == null) return;

        string time = dateController.GetNow().ToString("HH:mm", CultureInfo.InvariantCulture);
        if (time == lastClock) return;

        lastClock = time;
        clockText.text = time;
    }
}
