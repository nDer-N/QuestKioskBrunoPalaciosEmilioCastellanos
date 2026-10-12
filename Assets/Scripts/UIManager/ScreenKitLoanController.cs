using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the ScreenKitLoan GameObject.
/// Shows the kit number, the loan date and the loan period (start - end) from BookingStore.Data.
/// The LOAN button confirms the booking: BookingStore.ConfirmCurrentBooking() writes it to reservations.json.
/// </summary>
public class ScreenKitLoanController : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text kitNumberText;      // KitNumber
    [SerializeField] private TMP_Text dateText;           // DateLoan
    [SerializeField] private TMP_Text timeText;           // TimeLoanInterval

    [Header("Navigation")]
    [SerializeField] private Button loanButton;
    [SerializeField] private ScreenManager screenManager;
    [SerializeField] private ScreenType nextScreen = ScreenType.ScreenLoanReceipt;

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        if (loanButton != null) loanButton.onClick.AddListener(OnLoanClicked);
    }

    private void OnDestroy()
    {
        if (loanButton != null) loanButton.onClick.RemoveListener(OnLoanClicked);
    }

    // Everything was chosen on the previous screens, so refreshing when this screen opens is enough.
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

        if (timeText != null)
            timeText.text = FormatPeriod(d);
    }

    /// <summary>"October 12th"</summary>
    private static string FormatDate(DateTime date)
    {
        string month = date.ToString("MMMM", CultureInfo.InvariantCulture); // always English
        return $"{month} {date.Day}{GetOrdinalSuffix(date.Day)}";
    }

    /// <summary>Start time + interval -> "17:00 - 18:30". Display only, nothing is stored.</summary>
    private static string FormatPeriod(BookingData d)
    {
        DateTime? startTime = d.ToDateTime();
        if (!startTime.HasValue || d.timeInterval <= 0) return "-";

        DateTime start = startTime.Value;
        DateTime end = start.AddMinutes(d.timeInterval);

        return start.ToString("HH:mm", CultureInfo.InvariantCulture) + " - " +
               end.ToString("HH:mm", CultureInfo.InvariantCulture);
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

    // ---------------------------------------------------------------- loan

    private void OnLoanClicked()
    {
        // If this exact booking is already saved (e.g. the user came back with Back), don't add a duplicate.
        if (!IsAlreadyReserved() && !BookingStore.ConfirmCurrentBooking())
            return; // booking incomplete; ConfirmCurrentBooking already logged why

        if (screenManager == null)
        {
            Debug.LogError("[ScreenKitLoan] ScreenManager reference is missing.");
            return;
        }
        screenManager.SwitchScreen(nextScreen);
    }

    private static bool IsAlreadyReserved()
    {
        BookingData d = BookingStore.Data;

        foreach (BookingData r in BookingStore.Reservations)
        {
            if (r != null &&
                r.kitNumber == d.kitNumber &&
                r.year == d.year && r.month == d.month && r.day == d.day &&
                r.hour == d.hour && r.minute == d.minute &&
                r.timeInterval == d.timeInterval)
                return true;
        }
        return false;
    }
}
