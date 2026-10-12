using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Single shared access point for the BookingData.
/// Not a MonoBehaviour, so it lives outside any screen and survives screen switches.
///
/// From any script:
///     BookingStore.Data.kitNumber = 3;
///     BookingStore.NotifyChanged();   // lets listening screens refresh
/// </summary>
public static class BookingStore
{
    public static BookingData Data { get; private set; } = new BookingData();

    /// <summary>Raised whenever someone calls NotifyChanged().</summary>
    public static event Action Changed;

    public static void NotifyChanged() => Changed?.Invoke();

    public static void Reset()
    {
        Data = new BookingData();
        NotifyChanged();
    }

    // ---- Optional: persist to disk as JSON ----

    private static string FilePath => Path.Combine(Application.persistentDataPath, "booking.json");

    public static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true));
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

    // Static fields survive between Play sessions if Domain Reload is disabled in the editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Data = new BookingData();
        Changed = null;
    }
}
