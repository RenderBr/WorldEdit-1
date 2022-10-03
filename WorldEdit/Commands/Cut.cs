using System.IO;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using WorldEdit.Entities;

namespace WorldEdit.Commands
{
	public class Cut : WECommand
	{
		public Cut(int x, int y, int x2, int y2, TSPlayer plr)
			: base(x, y, x2, y2, plr)
		{
		}

		public override void Execute()
		{
			if (!CanUseCommand()) { return; }

			foreach (string fileName in Directory.EnumerateFiles(WorldEdit.DefaultDirectory, string.Format("redo-{0}-{1}-*.dat", Main.worldID, plr.Account.ID)))
				File.Delete(fileName);

			var entity = EditsEntity.GetAsync(plr.Account.ID)
				.GetAwaiter()
				.GetResult();

			int undoLevel = ++entity.UndoLevel;

			entity.RedoLevel = -1;
			
			string clipboard = Tools.GetClipboardPath(plr.Account.ID);

			string undoPath = Path.Combine(WorldEdit.DefaultDirectory, string.Format("undo-{0}-{1}-{2}.dat", Main.worldID, plr.Account.ID, undoLevel));

			Tools.SaveWorldSection(x, y, x2, y2, undoPath);
            Tools.ClearObjects(x, y, x2, y2);

			for (int i = x; i <= x2; i++)
			{
				for (int j = y; j <= y2; j++)
				{ Main.tile[i, j] = new Tile(); }
			}

			if (File.Exists(clipboard)) 
				File.Delete(clipboard);

			File.Copy(undoPath, clipboard);

			ResetSection();
			plr.SendSuccessMessage("Cut selection. ({0})", (x2 - x + 1) * (y2 - y + 1));
		}
	}
}
