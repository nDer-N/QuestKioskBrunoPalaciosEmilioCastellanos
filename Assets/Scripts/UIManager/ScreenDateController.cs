using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;


public class ScreenDateController : MonoBehaviour
{
    private const int DaysShown = 14;
    private static readonly int[] MinuteValues = { 0, 30 };

    [Header("Calendar")]
    [SerializeField] private Button dayButtonPrefab;          
    [SerializeField] private Transform calendarGrid;          
    [SerializeField] private DayOfWeek firstDayOfWeek = DayOfWeek.Monday;
    [SerializeField] private bool disablePastDays = true;
    [SerializeField] private TMP_Text DateText;

    [Header("Time pickers")]
    [SerializeField] private Button timeButtonPrefab;         
    [SerializeField] private Transform hoursContent;       
    [SerializeField] private Transform minutesContent;        
    [SerializeField] private int firstHour = 6;
    [SerializeField] private int lastHour = 19;
    [SerializeField] private bool lockPastMinutes = true;     

    [Header("Navigation")]
    [SerializeField] private Button nextButton;

    [Header("Selection look")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0f);

    [Header("Debug (leave off in builds)")]
    [SerializeField] private bool useDebugNow = false;
    [SerializeField] private string debugNow = "2026-10-10 08:15";

    private Button[] dayButtons;
    private DateTime[] dayDates;
    private Button[] hourButtons;
    private int[] hourValues;
    private Button[] minuteButtons;
    private bool built;

    

    private void OnEnable()
    {
        BuildIfNeeded();
        BookingStore.Changed += OnStoreChanged;

        // The kiosk may stay on all day, so recompute everything from the device clock each time the screen opens.
        RefreshCalendar();
        OnStoreChanged();
    }

    private void OnDisable()
    {
        BookingStore.Changed -= OnStoreChanged;
    }

    // ---------------------------------------------------------------- building

    private void BuildIfNeeded()
    {
        if (built) return;
        built = true;

        // Days
        dayButtons = new Button[DaysShown];
        dayDates = new DateTime[DaysShown];
        for (int i = 0; i < DaysShown; i++)
        {
            int index = i;
            Button b = Instantiate(dayButtonPrefab, calendarGrid);
            b.name = $"Day_{i}";
            b.onClick.AddListener(() => OnDayClicked(index));
            dayButtons[i] = b;
        }

        // Hours
        int hourCount = Mathf.Max(0, lastHour - firstHour + 1);
        hourButtons = new Button[hourCount];
        hourValues = new int[hourCount];
        for (int i = 0; i < hourCount; i++)
        {
            int index = i;
            hourValues[i] = firstHour + i;
            Button b = Instantiate(timeButtonPrefab, hoursContent);
            b.name = $"Hour_{hourValues[i]:00}";
            SetLabel(b, hourValues[i].ToString("00"));
            b.onClick.AddListener(() => OnHourClicked(index));
            hourButtons[i] = b;
        }

        // Minutes
        minuteButtons = new Button[MinuteValues.Length];
        for (int i = 0; i < MinuteValues.Length; i++)
        {
            int index = i;
            Button b = Instantiate(timeButtonPrefab, minutesContent);
            b.name = $"Minute_{MinuteValues[i]:00}";
            SetLabel(b, MinuteValues[i].ToString("00"));
            b.onClick.AddListener(() => OnMinuteClicked(index));
            minuteButtons[i] = b;
        }
    }

    // ---------------------------------------------------------------- clock

    private DateTime GetNow()
    {
        if (useDebugNow && DateTime.TryParse(debugNow, out DateTime parsed)) return parsed;
        return DateTime.Now; // device clock
    }

    // ---------------------------------------------------------------- calendar

