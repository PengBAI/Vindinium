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

		/// <summary>
		/// trouver les mine GOLD_MINE_NEUTRAL, GOLD_MINE_2, GOLD_MINE_3, GOLD_MINE_4
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
			for (int x = 0 ; x < size ; x++)
				{
				for (int y = 0 ; y < size ; y++)
					{
					switch (serverStuff.board[x][y])
						{
						case Tile.GOLD_MINE_2:
							AllOtherMine.Add(new Pos(x, y));
							break;
						case Tile.GOLD_MINE_3:
							AllOtherMine.Add(new Pos(x, y));
							break;
						case Tile.GOLD_MINE_4:
							AllOtherMine.Add(new Pos(x, y));
							break;
						case Tile.GOLD_MINE_NEUTRAL:
							AllOtherMine.Add(new Pos(x, y));
							break;
						case Tile.TAVERN:
							TavernPos.Add(new Pos(x, y));
							break;
						default:
							break;
						}
					}
				}
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
			//                        begin stratégies                                     //
			/////////////////////////////////////////////////////////////////////////////////
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
						int idHero = hasMostMine();
						if (idHero != serverStuff.myHero.id && serverStuff.myHero.life > (serverStuff.heroes[idHero].life + 20))
							{
							indexMin = moveToHero(path, idHero);
							if (path[indexMin].Count > 2)
								{
								// aller au MineNeutral
								path.Clear();
								indexMin = moveToNearestAllOtherMine(path, AllOtherMine);
								}
							}
						else
							{
							// aller au MineNeutral
							indexMin = moveToNearestAllOtherMine(path, AllOtherMine);
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
				path.Clear();
				AllOtherMine.Clear();
				TavernPos.Clear();
				Console.Out.WriteLine("completed turn " + serverStuff.currentTurn);
				}
			/////////////////////////////////////////////////////////////////////////////////
			//                         end  stratégies                                     //
			/////////////////////////////////////////////////////////////////////////////////

			if (serverStuff.errored)
				{
				Console.Out.WriteLine("error: " + serverStuff.errorText);
				}
			
			Console.Out.WriteLine("MyAIBot bot finished");
			}

		/// <summary>
		/// bouger à le plus proche OtherMine
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
		/// 
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
		/// 
		/// </summary>
		/// <param name="id"></param>
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
