using UnityEngine;
using System.Collections.Generic;

namespace FarmPuzzle.Tetris
{
    [CreateAssetMenu(fileName = "TetrisCropMap", menuName = "FarmPuzzle/Tetris Crop Map")]
    public class TetrisCropMapSO : ScriptableObject
    {
        [System.Serializable]
        public class CropMapping
        {
            public string productID;
            public Sprite tetrisSprite; // Dùng hình ảnh thật của nông sản để làm block Tetris thay vì màu trơn!
            public Color fallBackColor = Color.white;
        }

        public List<CropMapping> mappings = new List<CropMapping>();

        public CropMapping GetMapping(string productID)
        {
            return mappings.Find(x => x.productID == productID);
        }
    }
}
