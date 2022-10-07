using System.Reflection;
using eDriven.Audio;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioPlayer))]
[CanEditMultipleObjects]
public class AudioPlayerEditor : Editor
{
    //public bool SoundEnabled = true;
    public SerializedProperty SoundEnabled;

    //public float Volume = 1.0f;
    public SerializedProperty Volume;

    //public float Pitch = 1f;
    public SerializedProperty Pitch;

    //public float PitchRandomness;
    public SerializedProperty PitchRandomness;

    // ReSharper disable UnusedMember.Local
    [Obfuscation(Exclude = true)]
    void OnEnable()
    {
        // ReSharper restore UnusedMember.Local
        // Setup the SerializedProperties
        SoundEnabled = serializedObject.FindProperty("SoundEnabled");
        Volume = serializedObject.FindProperty("Volume");
        Pitch = serializedObject.FindProperty("Pitch");
        PitchRandomness = serializedObject.FindProperty("PitchRandomness");
    }

    [Obfuscation(Exclude = true)]
    public override void OnInspectorGUI()
    {
        // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
        serializedObject.Update();

        SoundEnabled.boolValue = EditorGUILayout.Toggle("SoundEnabled", SoundEnabled.boolValue);

        Volume.floatValue = EditorGUILayout.Slider("Volume", Volume.floatValue, 0, 1);
        ProgressBar(Volume.floatValue, "Volume");

        Pitch.floatValue = EditorGUILayout.Slider("Pitch", Pitch.floatValue, 0, 10);
        
        PitchRandomness.floatValue = EditorGUILayout.Slider("PitchRandomness", PitchRandomness.floatValue, 0, 1);
        
        // Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
        serializedObject.ApplyModifiedProperties();
    }

    // Custom GUILayout progress bar.
    static void ProgressBar (float value, string label) {
        // Get a rect for the progress bar using the same margins as a textfield:
        Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
        EditorGUI.ProgressBar (rect, value, label);
        EditorGUILayout.Space ();
    }
}