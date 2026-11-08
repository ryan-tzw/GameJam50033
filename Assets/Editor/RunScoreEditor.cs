using UnityEditor;
using UnityEngine;

namespace Jam50033
{
    [CustomEditor(typeof(RunScore))]
    public sealed class RunScoreEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            RunScore score = (RunScore)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Milestone Testing", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("During an active run, award a milestone bonus and play its feedback. Repeatable; actual distance and automatic milestones stay unchanged.", MessageType.Info);
            GameplaySettings.MilestoneData[] tiers = score.TestMilestones;
            if (tiers == null) return;
            using (new EditorGUI.DisabledScope(!score.CanTestMilestones))
            {
                for (int i = 0; i < tiers.Length; i++)
                {
                    GameplaySettings.MilestoneData tier = tiers[i];
                    if (tier == null || tier.intervalInTiles <= 0 || tier.bonus <= 0) continue;
                    if (GUILayout.Button($"Test {tier.intervalInTiles} Tiles (+{tier.bonus})")) score.TestMilestone(i);
                }
            }
        }
    }
}
