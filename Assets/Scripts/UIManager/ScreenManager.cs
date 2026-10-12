using System;
using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    // 1. Serializable struct to map the ScreenType to a GameObject
    [Serializable]
    public struct ScreenEntry
    {
        public ScreenType type;
        public GameObject screenObject;
    }

    // Array to hold all your screens
    [SerializeField] private ScreenEntry[] screens;

    /// <summary>The screen that is currently shown.</summary>
    public ScreenType CurrentScreen { get; private set; } = ScreenType.ScreenMain;

    /// <summary>Raised after every successful switch: (previous screen, new screen).</summary>
    public event Action<ScreenType, ScreenType> ScreenChanged;

    private void Start()
    {
        // Optional: Initialize by showing the main screen at start
        SwitchScreen(ScreenType.ScreenMain);
    }

    // 2. Method that switches between screens
    public void SwitchScreen(ScreenType targetScreen)
    {
        bool targetFound = false;

        foreach (var screen in screens)
        {
            if (screen.screenObject == null) continue;

            // If this is the target screen, show it and mark that we found it
            if (screen.type == targetScreen)
            {
                screen.screenObject.SetActive(true);
                targetFound = true;
            }
            // Otherwise, hide it
            else
            {
                screen.screenObject.SetActive(false);
            }
        }

        if (targetFound)
        {
            ScreenType previous = CurrentScreen;
            CurrentScreen = targetScreen;
            ScreenChanged?.Invoke(previous, targetScreen);
        }
        else
        {
            Debug.LogWarning($"[ScreenManager] Target screen '{targetScreen}' was not found in the screens array.");
        }
    }

    // Wrapper method for Unity UI Button onClick() 
    // Unity's default Inspector requires an int parameter for dropdowns
    public void SwitchScreenByIndex(int index)
    {
        if (index >= 0 && index < screens.Length)
        {
            SwitchScreen(screens[index].type);
        }
        else
        {
            Debug.LogError($"[ScreenManager] Invalid screen index: {index}");
        }
    }
}
