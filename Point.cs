using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vindinium
	{

	class Point
		{
		int x;
		int y;
		public Point(){}

		public Point(int x, int y)
			{
			this.x = x;
			this.y = y;
			}

		public int X
			{
			get { return x; }
			set { x = value; }
			}

		public int Y
			{
			get { return y; }
			set { y = value; }
			}
		}
	}
