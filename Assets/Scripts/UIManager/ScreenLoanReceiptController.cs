using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the ScreenLoanReceipt GameObject.
/// Shows the kit number, the loan date, and the start and end times (each in its own text).
/// The EXIT button wipes the in-progress booking with BookingStore.Reset() and goes back to ScreenMain,
/// so the next flow starts with nothing pre-selected. Confirmed reservations are not touched.
/// </summary>
public class ScreenLoanReceiptController : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text kitNumberText;      // KitNumber
    [SerializeField] private TMP_Text dateText;           // DateLoan
    [SerializeField] private TMP_Text startTimeText;      // PickupLoc/StartTime
    [SerializeField] private TMP_Text endTimeText;        // DropLoc/DropTime

    [Header("Exit")]
    [SerializeField] private Button exitButton;
    [SerializeField] private ScreenManager screenManager;
    [SerializeField] private ScreenType nextScreen = ScreenType.ScreenMain;

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnDestroy()
    {
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
    }

    private void OnEnable()
    {
        Refresh();
    }

    // ---------------------------------------------------------------- display

    private void Refresh()
    {
        BookingData d = BookingStore.Data;

        if (kitNumberText != null)
            kitNumberText.text = d.kitNumber >= 0 ? d.kitNumber.ToString() : "-";

        if (dateText != null)
            dateText.text = d.HasDate ? FormatDate(d.Date) : "-";

        // Start time + interval = end time (display only)
        DateTime? start = d.ToDateTime();
        bool hasPeriod = start.HasValue && d.timeInterval > 0;

        if (startTimeText != null)
            startTimeText.text = hasPeriod ? FormatTime(start.Value) : "-";

        if (endTimeText != null)
            endTimeText.text = hasPeriod ? FormatTime(start.Value.AddMinutes(d.timeInterval)) : "-";
    }

    private static string FormatTime(DateTime time)
    {
        return time.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>"October 12th"</summary>
    private static string FormatDate(DateTime date)
    {
        string month = date.ToString("MMMM", CultureInfo.InvariantCulture); // always English
        return $"{month} {date.Day}{GetOrdinalSuffix(date.Day)}";
    }

    private static string GetOrdinalSuffix(int day)
    {
        if (day >= 11 && day <= 13) return "th";
        switch (day % 10)
        {
            case 1: return "st";
            case 2: return "nd";
            case 3: return "rd";
            default: return "th";
        }
    }

    // ---------------------------------------------------------------- exit

    private void OnExitClicked()
    {
        BookingStore.Reset(); // new flow starts clean; saved reservations stay

        if (screenManager == null)
        {
            Debug.LogError("[ScreenLoanReceipt] ScreenManager reference is missing.");
            return;
        }
        screenManager.SwitchScreen(nextScreen);
    }
}
