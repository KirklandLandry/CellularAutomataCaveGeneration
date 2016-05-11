using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellularAutomataCaveGeneration
{
    // idea for another day: to make a dungeon where you can travel up have a list of 2d arrays. arrays = x,y and position in the list = z
    // make a classic dungeon generator like that.
    // also try to make a dungeon that mixes cellular automata caves and classic room based dungeons

    // TODO: //
    // 1. solidify the terminology. I bounce back and forth from region, point, cell, grid, adjacent room, connected room
    //    choose a consistent term for everything. maybe start by renaming point to cell (or tile or whatever it may be)


    // there's too much public everywhere, make more get/set and restrict access

    // seed 69 show an example of a bad pathway that is too long and almost cuts through another region
    // there should be a check to make sure that doesn't happen
    // if the main region is too far away, it should look for a closer region and connect to that.

    class Program
    {    
        static private int width, height;
        static float changeToStartAlive = 0.45f; // 0.4 for big room with obstacles, 0.45 for conected rooms
        // death 4, birth 5 gives good open room
        // death 3, birth 4/5 gives good caves
        static int deathLimit = 3;
        static int birthlimit = 4;
        static int numberOfSteps = 5; // 9 - 10 seems to be enough for it to stop changing
        static List<Region> regions;
        static int minimumRegionSize = 12;

        // 295177381 is really good
        static int seed;

        static bool auto;
        static void Main(string[] args)
        {

            width = 77;
            height = 70;
            auto = false;

            seed = -1925715175;//NewRandomSeed();

            bool loop = true;
            while(loop)
            {
                Console.WriteLine("COMMANDS:\nR: new map, move through steps manually\nRA: new map, move through steps automatically\nP: Set generation Params\nE: exit");

                string input = Console.ReadLine().ToUpper();
                if (input == "R")
                {
                    auto = false;
                    
                    NewMap();
                }
                else if (input == "RA")
                {
                    auto = true;
                    NewMap();
                }
                else if (input == "E")
                    loop = false;
                else if (input == "P")
                    SetParams();
            }
        }

        // change parameters
        static void SetParams()
        {
            bool loop = true;
            auto = false;
            while (loop)
            {
                Console.WriteLine("COMMANDS:\nSEED: change the seed value \nE: return to previous menu");

                string input = Console.ReadLine().ToUpper();
                if (input == "SEED")
                {
                    Console.WriteLine("Enter a number for a new seed, enter anything else for a random seed");
                    string seedInput = Console.ReadLine().ToUpper();
                    try
                    {
                        seed = Convert.ToInt32(seedInput);
                    }
                    catch
                    {
                        NewRandomSeed();
                    }
                    Console.WriteLine("New seed: " + seed.ToString());
                }
                else if (input == "E")
                    loop = false;
            }
        }

        static void NewRandomSeed()
        {
            // good test seed -> -2091306045;
            seed = Guid.NewGuid().GetHashCode(); //GUID = globally unique identifier
        }

        static void NewMap()
        {

            regions = new List<Region>();
            int[,] map = InitMap();

            PrintMap(map, "noise map");
            NextStep(auto, 1000);

            for (int i = 0; i < numberOfSteps; i++)
            {
                map = SimulationStep(map);
                PrintMap(map, "step " + i);
                NextStep(auto, 500);
            }
            if(!auto)
                Console.ReadLine();

            map = DetectRegions(map);
            
            PrintMap(map, "map with regions");
            NextStep(auto, 1000);

            map = RemoveSmallRegions(map);
            PrintMap(map, "map with small regions removed");
            NextStep(auto, 1000);


            PrintEdgeMap(map);
            NextStep(auto, 1000);


            map = ConnectRooms(map);
            PrintMap(map, "all regions Connected");


            GC.Collect();
        }


        static void NextStep(bool auto, int delay)
        {
            if (auto)
                System.Threading.Thread.Sleep(1000);
            else
                Console.ReadLine();
        }

        static void PrintMap(int[,] map, string title)
        {
            Console.Clear();
            Console.WriteLine(title);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (map[x, y] == 1)
                        Console.Write("*");
                    else if (map[x, y] == 69) // for testing to draw connecting regions, remove later.
                        Console.Write("~");
                    else
                    {
                        char letter = (char)('A' + (char)(map[x, y] % 27));
                        Console.Write(letter);
                    }                    
                }
                Console.WriteLine();
            }
        }

        static void PrintEdgeMap(int[,] map)
        {
            Console.Clear();
            Console.WriteLine("Edge map");
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (map[x, y] == 1)
                        Console.Write("*");
                    else
                    {
                        bool set = false;
                        for(int i = 0; i < regions.Count; i++)
                        {
                            if(regions[i].edgeCells.ContainsKey(Region.GetId(new Point(x,y))))
                            {
                                char letter = (char)('A' + (char)(map[x, y] % 27));
                                Console.Write(letter);
                                set = true;
                                break;
                            }
                            else{ }
                        }
                        if(!set)
                            Console.Write(" ");
                    }
                }
                Console.WriteLine();
            }
        }


        // generate noise map
        static int[,] InitMap()
        {
            int[,] map = new int[width, height];
            Random r = new Random(seed);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float temp = (float)Convert.ToDouble(r.Next(0, 100));
                    if (temp / 100 < changeToStartAlive)
                    {
                        map[x, y] = 1;
                    }
                }
            }
            return map;
        }


        // game of life rules
        // 1. if a living cell has < 2 living neighbours, it dies
        // 2. if a living cell has 2 or 3 living neighbours, it lives
        // 3. if a living cell has > 3 living neighbours, it dies
        // 4. if a dead cell has 3 living neighbours, it lives

        // our rules
        // 2 variables, deathlimit and birthlimit
        // if a living cell has a number of living neighbours less than the deathlimit then it dies
        // else it stays alive
        // if a dead cell has a number of living neighbours greater than the birthlimit then it becomes alive
        // else it stays dead

        // alive = filled (1)
        // dead = unfilled (0)
        static int[,] SimulationStep(int[,] oldMap)
        {
            int[,] newMap = new int[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // get the number of alive neighbours for the current cell
                    int aliveNeighbourCount = countAliveNeighbours(oldMap, x, y);

                    // guarentee an edge wall
                    if(y == 0 || x ==0 || x == width -1 || y == height - 1)
                    {
                        newMap[x, y] = 1;
                    }
                    else if(oldMap[x,y] > 0) // if it's alive ...
                    {
                        // ... and it doesn't have enough living neighbours
                        if(aliveNeighbourCount < deathLimit)
                        {
                            // it dies
                            newMap[x, y] = 0;
                        }
                        else
                        {
                            // it stays alive
                            newMap[x, y] = 1;
                        }
                    }
                    else // if it's dead ...
                    {
                        // ... and it has enough living neighbours
                        if(aliveNeighbourCount > birthlimit)
                        {
                            // it lives
                            newMap[x, y] = 1;
                        }
                        else
                        {
                            // it stays dead
                            newMap[x, y] = 0;
                        }
                    }
                }
            }
            return newMap;
        }


        // return the # of cells in a ring around map[x,y] that are alive
        // 1 = alive, 0 = dead
        static int countAliveNeighbours(int[,] map, int x, int y)
        {
            // getting the total count of alive neighbours
            int count = 0;        
            // n = neighbour, c = current cell
            // n n n
            // n c n
            // n n n
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    int neighbour_x = x + j;
                    int neighbour_y = y + i;
                    //ignore the middle point (the current point)
                    if (i == 0 && j == 0) { }
                    // if we're looking at or past a map edge
                    else if(neighbour_x < 0 || neighbour_x >= width || neighbour_y < 0 || neighbour_y >= height)
                    {
                        count++;
                    }
                    // just a normal cell
                    else if(map[neighbour_x,neighbour_y] == 1)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        // the map passed in here will be ...
        // 1 = filled
        // 0 = unfilled
        // track the positions of where 0 spaces start before hand so the loops are minimized.
        // ie -> keep track of the f
        static int[,] DetectRegions(int[,] oldMap) 
        { 
            // count will indicate regions. each region will have it's own number
            int regionID = 2;
            int[,] newMap = oldMap;

            // can ignore first and last rows/columns because they're set to always be walls
            // this numbering system is bad / inconstent. change 1 to unfilled, 0 to filled.
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    if(newMap[x,y]==1)
                    {
                        // ignore it, it's a wall
                    }
                    else if(newMap[x,y] == 0)
                    {
                        // floodfill to create a new region
                        Point currentPoint = new Point(x,y);  
                        Queue<Point> cellsToCheck = new Queue<Point>();
                        cellsToCheck.Enqueue(currentPoint);
                        Region newRegion = new Region(regionID);

                        while(cellsToCheck.Count > 0)
                        { 
                            // get the first point
                            currentPoint = cellsToCheck.Dequeue();
                            // if the dequeued point isn't part of this region
                            
                            if (newMap[currentPoint.x, currentPoint.y] != regionID )
                            {
                                // add the point to the region
                                newMap[currentPoint.x, currentPoint.y] = regionID;
                                
                                // if it's an edge cell
                                if(newMap[currentPoint.x + 1, currentPoint.y] == 1 ||
                                   newMap[currentPoint.x - 1, currentPoint.y] == 1 ||
                                   newMap[currentPoint.x, currentPoint.y + 1] == 1 ||
                                   newMap[currentPoint.x, currentPoint.y - 1] == 1 )
                                {
                                    newRegion.Add(currentPoint, true);
                                }
                                else { newRegion.Add(currentPoint, false); }
                                    

                                // because the border is always set to be walls, we don't need to worry about out of bounds errors
                                if (newMap[currentPoint.x + 1, currentPoint.y] == 0)
                                    cellsToCheck.Enqueue(new Point(currentPoint.x + 1, currentPoint.y));

                                if (newMap[currentPoint.x - 1, currentPoint.y] == 0)
                                    cellsToCheck.Enqueue(new Point(currentPoint.x - 1, currentPoint.y));

                                if (newMap[currentPoint.x, currentPoint.y + 1] == 0)
                                    cellsToCheck.Enqueue(new Point(currentPoint.x, currentPoint.y + 1));

                                if (newMap[currentPoint.x, currentPoint.y - 1] == 0)
                                    cellsToCheck.Enqueue(new Point(currentPoint.x, currentPoint.y - 1));
                            }
                        }
                        regions.Add(newRegion);
                        regionID++;
                    }
                    else
                    {
                        // already marked as a new region
                    }
                }
            }
            return newMap;
        }

        static int[,] RemoveSmallRegions(int[,] oldMap)
        {
            int[,] newMap = oldMap;
            for(int i = regions.Count - 1; i >= 0 ; i--)
            {
                if(regions[i].Size() < minimumRegionSize)
                {
                    for(int n = 0; n < regions[i].Size(); n++)
                    {
                        foreach (KeyValuePair<string, Point> pt in regions[i].cells)
                        {
                            newMap[pt.Value.x, pt.Value.y] = 1;
                        }
                    }
                    regions.RemoveAt(i);
                }
            }
            return newMap;
        }


        static DistanceBetweenTwoRegionsStruct GetDistanceBetweenTwoRegions(int currentRegionIndex, int adjacentRegionIndex, DistanceBetweenTwoRegionsStruct _distObj)
        {
            DistanceBetweenTwoRegionsStruct distObj = _distObj;

            foreach (KeyValuePair<string, Point> pt in regions[currentRegionIndex].cells)
            {
                foreach (KeyValuePair<string, Point> npt in regions[adjacentRegionIndex].cells)
                {
                    // a^2 + b^2 = c^2
                    double distanceToAdjacentRegion = Math.Pow((npt.Value.x - pt.Value.x), 2) + Math.Pow((npt.Value.y - pt.Value.y), 2);

                    if (distanceToAdjacentRegion < distObj.minDistance)
                    {
                        distObj.minDistance = distanceToAdjacentRegion;
                        distObj.currentRegionEdgeCellKey = pt.Key;
                        distObj.adjacentRegionEdgeCellKey = npt.Key;
                        distObj.connectedCellIndex = adjacentRegionIndex;
                    }
                }
            }
            return distObj;
        }

        struct DistanceBetweenTwoRegionsStruct
        {
            public double minDistance;
            public string currentRegionEdgeCellKey;
            public string adjacentRegionEdgeCellKey;
            public int connectedCellIndex;
            public DistanceBetweenTwoRegionsStruct(bool param = true) // useless variable, just can't have an empty ctor
            {
                minDistance = 9999;
                currentRegionEdgeCellKey = "0,0";
                adjacentRegionEdgeCellKey = "0,0";
                connectedCellIndex = 0; 
            }
        }


        static int[,] ConnectRooms(int[,] oldMap)
        {
            int[,] newMap = oldMap;
            int[,] tempMap = oldMap; // purely for drawing / testing. remove later.

            int mainRegionIndex = 0;
            int biggestRoomSize = 0;

            // get the biggest room, which will be our main room.
            for(int i = 0; i < regions.Count; i++)
            {
                if (regions[i].cells.Count > biggestRoomSize)
                {
                    biggestRoomSize = regions[i].cells.Count;
                    mainRegionIndex = i;
                }
            }
            // the main room is obviously connected to the main room
            regions[mainRegionIndex].connectedToMainRegion = true;

            // so now, every room needs to try to connect to the main room.
            // it needs at least 1 path to the main room

            // should try doing this a little differently to increase efficiency
            // start with a room and connect it to the next closest room
            // go to the next room on the list. if it has a connection, skip it.
            // go to the next room on the list. if it has a connection, skip it.... repeat
            // now rooms have at least 1 connection
            // now iterate through again and check if, from that region, there is a path to the main region (the biggest one)
            // this should reduce the time the algorithm below takes by a lot and also reduce the # of paths between regions.
            // similar to what sebastian lague's video says

            // !! SEED -1925715175 PROVES IT STILL CUTS THROUGH OTHER REGIONS TO GET TO THE MAIN REGION !!


            // this entire algorithm is horrible and expensive.
            // 4 for loops is ridiculous.
            for (int i = 0; i < regions.Count; i++)
            {
                //double minDistance = 9999;
                //string currentRegionEdgeCellKey = "0,0";
                //string adjacentRegionEdgeCellKey = "0,0";

                //int c = 0; // c for connected, it's a bad name but I wanted to keep the 1 letter so the line lengths are equal later. 100% aesthetic.
                //bool broken = false;


                // find the distance and cells to the main region
                // maybe this should be done later to save memory
                // actually, this entire program should be an object that gets destroyed once initialization is complete.

                DistanceBetweenTwoRegionsStruct distObj = new DistanceBetweenTwoRegionsStruct(true);

                if (i != mainRegionIndex)
                {
                    distObj = GetDistanceBetweenTwoRegions(i, mainRegionIndex, distObj);
                    /*foreach (KeyValuePair<string, Point> cr in regions[i].cells)
                    {
                        foreach (KeyValuePair<string, Point> mr in regions[mainRegionIndex].cells)
                        {
                            double distanceToMainRegion = Math.Pow((mr.Value.x - cr.Value.x), 2) + Math.Pow((mr.Value.y - cr.Value.y), 2);

                            if (distanceToMainRegion < minDistance)
                            {
                                minDistance = distanceToMainRegion;
                                currentRegionEdgeCellKey = cr.Key;
                                adjacentRegionEdgeCellKey = mr.Key;
                            }
                        }
                    }*/
                    regions[i].distanceToMainRegion = distObj.minDistance;
                    regions[i].thisRegionCell = distObj.currentRegionEdgeCellKey;
                    regions[i].mainRegionCell = distObj.adjacentRegionEdgeCellKey;
                }



                //minDistance = 9999;
                //currentRegionEdgeCellKey = "0,0";
                //adjacentRegionEdgeCellKey = "0,0";

                
                distObj = new DistanceBetweenTwoRegionsStruct(true);
                for(int n = 0; n < regions.Count; n++)
                {
                    // don't check the current region 
                    if (n == i)// || regions[i].IsRegionConnected(regions[n].id))
                    {
                        // do nothing
                    }
                    else
                    {
                        distObj = GetDistanceBetweenTwoRegions(i, n, distObj);

                        /*foreach (KeyValuePair<string, Point> pt in regions[i].cells)
                        {
                            foreach (KeyValuePair<string, Point> npt in regions[n].cells)
                            {
                                // a^2 + b^2 = c^2
                                double distanceToAdjacentRegion = Math.Pow((npt.Value.x - pt.Value.x), 2) + Math.Pow((npt.Value.y - pt.Value.y), 2);
                                
                                if (distanceToAdjacentRegion < minDistance)
                                {
                                    minDistance = distanceToAdjacentRegion;
                                    currentRegionEdgeCellKey = pt.Key;
                                    adjacentRegionEdgeCellKey = npt.Key;
                                    c = n;
                                }  
                            }
                        }*/
                    }
                }
                if (!regions[i].IsRegionConnected(regions[distObj.connectedCellIndex].id))// if the shortest is already connected, ignore it
                {
                    // we now have the 2 closest nodes from 2 different regions
                    // add the adjacencies to each region
                    regions[i].AddConnectedRegion(regions[distObj.connectedCellIndex].id, distObj.currentRegionEdgeCellKey, distObj.adjacentRegionEdgeCellKey);
                    regions[distObj.connectedCellIndex].AddConnectedRegion(regions[i].id, distObj.adjacentRegionEdgeCellKey, distObj.currentRegionEdgeCellKey);

                    // this room is connected to the main room
                    if (distObj.connectedCellIndex == mainRegionIndex)
                    {
                        regions[i].connectedToMainRegion = true;
                    }

                    // this whole bit should be done once the entire set of paths is completed
                    List<Point> lines = ReturnPathBetweenRegions(
                        new Point(regions[i].cells[distObj.currentRegionEdgeCellKey].x, regions[i].cells[distObj.currentRegionEdgeCellKey].y),
                        new Point(regions[distObj.connectedCellIndex].cells[distObj.adjacentRegionEdgeCellKey].x, regions[distObj.connectedCellIndex].cells[distObj.adjacentRegionEdgeCellKey].y));


                    foreach (Point p in lines)
                    {
                        newMap[p.x, p.y] = 69;
                    }

                }
            }



            List<int> checkedRegions = new List<int>();
            Queue<int> regionStack = new Queue<int>();
            regionStack.Enqueue(regions[mainRegionIndex].id);
            checkedRegions.Add(regions[mainRegionIndex].id);

            while (regionStack.Count > 0)
            {
                // get the next region to be checked
                int currentRegionId = regionStack.Dequeue();
                // get the index of that region
                int currentRegionIndex = getRegionIndex(currentRegionId);

                regions[currentRegionIndex].connectedToMainRegion = true;

                // get all the connected regions
                foreach (ConnectedRegion connectedRegion in regions[currentRegionIndex].connectedRegions)
                {
                    // if a connected region hasn't been checked
                    if (!checkedRegions.Contains(connectedRegion.ID))
                    {
                        // add that region id to the list of checked regions
                        regionStack.Enqueue(connectedRegion.ID);
                        checkedRegions.Add(connectedRegion.ID);
                    }
                }
            }
                

            /*

            List<int> regionsToCheck = new List<int>();
            for(int i = 0; i < regions.Count; i++)
            {
                regionsToCheck.Add(regions[i].id);
            }

            while(regionsToCheck.Count > 0)
            {
                // now that everything's done, check for connectivity to the main room
                // list of checked region id's
                List<int> checkedRegionsThisLoop = new List<int>();
                Queue<int> regionStack = new Queue<int>();
                regionStack.Enqueue(regionsToCheck[0]);
                checkedRegionsThisLoop.Add(regionsToCheck[0]);
                bool connectedToMainRegion = false;

                while (regionStack.Count > 0)
                {
                    // get the next region to be checked
                    int currentRegionId = regionStack.Dequeue();
                    // get the index of that region
                    int currentRegionIndex = getRegionIndex(currentRegionId);
                    // if you pop the main room, that means all regions checked in this loop are connected to the main region
                    if (currentRegionId == regions[mainRegionIndex].id)
                        connectedToMainRegion = true;


                    // get all the connected regions
                    foreach (ConnectedRegion connectedRegion in regions[currentRegionIndex].connectedRegions)
                    {
                        // if a connected region hasn't been checked
                        if (!checkedRegionsThisLoop.Contains(connectedRegion.connectedRegionID))
                        {
                            // add that region id to the list of checked regions
                            regionStack.Enqueue(connectedRegion.connectedRegionID);
                            checkedRegionsThisLoop.Add(connectedRegion.connectedRegionID);
                        }
                    }
                }
                // if that loop resulted in a connection to the main region
                if (connectedToMainRegion)
                {
                    //for (int i = 0; i < checkedRegionsThisLoop.Count; i++)
                    //{
                    //    regions[checkedRegionsThisLoop[i] - 2].connectedToMainRegion = true;  
                    //}
                }
                else
                {
                    double shortestDistanceToMainRegion = 9999999;
                    int closestRegionId = -1;
                    // need to make a connection between the closest region checked and the main room
                    for (int i = 0; i < checkedRegionsThisLoop.Count; i++)
                    {
                        if (regions[getRegionIndex(checkedRegionsThisLoop[i])].distanceToMainRegion < shortestDistanceToMainRegion)
                        {
                            shortestDistanceToMainRegion = regions[getRegionIndex(checkedRegionsThisLoop[i])].distanceToMainRegion;
                            closestRegionId = checkedRegionsThisLoop[i];
                        }
                    }
                    // we've now got the closest region to the main region

                    // check for -1, that means there's a problem
                    int regionIndex = getRegionIndex(closestRegionId);
                    string toCellId = regions[regionIndex].mainRegionCell;
                    string fromCellId = regions[regionIndex].thisRegionCell;

                    Point to = regions[mainRegionIndex].edgeCells[toCellId];
                    Point from = regions[regionIndex].edgeCells[fromCellId];
                    List<Point> lines = ReturnPathBetweenRegions(to, from);

                    regions[regionIndex].AddConnectedRegion(regions[mainRegionIndex].id, fromCellId, toCellId);
                    regions[mainRegionIndex].AddConnectedRegion(regions[regionIndex].id, toCellId, fromCellId);

                    foreach (Point p in lines)
                    {
                        newMap[p.x, p.y] = 69;
                    }

                }
                for (int i = 0; i < checkedRegionsThisLoop.Count; i++)
                {
                    regions[getRegionIndex(checkedRegionsThisLoop[i])].connectedToMainRegion = true;
                    regionsToCheck.Remove(checkedRegionsThisLoop[i]);
                }
            }*/

            return newMap;
        }


        static List<Point> ReturnPathBetweenRegions(Point to, Point from)
        {

            /*
             * gradient accumulation = dx/2
             * gradient = slope
             * if dx > dy
                 * x++
                 * gradient accumulation += dy
                 * if(gradient accumulation > dx)
                 *      y++
                 *      gradient accumulation -= dx
             *  else
             *      switch everything around (switch dx and dy / x and y)
             */


            // this will be our starting point
            int x = from.x;
            int y = from.y;

            // the should whole path thing should be it's own function so it'll be storing it in a list of points
            // it's pointless for now, but it'll make things easier later
            List<Point> lines = new List<Point>();
            lines.Add(to);

            // the total distances (in tiles) in x and y
            // used to decide if we increment in x or y (increment in whichever one is bigger)
            int dx = to.x - from.x;
            int dy = to.y - from.y;

            // the following bits are assigned assuming dx is greater than dy

            // the increment step occurs at each iteration of the loop
            // the gradient step occurs when the gradient accumulation passes a threshold
            // the direction x is incremented by at each step
            int incrementStep = Math.Sign(dx);
            // the direction y is incremented by
            int gradientStep = Math.Sign(dy);

            // absolute values of the distances
            int longestDistance = Math.Abs(dx);
            int shortestDistance = Math.Abs(dy);

            // non-inverted = dx > dy
            bool inverted = false;

            // if the dx is actually shorter than dy
            if (longestDistance < shortestDistance)
            {
                // switch everything
                inverted = true;
                longestDistance = Math.Abs(dy);
                shortestDistance = Math.Abs(dx);
                incrementStep = Math.Sign(dy);
                gradientStep = Math.Sign(dx);
            }

            // dx or dy divided by 2
            int gradientAccumulation = longestDistance / 2;

            for (int n = 0; n < longestDistance; n++)
            {
                lines.Add(new Point(x, y));


                if (inverted)   // if dy > dx
                    y += incrementStep;
                else            // if dx > dy
                    x += incrementStep;

                gradientAccumulation += shortestDistance;
                if (gradientAccumulation >= longestDistance)
                {
                    if (inverted)
                        x += gradientStep;
                    else
                        y += gradientStep;
                    gradientAccumulation -= longestDistance;
                }
            }
            return lines;
        }

        static int getRegionIndex(int regionId)
        {
            int regionIndex = -1;
            for (int i = 0; i < regions.Count; i++)
            {
                if(regionId == regions[i].id)
                {
                    regionIndex = i;
                    break;
                }
            }
            return regionIndex;
        }
    }
}
