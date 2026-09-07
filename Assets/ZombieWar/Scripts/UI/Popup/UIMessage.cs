using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMessage : UIPopup
{
    [Header("MessageBoard")]
    [SerializeField] private GameObject m_messageBoardPopup;
    [SerializeField] private Button m_middleButton;
    [SerializeField] private TextMeshProUGUI m_textMiddleBtn;
    [SerializeField] private Button m_closeButton;
    [SerializeField] private TextMeshProUGUI m_titleText;
    [SerializeField] private TextMeshProUGUI m_contentText;

    private Action m_buttonCallback;
    private Action m_buttonCloseMessCallback;

    public void OnEnable()
    {
        m_middleButton.onClick.AddListener(OnMiddleButtonPressed);
        m_closeButton.onClick.AddListener(OnCloseButtonPressed);
   
    }
    public void OnDisable()
    {
        m_middleButton.onClick.RemoveListener(OnMiddleButtonPressed);
        m_closeButton.onClick.RemoveListener(OnCloseButtonPressed);
    }

    private void OnMiddleButtonPressed()
    {
        Hide();
        m_buttonCallback?.Invoke();
        SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_Other);
    }

    private void OnCloseButtonPressed()
    {
        Hide();
        SoundManager.Instance.OnPlaySfxAudio(ESoundId.UI_Click_ButtonNegative);
        m_buttonCloseMessCallback?.Invoke();
    }


    public void OnShowMessageBox(string title, string content, string textbtn, Action btnCallback, Action btnClose = null)
                                    
    {
        Panel.SetActive(true);
        m_messageBoardPopup.SetActive(true);

        m_buttonCallback = btnCallback;
        m_buttonCloseMessCallback = btnClose;

        m_titleText.text = title;
        m_contentText.text = content;
        m_textMiddleBtn.text = textbtn;

        Show();
    } 
}
