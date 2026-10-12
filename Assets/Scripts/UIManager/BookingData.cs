using System;

/// <summary>
/// Holds everything the user picks across the kiosk screens.
/// -1 means "not set yet".
/// JsonUtility can't serialize DateTime, so the date/time are stored as ints.
/// </summary>
[Serializable]
public class BookingData
{
    // Selected day (the calendar button that was clicked)
    public int year = -1;
    public int month = -1;
    public int day = -1;

    // Selected time (24h clock, minute is 0 or 30)
    public int hour = -1;
    public int minute = -1;

    // Filled in by other screens
    public int kitNumber = -1;
    public int timeInterval = -1;

    public bool HasDate => year > 0 && month > 0 && day > 0;
    public bool HasTime => hour >= 0 && minute >= 0;
    public bool HasDateAndTime => HasDate && HasTime;

    /// <summary>Selected date (time part = 00:00). Only valid if HasDate.</summary>
    public DateTime Date => new DateTime(year, month, day);

    /// <summary>Full date + time, or null if either is missing.</summary>
    public DateTime? ToDateTime()
    {
        if (!HasDateAndTime) return null;
        return new DateTime(year, month, day, hour, minute, 0);
    }

    public void SetDate(DateTime date)
    {
        year = date.Year;
        month = date.Month;
        day = date.Day;
    }

    public void ClearDate() { year = month = day = -1; }
    public void ClearTime() { hour = minute = -1; }

    public void Clear()
    {
        ClearDate();
        ClearTime();
        kitNumber = -1;
        timeInterval = -1;
    }
}
