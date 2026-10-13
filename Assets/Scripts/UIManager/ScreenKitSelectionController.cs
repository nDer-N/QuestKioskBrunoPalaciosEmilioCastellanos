using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum KitStatus { Available, Reserved, Loaned }

/// <summary>Which pieces of the kit are present.</summary>
[Serializable]
public struct KitComponents
{
    public bool ChargerAvailable;
    public bool LControllerAvailable;
    public bool RControllerAvailable;
    public bool HeadsetAvailable;

    public static KitComponents AllAvailable => new KitComponents
    {
        ChargerAvailable = true,
        LControllerAvailable = true,
        RControllerAvailable = true,
        HeadsetAvailable = true
    };
}

[Serializable]
public class KitObject
{
    public int kitNumber;
    public KitComponents components;

    /// <summary>Computed by CheckAvailability(), not edited in the Inspector.</summary>
    [NonSerialized] public KitStatus status;

    public KitObject() : this(0) { }

    public KitObject(int number)
    {
        kitNumber = number;
        components = KitComponents.AllAvailable;
    }

    /// <summary>Text shown on the kit button.</summary>
    public string StatusLabel
    {
        get
        {
            switch (status)
            {
                case KitStatus.Reserved: return "RESERVED";
                case KitStatus.Loaned: return "LOANED";
                default: return "AVAILABLE";
            }
        }
    }

    public bool IsAvailable => status == KitStatus.Available;

    /// <summary>
    /// Checks this kit against every confirmed reservation for the slot the user asked for
    /// (request.date + request.time, lasting request.timeInterval minutes) and stores the result in 'status'.
    ///   AVAILABLE = no booking of this kit overlaps the requested slot.
    ///   LOANED    = an overlapping booking is active RIGHT NOW (start <= now < end): the kit is physically out.
    ///   RESERVED  = an overlapping booking exists, but it isn't active now (later today or another day).
    /// 'now' should be the same clock the rest of the app uses (ScreenDateController.GetNow()).
    /// </summary>
    public KitStatus CheckAvailability(BookingData request, IReadOnlyList<BookingData> reservations, DateTime now)
    {
        status = KitStatus.Available;

        DateTime? requestedStart = request.ToDateTime();
        if (!requestedStart.HasValue || request.timeInterval <= 0)
        {
            Debug.LogWarning("[KitObject] Date, time or time interval missing; can't check availability.");
            return status;
        }

        DateTime reqStart = requestedStart.Value;
        DateTime reqEnd = reqStart.AddMinutes(request.timeInterval);

        foreach (BookingData booking in reservations)
        {
            if (booking == null || booking.kitNumber != kitNumber) continue;

            DateTime? bookedStart = booking.ToDateTime();
            if (!bookedStart.HasValue || booking.timeInterval <= 0) continue;

            DateTime start = bookedStart.Value;
            DateTime end = start.AddMinutes(booking.timeInterval);

            // Back-to-back slots (one ends exactly when the other starts) don't overlap
            bool overlaps = start < reqEnd && reqStart < end;
            if (!overlaps) continue;

            // This booking is happening right now -> the kit is physically out
            if (start <= now && now < end)
            {
                status = KitStatus.Loaned;
                return status; // LOANED wins over RESERVED
            }

            status = KitStatus.Reserved;
        }

        return status;
    }
}

/// <summary>
/// Put this on the ScreenKitSelection GameObject.
/// Builds one KitOption button per kit (always 6), shows its number and status, and handles clicks:
///   available   -> stores the kit number in BookingStore and goes to the next screen
///   unavailable -> shows the not-available overlay on top of this screen (and logs "KIT UNAVAILABLE")
/// </summary>
public class ScreenKitSelectionController : MonoBehaviour
{
    private const int KitCount = 6;

    [Header("Kits (always 6, in display order)")]
    [SerializeField] private KitObject[] kits;

