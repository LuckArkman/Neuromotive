using System.Collections.Generic;

namespace Neural
{
    [System.Serializable]
    public class BrainSaveData
    {
        public List<LayerSaveData> layers = new List<LayerSaveData>();
    }
}