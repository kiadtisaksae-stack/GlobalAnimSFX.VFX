using System;
using System.Collections.Generic;
using GlobalAnimEvents.Core;
using GlobalAnimEvents.Player;
using UnityEditor;
using UnityEngine;

namespace GlobalAnimEvents.Editor
{
    public sealed class GlobalAnimEventEditorWindow : EditorWindow
    {
        private const string PreviewRootName = "__GlobalAnimEventsPreview";

        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private AnimEventTrackAsset trackAsset;
        [SerializeField] private int selectedMarkerIndex = -1;
        [SerializeField] private float scrubTime;
        [SerializeField] private bool autoPlayOnScrub;
        [SerializeField] private bool autoSaveEffect;
        [SerializeField] private int selectedClipEventIndex = -1;

        private GameObject _previewInstance;
        private GameObject _activeEffect;
        private Vector2 _scroll;
        private float _lastScrubTime;
        private Vector3 _lastSavedPos;
        private Quaternion _lastSavedRot;
        private Vector3 _lastSavedScale;

        [MenuItem("Tools/Global Anim Events/Editor")]
        public static void Open()
        {
            GlobalAnimEventEditorWindow win = GetWindow<GlobalAnimEventEditorWindow>("Global Anim Events");
            win.minSize = new Vector2(900f, 620f);
            win.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (_activeEffect == null || EditorApplication.isPlaying)
            {
                return;
            }

            if (autoSaveEffect && HasEffectTransformChanged())
            {
                SaveActiveToMarker();
            }
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll);

                previewPrefab = (GameObject)EditorGUILayout.ObjectField("Preview Prefab", previewPrefab, typeof(GameObject), false);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(previewPrefab == null))
                    {
                        if (GUILayout.Button("Create Preview On Scene")) CreatePreview();
                    }