    [Header("UI")]
    [SerializeField] private Button kitOptionPrefab;          // the KitOption prefab asset
    [SerializeField] private Transform kitContainer;          // the "Area" object
    [SerializeField] private string kitNumberObjectName = "KitNumber";
    [SerializeField] private string statusObjectName = "STATUS";
    [SerializeField] private Color availableColor = new Color(0.1f, 0.8f, 0.1f);
    [SerializeField] private Color unavailableColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color reservedColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] public GameObject noKits;                // "NoKits" object, shown when no kit is available
    [SerializeField] private bool noKitsAvailable;            // true while all 6 kits are unavailable (visible for debugging)

    [Header("Navigation")]
    [SerializeField] private ScreenManager screenManager;
    [SerializeField] private ScreenType nextScreen = ScreenType.ScreenKitInfo;
    [SerializeField] private ScreenNotAvailableController notAvailableOverlay; // shown on top when an unavailable kit is clicked

    [Header("Clock")]
    [SerializeField] private ScreenDateController dateController; // same "now" as the date screen (includes its debug time)

    [Header("Debug")]
    [SerializeField] private int debugKitToReserve = 1;

    private class KitView
    {
        public TMP_Text numberText;
        public TMP_Text statusText;
    }

    private KitView[] views;
    private bool built;

    public KitObject[] Kits => kits;

    /// <summary>Lets other screens (e.g. KitInfo) read a kit's data by its number.</summary>
    public KitObject GetKit(int kitNumber)
    {
        foreach (KitObject kit in kits)
            if (kit != null && kit.kitNumber == kitNumber) return kit;
        return null;
    }

    // ---------------------------------------------------------------- lifecycle

    private void OnValidate()
    {
        EnsureKits(); // keeps the array at exactly 6 elements in the Inspector
    }

    private void OnEnable()
    {
        BuildIfNeeded();
        BookingStore.Changed += Refresh;
        Refresh(); // date/time/interval may have changed since last time this screen was open
    }

    private void OnDisable()
    {
        BookingStore.Changed -= Refresh;
    }

    private void EnsureKits()
    {
        bool ok = kits != null && kits.Length == KitCount;
        if (ok)
        {
            foreach (KitObject kit in kits)
            {
                if (kit == null) { ok = false; break; }
            }
        }
        if (ok) return;

        KitObject[] old = kits;
        kits = new KitObject[KitCount];
        for (int i = 0; i < KitCount; i++)
            kits[i] = (old != null && i < old.Length && old[i] != null) ? old[i] : new KitObject(i + 1);
    }

    // ---------------------------------------------------------------- building

    private void BuildIfNeeded()
    {
        if (built) return;
        built = true;

        if (dateController == null)
            Debug.LogWarning("[ScreenKitSelection] Date Controller isn't assigned; using the real device clock for LOANED checks.");

        EnsureKits();
        views = new KitView[KitCount];

        for (int i = 0; i < KitCount; i++)
        {
            int index = i;
            Button button = Instantiate(kitOptionPrefab, kitContainer);
            button.name = $"KitOption_{kits[i].kitNumber}";
            button.onClick.AddListener(() => OnKitClicked(index));

            TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
            views[i] = new KitView
            {
                numberText = FindText(texts, kitNumberObjectName),
                statusText = FindText(texts, statusObjectName)
            };
        }
    }

    private static TMP_Text FindText(TMP_Text[] texts, string objectName)
    {
        foreach (TMP_Text text in texts)
            if (text.gameObject.name == objectName) return text;

        Debug.LogWarning($"[ScreenKitSelection] No TMP text named '{objectName}' inside the KitOption prefab.");
        return null;
    }

    // ---------------------------------------------------------------- refresh

    private void Refresh()
    {
        if (!built) return;

        BookingData request = BookingStore.Data;
        IReadOnlyList<BookingData> reservations = BookingStore.Reservations;
        DateTime now = dateController != null ? dateController.GetNow() : DateTime.Now;

        int availableCount = 0;

        for (int i = 0; i < KitCount; i++)
        {
            KitObject kit = kits[i];
            kit.CheckAvailability(request, reservations, now);

            if (kit.IsAvailable) availableCount++;

            if (views[i].numberText != null)
                views[i].numberText.text = kit.kitNumber.ToString();

            if (views[i].statusText != null)
            {
                views[i].statusText.text = kit.StatusLabel;
                if(kit.status == KitStatus.Reserved)
                {
                    views[i].statusText.color = reservedColor;
                }
                else
                {
                    views[i].statusText.color = kit.IsAvailable ? availableColor : unavailableColor;
                }
                
            }
        }

        noKitsAvailable = availableCount == 0;

        if (noKits != null)
            noKits.SetActive(noKitsAvailable);
    }

    // ---------------------------------------------------------------- click

    private void OnKitClicked(int index)
    {
        KitObject kit = kits[index];

        if (!kit.IsAvailable)
        {
            Debug.Log("KIT UNAVAILABLE");

            if (notAvailableOverlay != null)
                notAvailableOverlay.Show(kit.StatusLabel);
            else
                Debug.LogWarning("[ScreenKitSelection] Not Available Overlay isn't assigned.");

            return;
        }

        BookingStore.Data.kitNumber = kit.kitNumber;
        BookingStore.NotifyChanged();

        if (screenManager == null)
        {
            Debug.LogError("[ScreenKitSelection] ScreenManager reference is missing.");
            return;
        }
        screenManager.SwitchScreen(nextScreen);
    }

    // ---------------------------------------------------------------- debug (component's ⋮ menu, in Play mode)

    [ContextMenu("Debug: Reserve a kit with the current date/time/interval")]
    private void DebugReserve()
    {
        BookingData copy = JsonUtility.FromJson<BookingData>(JsonUtility.ToJson(BookingStore.Data));
        copy.kitNumber = debugKitToReserve;
        BookingStore.AddReservation(copy);
    }

    [ContextMenu("Debug: Clear all reservations")]
    private void DebugClear()
    {
        BookingStore.ClearReservations();
    }
}
