using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellularAutomataCaveGeneration
{
    class ConnectedRegion
    {
        public int ID;
        public string thisRegionEdgeCell;
        public string connectedRegionEdgeCell;

        public ConnectedRegion(int connectedRegionID, string thisRegionEdgeCell, string connectedRegionEdgeCell)
        {
            this.ID = connectedRegionID;
            this.thisRegionEdgeCell = thisRegionEdgeCell;
            this.connectedRegionEdgeCell = connectedRegionEdgeCell;
        }


    }
}
