using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on the ScreenNotAvailable GameObject.
/// It's an overlay: ScreenKitSelectionController calls Show() and it appears ON TOP of the kit selection
/// screen (no SwitchScreen, so the selection screen stays active underneath).
/// The X button just hides it again.
/// </summary>
public class ScreenNotAvailableController : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;   // STATUSTEXT
    [SerializeField] private Button closeButton;    // the X button

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
    }

    /// <summary>Shows the overlay with the kit's status ("LOANED" / "RESERVED").</summary>
    public void Show(string status)
    {
        if (statusText != null) statusText.text = status;

        transform.SetAsLastSibling(); // draw above its siblings (e.g. the Overlay with Back and the clock)
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
