using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class ActionManager : Singleton<ActionManager>
{
    public enum ActionType { Manipulation, Rotation, Zoom, Flip }

    private KeywordRecognizer speechRecognizer;
    private Dictionary<string, System.Action<PhraseRecognizedEventArgs>> voiceCommands;

    public event Action ResetEvent;
    public event Action FlipEvent;
    public ActionType CurrentAction { get; private set; }

    void Start()
    {
        InitializeVoiceCommands();
        SetupSpeechRecognition();
        SetDefaultAction();
    }

    private void InitializeVoiceCommands()
    {
        voiceCommands = new Dictionary<string, System.Action<PhraseRecognizedEventArgs>>
        {
            ["Move"] = ExecuteMoveCommand,
            ["Rotate"] = ExecuteRotateCommand,
            ["Zoom"] = ExecuteZoomCommand,
            ["Reset"] = ExecuteResetCommand,
            ["Flip"] = ExecuteFlipCommand
        };
    }

    private void SetupSpeechRecognition()
    {
        speechRecognizer = new KeywordRecognizer(voiceCommands.Keys.ToArray());
        speechRecognizer.OnPhraseRecognized += ProcessVoiceCommand;
        speechRecognizer.Start();
    }

    private void SetDefaultAction()
    {
        GestureManager.Instance.SetActiveRecognizer(GestureManager.Instance.ManipulationRecognizer);
        CurrentAction = ActionType.Manipulation;
    }

    void OnDestroy()
    {
        speechRecognizer?.Dispose();
    }

    private void ProcessVoiceCommand(PhraseRecognizedEventArgs args)
    {
        if (voiceCommands.TryGetValue(args.text, out var command))
            command.Invoke(args);
    }

    private void ExecuteMoveCommand(PhraseRecognizedEventArgs args)
    {
        GestureManager.Instance.SetActiveRecognizer(GestureManager.Instance.ManipulationRecognizer);
        CurrentAction = ActionType.Manipulation;
    }

    private void ExecuteRotateCommand(PhraseRecognizedEventArgs args)
    {
        GestureManager.Instance.SetActiveRecognizer(GestureManager.Instance.NavigationRecognizer);
        CurrentAction = ActionType.Rotation;
    }

    private void ExecuteZoomCommand(PhraseRecognizedEventArgs args)
    {
        GestureManager.Instance.SetActiveRecognizer(GestureManager.Instance.NavigationRecognizer);
        CurrentAction = ActionType.Zoom;
    }

    private void ExecuteResetCommand(PhraseRecognizedEventArgs args)
    {
        ResetEvent?.Invoke();
    }

    private void ExecuteFlipCommand(PhraseRecognizedEventArgs args)
    {
        FlipEvent?.Invoke();
        CurrentAction = ActionType.Flip;
    }
}
