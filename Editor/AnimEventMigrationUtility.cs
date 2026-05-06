using System.Collections.Generic;
using GlobalAnimEvents.Core;
using UnityEditor;
using UnityEngine;

namespace GlobalAnimEvents.Editor
{
    public static class AnimEventMigrationUtility
    {
        private const string LegacyVfx = "Anim_SpawnVfx";
        private const string LegacySfx = "Anim_PlaySfx";
        private const string NewEmit = "AnimEvent_Emit";

        [MenuItem("Tools/Global Anim Events/Migrate Selected Clips")]
        public static void MigrateSelectedClips()
        {
            Object[] selected = Selection.objects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Global Anim Events", "Select AnimationClip assets first.", "OK");
                return;
            }

            int migrated = 0;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] is AnimationClip clip)
                {
                    if (MigrateClip(clip))
                    {
                        migrated++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Global Anim Events", $"Migrated clips: {migrated}", "OK");
        }

        private static bool MigrateClip(AnimationClip clip)
        {
            string clipPath = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrWhiteSpace(clipPath) || !clipPath.EndsWith(".anim"))
            {
                return false;
            }

            AnimationEvent[] src = AnimationUtility.GetAnimationEvents(clip);
            List<AnimEventMarker> markers = new List<AnimEventMarker>();
            List<AnimationEvent> rewritten = new List<AnimationEvent>();
            int nextId = 1;

            for (int i = 0; i < src.Length; i++)
            {
                AnimationEvent evt = src[i];
                if (evt.functionName == LegacyVfx || evt.functionName == LegacySfx)
                {
                    AnimEventMarker marker = new AnimEventMarker
                    {
                        markerId = nextId++,
                        eventId = $"Event{nextId}",
                        type = evt.functionName == LegacyVfx ? AnimEventType.Vfx : AnimEventType.Sfx,
                        time = evt.time,
                        socketName = evt.stringParameter ?? string.Empty,
                        prefab = evt.objectReferenceParameter as GameObject,
                        audioClip = evt.objectReferenceParameter as AudioClip,
                        intValue = evt.intParameter,
                        floatValue = evt.floatParameter
                    };
                    markers.Add(marker);
                    rewritten.Add(new AnimationEvent
                    {
                        functionName = NewEmit,
                        time = evt.time,
                        intParameter = marker.markerId
                    });
                }
                else
                {
                    rewritten.Add(evt);
                }
            }

            if (markers.Count == 0)
            {
                return false;
            }

            string dir = System.IO.Path.GetDirectoryName(clipPath)?.Replace('\\', '/');
            string baseName = System.IO.Path.GetFileNameWithoutExtension(clipPath);
            string trackPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{baseName}_AnimEventTrack.asset");

            AnimEventTrackAsset track = ScriptableObject.CreateInstance<AnimEventTrackAsset>();
            track.animationClip = clip;
            track.events = markers;
            AssetDatabase.CreateAsset(track, trackPath);

            AnimationUtility.SetAnimationEvents(clip, rewritten.ToArray());
            EditorUtility.SetDirty(clip);
            EditorUtility.SetDirty(track);
            return true;
        }
    }
}
