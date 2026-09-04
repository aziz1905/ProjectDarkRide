using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Editor GUI untuk AmbientLampAnomaly:
/// Otomatis menyembunyikan settingan yang tidak relevan berdasarkan Mode yang dipilih di dropdown!
/// </summary>
[CustomEditor(typeof(AmbientLampAnomaly))]
[CanEditMultipleObjects]
public class AmbientLampAnomalyEditor : Editor
{
    private SerializedProperty anomalyModeProp;
    private SerializedProperty stayUntilRestoredProp;
    private SerializedProperty minFlickerSpeedProp;
    private SerializedProperty maxFlickerSpeedProp;
    private SerializedProperty flickerBeforeBlackoutProp;
    private SerializedProperty emissionColorProp;
    private SerializedProperty normalEmissionGlowProp;
    private SerializedProperty audioSourceProp;
    private SerializedProperty glitchSoundProp;
    private SerializedProperty restoreSoundProp;

    private void OnEnable()
    {
        anomalyModeProp = serializedObject.FindProperty("anomalyMode");
        stayUntilRestoredProp = serializedObject.FindProperty("stayUntilRestored");
        minFlickerSpeedProp = serializedObject.FindProperty("minFlickerSpeed");
        maxFlickerSpeedProp = serializedObject.FindProperty("maxFlickerSpeed");
        flickerBeforeBlackoutProp = serializedObject.FindProperty("flickerBeforeBlackout");
        emissionColorProp = serializedObject.FindProperty("emissionColor");
        normalEmissionGlowProp = serializedObject.FindProperty("normalEmissionGlow");
        audioSourceProp = serializedObject.FindProperty("audioSource");
        glitchSoundProp = serializedObject.FindProperty("glitchSound");
        restoreSoundProp = serializedObject.FindProperty("restoreSound");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. PILIHAN MODE DROPDOWN
        EditorGUILayout.LabelField("PILIHAN MODE ANOMALI", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(anomalyModeProp, new GUIContent("Anomaly Mode"));

        int currentMode = anomalyModeProp.enumValueIndex; // 0: AsynchronousFlicker, 1: BlackoutMatiTotal

        EditorGUILayout.Space(6);

        // 2. KONDISI 1: JIKA MODE ASYNCHRONOUS FLICKER
        if (currentMode == 0)
        {
            EditorGUILayout.LabelField("PENGATUR KEDIP (ASYNCHRONOUS FLICKER)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stayUntilRestoredProp, new GUIContent("Stay Flickering Until Restored"));
            EditorGUILayout.PropertyField(minFlickerSpeedProp, new GUIContent("Min Flicker Speed (Cepat)"));
            EditorGUILayout.PropertyField(maxFlickerSpeedProp, new GUIContent("Max Flicker Speed (Lambat)"));
        }
        // 3. KONDISI 2: JIKA MODE BLACKOUT MATI TOTAL
        else
        {
            EditorGUILayout.LabelField("PENGATURAN BLACKOUT (MATI TOTAL)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stayUntilRestoredProp, new GUIContent("Stay Off Until Restored"));
            EditorGUILayout.PropertyField(flickerBeforeBlackoutProp, new GUIContent("Flicker Before Blackout (3x)"));
        }

        EditorGUILayout.Space(8);

        // 4. VISUAL & EMISSION SETTINGS
        EditorGUILayout.LabelField("VISUAL & BOLA PIJAR GLOW", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(emissionColorProp, new GUIContent("Emission Color"));
        EditorGUILayout.PropertyField(normalEmissionGlowProp, new GUIContent("Normal Emission Glow"));

        EditorGUILayout.Space(8);

        // 5. AUDIO SFX
        EditorGUILayout.LabelField("AUDIO SFX (OPSIONAL)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(audioSourceProp, new GUIContent("Audio Source"));
        EditorGUILayout.PropertyField(glitchSoundProp, new GUIContent("Glitch Sound"));
        EditorGUILayout.PropertyField(restoreSoundProp, new GUIContent("Restore Sound"));

        serializedObject.ApplyModifiedProperties();
    }
}