    private void RefreshCalendar()
    {
        DateTime today = GetNow().Date;

        // Go back to the first day of the week that today belongs to
        int diff = ((int)today.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
        DateTime weekStart = today.AddDays(-diff);

        for (int i = 0; i < DaysShown; i++)
        {
            DateTime date = weekStart.AddDays(i);
            dayDates[i] = date;
            SetLabel(dayButtons[i], date.Day.ToString());
            dayButtons[i].interactable = !(disablePastDays && date < today);
        }
    }

    // ---------------------------------------------------------------- click handlers

    private void OnDayClicked(int index)
    {
        BookingStore.Data.SetDate(dayDates[index]);
        BookingStore.NotifyChanged();
    }

    private void OnHourClicked(int index)
    {
        BookingStore.Data.hour = hourValues[index];
        BookingStore.NotifyChanged();
    }

    private void OnMinuteClicked(int index)
    {
        BookingStore.Data.minute = MinuteValues[index];
        BookingStore.NotifyChanged();
    }

    // ---------------------------------------------------------------- refresh (runs on any data change)

    private void OnStoreChanged()
    {
        if (!built) return;

        DateTime now = GetNow();
        ValidateSelection(now);
        RefreshTimeLocks(now);
        RefreshVisuals();

        if (nextButton != null)
            nextButton.interactable = BookingStore.Data.HasDateAndTime;
    }

    private bool IsSelectedDateToday(DateTime now)
    {
        BookingData d = BookingStore.Data;
        return d.HasDate && d.Date == now.Date;
    }

    private bool IsHourLocked(int hour, DateTime now)
    {
        return IsSelectedDateToday(now) && hour < now.Hour;
    }

    private bool IsMinuteLocked(int minute, DateTime now)
    {
        BookingData d = BookingStore.Data;
        return lockPastMinutes && IsSelectedDateToday(now) && d.hour == now.Hour && minute < now.Minute;
    }

    /// <summary>Drops any stored selection that is no longer valid (old day, locked hour, locked minute).</summary>
    private void ValidateSelection(DateTime now)
    {
        BookingData d = BookingStore.Data;

        if (d.HasDate)
        {
            bool valid = false;
            for (int i = 0; i < DaysShown; i++)
            {
                if (dayDates[i] == d.Date && dayButtons[i].interactable) { valid = true; break; }
            }
            if (!valid) { d.ClearDate(); d.ClearTime(); }
        }

        if (d.hour >= 0 && IsHourLocked(d.hour, now)) d.hour = -1;
        if (d.minute >= 0 && IsMinuteLocked(d.minute, now)) d.minute = -1;
    }

    private void RefreshTimeLocks(DateTime now)
    {
        for (int i = 0; i < hourButtons.Length; i++)
            hourButtons[i].interactable = !IsHourLocked(hourValues[i], now);

        for (int i = 0; i < minuteButtons.Length; i++)
            minuteButtons[i].interactable = !IsMinuteLocked(MinuteValues[i], GetNow());
    }

    private void RefreshVisuals()
    {
        BookingData d = BookingStore.Data;

        for (int i = 0; i < DaysShown; i++)
            Tint(dayButtons[i], d.HasDate && dayDates[i] == d.Date);

        for (int i = 0; i < hourButtons.Length; i++)
            Tint(hourButtons[i], d.hour == hourValues[i]);

        for (int i = 0; i < minuteButtons.Length; i++)
            Tint(minuteButtons[i], d.minute == MinuteValues[i]);
        
        UpdateDateLabel();
    }

    private void UpdateDateLabel()
    {
        if (DateText == null) return;

        BookingData d = BookingStore.Data;
    if (!d.HasDate)
    {
        DateText.text = "";   
        return;
    }

    DateTime date = d.Date;
    string month = date.ToString("MMMM", CultureInfo.InvariantCulture);
    DateText.text = $"{month} {date.Day}{GetOrdinalSuffix(date.Day)}";
    }

    private static string GetOrdinalSuffix(int day)
{
    if (day >= 11 && day <= 13) return "th";   // 11th, 12th, 13th
    switch (day % 10)
    {
        case 1: return "st";
        case 2: return "nd";
        case 3: return "rd";
        default: return "th";
    }
}

    // ---------------------------------------------------------------- helpers

    private void Tint(Button b, bool selected)
    {
        if (b.image != null) b.image.color = selected ? selectedColor : normalColor;
    }

    private static void SetLabel(Button b, string text)
    {
        TMP_Text label = b.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = text;
    }
}
