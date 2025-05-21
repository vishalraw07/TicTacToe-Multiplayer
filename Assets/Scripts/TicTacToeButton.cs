using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class TicTacToeButton : MonoBehaviour
{
    private Button uiButton;
    private int buttonIndex;
    [SerializeField] private AudioSource clickAudioSource; // For button click sound
    [SerializeField] private AudioClip clickSound; // Click sound for buttons

    public delegate void ButtonPressedHandler(int index);
    public static event ButtonPressedHandler OnButtonPressed;

    void Awake()
    {
        uiButton = GetComponent<Button>();
        if (uiButton == null)
        {
            Debug.LogError($"TicTacToeButton on {gameObject.name} is missing Button component.");
            return;
        }
        if (clickAudioSource == null)
        {
            Debug.LogError($"TicTacToeButton on {gameObject.name} is missing AudioSource component.");
            return;
        }
        if (clickSound == null)
        {
            Debug.LogError($"TicTacToeButton on {gameObject.name} is missing click sound clip.");
            return;
        }

        if (!int.TryParse(gameObject.name, out buttonIndex) || buttonIndex < 0 || buttonIndex > 8)
        {
            Debug.LogError($"TicTacToeButton on {gameObject.name} has invalid name. Must be an integer from 0 to 8.");
            return;
        }

        uiButton.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        if (!uiButton.interactable)
        {
            Debug.LogWarning($"Button {buttonIndex} clicked by {PhotonNetwork.NickName}, but not interactable.");
            return;
        }

        if (clickAudioSource != null && clickSound != null)
        {
            clickAudioSource.PlayOneShot(clickSound);
            Debug.Log($"Playing click sound for button {buttonIndex}.");
        }

        Debug.Log($"Button {buttonIndex} clicked by {PhotonNetwork.NickName}. Notifying controller.");
        OnButtonPressed?.Invoke(buttonIndex);
    }
}