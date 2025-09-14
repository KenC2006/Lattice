using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class StringEvent : UnityEvent<string> { }

public class KeyboardInput : MonoBehaviour 
{
    public StringEvent keyboardDone;
    public string titleText;
    private TouchScreenKeyboard activeKeyboard;

    void Start()
    {
#if WINDOWS_UWP
        InitializeKeyboard();
#else
        SimulateEditorInput();
#endif
    }

    void Update()
    {
        CheckKeyboardCompletion();
    }

    private void InitializeKeyboard()
    {
        activeKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false, titleText);
    }

    private void SimulateEditorInput()
    {
        keyboardDone?.Invoke("127.0.0.1");
    }

    private void CheckKeyboardCompletion()
    {
        if (!TouchScreenKeyboard.visible && activeKeyboard?.done == true)
        {
            keyboardDone?.Invoke(activeKeyboard.text);
            activeKeyboard = null;
        }
    }
}
