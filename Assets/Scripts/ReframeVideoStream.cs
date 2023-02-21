using UnityEngine;
using UnityEngine.UI;

public class ReframeVideoStream : MonoBehaviour
{

    public Transform headsetRotation;
    public InputField input;
    public Button sendButton;
    public float pollingTime = 1f;


    void Start()
    {
        InvokeRepeating("PollCoordinates", 0.0f, pollingTime);
    }

    void PollCoordinates()
    {
        input.text = (headsetRotation.rotation.eulerAngles.y).ToString();
        sendButton.onClick.Invoke();
    }
}
