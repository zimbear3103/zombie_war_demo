using System;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] UIScreen[] m_uiScreens;
    [SerializeField] UIPopup[] m_uiPopups;

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    #region UIScreen
    private void HideAllUIScreen()
    {
        for (int i = 0; i < m_uiScreens.Length; i++)
        {
            if (m_uiScreens[i] != null)
                m_uiScreens[i].Hide();
        }
    }

    private T GetUIScreen<T>() where T : UIScreen
    {
        foreach (var screen in m_uiScreens)
        {
            if (screen is T)
            {
                return (T)screen;
            }
        }
        return null;
    }
    public UIScreen GetUIScreen(ScreenType screen)
    {
        for (int i = 0; i < m_uiScreens.Length; i++)
        {
            if (m_uiScreens[i] != null && m_uiScreens[i].Type == screen)
            {
                return m_uiScreens[i];
            }
        }

        return null;
    }
    public void ShowScreen(ScreenType screen)
    {
        HideAllUIScreen();

        var uiScreen = GetUIScreen(screen);
        uiScreen?.Show();
    }

    public ScreenType GetCurrentScreen()
    {
        ScreenType currentScreen = ScreenType.Home;
        for (int i = 0; i < m_uiScreens.Length; i++)
        {
            if (m_uiScreens[i] != null && m_uiScreens[i].Panel.activeSelf)
            {
                currentScreen = m_uiScreens[i].Type;
            }
        }
        return currentScreen;
    }
    #endregion

    #region UIPopup
    public void HideAllUIPopup()
    {
        for (int i = 0; i < m_uiPopups.Length; i++)
        {
            if (m_uiPopups[i] != null)
                m_uiPopups[i].Hide();
        }
    }

    private T GetUIPopup<T>() where T : UIPopup
    {
        foreach (var screen in m_uiPopups)
        {
            if (screen is T)
            {
                return (T)screen;
            }
        }
        return null;
    }
    private UIPopup GetUIPopup(PopupType screen)
    {
        for (int i = 0; i < m_uiPopups.Length; i++)
        {
            if (m_uiPopups[i] != null && m_uiPopups[i].Type == screen)
            {
                return m_uiPopups[i];
            }
        }

        return null;
    }
    public void ShowPopup(PopupType screen)
    {
        HideAllUIPopup();

        var uiScreen = GetUIPopup(screen);
        uiScreen?.Show();
    }
    #endregion

    public void ShowMessageBox(string title, string content, string textbtn, Action btnCallback, Action btnClose = null)
    {
        var uiMessage = GetUIPopup<UIMessage>();
        if (uiMessage != null)
        {
            uiMessage.OnShowMessageBox(title, content, textbtn, btnCallback, btnClose);
        }
        else
        {
            Debug.LogError("UIMessage popup not found!");
        }
    }

}
