using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vindinium
	{
	class MyAiBot
		{
		private ServerStuff serverStuff;
		private AStar aStar;

		public MyAiBot(ServerStuff serverStuff)
			{
			this.serverStuff = serverStuff;
			}

		//starts everything
		public void run()
			{
			Console.Out.WriteLine("MyAIBot bot running");

			serverStuff.createGame();

			if (serverStuff.errored == false)
				{
				//opens up a webpage so you can view the game, doing it async so we dont time out
				new Thread(delegate()
				{
					System.Diagnostics.Process.Start(serverStuff.viewURL);
				}).Start();
				}
			/////////////////////////////////////////////////////////////////////////////////
			//                        BEGIN: stratégies                                    //
			//-----------------------------------------------------------------------------//
			while (serverStuff.finished == false && serverStuff.errored == false)
				{

				List<Pos> TavernPos = new List<Pos>();
				List<Pos> AllOtherMine = new List<Pos>();
				// un ensemble de paths à destinations
				List<List<Point>> path = new List<List<Point>>();

				aStar = new AStar(serverStuff.board, serverStuff.myHero.id);
				// touver les positions Mine et Tavern
				findAllPos(TavernPos, AllOtherMine);
				// prendre cet index comme décision
				int indexMin = 0;

				if (serverStuff.myHero.mineCount > AllOtherMine.Count)
					{
					// si tous les mine à moi, je reste à Tavern
					indexMin = moveToNearestTavern(path, TavernPos);
					}
				else
					{
					if (serverStuff.myHero.life > 45)
						{
						int idHero = hasLessLife();
						//int idHero = hasMostMine();
						if (idHero != serverStuff.myHero.id && serverStuff.myHero.life > serverStuff.heroes[idHero - 1].life)
							{
							indexMin = moveToHero(path, idHero - 1);
							if (path[indexMin].Count > 2)
								{
								// aller au MineNeutral
								path.Clear();
								indexMin = moveToNearestAllOtherMine(path, AllOtherMine);
								}
							}
						else
							{

							if (isClosestHero())
								{
								// aller au Tavern
								indexMin = moveToNearestTavern(path, TavernPos);
								}
							else
								{
								// aller au MineNeutral
								indexMin = moveToNearestAllOtherMine(path, AllOtherMine);
								}
							}
						}
					else
						{
						// aller au Tavern
						indexMin = moveToNearestTavern(path, TavernPos);
						}
					}
				// bouger à destination
				if (path[indexMin].Count != 0)
					{
					int deltaX = path[indexMin][0].X - serverStuff.myHero.pos.x;
					int deltaY = path[indexMin][0].Y - serverStuff.myHero.pos.y;
					if (deltaX == 0 && deltaY == 0)
						{
						serverStuff.moveHero(Direction.Stay);
						}
					else if (deltaX == -1 && deltaY == 0)
						{
						serverStuff.moveHero(Direction.North);
						}
					else if (deltaX == 1 && deltaY == 0)
						{
						serverStuff.moveHero(Direction.South);
						}
					else if (deltaX == 0 && deltaY == -1)
						{
						serverStuff.moveHero(Direction.West);
						}
					else if (deltaX == 0 && deltaY == 1)
						{
						serverStuff.moveHero(Direction.East);
						}
					}
				// arriver à destination
				else
					{
					serverStuff.moveHero(Direction.Stay);
					}
				// supprimer des donnée de ce tour
				path.Clear();
				AllOtherMine.Clear();
				TavernPos.Clear();
				Console.Out.WriteLine("completed turn " + serverStuff.currentTurn);
				}
			//-----------------------------------------------------------------------------//
			//                         END:  stratégies                                    //
			/////////////////////////////////////////////////////////////////////////////////

			if (serverStuff.errored)
				{
				Console.Out.WriteLine("error: " + serverStuff.errorText);
				}

			Console.Out.WriteLine("MyAIBot bot finished");
			}


		//////////////////////////////////////////////////////////////////////////////////////
		//    BEGIN:    Toutes les fonctions servent à calculer des info(path) dans map		//
		//////////////////////////////////////////////////////////////////////////////////////


		/// <summary>
		/// trouver les mine GOLD_MINE_NEUTRAL, GOLD_MINE_(1, 2, 3, 4) sauf mon mine
		/// récupérer les positons dans le map
		///                  |----------------> Y
		///                  |
		///                  |
		///                  |
		///                  |
		///                  |
		///                  v X
		/// </summary>
		private void findAllPos(List<Pos> TavernPos, List<Pos> AllOtherMine)
			{
			int size = serverStuff.board.Length;
			int idMyHero = serverStuff.myHero.id;
			for (int x = 0 ; x < size ; x++)
				{
				for (int y = 0 ; y < size ; y++)
					{
					switch (serverStuff.board[x][y])
						{
						case Tile.GOLD_MINE_1:
							// si le mine à moi, on break
							if (idMyHero == 1)
								{
								break;
								}
							else
								{
								goto case Tile.GOLD_MINE_NEUTRAL;
								}
						case Tile.GOLD_MINE_2:
							if (idMyHero == 2)
								{
								break;
								}
							else
								{
								goto case Tile.GOLD_MINE_NEUTRAL;
								}
						case Tile.GOLD_MINE_3:
							if (idMyHero == 3)
								{
								break;
								}
							else
								{
								goto case Tile.GOLD_MINE_NEUTRAL;
								}
						case Tile.GOLD_MINE_4:
							if (idMyHero == 4)
								{
								break;
								}
							else
								{
								goto case Tile.GOLD_MINE_NEUTRAL;
								}
						case Tile.GOLD_MINE_NEUTRAL:
							AllOtherMine.Add(new Pos(x, y));
							break;
						case Tile.TAVERN:
							TavernPos.Add(new Pos(x, y));
							break;
						}
					}
				}
			}

		/// <summary>
		/// bouger à le plus proche OtherMine sauf lequel à moi
		/// </summary>
		/// <param name="path">ensemble de path à tous les MineNeutrals</param>
		/// <returns>index de plus proche MineNeutral dans List path</returns>
		private int moveToNearestAllOtherMine(List<List<Point>> path, List<Pos> AllOtherMine)
			{
			int indexMin = 0;
			int pathMin = int.MaxValue;
			List<Point> tmpPath;
			// trouver le plus proche MineNeutral
			foreach (Pos tmpPos in AllOtherMine)
				{
				// start position est le position de MyHero
				Point startPt = new Point(serverStuff.myHero.pos.x, serverStuff.myHero.pos.y);
				// trouver le position FREE au tours du Mine
				Point endPt = new Point(tmpPos.x, tmpPos.y);
				// chercher le plus court chemin de tous les mines
				tmpPath = aStar.FindPath(startPt, endPt);
				// tous les path à MineNeutral
				path.Add(tmpPath);
				// garder l'index du plus court chemin
				if (tmpPath.Count <= pathMin)
					{
					pathMin = tmpPath.Count;
					indexMin = path.Count - 1;
					}
				}
			return indexMin;
			}

		/// <summary>
		/// bouger à le plus proche Tavern
		/// </summary>
		/// <param name="path">ensemble de path à tous les Taverns</param>
		/// <returns>index de plus proche Tavern dans List path</returns>
		private int moveToNearestTavern(List<List<Point>> path, List<Pos> TavernPos)
			{
			int indexMin = 0;
			int pathMin = int.MaxValue;
			List<Point> tmpPath;
			// trouver le plus proche Tavern
			foreach (Pos tmpPos in TavernPos)
				{
				// start position est le position de MyHero
				Point startPt = new Point(serverStuff.myHero.pos.x, serverStuff.myHero.pos.y);
				// trouver le position FREE au tours du Tavern
				Point endPt = new Point(tmpPos.x, tmpPos.y);
				// chercher le plus court chemin de tous les Taverns
				tmpPath = aStar.FindPath(startPt, endPt);
				// tous les path à Taverns
				path.Add(tmpPath);
				// garder l'index du plus court chemin
				if (tmpPath.Count <= pathMin)
					{
					pathMin = tmpPath.Count;
					indexMin = path.Count - 1;
					}
				}
			return indexMin;
			}

		/// <summary>
		/// Mon hero a moins de life que l'autre qui est proche de moi
		/// </summary>
		/// <returns></returns>
		private bool isClosestHero()
			{
			List<List<Point>> path = new List<List<Point>>();
			List<Pos> HeroPos = new List<Pos>();
			List<Point> tmpPathToHero;
			int indexMin = 0;
			int pathMin = int.MaxValue;
			for (int i = 0 ; i < 4 ; i++)
				{
				if (i + 1 != serverStuff.myHero.id)
					{
					HeroPos.Add(new Pos(serverStuff.heroes[i].pos.x, serverStuff.heroes[i].pos.y));
					}
				}

			foreach (Pos tmpPos in HeroPos)
				{
				// start position est le position de MyHero
				Point startPt = new Point(serverStuff.myHero.pos.x, serverStuff.myHero.pos.y);
				// trouver le position FREE au tours du Tavern
				Point endPt = new Point(tmpPos.x, tmpPos.y);
				// chercher le plus court chemin de tous les Taverns
				tmpPathToHero = aStar.FindPath(startPt, endPt);
				// tous les path à Taverns
				path.Add(tmpPathToHero);
				// garder l'index du plus court chemin
				if (tmpPathToHero.Count <= pathMin)
					{
					pathMin = tmpPathToHero.Count;
					indexMin = path.Count - 1;
					}
				}
			// s'il a hero proche de moi à distance 2
			if (path[indexMin].Count <= 2)
				{
				return true;
				}
			else
				{
				return false;
				}
			}

		/// <summary>
		/// trouver Hero qui a le plus Mine
		/// </summary>
		/// <returns>id de hero avec plus de mine </returns>
		private int hasMostMine()
			{
			int[] countMine = {serverStuff.heroes[0].mineCount, serverStuff.heroes[1].mineCount, 
								serverStuff.heroes[2].mineCount, serverStuff.heroes[3].mineCount};
			int max = countMine.Max();
			// Positioning max
			int index = Array.IndexOf(countMine, max);
			return index;
			}

		/// <summary>
		/// quel hero a le moins life
		/// </summary>
		/// <returns> id de hero</returns>
		private int hasLessLife()
			{
			int[] countLift = {serverStuff.heroes[0].life, serverStuff.heroes[1].life, 
								serverStuff.heroes[2].life, serverStuff.heroes[3].life};
			int min = countLift.Min();
			int index = Array.IndexOf(countLift, min);
			return index + 1;
			}

		/// <summary>
		/// bouger à Hero
		/// </summary>
		/// <param name="id"> id de Hero</param>
		/// <returns></returns>
		private int moveToHero(List<List<Point>> path, int id)
			{
			// start position est le position de MyHero
			Point startPt = new Point(serverStuff.myHero.pos.x, serverStuff.myHero.pos.y);
			// trouver le position de Hero avec id
			Point endPt = new Point(serverStuff.heroes[id].pos.x, serverStuff.heroes[id].pos.y);
			path.Add(aStar.FindPath(startPt, endPt));
			return 0;
			}
		}
	}
