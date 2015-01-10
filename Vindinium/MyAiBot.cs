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
        private int lastTurnMyLife = 0; // MyHero life dans le dernier turn
        private int lastTurnMine = 0;  // le nombre de mon mine dans le dernier turn
        private int lastHeroId = 0;
        private bool killHero = true; // true -> HERO ou false -> MINE
        private int killStep = 0;  // Count les steps de suivre Heros
        private Pos lastPos = null; // postion de Mon Hero de dernier turn
        private int postionCount = 0;

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
            List<Pos> TavernPos = new List<Pos>(4);
            List<Pos> AllOtherMine = new List<Pos>();
            // un ensemble de paths à destinations
            List<List<Point>> path = new List<List<Point>>();

            // option à tuer Heros, si toujours le suivre mais ne peut pas arriver à tuer
            if (serverStuff.myHero.mineCount != lastTurnMine)
            {
                killHero = true;
                killStep = 0;
            }
            // ne tuer pas Hero qui ne peut pas être tué, et aller au Mine
            if (killStep > serverStuff.board.Length / 3)
            {
                killHero = false;
            }

            while (serverStuff.finished == false && serverStuff.errored == false)
            {

                aStar = new AStar(serverStuff.board, serverStuff);
                // touver les positions Mine et Tavern
                findAllPos(TavernPos, AllOtherMine);
                // prendre cet index d'ensemble de path comme décision, index avec minimum longeur par défault
                int indexPath = 0;

                if (AllOtherMine.Count < serverStuff.myHero.mineCount && serverStuff.myHero.mineCount > 4)
                {
                    // si tous les mine à moi, je reste à Tavern
                    indexPath = moveToNearestTavern(path, TavernPos);
                }
                else
                {
                    if (serverStuff.myHero.life > 35)
                    {
                        // tuer hero qui a le plus mine
                        int idHeroMostMine = hasMostMine();
                        if (idHeroMostMine != serverStuff.myHero.id && serverStuff.myHero.life > 5 + serverStuff.heroes[idHeroMostMine - 1].life)
                        {

                            indexPath = moveToHero(path, idHeroMostMine);

                            if (path[indexPath].Count > (serverStuff.board.Length / 3 > 6 ? 6 : serverStuff.board.Length / 3))
                            {

                                // aller au MineNeutral
                                path.Clear();
                                // hero proche de moi dans x length distances
                                int idHeroNear = isClosestHero(serverStuff.board.Length / 4 < 4 ? serverStuff.board.Length / 4 : 4);
                                if (idHeroNear != 0)
                                {
                                    if (serverStuff.heroes[idHeroNear - 1].life + 10 < serverStuff.myHero.life)
                                    {
                                        // même Hero que turn dernier
                                        // si on ne peut pas suivre le Hero, arrêter le tuer
                                        if (lastHeroId == idHeroNear)
                                        {
                                            killStep++;
                                        }
                                        else
                                        {
                                            killHero = true;
                                            killStep = 0;
                                        }

                                        // tuer le hero
                                        if (killHero)
                                        {
                                            if (serverStuff.heroes[idHeroNear - 1].crashed == false)
                                            {
                                                indexPath = moveToHero(path, idHeroNear);
                                                // il y a un Mine plus proche, aller au Mine
                                                if (killStep > 3 && IsMineNearBy(path[indexPath].Count))
                                                {
                                                    path.Clear();
                                                    // aller au MineNeutral
                                                    indexPath = moveToNearestAllOtherMine(path, AllOtherMine);
                                                    killHero = true;
                                                    killStep = 0;
                                                }
                                                lastHeroId = idHeroNear;
                                            }
                                            else
                                            {
                                                // aller au MineNeutral
                                                indexPath = moveToNearestAllOtherMine(path, AllOtherMine);
                                            }
                                        }
                                        else
                                        {
                                            indexPath = moveToNearestAllOtherMine(path, AllOtherMine);
                                        }
                                    }
                                    else
                                    {
                                        // aller au Tavern proche mon place origine
                                        indexPath = moveToNearestTavern(path, TavernPos);
                                    }
                                }
                                else
                                {
                                    // a1ller au MineNeutral
                                    indexPath = moveToNearestAllOtherMine(path, AllOtherMine);
                                }
                            }
                        }
                        else
                        {
                            // hero proche de moi dans 4 length distances
                            int idHeroNear = isClosestHero(serverStuff.board.Length / 4 < 4 ? serverStuff.board.Length / 4 : 4);
                            if (idHeroNear != 0)
                            {
                                if (serverStuff.heroes[idHeroNear - 1].life + 10 < serverStuff.myHero.life)
                                {
                                    // même Hero que turn dernier
                                    // si on ne peut pas suivre le Hero, arrêter le tuer
                                    if (lastHeroId == idHeroNear)
                                    {
                                        killStep++;
                                    }
                                    else
                                    {
                                        killHero = true;
                                        killStep = 0;
                                    }

                                    // tuer le hero
                                    if (killHero)
                                    {
                                        if (serverStuff.heroes[idHeroNear - 1].crashed == false)
                                        {
                                            indexPath = moveToHero(path, idHeroNear);
                                            // il y a un Mine plus proche, aller au Mine
                                            if (killStep > 3 && IsMineNearBy(path[indexPath].Count))
                                            {
                                                path.Clear();
                                                // aller au MineNeutral
                                                indexPath = moveToNearestAllOtherMine(path, AllOtherMine);
                                                killHero = true;
                                                killStep = 0;
                                            }
                                            lastHeroId = idHeroNear;
                                        }
                                        else
                                        {
                                            // aller au MineNeutral
                                            indexPath = moveToNearestAllOtherMine(path, AllOtherMine);
                                        }
                                    }
                                    else
                                    {
                                        indexPath = moveToNearestAllOtherMine(path, AllOtherMine);
                                    }
                                }
                                else
                                {
                                    // aller au Tavern proche mon place origine
                                    indexPath = moveToNearestTavern(path, TavernPos);
                                }
                            }
                            else
                            {
                                // a1ller au MineNeutral
                                indexPath = moveToNearestAllOtherMine(path, AllOtherMine);
                            }
                        }
                    }
                    else
                    {
                        // aller au Tavern
                        indexPath = moveToNearestTavern(path, TavernPos);
                    }
                }

                // boire 2 fois bière 
                if (serverStuff.myHero.life - lastTurnMyLife == 49 && serverStuff.myHero.life < 85)
                {
                    path.Clear();
                    // aller au Tavern
                    indexPath = moveToNearestTavern(path, TavernPos);
                }
                lastTurnMyLife = serverStuff.myHero.life;

                // Mon Hero ne bourge pas après 4 turns
                if ( lastPos == serverStuff.myHero.pos)   //lastPos.x == serverStuff.myHero.pos.x && lastPos.y == serverStuff.myHero.pos.y)
                {
                    postionCount++;
                }
                else
                {
                    postionCount = 0;
                }
                if (postionCount > 4 && (AllOtherMine.Count > serverStuff.myHero.mineCount || serverStuff.myHero.mineCount < 4))
                {
                    path.Clear();
                    // si tous les mine à moi, je reste à Tavern
                    indexPath = moveToNearestTavern(path, TavernPos);
                    postionCount = 0;
                }
                lastPos = serverStuff.myHero.pos;

                // s'il y a pas de path disponible, changer un autre
                if (path[indexPath].Count == 0)
                {
                    int tmpCount = int.MaxValue;
                    for (int i = 0; i < path.Count; i++)
                    {
                        if (path[i].Count != 0 && path[i].Count < tmpCount)
                        {
                            indexPath = i;
                            tmpCount = path[i].Count;
                        }
                    }
                }

                // si pas de path 
                if (path[indexPath].Count == 0)
                {
                    path.Clear();
                    // aller au Tavern
                    indexPath = moveToNearestTavern(path, TavernPos);
                    if (path[indexPath].Count == 0)
                    {
                        path.Clear();
                        // aller au hero
                        int idHeroProche = isClosestHero(serverStuff.board.Length);
                        if (idHeroProche != 0)
                        {
                            indexPath = moveToHero(path, idHeroProche);
                        }
                        else
                        {
                            path.Add(new List<Point>());
                            indexPath = 0;
                        }
                    }
                }

                lastTurnMine = serverStuff.myHero.mineCount;
                // bouger à destination
                if (path[indexPath].Count == 0)
                {
                    serverStuff.moveHero(Direction.Stay);
                }
                else
                {
                    int deltaX = path[indexPath][0].X - serverStuff.myHero.pos.x;
                    int deltaY = path[indexPath][0].Y - serverStuff.myHero.pos.y;
                    if (deltaX == -1 && deltaY == 0)
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
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
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
        /// <returns>si existe return id de hero, sinon 0</returns>
        private int isClosestHero(int distance)
        {
            List<Pos> HeroPos = new List<Pos>();
            List<Point> tmpPathToHero;
            //int indexMin = 0;
            int indexHero = -1;
            int pathMin = int.MaxValue;

            for (int i = 0; i < 4; i++)
            {
                if (i + 1 != serverStuff.myHero.id)// && serverStuff.heroes[i].crashed == false)
                {
                    // start position est le position de MyHero
                    Point startPt = new Point(serverStuff.myHero.pos.x, serverStuff.myHero.pos.y);
                    // trouver le position FREE au tours du Tavern
                    Point endPt = new Point(serverStuff.heroes[i].pos.x, serverStuff.heroes[i].pos.y);
                    // chercher le plus court chemin de tous les Taverns
                    tmpPathToHero = aStar.FindPath(startPt, endPt);
                    // garder l'index du plus court chemin
                    if (tmpPathToHero.Count < pathMin)
                    {
                        pathMin = tmpPathToHero.Count;
                        if (pathMin <= distance)
                        {
                            indexHero = i;
                        }
                    }
                }
            }
            return indexHero + 1;
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
            return index + 1;
        }

        /// <summary>
        /// quel hero a le moins life
        /// </summary>
        /// <returns> id de hero</returns>
        private int hasLowerestLife()
        {
            int[] countLife = {serverStuff.heroes[0].life, serverStuff.heroes[1].life, 
								serverStuff.heroes[2].life, serverStuff.heroes[3].life};
            int min = countLife.Min();
            int index = Array.IndexOf(countLife, min);
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
            Point endPt = new Point(serverStuff.heroes[id - 1].pos.x, serverStuff.heroes[id - 1].pos.y);
            path.Add(aStar.FindPath(startPt, endPt));
            return 0;
        }

        /// <summary>
        /// Est-ce qu'il y a un Mine dans une distance donnée
        /// </summary>
        /// <param name="distance">nombre de distance</param>
        /// <returns></returns>
        private bool IsMineNearBy(int distance)
        {
            int maxIndex = serverStuff.board.Length - 1;
            int xMin = serverStuff.myHero.pos.x - distance;
            xMin = xMin < 0 ? 0 : xMin;
            int xMax = xMin + 2 * distance;
            xMax = xMax > maxIndex ? maxIndex : xMax;
            int yMin = serverStuff.myHero.pos.y - distance;
            yMin = yMin < 0 ? 0 : yMin;
            int yMax = yMin + 2 * distance;
            yMax = yMax > maxIndex ? maxIndex : yMax;
            int idMyHero = serverStuff.myHero.id;

            for (int i = xMin; i < xMax; i++)
            {
                for (int j = yMin; j < yMax; j++)
                {
                    switch (serverStuff.board[i][j])
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
                            return true;
                    }
                }
            }
            return false;
        }

    }
}