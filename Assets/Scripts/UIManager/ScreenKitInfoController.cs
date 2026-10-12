using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the ScreenKitInfo GameObject.
/// Reads the kit chosen on ScreenKitSelection (BookingStore.Data.kitNumber), shows its number and status,
/// and colors each part's IsPartThere panel green (component present) or red (missing).
/// The Next button goes to ScreenKitLoan.
/// </summary>
public class ScreenKitInfoController : MonoBehaviour
{
    // Must match the names of the prefab instances under PartInfoList
    private static readonly string[] PartNames = { "Headset", "RController", "LController", "Charger" };

    [Header("Data source")]
    [SerializeField] private ScreenKitSelectionController kitSelection;

    [Header("Kit header")]
    [SerializeField] private TMP_Text kitNumberText;
    [SerializeField] private string kitNumberFormat = "KIT # {0}";
    [SerializeField] private TMP_Text kitStatusText;

    [Header("Parts")]
    [SerializeField] private Transform partInfoList;          // the PartInfoList object
    [SerializeField] private string indicatorName = "IsPartThere";
    [SerializeField] private Color presentColor = new Color(0f, 0.85f, 0f);
    [SerializeField] private Color missingColor = new Color(0.85f, 0.15f, 0.15f);

    [Header("Navigation")]
    [SerializeField] private Button nextButton;
    [SerializeField] private ScreenManager screenManager;
    [SerializeField] private ScreenType nextScreen = ScreenType.ScreenKitLoan;

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
    }

    private void OnDestroy()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextClicked);
    }

    // The kit number is stored before this screen opens, so refreshing on enable is enough.
    private void OnEnable()
    {
        Refresh();
    }

    // ---------------------------------------------------------------- display

    private void Refresh()
    {
        int kitNumber = BookingStore.Data.kitNumber;
        KitObject kit = kitSelection != null ? kitSelection.GetKit(kitNumber) : null;

        if (kit == null)
        {
            Debug.LogWarning($"[ScreenKitInfo] No kit found for kit number {kitNumber}.");
            return;
        }

        if (kitNumberText != null)
            kitNumberText.text = string.Format(kitNumberFormat, kit.kitNumber);

        if (kitStatusText != null)
        {
            kitStatusText.text = kit.StatusLabel;
            kitStatusText.color = kit.IsAvailable ? presentColor : missingColor;
        }

        foreach (string part in PartNames)
            SetPartColor(part, IsPartPresent(kit.components, part));
    }

    private static bool IsPartPresent(KitComponents components, string partName)
    {
        switch (partName)
        {
            case "Headset": return components.HeadsetAvailable;
            case "RController": return components.RControllerAvailable;
            case "LController": return components.LControllerAvailable;
            case "Charger": return components.ChargerAvailable;
            default: return false;
        }
    }

    private void SetPartColor(string partName, bool present)
    {
        Transform indicator = partInfoList != null ? partInfoList.Find($"{partName}/{indicatorName}") : null;
        if (indicator == null)
        {
            Debug.LogWarning($"[ScreenKitInfo] Couldn't find '{partName}/{indicatorName}' under PartInfoList.");
            return;
        }

        Image image = indicator.GetComponent<Image>();
        if (image != null) image.color = present ? presentColor : missingColor;
    }

    // ---------------------------------------------------------------- navigation

    private void OnNextClicked()
    {
        if (screenManager == null)
        {
            Debug.LogError("[ScreenKitInfo] ScreenManager reference is missing.");
            return;
        }
        screenManager.SwitchScreen(nextScreen);
    }
}
