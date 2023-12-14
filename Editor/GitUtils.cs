using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoreUtilsFx.Editor
{
    public static class GitUtils
    {
        private const string GitKeepFilename = ".gitkeep";

        [MenuItem("Tools/Project Setup/GitKeep empty folders")]
        private static void GitKeepEmptyFolders()
        {
            int createdCount = 0;
            bool directoriesSelected = false;
            foreach (var obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                var assetPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(assetPath) || !Directory.Exists(assetPath))
                {
                    continue;
                }

                directoriesSelected = true;
                HandleFolder(assetPath);
            }

            if (!directoriesSelected)
            {
                Debug.LogWarning("GitKeep: No directories selected!");
                return;
            }

            var plural = createdCount != 1 ? "s" : "";
            Debug.Log($"GitKeep: {createdCount} {GitKeepFilename} file{plural} added.");
            return;
        
            void HandleFolder(string folderPath)
            {
                var directory = new DirectoryInfo(folderPath);
            
                var created = CreateGitKeepIfEmpty(directory);
                if (created)
                {
                    createdCount += 1;
                }

                var folders = directory.GetDirectories();
                foreach (var folder in folders)
                {
                    HandleFolder(folder.FullName);
                }
            }
        
            bool CreateGitKeepIfEmpty(DirectoryInfo directory)
            {
                bool empty = directory.GetDirectories().Length == 0 && directory.GetFiles().Length == 0;
                if (empty)
                {
                    var gitKeepPath = Path.Combine(directory.FullName, GitKeepFilename);
                    File.Create(gitKeepPath).Dispose();
                    return true;
                }

                return false;
            }
        }
    }
}
