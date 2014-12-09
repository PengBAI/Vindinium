using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vindinium
	{
	class AStar
		{
		private SearchNode[,] searchNodes;
		private List<SearchNode> openList = new List<SearchNode>();
		private List<SearchNode> closedList = new List<SearchNode>();

		private int width;
		private int height;
		private Tile[][] map;
		private int MyHeroId;

		public AStar(Tile[][] board, int id)
			{
			width = board.Length;
			height = board.Length;
			map = board;
			MyHeroId = id;
			}

		public List<Point> FindPath(Point startPoint, Point endPoint )
			{
			// initialiser 
			InitializeSearchNodes(map,startPoint, endPoint);

			if (startPoint == endPoint)
				{
				return new List<Point>();
				}
			///////////////////////////////////////////////////////////////////
			// Step 1: Clear the open and closed lists and reset all of the  //
			// nodes F and G values incase they're still set from last time. //
			///////////////////////////////////////////////////////////////////
			ResetSearchNodes();

			// Store references to start and end nodes for convience.
			SearchNode startNode = searchNodes[startPoint.X, startPoint.Y];
			SearchNode endNode = searchNodes[endPoint.X, endPoint.Y];

			///////////////////////////////////////////////////////////////////
			// Step 2: Set the start node's G value to 0 and its F value to  //
			//         the estimated distance between the start node and goal//
			//         node.  (This is where the heuristic comes in) and add //
			//         it to the open list                                   //
			///////////////////////////////////////////////////////////////////
			startNode.InOpenList = true;
			startNode.DistanceToGoal = Heuristic(startPoint, endPoint);
			startNode.DistanceTraveled = 0;
			openList.Add(startNode);

			///////////////////////////////////////////////////////////////////
			// Step 3: While there are still nodes on the open list...       //
			///////////////////////////////////////////////////////////////////
			while (openList.Count > 0)
				{
				// Find the node with the lowest F value
				SearchNode currentNode = FindBestNode();

				// If the open list is empty or no node can be found
				if (currentNode == null)
					{
					break;
					}

				// If we've reached our goal
				if (currentNode == endNode)
					{
					return FindFinalPath(startNode, endNode);
					}

				// If not, keep going through the open list
				for (int i = 0 ; i < currentNode.Neighbors.Length ; i++)
					{
					SearchNode neighbor = currentNode.Neighbors[i];
					if (neighbor == null || neighbor.Walkable == false)
						{
						continue;
						}

					float distanceTraveled = currentNode.DistanceTraveled + 1;
					float heuristic = Heuristic(neighbor.Position, endPoint);

					// If the neighbor isn't in the closed or open list
					if (neighbor.InOpenList == false && neighbor.InClosedList == false)
						{
						neighbor.DistanceTraveled = distanceTraveled;
						neighbor.DistanceToGoal = distanceTraveled + heuristic;
						neighbor.Parent = currentNode;
						neighbor.InOpenList = true;
						openList.Add(neighbor);
						}
					else if (neighbor.InOpenList || neighbor.InClosedList)
						{
						if (neighbor.DistanceTraveled > distanceTraveled)
							{
							neighbor.DistanceTraveled = distanceTraveled;
							neighbor.DistanceToGoal = distanceTraveled + heuristic;
							neighbor.Parent = currentNode;
							}
						}
					}
				openList.Remove(currentNode);
				currentNode.InClosedList = true;
				}

			return new List<Point>();
			}

		private float Heuristic(Point point1, Point point2)
			{
			return Math.Abs(point1.X - point2.X)
				 + Math.Abs(point1.Y - point2.Y);
			}

		private void ResetSearchNodes()
			{
			openList.Clear();
			closedList.Clear();

			for (int x = 0 ; x < width ; x++)
				{
				for (int y = 0 ; y < height ; y++)
					{
					SearchNode node = searchNodes[x, y];
					if (node == null)
						{
						continue;
						}
					node.InOpenList = false;
					node.InClosedList = false;
					node.DistanceTraveled = float.MaxValue;
					node.DistanceToGoal = float.MaxValue;
					}
				}
			}

		private List<Point> FindFinalPath(SearchNode startNode, SearchNode endNode)
			{
			closedList.Add(endNode);
			SearchNode parentTile = endNode.Parent;
			while (parentTile != startNode)
				{
				closedList.Add(parentTile);
				parentTile = parentTile.Parent;
				}

			List<Point> finalPath = new List<Point>();

			for (int i = closedList.Count - 1 ; i >= 0 ; i--)
				{
				finalPath.Add(new Point(closedList[i].Position.X,
										closedList[i].Position.Y));
				}
			return finalPath;
			}

		private SearchNode FindBestNode()
			{
			SearchNode currentTile = openList[0];
			float smallestDistanceToGoal = float.MaxValue;
			for (int i = 0 ; i < openList.Count ; i++)
				{
				if (openList[i].DistanceToGoal < smallestDistanceToGoal)
					{
					currentTile = openList[i];
					smallestDistanceToGoal = currentTile.DistanceToGoal;
					}
				}
			return currentTile;
			}

		private void InitializeSearchNodes(Tile[][] map, Point startPoint, Point endPoint)
			{
			searchNodes = new SearchNode[width, height];
			for (int x = 0 ; x < width ; x++)
				{
				for (int y = 0 ; y < height ; y++)
					{
					SearchNode node = new SearchNode();
					node.Position = new Point(x, y);
					node.Walkable = map[x][y] == Tile.FREE || map[x][y] == Tile.HERO_1 || map[x][y] == Tile.HERO_2 ||
									map[x][y] == Tile.HERO_3 || map[x][y] == Tile.HERO_4;
					if (node.Walkable)
						{
						node.Neighbors = new SearchNode[4];
						searchNodes[x, y] = node;
						}
					}
				}
			// ajouter endPoint
			SearchNode endNode = new SearchNode();
			endNode.Position = new Point(endPoint.X, endPoint.Y);
			endNode.Walkable = true;
			endNode.Neighbors = new SearchNode[4];
			searchNodes[endPoint.X, endPoint.Y] = endNode;
			// ajouter startPoint
			SearchNode startNode = new SearchNode();
			startNode.Position = new Point(startPoint.X, startPoint.Y);
			startNode.Walkable = true;
			startNode.Neighbors = new SearchNode[4];
			searchNodes[startPoint.X, startPoint.Y] = startNode;

			// ajouter neighbors
			for (int x = 0 ; x < width ; x++)
				{
				for (int y = 0 ; y < height ; y++)
					{
					SearchNode node = searchNodes[x, y];
					if (node == null || node.Walkable == false)
						{
						continue;
						}

					Point[] neighbors = new Point[]
                    {
                        new Point(x ,y - 1),
                        new Point(x ,y + 1),
                        new Point(x - 1, y),
                        new Point(x + 1, y),
                    };

					for (int i = 0 ; i < neighbors.Length ; i++)
						{
						Point position = neighbors[i];
						if (position.X < 0 || position.X > width - 1 ||
						   position.Y < 0 || position.Y > height - 1)
							{
							continue;
							}
						SearchNode neighbor = searchNodes[position.X, position.Y];
						if (neighbor == null || neighbor.Walkable == false)
							{
							continue;
							}
						node.Neighbors[i] = neighbor;
						}
					}
				}
			}
		}
	}
