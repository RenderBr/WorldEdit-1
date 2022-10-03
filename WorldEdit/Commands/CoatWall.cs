using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit.Commands
{
    public class CoatWall : WECommand
    {
		private int coat;
		private Expression expression;

		public CoatWall(int x, int y, int x2, int y2, MagicWand magicWand, TSPlayer plr, int coat, Expression expression)
			: base(x, y, x2, y2, magicWand, plr)
		{
			this.coat = coat;
			this.expression = expression ?? new TestExpression(new Test((t, h, k) => true));
		}

		public override void Execute()
		{
			if (!CanUseCommand()) { return; }
			Clipboard.PrepareUndo(x, y, x2, y2, plr);
			int edits = 0;
			for (int i = x; i <= x2; i++)
			{
				for (int j = y; j <= y2; j++)
				{
					var tile = Main.tile[i, j];

					switch (coat)
					{
						case 0:
							if (tile.wall > 0 && tile.fullbrightWall())
                            {
								if (select(i, j, plr) && expression.Evaluate(tile, i, j) && magicWand.InSelection(i, j))
								{
									tile.fullbrightWall(false);
									edits++;
								}
                            }
							if (tile.wall > 0 && tile.invisibleWall())
                            {
								if (select(i, j, plr) && expression.Evaluate(tile, i, j) && magicWand.InSelection(i, j))
								{
									tile.invisibleWall(false);
									edits++;
								}
                            }
							break;
						case 1:
							if (tile.wall > 0 && !tile.fullbrightWall() && select(i, j, plr) && expression.Evaluate(tile, i, j) && magicWand.InSelection(i, j))
							{
								tile.fullbrightWall(true);
								edits++;
							}
							break;
						case 2:
							if (tile.wall > 0 && !tile.invisibleWall() && select(i, j, plr) && expression.Evaluate(tile, i, j) && magicWand.InSelection(i, j))
							{
								tile.invisibleWall(true);
								edits++;
							}
							break;
					}
				}
			}
			ResetSection();
			plr.SendSuccessMessage("Coated walls. ({0})", edits);
		}
	}
}
