using UnityEngine;
using System;

public class OfflineTimeManager : MonoBehaviour
{
    private const string LAST_PLAY_TIME_KEY = "LastPlayTime";

    void Start()
    {
        // Tính thời gian offline (có thể dùng để hiện UI thông báo "Bro đã đi vắng X phút")
        CalculateOfflineTime();

        // CỰC KỲ QUAN TRỌNG: Load lại trạng thái cây trồng ngay khi vào game
        // (Lưu ý: Gọi hàm này SAU KHI GridManager đã GenerateGrid xong nhé)
        Invoke(nameof(LoadFarmState), 0.1f);
    }

    void OnApplicationQuit()
    {
        SaveCurrentTime();
        SaveFarmState();
    }

    void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            SaveCurrentTime();
            SaveFarmState();
        }
        else
        {
            CalculateOfflineTime();
            // Không cần gọi LoadFarmState ở đây vì khi Pause, game vẫn giữ data trên RAM
        }
    }

    private void SaveCurrentTime()
    {
        // Chuyển sang dùng UtcNow để ĐỒNG BỘ hoàn toàn với cách tính của LandPlot.cs
        DateTime currentTime = DateTime.UtcNow;
        PlayerPrefs.SetString(LAST_PLAY_TIME_KEY, currentTime.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    private void CalculateOfflineTime()
    {
        if (PlayerPrefs.HasKey(LAST_PLAY_TIME_KEY))
        {
            long temp = Convert.ToInt64(PlayerPrefs.GetString(LAST_PLAY_TIME_KEY));
            DateTime lastPlayTime = DateTime.FromBinary(temp);

            // Dùng UtcNow thay vì Now để tránh lỗi sai múi giờ
            TimeSpan timeDifference = DateTime.UtcNow - lastPlayTime;
            double offlineSeconds = timeDifference.TotalSeconds;

            Debug.Log($"Bro đã offline: {offlineSeconds} giây. Cây trồng đã tự động bắt kịp tiến độ!");
        }
    }

    // ================= CHỨC NĂNG LƯU & TẢI TRẠNG THÁI CÂY TRỒNG =================

    private void SaveFarmState()
    {
        // Tìm tất cả các ô đất trong scene
        LandPlot[] allPlots = FindObjectsOfType<LandPlot>();
        PlayerPrefs.SetInt("TotalPlots", allPlots.Length);

        for (int i = 0; i < allPlots.Length; i++)
        {
            LandPlot plot = allPlots[i];
            string prefix = "Plot_" + i + "_";

            // Lưu trạng thái có cây hay không
            PlayerPrefs.SetInt(prefix + "Occupied", plot.isOccupied ? 1 : 0);

            if (plot.isOccupied)
            {
                // Lưu tên của Scriptable Object để lúc load tìm lại cho đúng
                PlayerPrefs.SetString(prefix + "CropName", plot.GetCropData().name);

                // Lưu thời gian gieo hạt
                PlayerPrefs.SetString(prefix + "PlantedTime", plot.GetPlantedTime().ToBinary().ToString());

                // Lưu nhu cầu hiện tại (ép kiểu Enum về int)
                PlayerPrefs.SetInt(prefix + "Need", (int)plot.currentNeed);
                PlayerPrefs.SetInt(prefix + "NeedSpawned", plot.needHasSpawned ? 1 : 0);
            }
        }
        PlayerPrefs.Save();
        Debug.Log("Đã lưu trạng thái toàn bộ khu vườn!");
    }

    private void LoadFarmState()
    {
        if (!PlayerPrefs.HasKey("TotalPlots")) return;

        LandPlot[] allPlots = FindObjectsOfType<LandPlot>();
        int savedPlotsCount = PlayerPrefs.GetInt("TotalPlots");

        for (int i = 0; i < Mathf.Min(allPlots.Length, savedPlotsCount); i++)
        {
            LandPlot plot = allPlots[i];
            string prefix = "Plot_" + i + "_";

            bool isOccupied = PlayerPrefs.GetInt(prefix + "Occupied", 0) == 1;

            if (isOccupied)
            {
                // Tìm lại CropDataSO dựa trên tên đã lưu (Yêu cầu: File SO phải đặt trong thư mục Resources)
                string cropName = PlayerPrefs.GetString(prefix + "CropName");
                CropDataSO loadedCropData = Resources.Load<CropDataSO>(cropName);

                long timeBinary = Convert.ToInt64(PlayerPrefs.GetString(prefix + "PlantedTime"));
                DateTime plantedTime = DateTime.FromBinary(timeBinary);

                CropStatus.CropNeed need = (CropStatus.CropNeed)PlayerPrefs.GetInt(prefix + "Need", 0);
                bool needSpawned = PlayerPrefs.GetInt(prefix + "NeedSpawned", 0) == 1;

                // Gọi hàm SetData bro đã viết sẵn để đẩy data vào
                plot.SetData(true, loadedCropData, plantedTime, need, needSpawned);
            }
            else
            {
                plot.ClearPlot();
            }
        }
        Debug.Log("Đã tải xong trạng thái khu vườn!");
    }
}