using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the ScreenTimeInterval GameObject.
/// Each interval button carries a Unity Tag ("30m", "60m", "90m", "120m").
/// Clicking a button reads its tag, converts it to minutes and stores it in BookingStore.Data.timeInterval.
/// Next stays disabled until one of the 4 intervals is selected.
/// </summary>
public class ScreenTimeIntervalController : MonoBehaviour
{
    private static readonly int[] ValidIntervals = { 30, 60, 90, 120 };

    [Header("Buttons (one per interval, each tagged 30m / 60m / 90m / 120m)")]
    [SerializeField] private Button[] intervalButtons;

    [Header("Navigation")]
    [SerializeField] private Button nextButton;

    [Header("Selection look")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0f);

    private bool listenersAdded;

    // ---------------------------------------------------------------- lifecycle

    private void OnEnable()
    {
        AddListenersOnce();
        BookingStore.Changed += Refresh;
        Refresh(); // restores the highlight if the user comes back with the Back button
    }

    private void OnDisable()
    {
        BookingStore.Changed -= Refresh;
    }

    private void AddListenersOnce()
    {
        if (listenersAdded) return;
        listenersAdded = true;

        foreach (Button button in intervalButtons)
        {
            if (button == null) continue;
            Button captured = button;
            captured.onClick.AddListener(() => OnIntervalClicked(captured));
        }
    }

    // ---------------------------------------------------------------- click

    private void OnIntervalClicked(Button button)
    {
        string tag = button.gameObject.tag;

        if (!TryParseTag(tag, out int minutes))
        {
            Debug.LogWarning($"[ScreenTimeInterval] Button '{button.name}' has tag '{tag}', expected 30m, 60m, 90m or 120m.");
            return;
        }

        BookingStore.Data.timeInterval = minutes;
        BookingStore.NotifyChanged();
    }

    // ---------------------------------------------------------------- refresh

    private void Refresh()
    {
        int selected = BookingStore.Data.timeInterval;

        foreach (Button button in intervalButtons)
        {
            if (button == null || button.image == null) continue;

            bool isSelected = TryParseTag(button.gameObject.tag, out int minutes) && minutes == selected;
            button.image.color = isSelected ? selectedColor : normalColor;
        }

        if (nextButton != null)
            nextButton.interactable = Array.IndexOf(ValidIntervals, selected) >= 0;
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>"90m" -> 90. Returns false if the tag isn't one of the 4 valid options.</summary>
    private static bool TryParseTag(string tag, out int minutes)
    {
        minutes = -1;
        if (string.IsNullOrEmpty(tag)) return false;

        string digits = tag.TrimEnd('m', 'M');
        if (!int.TryParse(digits, out minutes)) return false;

        return Array.IndexOf(ValidIntervals, minutes) >= 0;
    }
}
