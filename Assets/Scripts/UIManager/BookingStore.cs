using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Single shared access point for booking data. Not a MonoBehaviour, so it lives outside any screen
/// and survives screen switches.
///
/// Two things live here:
///   1) Data          -> the booking currently being built by the user (booking.json if you call Save()).
///   2) Reservations  -> every CONFIRMED booking (reservations.json). Used to work out kit availability.
///
/// From any script:
///     BookingStore.Data.kitNumber = 3;
///     BookingStore.NotifyChanged();         // lets listening screens refresh
///     BookingStore.ConfirmCurrentBooking(); // when the loan is confirmed: adds it to Reservations and saves
/// </summary>
public static class BookingStore
{
    // ------------------------------------------------------------ current booking (in progress)

    public static BookingData Data { get; private set; } = new BookingData();

    /// <summary>Raised whenever someone calls NotifyChanged().</summary>
    public static event Action Changed;

    public static void NotifyChanged() => Changed?.Invoke();

    /// <summary>Starts a fresh in-progress booking. Confirmed reservations are NOT touched.</summary>
    public static void Reset()
    {
        Data = new BookingData();
        NotifyChanged();
    }

    public static string FilePath => Path.Combine(Application.persistentDataPath, "booking.json");

    public static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
            Debug.Log($"[BookingStore] Saved booking to {FilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[BookingStore] Could not save: {e.Message}");
        }
    }

    public static bool Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return false;

            var loaded = JsonUtility.FromJson<BookingData>(File.ReadAllText(FilePath));
            if (loaded == null) return false;

            Data = loaded;
            NotifyChanged();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[BookingStore] Could not load: {e.Message}");
            return false;
        }
    }

    // ------------------------------------------------------------ confirmed reservations

    // JsonUtility can't serialize a bare List at the root, so it gets wrapped.
    [Serializable]
    public class ReservationFile
    {
        public List<BookingData> reservations = new List<BookingData>();
    }

    private static List<BookingData> reservations; // null until first use, then loaded from disk

    public static string ReservationsPath => Path.Combine(Application.persistentDataPath, "reservations.json");

    public static IReadOnlyList<BookingData> Reservations
    {
        get
        {
            EnsureReservationsLoaded();
            return reservations;
        }
    }

    /// <summary>
    /// Call this when the user confirms the loan. Copies the current booking into the
    /// reservations list and writes reservations.json. Returns false if the booking is incomplete.
    /// </summary>
    public static bool ConfirmCurrentBooking()
    {
        if (!Data.HasDateAndTime || Data.timeInterval <= 0 || Data.kitNumber < 0)
        {
            Debug.LogWarning("[BookingStore] Can't confirm: date, time, time interval and kit number are all required.");
            return false;
        }

        AddReservation(Data);
        return true;
    }

    /// <summary>Adds a copy of the given booking to the reservations and saves.</summary>
    public static void AddReservation(BookingData booking)
    {
        EnsureReservationsLoaded();
        reservations.Add(Clone(booking));
        SaveReservations();
        NotifyChanged();
    }

    /// <summary>Wipes every confirmed reservation (useful while testing).</summary>
    public static void ClearReservations()
    {
        EnsureReservationsLoaded();
        reservations.Clear();
        SaveReservations();
        NotifyChanged();
    }

    private static void EnsureReservationsLoaded()
    {
        if (reservations != null) return;

        reservations = new List<BookingData>();
        try
        {
            if (!File.Exists(ReservationsPath)) return;

            var file = JsonUtility.FromJson<ReservationFile>(File.ReadAllText(ReservationsPath));
            if (file != null && file.reservations != null) reservations = file.reservations;
        }
        catch (Exception e)
        {
            Debug.LogError($"[BookingStore] Could not load reservations: {e.Message}");
        }
    }

    private static void SaveReservations()
    {
        try
        {
            var file = new ReservationFile { reservations = reservations };
            File.WriteAllText(ReservationsPath, JsonUtility.ToJson(file, true));
            Debug.Log($"[BookingStore] Saved reservations to {ReservationsPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[BookingStore] Could not save reservations: {e.Message}");
        }
    }

    private static BookingData Clone(BookingData source)
    {
        return JsonUtility.FromJson<BookingData>(JsonUtility.ToJson(source));
    }

    // Static fields survive between Play sessions if Domain Reload is disabled in the editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Data = new BookingData();
        Changed = null;
        reservations = null;
    }
}
