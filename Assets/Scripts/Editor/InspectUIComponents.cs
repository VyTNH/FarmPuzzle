using UnityEngine;
using UnityEditor;
using System.Text;

public class InspectUIComponents
{
    [InitializeOnLoadMethod]
    static void Inspect()
    {
        StringBuilder sb = new StringBuilder();
        var allGo = Resources.FindObjectsOfTypeAll<GameObject>();
        
        foreach (var go in allGo)
        {
            if (go.scene.IsValid() && (go.name == "Panel_Quests" || go.name == "Canvas_Inventory" || go.name == "QuestHUD" || go.name == "InventoryPanel"))
            {
                sb.AppendLine($"GameObject: {go.name} (Active: {go.activeInHierarchy})");
                foreach (var param in go.GetComponents<Component>())
                {
                    sb.AppendLine($"- Component: {param.GetType().Name}");
                }
            }
        }
        System.IO.File.WriteAllText("d:/UnityProjects/farmpuzzle/ui_inspect.log", sb.ToString());
    }
}
