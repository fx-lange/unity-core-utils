using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CoreUtilsFx.Editor
{
    public static class SetupUtils
    {
        private const string Project = "_Project";
        private const string Tests = "_Tests";

        private static readonly List<string> ProjectFolders = new()
            { "01_Scripts", "02_Prefabs", "03_Resources", "04_Audio", "05_Video", "10_Main", "30_Bananas" };

        [MenuItem("Tools/Project Setup/Create Default Structure")]
        private static void CreateFolderStructure()
        {
            var assetsFolder = Application.dataPath;
            var projectFolder = Path.Combine(assetsFolder, Project);
            var testFolder = Path.Combine(assetsFolder, Tests);
            if (!Directory.Exists(projectFolder))
            {
                Directory.CreateDirectory(projectFolder);
            }

            if (!Directory.Exists(testFolder))
            {
                Directory.CreateDirectory(testFolder);
            }

            foreach (var folder in ProjectFolders)
            {
                var path = Path.Combine(projectFolder, folder);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}