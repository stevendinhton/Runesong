/*
 * 
 * 

            if (cachedParams.Contains(pathfindingParams)) {
                int index = Array.FindIndex(cachedParams, current => {
                    return start.x == current.startPosition.x && start.y == current.startPosition.y
                        && end.x == current.endPosition.x && end.y == current.endPosition.y;
                });

                Debug.Log("Using previously found path");

                pathNodeArray = new NativeArray<PathNode>(cachedMap[index], Allocator.TempJob);
            } else {
                pathNodeArray = getPathNodeArray(mapTiles, gridSize, pathfindingParams.endPosition);
                cachedMap[mapsCreated] = pathNodeArray.ToArray();
                cachedParams[mapsCreated] = pathfindingParams;
                mapsCreated++;
            }


            */