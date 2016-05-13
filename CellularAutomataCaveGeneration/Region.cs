using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellularAutomataCaveGeneration
{



    class Region
    {
        public int id;

        public Dictionary<string, Point> edgeCells;
        public Dictionary<string, Point> cells;

        public List<ConnectedRegion> connectedRegions; // 

        // change this to it's onw connected region object
        public bool connectedToMainRegion = false;
        //public double distanceToMainRegion = 0;  // if it remains 0, that means this is the main region
        //public string thisRegionCell; // the cell in this region
        //public string mainRegionCell; // the cell in the main region

        // this list will have 
        // a key telling the adjacent region id
        // a distance to that region
        public Dictionary<int, DistanceBetweenTwoRegionsStruct> distanceToAdjacentRegions;

        public Region(int id)
        {
            this.id = id;
            cells = new Dictionary<string, Point>();
            edgeCells = new Dictionary<string, Point>();
            connectedRegions = new List<ConnectedRegion>();
            distanceToAdjacentRegions = new Dictionary<int, DistanceBetweenTwoRegionsStruct>();
        }
        
        public void Add(Point p, bool edge)
        {
            cells.Add(GetId(p), p);
            if (edge)
                edgeCells.Add(GetId(p), p);
        }

        public int Size()
        {
            return cells.Count;
        }

        public static string GetId(Point p)
        {
            return (p.x.ToString() + "," + p.y.ToString());
        }

        public bool IsRegionConnected(int id)
        {
            for(int i = 0; i < connectedRegions.Count; i++)
            {
                if (connectedRegions[i].ID == id)
                    return true;
            }
            return false;
        }

        public void AddConnectedRegion(int _connectedRoomID, string _thisRegionEdgeCell, string _connectedRegionEdgeCell)
        {
            connectedRegions.Add(new ConnectedRegion(_connectedRoomID, _thisRegionEdgeCell, _connectedRegionEdgeCell));
        }

    }
}
