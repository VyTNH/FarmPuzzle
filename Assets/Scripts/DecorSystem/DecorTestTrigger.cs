using UnityEngine;

namespace FarmPuzzle.DecorSystem
{
    /// <summary>
    /// Component dùng để kích hoạt test tính năng xây nhà từ UI một cách an toàn mà không bị lỗi Serialize của Unity.
    /// </summary>
    public class DecorTestTrigger : MonoBehaviour
    {
        public string decorID = "decor_fence";
        public GameObject decorPrefab;

        public void TriggerPlacement()
        {
            if (PlacementController.Instance != null && decorPrefab != null)
            {
                PlacementController.Instance.StartPlacement(decorID, decorPrefab);
            }
            else
            {
                Debug.LogError("[DecorTest] Thiếu lưới PlacementController hoặc DecorPrefab!");
            }
        }
    }
}
