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
    public class Coat : WECommand
    {
		private int coat;
		private Expression expression;

		public Coat(int x, int y, int x2, int y2, MagicWand magicWand, TSPlayer plr, int coat, Expression expression)
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
							if (tile.active() && tile.fullbrightBlock())
							{
								if (select(i, j, plr) && expression.Evaluate(tile, i, j) && magicWand.InSelection(i, j))
								{
									tile.fullbrightBlock(false);
									edits++;
								}
							}
							if (tile.active() && tile.invisibleBlock())
							{
								if (select(i, j, plr) && expression.Evaluate(tile, i, j) && magicWand.InSelection(i, j))
								{
									tile.invisibleBlock(false);
									edits++;
								}
							}
							break;
						case 1:
							if (tile.active() && !tile.fullbrightBlock() && select(i, j, plr) && expression.Evaluate(tile, i, j) && magicWand.InSelection(i, j))
							{
								tile.fullbrightBlock(true);
								edits++;
							}
							break;
						case 2:
							if (tile.active() && !tile.invisibleBlock() && select(i, j, plr) && expression.Evaluate(tile, i, j) && magicWand.InSelection(i, j))
                            {
								tile.invisibleBlock(true);
								edits++;
                            }
							break;
					}
				}
			}
			ResetSection();
			plr.SendSuccessMessage("Coated tiles. ({0})", edits);
		}
	}
}
