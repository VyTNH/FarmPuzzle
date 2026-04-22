using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

namespace FarmPuzzle.EditorTools
{
    [InitializeOnLoad]
    public class AddMobileResolutions
    {
        static AddMobileResolutions()
        {
            AddCustomSize(GameViewSizeGroupType.Standalone, 1080, 1920, "Iphone 8 Plus (9:16)");
            AddCustomSize(GameViewSizeGroupType.Standalone, 1125, 2436, "Iphone X/13 (10:19)");
            AddCustomSize(GameViewSizeGroupType.Standalone, 1284, 2778, "Iphone 13 Pro Max");
        }

        public static void AddCustomSize(GameViewSizeGroupType sizeGroupType, int width, int height, string text)
        {
            var gameViewSizesInstance = GetGroup(sizeGroupType);
            var addCustomSizeMethod = gameViewSizesInstance.GetType().GetMethod("AddCustomSize", BindingFlags.Public | BindingFlags.Instance);
            var gameViewSizeConstructor = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize").GetConstructor(new Type[] { typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeType"), typeof(int), typeof(int), typeof(string) });
            var newSize = gameViewSizeConstructor.Invoke(new object[] { 1, width, height, text });
            var addCustomSizeMethod2 = gameViewSizesInstance.GetType().GetMethod("AddCustomSize", BindingFlags.Public | BindingFlags.Instance);
            addCustomSizeMethod.Invoke(gameViewSizesInstance, new object[] { newSize });
        }

        static object GetGroup(GameViewSizeGroupType type)
        {
            var gameViewSizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
            var instanceProp = singleType.GetProperty("instance");
            var getGroupMethod = gameViewSizesType.GetMethod("GetGroup");
            return getGroupMethod.Invoke(instanceProp.GetValue(null, null), new object[] { (int)type });
        }
    }
}
