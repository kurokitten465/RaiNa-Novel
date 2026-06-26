#if UNITY_EDITOR
using UnityEditor;
using VNFramework.Runtime.Core;

namespace VNFramework.Editor
{
    /// <summary>
    /// Automatically seeds default tracks when a new VNTimeline asset is created.
    /// This keeps the designer experience smooth — no manual track setup required.
    /// Editor-only; entirely stripped from runtime builds.
    /// </summary>
    internal class VNTimelineInitializer : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                var timeline = AssetDatabase.LoadAssetAtPath<VNTimeline>(path);
                if (timeline == null) continue;

                // Only init if the timeline has no tracks yet (freshly created).
                if (timeline.tracks.Count == 0)
                    timeline.InitDefaultTracks();
            }
        }
    }
}
#endif
