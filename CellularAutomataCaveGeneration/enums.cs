using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellularAutomataCaveGeneration
{
    public struct DistanceBetweenTwoRegionsStruct
    {
        public double minDistance;
        public double rawDistance;
        public string currentRegionEdgeCellKey;
        public string adjacentRegionEdgeCellKey;
        public int connectedCellIndex;
        public int currentRegionIndex;

        public string rawCurrentRegionEdgeCellKey;
        public string rawAdjacentRegionEdgeCellKey;
        public int rawAdjacentRegionIndex;
        public int rawCurrentRegionIndex;
        public DistanceBetweenTwoRegionsStruct(bool param = true) // useless variable, just can't have an empty ctor
        {
            minDistance = 9999999999;
            currentRegionEdgeCellKey = "0,0";
            adjacentRegionEdgeCellKey = "0,0";
            connectedCellIndex = 0;
            currentRegionIndex = 0;

            rawDistance = 9999999999;
            rawAdjacentRegionIndex = 0;
            rawCurrentRegionIndex = 0;
            rawCurrentRegionEdgeCellKey = "0,0";
            rawAdjacentRegionEdgeCellKey = "0,0";
        }
    }
}