                    if (GUILayout.Button("Clear Preview")) ClearPreview();
                }

                trackAsset = (AnimEventTrackAsset)EditorGUILayout.ObjectField("Track Asset", trackAsset, typeof(AnimEventTrackAsset), false);
                if (GUILayout.Button("Create New Track")) CreateTrackAsset();

                if (trackAsset != null)
                {
                    trackAsset.animationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", trackAsset.animationClip, typeof(AnimationClip), false);
                    DrawClipEventsPanel();

                    autoPlayOnScrub = GUILayout.Toggle(autoPlayOnScrub, "Active Scrub Event Play", "Button");
                    autoSaveEffect = GUILayout.Toggle(autoSaveEffect, "Auto Save Effect", "Button");

                    float clipLen = trackAsset.animationClip != null ? Mathf.Max(0.01f, trackAsset.animationClip.length) : 1f;
                    scrubTime = EditorGUILayout.Slider("Scrub Time", scrubTime, 0f, clipLen);
                    if (!Mathf.Approximately(_lastScrubTime, scrubTime))
                    {
                        ApplyScrub();
                        if (autoPlayOnScrub) RefreshAutoPlayState();
                        _lastScrubTime = scrubTime;
                    }

                    DrawMarkerToolbar();
                    DrawMarkerList();
                }
                else
                {
                    EditorGUILayout.HelpBox("Assign AnimEventTrackAsset first.", MessageType.Info);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawClipEventsPanel()
        {
            if (trackAsset == null || trackAsset.animationClip == null) return;
            AnimationEvent[] evts = AnimationUtility.GetAnimationEvents(trackAsset.animationClip);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"Clip Events ({evts.Length})", EditorStyles.boldLabel);
                DrawEventTimeline(evts);
                for (int i = 0; i < evts.Length; i++)
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        bool selected = selectedClipEventIndex == i;
                        if (GUILayout.Button(selected ? $"> {evts[i].functionName}" : evts[i].functionName, GUILayout.Width(180f)))
                        {
                            selectedClipEventIndex = i;
                            scrubTime = evts[i].time;
                            ApplyScrub();
                        }

                        GUILayout.Label($"t={evts[i].time:0.###}", GUILayout.Width(90f));
                        if (GUILayout.Button("Delete", GUILayout.Width(72f)))
                        {
                            List<AnimationEvent> list = new List<AnimationEvent>(evts);
                            list.RemoveAt(i);
                            AnimationUtility.SetAnimationEvents(trackAsset.animationClip, list.ToArray());
                            EditorUtility.SetDirty(trackAsset.animationClip);
                            AssetDatabase.SaveAssets();
                            break;
                        }
                    }
                }
            }
        }

        private void DrawEventTimeline(AnimationEvent[] evts)
        {
            Rect rect = GUILayoutUtility.GetRect(10f, 24f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.11f, 0.11f, 0.11f));
            float len = trackAsset != null && trackAsset.animationClip != null ? Mathf.Max(0.01f, trackAsset.animationClip.length) : 1f;
            for (int i = 0; i < evts.Length; i++)
            {
                float n = Mathf.Clamp01(evts[i].time / len);
                float x = Mathf.Lerp(rect.xMin + 2f, rect.xMax - 2f, n);
                EditorGUI.DrawRect(new Rect(x - 1f, rect.yMin + 1f, 2f, rect.height - 2f), new Color(0.3f, 0.75f, 1f));
            }
        }

        private void DrawMarkerToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Add Marker"))
                {
                    Undo.RecordObject(trackAsset, "Add Marker");
                    trackAsset.events.Add(new AnimEventMarker
                    {
                        markerId = GetNextMarkerId(),
                        eventId = $"Event{trackAsset.events.Count + 1}",
                        time = scrubTime,
                        normalizedTime = GetNormalized(scrubTime)
                    });
                    selectedMarkerIndex = trackAsset.events.Count - 1;
                    EditorUtility.SetDirty(trackAsset);
                }

                using (new EditorGUI.DisabledScope(selectedMarkerIndex < 0 || selectedMarkerIndex >= trackAsset.events.Count))
                {
                    if (GUILayout.Button("Duplicate"))
                    {
                        Undo.RecordObject(trackAsset, "Duplicate Marker");
                        AnimEventMarker c = Clone(trackAsset.events[selectedMarkerIndex]);
                        c.markerId = GetNextMarkerId();
                        trackAsset.events.Insert(selectedMarkerIndex + 1, c);
                        selectedMarkerIndex++;
                        EditorUtility.SetDirty(trackAsset);
                    }
                }
            }
        }

        private void DrawMarkerList()
        {
            for (int i = 0; i < trackAsset.events.Count; i++)
            {
                AnimEventMarker m = trackAsset.events[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button($"Event{i + 1}", GUILayout.Width(90f)))
                        {
                            selectedMarkerIndex = selectedMarkerIndex == i ? -1 : i;
                        }

                        if (GUILayout.Button("X", GUILayout.Width(24f)))
                        {
                            Undo.RecordObject(trackAsset, "Delete Marker");
                            trackAsset.events.RemoveAt(i);
                            selectedMarkerIndex = Mathf.Clamp(selectedMarkerIndex, -1, trackAsset.events.Count - 1);
                            EditorUtility.SetDirty(trackAsset);
                            return;
                        }

                        GUILayout.Label($"{m.type} id={m.markerId} @ {m.time:0.###}s", EditorStyles.miniLabel);
                    }

                    if (selectedMarkerIndex == i)
                    {
                        DrawMarkerInspector(m);
                    }
                }
            }
        }

        private void DrawMarkerInspector(AnimEventMarker m)
        {
            m.eventId = EditorGUILayout.TextField("Event Id", m.eventId);
            m.type = (AnimEventType)EditorGUILayout.EnumPopup("Type", m.type);
            m.time = EditorGUILayout.FloatField("Time", m.time);
            m.normalizedTime = GetNormalized(m.time);
            m.socketName = EditorGUILayout.TextField("Socket", m.socketName);
            m.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", m.prefab, typeof(GameObject), false);
            m.audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio", m.audioClip, typeof(AudioClip), false);
            m.localPositionOffset = EditorGUILayout.Vector3Field("Pos Offset", m.localPositionOffset);
            m.localEulerOffset = EditorGUILayout.Vector3Field("Rot Offset", m.localEulerOffset);
            m.localScale = EditorGUILayout.Vector3Field("Scale", m.localScale);
            m.floatValue = EditorGUILayout.FloatField("Float", m.floatValue);
            m.intValue = EditorGUILayout.IntField("Int", m.intValue);
            m.stringValue = EditorGUILayout.TextField("String", m.stringValue);
            m.vectorValue = EditorGUILayout.Vector3Field("Vector", m.vectorValue);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(m.prefab == null))
                {
                    if (GUILayout.Button("Play Effect")) PlayEffect(m);
                }

                if (GUILayout.Button("Stop Effect")) StopEffect();
            }

            using (new EditorGUI.DisabledScope(_activeEffect == null))
            {
                if (GUILayout.Button("Save Effect Position/Rotation")) SaveActiveToMarker();
            }

            using (new EditorGUI.DisabledScope(m.prefab == null))
            {
                if (GUILayout.Button("Save New Copy VFX Prefab"))
                {
                    SaveNewPrefabCopy(m);
                }
            }

            using (new EditorGUI.DisabledScope(trackAsset == null || trackAsset.animationClip == null))
            {
                if (GUILayout.Button("Append Event To Clip"))
                {
                    AppendMarkerToClip(m);
                }
            }
        }

        private void AppendMarkerToClip(AnimEventMarker marker)
        {
            if (trackAsset == null || trackAsset.animationClip == null) return;
            string path = AssetDatabase.GetAssetPath(trackAsset.animationClip);
            if (!string.IsNullOrWhiteSpace(path) && !path.EndsWith(".anim"))
            {
                Debug.LogWarning("[GlobalAnimEvents] Clip is likely read-only. Duplicate to .anim first.");
                return;
            }

            AnimationEvent evt = new AnimationEvent
            {
                functionName = "AnimEvent_Emit",
                time = Mathf.Clamp(marker.time, 0f, trackAsset.animationClip.length),
                intParameter = marker.markerId
            };

            List<AnimationEvent> events = new List<AnimationEvent>(AnimationUtility.GetAnimationEvents(trackAsset.animationClip));
            events.Add(evt);
            AnimationUtility.SetAnimationEvents(trackAsset.animationClip, events.ToArray());
            EditorUtility.SetDirty(trackAsset.animationClip);
            AssetDatabase.SaveAssets();
        }

        private void CreatePreview()
        {
            if (previewPrefab == null) return;
            ClearPreview();
            _previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(previewPrefab);
            if (_previewInstance == null) _previewInstance = Instantiate(previewPrefab);
            _previewInstance.name = PreviewRootName;
            _previewInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (_previewInstance.GetComponent<AnimEventPlayer>() == null)
            {
                _previewInstance.AddComponent<AnimEventPlayer>();
            }

            ApplyScrub();
            Selection.activeGameObject = _previewInstance;
        }

        private void ClearPreview()
        {
            StopEffect();
            GameObject root = GameObject.Find(PreviewRootName);
            if (root != null) DestroyImmediate(root);
            _previewInstance = null;
        }

        private void ApplyScrub()
        {
            if (_previewInstance == null) _previewInstance = GameObject.Find(PreviewRootName);
            if (_previewInstance == null || trackAsset == null || trackAsset.animationClip == null) return;
            trackAsset.animationClip.SampleAnimation(_previewInstance, Mathf.Clamp(scrubTime, 0f, trackAsset.animationClip.length));
            SceneView.RepaintAll();
        }

        private void RefreshAutoPlayState()
        {
            if (trackAsset == null || trackAsset.events == null || trackAsset.events.Count == 0)
            {
                StopEffect();
                return;
            }

            int idx = selectedMarkerIndex >= 0 && selectedMarkerIndex < trackAsset.events.Count ? selectedMarkerIndex : -1;
            if (idx < 0)
            {
                float latest = float.MinValue;
                for (int i = 0; i < trackAsset.events.Count; i++)
                {
                    if (trackAsset.events[i].prefab == null) continue;
                    if (trackAsset.events[i].time <= scrubTime && trackAsset.events[i].time > latest)
                    {
                        latest = trackAsset.events[i].time;
                        idx = i;
                    }
                }
            }

            if (idx < 0 || scrubTime < trackAsset.events[idx].time)
            {
                StopEffect();
                return;
            }

            if (_activeEffect == null)
            {
                PlayEffect(trackAsset.events[idx]);
            }
        }

        private void PlayEffect(AnimEventMarker marker)
        {
            if (_previewInstance == null) _previewInstance = GameObject.Find(PreviewRootName);
            if (_previewInstance == null || marker == null || marker.prefab == null) return;
            StopEffect();

            Transform anchor = ResolveAnchor(_previewInstance.transform, marker.socketName);
            _activeEffect = (GameObject)PrefabUtility.InstantiatePrefab(marker.prefab);
            if (_activeEffect == null) _activeEffect = Instantiate(marker.prefab);
            _activeEffect.transform.position = anchor.TransformPoint(marker.localPositionOffset);
            _activeEffect.transform.rotation = anchor.rotation * Quaternion.Euler(marker.localEulerOffset);
            _activeEffect.transform.localScale = marker.localScale == Vector3.zero ? Vector3.one : marker.localScale;

            CacheEffectTransform();
            Selection.activeGameObject = _activeEffect;
            SceneView.RepaintAll();
        }

        private void StopEffect()
        {
            if (_activeEffect != null)
            {
                DestroyImmediate(_activeEffect);
                _activeEffect = null;
            }
        }

        private void SaveActiveToMarker()
        {
            if (_activeEffect == null || _previewInstance == null || trackAsset == null) return;
            int idx = selectedMarkerIndex;
            if (idx < 0 || idx >= trackAsset.events.Count) return;
            AnimEventMarker marker = trackAsset.events[idx];
            Transform anchor = ResolveAnchor(_previewInstance.transform, marker.socketName);
            marker.localPositionOffset = anchor.InverseTransformPoint(_activeEffect.transform.position);
            marker.localEulerOffset = (Quaternion.Inverse(anchor.rotation) * _activeEffect.transform.rotation).eulerAngles;
            marker.localScale = _activeEffect.transform.localScale;
            EditorUtility.SetDirty(trackAsset);
            CacheEffectTransform();
        }

        private void SaveNewPrefabCopy(AnimEventMarker marker)
        {
            string source = AssetDatabase.GetAssetPath(marker.prefab);
            if (string.IsNullOrWhiteSpace(source)) return;
            string dir = System.IO.Path.GetDirectoryName(source)?.Replace('\\', '/');
            string baseName = System.IO.Path.GetFileNameWithoutExtension(source);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{baseName}_Copy.prefab");
            if (!AssetDatabase.CopyAsset(source, path)) return;
            AssetDatabase.ImportAsset(path);
            marker.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EditorUtility.SetDirty(trackAsset);
            AssetDatabase.SaveAssets();
        }

        private bool HasEffectTransformChanged()
        {
            Transform t = _activeEffect.transform;
            return t.position != _lastSavedPos || t.rotation != _lastSavedRot || t.localScale != _lastSavedScale;
        }

        private void CacheEffectTransform()
        {
            if (_activeEffect == null) return;
            Transform t = _activeEffect.transform;
            _lastSavedPos = t.position;
            _lastSavedRot = t.rotation;
            _lastSavedScale = t.localScale;
        }

        private int GetNextMarkerId()
        {
            int maxId = 0;
            for (int i = 0; i < trackAsset.events.Count; i++)
            {
                maxId = Mathf.Max(maxId, trackAsset.events[i].markerId);
            }

            return maxId + 1;
        }

        private float GetNormalized(float time)
        {
            if (trackAsset == null || trackAsset.animationClip == null || trackAsset.animationClip.length <= 0f) return 0f;
            return Mathf.Clamp01(time / trackAsset.animationClip.length);
        }

        private static AnimEventMarker Clone(AnimEventMarker s)
        {
            return new AnimEventMarker
            {
                eventId = $"{s.eventId}_Copy",
                type = s.type,
                time = s.time,
                normalizedTime = s.normalizedTime,
                socketName = s.socketName,
                prefab = s.prefab,
                audioClip = s.audioClip,
                localPositionOffset = s.localPositionOffset,
                localEulerOffset = s.localEulerOffset,
                localScale = s.localScale,
                floatValue = s.floatValue,
                intValue = s.intValue,
                stringValue = s.stringValue,
                vectorValue = s.vectorValue,
                payload = new AnimEventPayload
                {
                    floatValue = s.payload != null ? s.payload.floatValue : 0f,
                    intValue = s.payload != null ? s.payload.intValue : 0,
                    stringValue = s.payload != null ? s.payload.stringValue : string.Empty,
                    vectorValue = s.payload != null ? s.payload.vectorValue : Vector3.zero
                }
            };
        }

        private void CreateTrackAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Anim Event Track", "AnimEventTrack", "asset", "Choose save path");
            if (string.IsNullOrWhiteSpace(path)) return;
            AnimEventTrackAsset asset = CreateInstance<AnimEventTrackAsset>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            trackAsset = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static Transform ResolveAnchor(Transform root, string socketName)
        {
            if (root == null || string.IsNullOrWhiteSpace(socketName)) return root;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == socketName) return all[i];
            }

            return root;
        }
    }
}
