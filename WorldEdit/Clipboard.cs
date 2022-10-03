using Auxiliary.Configuration;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;
using WorldEdit.Configuration;
using WorldEdit.Entities;

namespace WorldEdit
{
    public static class Clipboard
    {
        public static void PrepareUndo(int x, int y, int x2, int y2, TSPlayer plr)
        {
            if (!plr.RealPlayer)
                return;

            var entity = EditsEntity.GetAsync(plr.Account.ID).GetAwaiter().GetResult();

            entity.UndoLevel += 1;
            entity.RedoLevel = 0;

            // get undo path.
            string format = string.Format("undo-{0}-{1}-{2}.dat", Main.worldID, plr.Account.ID, entity.UndoLevel);
            string path = Path.Combine(WorldEdit.DefaultDirectory, format);
            
            // save section to path.
            Tools.SaveWorldSection(x, y, x2, y2, path);

            // delete all redo files
            foreach (string fileName in Directory.EnumerateFiles(WorldEdit.DefaultDirectory,
                string.Format("redo-{0}-{1}-*.dat", Main.worldID, plr.Account.ID)))
            {
                File.Delete(fileName);
            }

            // delete the last file
            File.Delete(Path.Combine(WorldEdit.DefaultDirectory, 
                string.Format("undo-{0}-{1}-{2}.dat", Main.worldID, plr.Account.ID, entity.UndoLevel - Configuration<WorldEditSettings>.Settings.DefaultUndoAmount)));
        }

        public static bool Undo(int id)
        {
            if (id <= 0)
                return false;

            var entity = EditsEntity.GetAsync(id).GetAwaiter().GetResult();

            var ul = entity.UndoLevel;
            var rl = entity.RedoLevel;

            rl += 1;
            ul -= 1;

            if (ul < 0)
                return false;

            var undoformat = string.Format("undo-{0}-{1}-{2}.dat", Main.worldID, id, entity.UndoLevel);
            var up = Path.Combine("worldedit", undoformat);
            entity.UndoLevel = ul;

            if (!File.Exists(up))
                return false;

            var redoformat = string.Format("redo-{0}-{1}-{2}.dat", Main.worldID, id, rl);
            var rp = Path.Combine("worldedit", redoformat);
            entity.RedoLevel = rl;

            Rectangle size = ReadSize(up);

            Tools.SaveWorldSection(Math.Max(0, size.X), Math.Max(0, size.Y),
                Math.Min(size.X + size.Width - 1, Main.maxTilesX - 1),
                Math.Min(size.Y + size.Height - 1, Main.maxTilesY - 1), rp);

            Tools.LoadWorldSection(up);

            File.Delete(up);
            return true;
        }

        public static bool Redo(int id)
        {
            if (id <= 0)
                return false;

            var entity = EditsEntity.GetAsync(id).GetAwaiter().GetResult();

            var ul = entity.UndoLevel;
            var rl = entity.RedoLevel;

            rl -= 1;
            ul += 1;

            if (rl < 0)
                return false;

            var redoformat = string.Format("redo-{0}-{1}-{2}.dat", Main.worldID, id, entity.RedoLevel);
            var rp = Path.Combine("worldedit", redoformat);
            entity.RedoLevel = rl;

            if (!File.Exists(rp))
                return false;

            var undoformat = string.Format("undo-{0}-{1}-{2}.dat", Main.worldID, id, ul);
            var up = Path.Combine("worldedit", undoformat);
            entity.UndoLevel = ul;

            Rectangle size = ReadSize(rp);

            Tools.SaveWorldSection(Math.Max(0, size.X), Math.Max(0, size.Y),
                Math.Min(size.X + size.Width - 1, Main.maxTilesX - 1),
                Math.Min(size.Y + size.Height - 1, Main.maxTilesY - 1), up);
            Tools.LoadWorldSection(rp);

            File.Delete(rp);
            return true;
        }

        public static Rectangle ReadSize(string path)
            => ReadSize(File.Open(path, FileMode.Open));

        public static Rectangle ReadSize(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                return new Rectangle
                (
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32()
                );
            }
        }
    }
}
