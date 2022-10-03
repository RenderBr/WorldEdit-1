﻿using Terraria.ID;

namespace WorldEdit.Biomes
{
    public class Hell : Biome
    {
        public override int Dirt => TileID.Ash;
        public override int[] Grass => new int[] { TileID.Ash };
        public override int Stone => TileID.Hellstone;
        public override int Ice => TileID.Hellstone;
        public override int Clay => TileID.Ash;
        public override int Sand => TileID.Silt;
        public override int HardenedSand => TileID.Ash;
        public override int Sandstone => TileID.Hellstone;
        public override int Plants => -1;
        public override int TallPlants => -1;
        public override int Vines => -1;
        public override int Thorn => -1;

        public override ushort DirtWall => 0;
        public override ushort DirtWallUnsafe => 0;
        public override ushort CaveWall => 0;
        public override ushort DirtWallUnsafe1 => 0;
        public override ushort DirtWallUnsafe2 => 0;
        public override ushort DirtWallUnsafe3 => 0;
        public override ushort DirtWallUnsafe4 => 0;
        public override ushort StoneWall => 0;
        public override ushort HardenedSandWall => 0;
        public override ushort SandstoneWall => 0;
        public override ushort GrassWall => 0;
        public override ushort GrassWallUnsafe => 0;
        public override ushort FlowerWall => 0;
        public override ushort FlowerWallUnsafe => 0;

        public override ushort CaveWall1 => 0;
        public override ushort CaveWall2 => 0;
        public override ushort CaveWall3 => 0;
        public override ushort CaveWall4 => 0;
    }
}