using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Assets.Scripts
{
    public static class ResourcesManager
    {
        private const string scriptableObjectFileExtension = "asset";
        private const string pathToUnitConfigs = "Units/";

        public static T LoadSriptableObject<T>(string folderName, string fileName) where T : ScriptableObject
        {
            string filePath = Path.Combine(folderName, fileName);

            T scriptableObject = Resources.Load<T>(filePath);

            if (scriptableObject != null)
            {
                return scriptableObject;
            }
            else
            {
                Debug.LogErrorFormat("{0} not found: {1}", typeof(T).Name, filePath);
                return null;
            }
        }
    }
}