using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace NightWatch.Editor
{
    [InitializeOnLoad]
    public static class TmpImporter
    {
        const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        static TmpImporter()
        {
            if (File.Exists(TmpSettingsPath)) return;

            TMP_PackageResourceImporter.ImportResources(true, false, false);
            Debug.Log("[Night Watch] TMP Essentials auto-imported.");
        }

        [MenuItem("Night Watch/Import TMP Essentials")]
        public static void Import()
        {
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();
            Debug.Log("[Night Watch] TMP Essentials imported.");
        }
    }
}
