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


    // TODO: ConnectRooms(int[,] oldMap) is basically the same thing twice. slim it down, move it to it's own thing.
    // TODO: make pathways thicker
    // TODO: change DetectRegions(int[,] oldMap) so region id's start at 0

    // NOTE: any room sizes beyond 100x100 will kill the speed in map = ConnectRooms(map) section. needs huge optimizations. 
    // 70x70   -> 0s
    // 100x100 -> 4s
    // 130x130 -> 7s
    // 150x150 -> 29s
    // 172x170 -> 42s
    // that many for loops is obscene. some of that info should be calculated earlier.
    // when it first generates distances for all regions it should save the distance to every region. Use this later for joining to the main region

    // FIRST FIX: it was checking cells instead of edgecells. changed that
    // 70x70   -> 200  ms
    // 100x100 -> 1004 ms
    // 130x130 -> 3345 ms
    // 150x150 -> 8029 ms
    // 172x170 -> 14321ms


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

        static int pathwayCode = -1;

        static int seed;
        static bool randSeed = true;

        static bool auto;
        static void Main(string[] args)
        {

            width = 70;
            height = 70;
            auto = false;


            bool loop = true;
            while(loop)
            {
                Console.WriteLine("COMMANDS:\nR: new map, move through steps manually\nRA: new map, move through steps automatically\nP: Set generation Params\nE: exit");

                if (randSeed)
                    NewRandomSeed();
                seed = 1847589810;
                string input = Console.ReadLine().ToUpper();
                if (input == "R")
                {
                    auto = false;
                    NewMap(true);
                }
                else if (input == "RA")
                {
                    auto = true;
                    NewMap(true);
                }
                else if(input == "G")
                {
                    NewMap(false);
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
                Console.WriteLine("COMMANDS:\nSEED: change the seed value \nSIZE: change width / height\nE: return to previous menu");

                string input = Console.ReadLine().ToUpper();
                if (input == "SEED")
                {
                    Console.WriteLine("Enter a number for a new seed, enter anything else for a random seed");
                    string seedInput = Console.ReadLine().ToUpper();
                    try
                    {
                        randSeed = false;
                        seed = Convert.ToInt32(seedInput);
                    }
                    catch
                    {
                        randSeed = true;
                        NewRandomSeed();
                    }
                    Console.WriteLine("New seed: " + seed.ToString());
                }
                else if(input == "SIZE")
                {
                    Console.WriteLine("CURRENT: width=" + width + " height=" + height + "\n");
                    Console.Write("new width: ");
                    string tempWidth = Console.ReadLine();
                    bool firstFailed = false;
                    try
                    {
                        width = Convert.ToInt32(tempWidth);
                    }
                    catch
                    {
                        Console.WriteLine("\nincorrect format\n");
                        firstFailed = true;
                    }
                    if(!firstFailed)
                    {
                        Console.Write("new height: ");
                        string tempHeight = Console.ReadLine();

                        try
                        {
                            height = Convert.ToInt32(tempHeight);
                        }
                        catch
                        {
                            Console.WriteLine("\nincorrect format\n");
                        }
                    }
                }
                else if (input == "E")
                    loop = false;
            }
        }

        static void NewRandomSeed()
        {
            // good seeds
            // -2091306045;
            // -1925715175 // exposes overlap error
            // 295177381 is really good
            seed = Guid.NewGuid().GetHashCode(); //GUID = globally unique identifier
        }

        static void NewMap(bool step)
        {
            if(step)
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
                if (!auto)
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
                Console.WriteLine("Seed: " + seed.ToString() + "\n\n");
            }
            else
            {
                long start = DateTime.Now.Ticks;
                regions = new List<Region>();
                int[,] map = InitMap();
                Console.WriteLine("initialization done\n");

                for (int i = 0; i < numberOfSteps; i++)
                {
                    map = SimulationStep(map);
                }
                Console.WriteLine("clearing steps done\n");

                map = DetectRegions(map);
                Console.WriteLine("region detection done\n");
                map = RemoveSmallRegions(map);
                Console.WriteLine("removing small regions done\n");
                map = ConnectRooms(map);
                Console.WriteLine("ensuring room connection done\n");

                long end = DateTime.Now.Ticks - start;

                //PrintMap(map, "all regions Connected");
                Console.WriteLine("ticks: " + end.ToString() + "\nms: " + (end / TimeSpan.TicksPerMillisecond).ToString() + "\ns: " + (end / TimeSpan.TicksPerSecond).ToString());
                Console.WriteLine("Seed: " + seed.ToString() + "\n\n");
            }

            GC.Collect();
        }


        static void NextStep(bool auto, int delay)
        {
            if (auto)
                System.Threading.Thread.Sleep(delay);
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
                    else if (map[x, y] == pathwayCode) // for testing to draw connecting regions, remove later.
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
                    float chance = (float)Convert.ToDouble(r.Next(0, 100)) / 100;
                    if (chance < changeToStartAlive)
                    {
                        map[x, y] = 1;
                    }
                }
            }
            return map;
        }


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
                    // if we're looking at or past a map edge OR if it's just a normal cell (map[neighbour_x,neighbour_y]==1)
                    else if(neighbour_x < 0 || neighbour_x >= width || neighbour_y < 0 || neighbour_y >= height || map[neighbour_x,neighbour_y] == 1)
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

        // change this so regionID starts at 0
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
                    else if (newMap[x, y] == 0) // if it's not already marked as a new region
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

            // for finding the shortest distance JUST for this iteration (not overall like the above)
            DistanceBetweenTwoRegionsStruct distObjThisIteration = new DistanceBetweenTwoRegionsStruct(true);

            foreach (KeyValuePair<string, Point> pt in regions[currentRegionIndex].edgeCells)
            {
                foreach (KeyValuePair<string, Point> npt in regions[adjacentRegionIndex].edgeCells)
                {
                    // a^2 + b^2 = c^2
                    double distanceToAdjacentRegion = Math.Pow((npt.Value.x - pt.Value.x), 2) + Math.Pow((npt.Value.y - pt.Value.y), 2);

                    if (distanceToAdjacentRegion < distObj.minDistance)
                    {
                        distObj.minDistance = distanceToAdjacentRegion;
                        distObj.currentRegionEdgeCellKey = pt.Key;
                        distObj.adjacentRegionEdgeCellKey = npt.Key;
                        distObj.connectedCellIndex = adjacentRegionIndex;
                        distObj.currentRegionIndex = currentRegionIndex;
                    }

                    if(distanceToAdjacentRegion < distObjThisIteration.minDistance)
                    {
                        distObjThisIteration.minDistance = distanceToAdjacentRegion;

                        distObj.rawDistance = distanceToAdjacentRegion;
                        distObj.rawCurrentRegionEdgeCellKey = pt.Key;
                        distObj.rawAdjacentRegionEdgeCellKey = npt.Key;
                        distObj.rawAdjacentRegionIndex = adjacentRegionIndex;
                        distObj.rawCurrentRegionIndex = currentRegionIndex;
                    }
                    
                }
            }

            return distObj;
        }




        static int GetMainRegionIndex()
        {
            int biggestRoomSize = 0;
            int mainRegionIndex = 0;
            // get the biggest room, which will be our main room.
            for (int i = 0; i < regions.Count; i++)
            {
                if (regions[i].cells.Count > biggestRoomSize)
                {
                    biggestRoomSize = regions[i].cells.Count;
                    mainRegionIndex = i;
                }
            }
            return mainRegionIndex;
        }

        static void ConnectToMainRegion(int mainRegionIndex)
        {
            List<int> checkedRegions = new List<int>();
            Queue<int> regionQueue = new Queue<int>();
            regionQueue.Enqueue(regions[mainRegionIndex].id);
            checkedRegions.Add(regions[mainRegionIndex].id);

            // this will mark every region attatched to the main region as such
            while (regionQueue.Count > 0)
            {
                // get the next region to be checked
                int currentRegionId = regionQueue.Dequeue();
                // get the index of that region
                int currentRegionIndex = getRegionIndex(currentRegionId);
                // mark the region as attatched to the main region
                regions[currentRegionIndex].connectedToMainRegion = true;

                // get all the connected regions
                foreach (ConnectedRegion connectedRegion in regions[currentRegionIndex].connectedRegions)
                {
                    // if a connected region hasn't been checked
                    if (!checkedRegions.Contains(connectedRegion.ID))
                    {
                        // add that region id to the list of checked regions
                        regionQueue.Enqueue(connectedRegion.ID);
                        checkedRegions.Add(connectedRegion.ID);
                    }
                }
            }
        }


        static int[,] Connect(int[,] oldMap, int outerCounter, int innerCounter, bool connectingToMainRegion)
        {
            int[,] newMap = oldMap;
            DistanceBetweenTwoRegionsStruct distObj = new DistanceBetweenTwoRegionsStruct(true); ;
            for (int i = 0; i < outerCounter; i++)
            {
                //if(!connectingToMainRegion)
                distObj = new DistanceBetweenTwoRegionsStruct(true);


                for (int n = 0; n < innerCounter; n++)
                {
                    // if you're not checking the current region
                    if (n != i)
                    {
                        distObj = GetDistanceBetweenTwoRegions(i, n, distObj);
                        // give this region the distance to the adjacent region 
                        regions[i].distanceToAdjacentRegions.Add(regions[n].id, distObj);
                    }
                }
                //if (!connectingToMainRegion)
                //{
                // if the shortest path is already connected, ignore it
                if (!regions[i].IsRegionConnected(regions[distObj.connectedCellIndex].id))
                {
                    // we now have the 2 closest nodes from 2 different regions
                    // add the adjacencies to each region
                    regions[i].AddConnectedRegion(regions[distObj.connectedCellIndex].id, distObj.currentRegionEdgeCellKey, distObj.adjacentRegionEdgeCellKey);
                    regions[distObj.connectedCellIndex].AddConnectedRegion(regions[i].id, distObj.adjacentRegionEdgeCellKey, distObj.currentRegionEdgeCellKey);

                    // this whole bit should be done once the entire set of paths is completed
                    List<Point> lines = ReturnPathBetweenRegions(
                        new Point(regions[i].cells[distObj.currentRegionEdgeCellKey].x, regions[i].cells[distObj.currentRegionEdgeCellKey].y),
                        new Point(regions[distObj.connectedCellIndex].cells[distObj.adjacentRegionEdgeCellKey].x, regions[distObj.connectedCellIndex].cells[distObj.adjacentRegionEdgeCellKey].y));

                    // change the map to have the connection
                    foreach (Point p in lines)
                    {
                        newMap[p.x, p.y] = pathwayCode;
                    }
                }
                //}

            }
            return newMap;
        }

        /*static int[,] Connect(int[,] oldMap, int outerCounter, int innerCounter)
        {
            int[,] newMap = oldMap;
            for (int i = 0; i < regions.Count; i++)
            {
                DistanceBetweenTwoRegionsStruct distObj = new DistanceBetweenTwoRegionsStruct(true);
                for (int n = 0; n < regions.Count; n++)
                {
                    // if you're not checking the current region
                    if (n != i)
                    {
                        distObj = GetDistanceBetweenTwoRegions(i, n, distObj);
                    }
                }

                // if the shortest path is already connected, ignore it
                if (!regions[i].IsRegionConnected(regions[distObj.connectedCellIndex].id))
                {
                    // we now have the 2 closest nodes from 2 different regions
                    // add the adjacencies to each region
                    regions[i].AddConnectedRegion(regions[distObj.connectedCellIndex].id, distObj.currentRegionEdgeCellKey, distObj.adjacentRegionEdgeCellKey);
                    regions[distObj.connectedCellIndex].AddConnectedRegion(regions[i].id, distObj.adjacentRegionEdgeCellKey, distObj.currentRegionEdgeCellKey);

                    // this whole bit should be done once the entire set of paths is completed
                    List<Point> lines = ReturnPathBetweenRegions(
                        new Point(regions[i].cells[distObj.currentRegionEdgeCellKey].x, regions[i].cells[distObj.currentRegionEdgeCellKey].y),
                        new Point(regions[distObj.connectedCellIndex].cells[distObj.adjacentRegionEdgeCellKey].x, regions[distObj.connectedCellIndex].cells[distObj.adjacentRegionEdgeCellKey].y));

                    // change the map to have the connection
                    foreach (Point p in lines)
                    {
                        oldMap[p.x, p.y] = pathwayCode;
                    }
                }
            }
            return NewMap;
        }*/

        static int[,] ConnectRooms(int[,] oldMap)
        {
            int[,] newMap = oldMap;
            int mainRegionIndex = GetMainRegionIndex();
            regions[mainRegionIndex].connectedToMainRegion = true;


            newMap = Connect(newMap, regions.Count, regions.Count, false);
            ConnectToMainRegion(mainRegionIndex);



            bool complete = false; 
            while(!complete)
            { 
                List<int> regionsAccessibleFromMainRegion = new List<int>();
                List<int> regionsNotAccessibleFromMainRegion = new List<int>();

                // sort all remaining rooms into 2 lists. connected to main room and not connected to main room.
                for (int i = 0; i < regions.Count; i++)
                {
                    if (regions[i].connectedToMainRegion)
                    {
                        regionsAccessibleFromMainRegion.Add(regions[i].id);
                    }
                    else
                    {
                        regionsNotAccessibleFromMainRegion.Add(regions[i].id);
                    }
                }

                if (regionsNotAccessibleFromMainRegion.Count <= 0)
                {
                    complete = true;
                }
                else
                {
                    DistanceBetweenTwoRegionsStruct distToMainRegionAccessibleRegion = new DistanceBetweenTwoRegionsStruct(true);

                    //for (int i = 0; i < regionsAccessibleFromMainRegion.Count; i++)
                    for (int i = 0; i < regionsNotAccessibleFromMainRegion.Count; i++)
                    {

                        foreach (KeyValuePair<int, DistanceBetweenTwoRegionsStruct> d in regions[getRegionIndex(regionsNotAccessibleFromMainRegion[i])].distanceToAdjacentRegions)
                        {
                            if (d.Value.rawDistance < distToMainRegionAccessibleRegion.rawDistance && regions[d.Value.rawAdjacentRegionIndex].connectedToMainRegion)
                            {
                                distToMainRegionAccessibleRegion = d.Value;
                            }
                        }

                        /*
                        //for (int n = 0; n < regionsNotAccessibleFromMainRegion.Count; n++)
                        int index = getRegionIndex(regionsNotAccessibleFromMainRegion[i]);

                        for (int n = 0; n < regions[index].distanceToAdjacentRegions.Count; n++)
                        {
                            //if(n != i)
                            if(regionsNotAccessibleFromMainRegion[i] != regions[n].id)
                            {
                                //distToMainRegionAccessibleRegion = GetDistanceBetweenTwoRegions(getRegionIndex(regionsAccessibleFromMainRegion[i]), getRegionIndex(regionsNotAccessibleFromMainRegion[n]), distToMainRegionAccessibleRegion);
                                //if(regions[i].distanceToAdjacentRegions[regions[n].id].minDistance < distToMainRegionAccessibleRegion.minDistance)

                                //int currentIndex = getRegionIndex(regionsNotAccessibleFromMainRegion[i]);

                                int adjacentId = regions[index].distanceToAdjacentRegions.ElementAt(n).Value.connectedCellIndex;//regions[n].id;
                                Region r = regions[index];
                                Dictionary<int, DistanceBetweenTwoRegionsStruct> d = r.distanceToAdjacentRegions;
                                double minDist = d[adjacentId].minDistance;

                                if (minDist  < distToMainRegionAccessibleRegion.minDistance
                                    && regionsAccessibleFromMainRegion.Contains(adjacentId))
                                {
                                    DistanceBetweenTwoRegionsStruct temp = regions[index].distanceToAdjacentRegions[adjacentId];
                                    distToMainRegionAccessibleRegion.minDistance = temp.minDistance;
                                    distToMainRegionAccessibleRegion.currentRegionEdgeCellKey = temp.currentRegionEdgeCellKey;
                                    distToMainRegionAccessibleRegion.adjacentRegionEdgeCellKey = temp.adjacentRegionEdgeCellKey;
                                    distToMainRegionAccessibleRegion.connectedCellIndex = temp.connectedCellIndex;
                                    distToMainRegionAccessibleRegion.currentRegionIndex = temp.currentRegionIndex;
                                }
                                
                                //distToMainRegionAccessibleRegion
                            }
                        }*/
                    }
                    
                    // if the shortest path is already connected, ignore it
                    if (!regions[distToMainRegionAccessibleRegion.rawCurrentRegionIndex].IsRegionConnected(regions[distToMainRegionAccessibleRegion.rawAdjacentRegionIndex].id))
                    {
                        // we now have the 2 closest nodes from 2 different regions
                        // add the adjacencies to each region
                        regions[distToMainRegionAccessibleRegion.rawCurrentRegionIndex].AddConnectedRegion(regions[distToMainRegionAccessibleRegion.rawAdjacentRegionIndex].id, distToMainRegionAccessibleRegion.currentRegionEdgeCellKey, distToMainRegionAccessibleRegion.adjacentRegionEdgeCellKey);
                        regions[distToMainRegionAccessibleRegion.rawAdjacentRegionIndex].AddConnectedRegion(regions[distToMainRegionAccessibleRegion.rawCurrentRegionIndex].id, distToMainRegionAccessibleRegion.adjacentRegionEdgeCellKey, distToMainRegionAccessibleRegion.currentRegionEdgeCellKey);


                        // this whole bit should be done once the entire set of paths is completed
                        string fromCellKey = distToMainRegionAccessibleRegion.rawCurrentRegionEdgeCellKey;
                        int fromRegionIndex = distToMainRegionAccessibleRegion.rawCurrentRegionIndex;
                        Region r1 = regions[fromRegionIndex];
                        int x1 = r1.cells[fromCellKey].x;
                        int y1 = regions[fromRegionIndex].cells[fromCellKey].y;

                        string toCellKey = distToMainRegionAccessibleRegion.rawAdjacentRegionEdgeCellKey;
                        int toCellindex = distToMainRegionAccessibleRegion.rawAdjacentRegionIndex;
                        
                        Region r = regions[toCellindex];
                        int x2 = r.cells[toCellKey].x;
                        int y2 = regions[toCellindex].cells[toCellKey].y;

                        List<Point> lines = ReturnPathBetweenRegions(
                            new Point(x2, y2),
                            new Point(x1, y1));

                        // change the map to have the connection
                        foreach (Point p in lines)
                        {
                            newMap[p.x, p.y] = pathwayCode;
                        }
                    }  
                    
                    // for some reason this hangs
                    //newMap = Connect(newMap, regionsAccessibleFromMainRegion.Count, regionsNotAccessibleFromMainRegion.Count, true);
                    ConnectToMainRegion(mainRegionIndex);
                }
                PrintMap(newMap, " ");
            }
            return newMap;
        }


        static List<Point> ReturnPathBetweenRegions(Point to, Point from)
        {
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
