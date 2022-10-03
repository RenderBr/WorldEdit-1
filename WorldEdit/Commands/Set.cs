﻿using Terraria;
using TShockAPI;
using WorldEdit.Expressions;

namespace WorldEdit.Commands
{
	public class Set : WECommand
	{
		private Expression expression;
		private int tileType;

		public Set(int x, int y, int x2, int y2, MagicWand magicWand, TSPlayer plr, int tileType, Expression expression)
			: base(x, y, x2, y2, magicWand, plr)
		{
			this.tileType = tileType;
			this.expression = expression ?? new TestExpression(new Test((t, h, k) => true));
		}

		public override void Execute()
        {
            if (!CanUseCommand()) 
				return;

			Clipboard.PrepareUndo(x, y, x2, y2, plr);

			int edits = 0;

			for (int i = x; i <= x2; i++)
			{
				for (int j = y; j <= y2; j++)
				{
					if (Tools.CanSet(true, Main.tile[i, j], tileType,
                        select, expression, magicWand, i, j, plr))
					{
						SetTile(i, j, tileType);
						edits++;
					}
				}
			}

			ResetSection();
			plr.SendSuccessMessage("Set tiles. ({0})", edits);
		}
	}
}