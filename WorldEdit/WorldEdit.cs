﻿using Auxiliary.Configuration;
using Microsoft.Xna.Framework;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Utilities;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;
using WorldEdit.Biomes;
using WorldEdit.Commands;
using WorldEdit.Configuration;
using WorldEdit.Expressions;
using WorldEdit.Extensions;

namespace WorldEdit
{
	public delegate bool Selection(int i, int j, TSPlayer player);

    [ApiVersion(2, 1)]
	public class WorldEdit : TerrariaPlugin
    {
		private readonly CancellationTokenSource _cancel = new();
		private readonly BlockingCollection<WECommand> _commandQueue = new();

		public const string DefaultDirectory = "worldedit";

		public static readonly HandlerCollection<CanEditEventArgs> CanEdit;

		/// <summary>
		///		A list of biome names to their respective types.
		/// </summary>
		public static Dictionary<string, Biomes.Biome> Biomes { get; private set; } = new();

		/// <summary>
		///		A list of color names to their respective types.
		/// </summary>
		public static Dictionary<string, int> Colors { get; private set; } = new();

		/// <summary>
		///		A list of coat names to their respective types.
		/// </summary>
		public static Dictionary<string, int> Coats { get; private set; } = new();

		/// <summary>
		///		A list of selection names to their respective types.
		/// </summary>
		public static Dictionary<string, Selection> Selections { get; private set; } = new();

		/// <summary>
		///		A list of tile names to their respective types.
		/// </summary>
		public static Dictionary<string, int> Tiles { get; private set; } = new();

		/// <summary>
		///		A list of wall names to their respective types.
		/// </summary>
		public static Dictionary<string, int> Walls { get; private set; } = new();

		/// <summary>
		///		A list of slope names to their respective types.
		/// </summary>
		public static Dictionary<string, int> Slopes { get; private set; } = new();

		public override string Author
			=> "TBC Developers";

		public override string Description
			=> "Adds commands for mass editing of blocks. Documentation at: https://github.com/Rozen4334/WorldEdit/wiki";

		public override string Name
			=> "WorldEdit";

		public override Version Version
			=> new(1, 0);

		static WorldEdit()
		{
			CanEdit = (Activator.CreateInstance(typeof(HandlerCollection<CanEditEventArgs>),
				BindingFlags.Instance | BindingFlags.NonPublic,
				null, new object[] { "CanEditHook" }, null) as HandlerCollection<CanEditEventArgs>)!;
		}

		public WorldEdit(Main game) 
			: base(game)
		{
			Order = 1;
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.NetGetData.Register(this, OnGetData);

			Configuration<WorldEditSettings>.Load("WorldEdit");

			if (!Directory.Exists(Configuration<WorldEditSettings>.Settings.SchematicDirectory))
				Directory.CreateDirectory(TShock.SavePath);

			GeneralHooks.ReloadEvent += (x) =>
			{
				Configuration<WorldEditSettings>.Load("WorldEdit");

				Tools.MaxUndos = Configuration<WorldEditSettings>.Settings.DefaultUndoAmount;
				MagicWand.MaxPointCount = Configuration<WorldEditSettings>.Settings.MagicWandTileLimit;

				x.Player.SendSuccessMessage("[WorldEdit] Reloaded configuration.");
			};
		} 

		private void OnInitialize(EventArgs e)
		{
			if (!Directory.Exists(DefaultDirectory))
				Directory.CreateDirectory(DefaultDirectory);

			#region Commands

			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.documentation", Documentation, "/edithelp", "/wedithelp")
			{
				HelpText = "Displays link to the documentation page."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.utils.activate", Activate, "/activate")
			{
				HelpText = "Activates non-working signs, chests or item frames."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.selection.all", All, "/all")
			{
				HelpText = "Sets the worldedit selection to the entire world."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.biome", Biome, "/biome")
			{
				HelpText = "Converts biomes in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.clipboard.copy", Copy, "/copy", "/c")
			{
				HelpText = "Copies the worldedit selection to the clipboard."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.clipboard.cut", Cut, "/cut")
			{
				HelpText = "Copies the worldedit selection to the clipboard, then deletes it."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.utils.drain", Drain, "/drain")
			{
				HelpText = "Drains liquids in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.fill", Fill, "/fill")
			{
				HelpText = "Fills the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.fillwall", FillWall, "/fillwall", "/fillw")
			{
				HelpText = "Fills the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.utils.fixghosts", FixGhosts, "/fixghosts")
			{
				HelpText = "Fixes invisible signs, chests and item frames."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.utils.fixgrass", FixGrass, "/fixgrass")
			{
				HelpText = "Fixes suffocated grass in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.utils.fixhalves", FixHalves, "/fixhalves")
			{
				HelpText = "Fixes half blocks in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.utils.fixslopes", FixSlopes, "/fixslopes")
			{
				HelpText = "Fixes covered slopes in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.clipboard.flip", Flip, "/flip")
			{
				HelpText = "Flips the worldedit clipboard."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.utils.flood", Flood, "/flood")
			{
				HelpText = "Floods liquids in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.magic.wand", MagicWandTool, "/magicwand", "/mwand")
			{
				HelpText = "Creates worldedit selection from contiguous tiles that are matching boolean expression."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.utils.killempty", KillEmpty, "/killempty")
			{
				HelpText = "Deletes empty signs and/or chests (only entities, doesn't remove tiles)."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.move", Move, "/move")
			{
				HelpText = "Moves tiles from the worldedit selection to new area."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.utils.mow", Mow, "/mow")
			{
				HelpText = "Mows grass, thorns, and vines in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.selection.near", Near, "/near")
			{
				AllowServer = false,
				HelpText = "Sets the worldedit selection to a radius around you."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.selection.outline", Outline, "/outline", "/ol")
			{
				HelpText = "Sets block outline around blocks in area."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.selection.outlinewall", OutlineWall, "/outlinewall", "/olw")
			{
				HelpText = "Sets wall outline around walls in area."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.paint", Paint, "/paint", "/pa")
			{
				HelpText = "Paints tiles in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.paintwall", PaintWall, "/paintwall", "/paw")
			{
				HelpText = "Paints walls in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.coat", Coat, "/coat")
			{
				HelpText = "Coats tiles in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.coatwall", CoatWall, "/coatwall")
			{
				HelpText = "Coats walls in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.clipboard.paste", Paste, "/paste", "/p")
			{
				HelpText = "Pastes the clipboard to the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.clipboard.spaste", SPaste, "/spaste", "/sp")
			{
				HelpText = "Pastes the clipboard to the worldedit selection with certain conditions."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.selection.point", Point1, "/point1", "/p1", "p1")
			{
				HelpText = "Sets the positions of the worldedit selection's first point."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.selection.point", Point2, "/point2", "/p2", "p2")
			{
				HelpText = "Sets the positions of the worldedit selection's second point."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.history.redo", Redo, "/redo")
			{
				HelpText = "Redoes a number of worldedit actions."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.selection.region", RegionCmd, "/region", "/reg")
			{
				HelpText = "Selects a region as a worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.replace", Replace, "/replace", "/rep")
			{
				HelpText = "Replaces tiles in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.replacewall", ReplaceWall, "/replacewall", "/repw")
			{
				HelpText = "Replaces walls in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.selection.resize", Resize, "/resize", "/res")
			{
				HelpText = "Resizes the worldedit selection in a direction."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.clipboard.rotate", Rotate, "/rotate", "/rot")
			{
				HelpText = "Rotates the worldedit clipboard."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.schematic", Schematic, "/schematic", "/schem", "/sc", "sc")
			{
				HelpText = "Manages worldedit schematics."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.schematic.private", PrivateSchematic, "/private-schematic", "/priv-schem", "/psc", "psc")
			{
				HelpText = "Manages private worldedit schematics."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.selection.selecttype", Select, "/select", "/sel")
			{
				HelpText = "Sets the worldedit selection function."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.set", Set, "/set", "/s", "s")
			{
				HelpText = "Sets tiles in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.setgrass", SetGrass, "/setgrass", "/sg")
			{
				HelpText = "Sets certain grass in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.setwall", SetWall, "/setwall", "/sw", "sw")
			{
				HelpText = "Sets walls in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.setwire", SetWire, "/setwire", "/swire")
			{
				HelpText = "Sets wires in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.shape", Shape, "/shape")
			{
				HelpText = "Draws line/rectangle/ellipse/triangle in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.shape", Shape, "/shapefill", "/shapef")
			{
				HelpText = "Draws line/rectangle/ellipse/triangle in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.shape", Shape, "/shapewall", "/shapew")
			{
				HelpText = "Draws line/rectangle/ellipse/triangle in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.shape", Shape, "/shapewallfill", "/shapewf")
			{
				HelpText = "Draws line/rectangle/ellipse/triangle in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.utils.size", Size, "/size")
			{
				HelpText = "Shows size of clipboard or schematic."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.slope", Slope, "/slope", "/sl")
			{
				HelpText = "Slopes tiles in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.delslope", SlopeDelete, "/delslope", "/delslopes", "/dslope", "/dslopes")
			{
				HelpText = "Removes slopes in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.smooth", Smooth, "/smooth", "/sm")
			{
				HelpText = "Smooths blocks in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.selection.inactive", Inactive, "/inactive", "/ia")
			{
				HelpText = "Sets the inactive status in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.selection.shift", Shift, "/shift")
			{
				HelpText = "Shifts the worldedit selection in a direction."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.selection.text", Text, "/text")
			{
				HelpText = "Creates text with alphabet statues in the worldedit selection."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.history.undo", Undo, "/undo")
			{
				HelpText = "Undoes a number of worldedit actions."
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.clipboard.scale", Scale, "/scale")
			{
				HelpText = "Scale the clipboard"
			});
			TShockAPI.Commands.ChatCommands.Add(new Command("worldedit.region.actuator", Actuator, "/actuator")
			{
				HelpText = "Sets actuators in the worldedit selection."
			});
			#endregion

			#region Biomes
			Biomes.Add("crimson", new Crimson());
			Biomes.Add("corruption", new Corruption());
			Biomes.Add("hallow", new Hallow());
			Biomes.Add("jungle", new Jungle());
			Biomes.Add("mushroom", new Mushroom());
			Biomes.Add("normal", new Forest());
			Biomes.Add("forest", new Forest());
			Biomes.Add("snow", new Snow());
			Biomes.Add("ice", new Snow());
			Biomes.Add("desert", new Desert());
			Biomes.Add("sand", new Desert());
			Biomes.Add("hell", new Hell());
			#endregion

			#region Colors
			Colors.Add("blank", 0);
			Colors.Add("none", 0);

			Main.player[Main.myPlayer] = new Player();
			var item = new Item();
			for (var i = 1; i < Main.maxItemTypes; i++)
			{
				item.netDefaults(i);

				if (item.paint <= 0)
				{
					continue;
				}

				var name = TShockAPI.Localization.EnglishLanguage.GetItemNameById(i);
				Colors.Add(name.Substring(0, name.Length - 6).Replace(" ", "").ToLowerInvariant(), item.paint);
			}
			#endregion

			#region Coats
			Coats.Add("none", 0);
			Coats.Add("remove", 0);
			Coats.Add("fullbright", 1);
			Coats.Add("illuminant", 1);
			Coats.Add("invisible", 2);
			Coats.Add("echo", 2);
			#endregion

			#region Selections
			Selections.Add("altcheckers", (i, j, plr) => ((i + j) & 1) == 0);
			Selections.Add("checkers", (i, j, plr) => ((i + j) & 1) == 1);
			Selections.Add("ellipse", (i, j, plr) =>
			{
				PlayerInfo info = plr.GetPlayerInfo();
				return Tools.InEllipse(Math.Min(info.X, info.X2),
					Math.Min(info.Y, info.Y2), Math.Max(info.X, info.X2),
					Math.Max(info.Y, info.Y2), i, j);
			});
			Selections.Add("normal", (i, j, plr) => true);
			Selections.Add("border", (i, j, plr) =>
			{
				PlayerInfo info = plr.GetPlayerInfo();
				return i == info.X || i == info.X2 || j == info.Y || j == info.Y2;
			});
			Selections.Add("outline", (i, j, plr) =>
			{
				return ((i > 0) && (j > 0) && (i < Main.maxTilesX - 1) && (j < Main.maxTilesY - 1)
					&& (Main.tile[i, j].active())
					&& ((!Main.tile[i - 1, j].active()) || (!Main.tile[i, j - 1].active())
					|| (!Main.tile[i + 1, j].active()) || (!Main.tile[i, j + 1].active())
					|| (!Main.tile[i + 1, j + 1].active()) || (!Main.tile[i - 1, j - 1].active())
					|| (!Main.tile[i - 1, j + 1].active()) || (!Main.tile[i + 1, j - 1].active())));
			});
			Selections.Add("random", (i, j, plr) => Main.rand.NextDouble() >= 0.5);
			#endregion

			#region Tiles
			Tiles.Add("air", -1);
			Tiles.Add("lava", -2);
			Tiles.Add("honey", -3);
			Tiles.Add("shimmer", -4);
			Tiles.Add("water", -5);

			foreach (var fi in typeof(Terraria.ID.TileID).GetFields())
			{
				if (fi.FieldType != typeof(ushort) || !fi.IsLiteral || fi.Name == "Count")
				{
					continue;
				}

				string name = fi.Name;
				var sb = new StringBuilder();
				for (int i = 0; i < name.Length; i++)
				{
					if (char.IsUpper(name[i]))
						sb.Append(' ').Append(char.ToLower(name[i]));
					else
						sb.Append(name[i]);
				}
				Tiles.Add(sb.ToString(1, sb.Length - 1), (ushort)fi.GetValue(null)!);
			}
			#endregion

			#region Walls
			Walls.Add("air", 0);

			foreach (var fi in typeof(WallID).GetFields())
			{
				if (fi.FieldType != typeof(ushort) || !fi.IsLiteral || fi.Name == "None" || fi.Name == "Count")
				{
					continue;
				}

				string name = fi.Name;
				var sb = new StringBuilder();
				for (int i = 0; i < name.Length; i++)
				{
					if (char.IsUpper(name[i]))
						sb.Append(' ').Append(char.ToLower(name[i]));
					else
						sb.Append(name[i]);
				}
				Walls.Add(sb.ToString(1, sb.Length - 1), (ushort)fi.GetValue(null)!);
			}
			#endregion

			#region Slopes
			Slopes.Add("none", 0);
			Slopes.Add("t", 1);
			Slopes.Add("tr", 2);
			Slopes.Add("ur", 2);
			Slopes.Add("tl", 3);
			Slopes.Add("ul", 3);
			Slopes.Add("br", 4);
			Slopes.Add("dr", 4);
			Slopes.Add("bl", 5);
			Slopes.Add("dl", 5);
			#endregion

			ThreadPool.QueueUserWorkItem(QueueCallback!);
		}

		private void OnGetData(GetDataEventArgs e)
		{
			if (e.Handled)
				return;

			switch (e.MsgID)
			{
				#region Packet 17 - Tile

				case PacketTypes.Tile:
					PlayerInfo info = TShock.Players[e.Msg.whoAmI].GetPlayerInfo();
					if (info.Point != 0)
					{
                        using var reader = new BinaryReader(new MemoryStream(e.Msg.readBuffer, e.Index, e.Length));

                        reader.ReadByte();
                        int x = reader.ReadInt16();
                        int y = reader.ReadInt16();
                        if (x >= 0 && y >= 0 && x < Main.maxTilesX && y < Main.maxTilesY)
                        {
                            if (info.Point == 1)
                            {
                                info.X = x;
                                info.Y = y;
                                TShock.Players[e.Msg.whoAmI].SendInfoMessage("Set point 1.");
                            }
                            else if (info.Point == 2)
                            {
                                info.X2 = x;
                                info.Y2 = y;
                                TShock.Players[e.Msg.whoAmI].SendInfoMessage("Set point 2.");
                            }
                            else if (info.Point == 3)
                            {
                                List<string> regions = TShock.Regions.InAreaRegionName(x, y).ToList();
                                if (regions.Count == 0)
                                {
                                    TShock.Players[e.Msg.whoAmI].SendErrorMessage("No region exists there.");
                                }
                                else
                                {
                                    Region curReg = TShock.Regions.GetRegionByName(regions[0]);
                                    info.X = curReg.Area.Left;
                                    info.Y = curReg.Area.Top;
                                    info.X2 = curReg.Area.Right;
                                    info.Y2 = curReg.Area.Bottom;
                                    TShock.Players[e.Msg.whoAmI].SendInfoMessage("Set region.");
                                }
                            }
                            else if (info.Point == 4)
                            {
                                if (!MagicWand.GetMagicWandSelection(x, y,
                                    info.SavedExpression!,
                                    TShock.Players[e.Msg.whoAmI], out MagicWand selection))
                                {
                                    TShock.Players[e.Msg.whoAmI].SendErrorMessage("Can't " +
                                        "start counting magic wand selection from this tile.");
                                }
                                else
                                {
                                    info.MagicWand = selection;
                                    TShock.Players[e.Msg.whoAmI].SendSuccessMessage("Set magic wand selection.");
                                }
                                info.SavedExpression = null!;
                            }
                            info.Point = 0;
                            e.Handled = true;
                            TShock.Players[e.Msg.whoAmI].SendTileRect((short)x, (short)y, 3, 3);
                        }
                    }
					return;

				#endregion

				#region Packet 109 - MassWireOperation

				case PacketTypes.MassWireOperation:
					PlayerInfo data = TShock.Players[e.Msg.whoAmI].GetPlayerInfo();
					if (data.Point != 0)
					{
                        using var reader = new BinaryReader(new MemoryStream(e.Msg.readBuffer, e.Index, e.Length));

                        int startX = reader.ReadInt16();
                        int startY = reader.ReadInt16();
                        int endX = reader.ReadInt16();
                        int endY = reader.ReadInt16();

                        if (startX >= 0 && startY >= 0 && endX >= 0 && endY >= 0 && startX < Main.maxTilesX && startY < Main.maxTilesY && endX < Main.maxTilesX && endY < Main.maxTilesY)
                        {
                            if (data.Point == 4)
                            {
                                if (!MagicWand.GetMagicWandSelection(startX, startY,
                                    data.SavedExpression!,
                                    TShock.Players[e.Msg.whoAmI], out MagicWand selection))
                                {
                                    TShock.Players[e.Msg.whoAmI].SendErrorMessage("Can't " +
                                        "start counting magic wand selection from this tile.");
                                }
                                else
                                {
                                    data.MagicWand = selection;
                                    TShock.Players[e.Msg.whoAmI].SendSuccessMessage("Set magic wand selection.");
                                }
                                data.SavedExpression = null!;
                            }
                            else if (startX == endX && startY == endY)
                            {
                                // Set a single point
                                if (data.Point == 1)
                                {
                                    data.X = startX;
                                    data.Y = startY;
                                    TShock.Players[e.Msg.whoAmI].SendInfoMessage("Set point 1.");
                                }
                                else if (data.Point == 2)
                                {
                                    data.X2 = startX;
                                    data.Y2 = startY;
                                    TShock.Players[e.Msg.whoAmI].SendInfoMessage("Set point 2.");
                                }
                                else if (data.Point == 3)
                                {
                                    List<string> regions = TShock.Regions.InAreaRegionName(startX, startY).ToList();
                                    if (regions.Count == 0)
                                    {
                                        TShock.Players[e.Msg.whoAmI].SendErrorMessage("No region exists there.");
                                    }
                                    else
                                    {
                                        Region curReg = TShock.Regions.GetRegionByName(regions[0]);
                                        data.X = curReg.Area.Left;
                                        data.Y = curReg.Area.Top;
                                        data.X2 = curReg.Area.Right;
                                        data.Y2 = curReg.Area.Bottom;
                                        TShock.Players[e.Msg.whoAmI].SendInfoMessage("Set region.");
                                    }
                                }
                            }
                            else
                            {
                                // Set both points at the same time
                                if (data.Point == 1 || data.Point == 2)
                                {
                                    data.X = startX;
                                    data.Y = startY;
                                    data.X2 = endX;
                                    data.Y2 = endY;
                                    TShock.Players[e.Msg.whoAmI].SendInfoMessage("Set area.");
                                }
                                else if (data.Point == 3)
                                {
                                    // Set topmost region inside the selection
                                    int x = Math.Min(startX, endX);
                                    int y = Math.Min(startY, endY);
                                    int width = Math.Max(startX, endX) - x;
                                    int height = Math.Max(startY, endY) - y;

                                    Rectangle rect = new(x, y, width, height);

                                    List<Region> regions = TShock.Regions.Regions.FindAll(r => rect.Intersects(r.Area));
                                    if (regions.Count == 0)
                                    {
                                        TShock.Players[e.Msg.whoAmI].SendErrorMessage("No region exists there.");
                                    }
                                    else
                                    {
                                        Region curReg = TShock.Regions.GetTopRegion(regions);
                                        data.X = curReg.Area.Left;
                                        data.Y = curReg.Area.Top;
                                        data.X2 = curReg.Area.Right;
                                        data.Y2 = curReg.Area.Bottom;
                                        TShock.Players[e.Msg.whoAmI].SendInfoMessage("Set region.");
                                    }
                                }
                            }
                            data.Point = 0;
                            e.Handled = true;
                        }
                    }
					return;

					#endregion
			}
		}

		private void QueueCallback(object context)
		{
			while (!Netplay.Disconnect)
			{
				WECommand? command = null;
				try
				{
					if (!_commandQueue.TryTake(out command, -1, _cancel.Token))
						return;

					if (Main.rand == null)
						Main.rand = new UnifiedRandom();

					command.Position();
					command.Execute();
				}
				catch (OperationCanceledException)
				{
					return;
				}
				catch (Exception e)
				{
					TShock.Log.ConsoleError(e.ToString());
					TSPlayer? plr = command?.plr;

					if (plr?.Active ?? false)
						plr.SendErrorMessage("WorldEdit command failed, check logs for more details.");
				}
			}
		}

		private void Documentation(CommandArgs e)
		{
			if (e.Parameters.Count != 0)
				return;
			else e.Player.SendInfoMessage("You can find the worldedit documentation on: 'https://github.com/Rozen4334/WorldEdit/wiki'!");
		}

		private void Activate(CommandArgs e)
		{
			if (e.Parameters.Count != 1)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //activate <sign/chest/itemframe/sensor/dummy/all>");
				return;
			}

			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection.");
				return;
			}

			byte action;
			switch (e.Parameters[0].ToLowerInvariant())
			{
				case "s":
				case "sign":
					{
						action = 0;
						break;
					}
				case "c":
				case "chest":
					{
						action = 1;
						break;
					}
				case "i":
				case "item":
				case "frame":
				case "itemframe":
					{
						action = 2;
						break;
					}
				case "l":
				case "logic":
				case "sensor":
				case "logicsensor":
					{
						action = 3;
						break;
					}
				case "d":
				case "dummy":
				case "targetdummy":
					{
						action = 4;
						break;
					}
				case "a":
				case "all":
					{
						action = 255;
						break;
					}
				default:
					{
						e.Player.SendErrorMessage("Invalid activation type '{0}'.", e.Parameters[0]);
						return;
					}
			}

			_commandQueue.Add(new Activate(info.X, info.Y, info.X2, info.Y2, e.Player, action));
		}

		private void Actuator(CommandArgs e)
		{
			string param = (e.Parameters.Count == 0) ? "" : e.Parameters[0].ToLowerInvariant();
			if (param != "off" && param != "on")
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //actuator <on/off> [=> boolean expr...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection.");
				return;
			}
			bool remove = (param == "off");

			Expression? expression = null;
			if (e.Parameters.Count > 1)
			{
				if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
				{
					e.Player.SendErrorMessage("Invalid expression!");
					return;
				}
			}

			_commandQueue.Add(new Actuator(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, expression!, remove));
		}

		private void All(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			info.X = info.Y = 0;
			info.X2 = Main.maxTilesX - 1;
			info.Y2 = Main.maxTilesY - 1;
			e.Player.SendSuccessMessage("Selected all tiles.");
		}

		private void Biome(CommandArgs e)
		{
			if (e.Parameters.Count != 2)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //biome <biome 1> <biome 2>");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection.");
				return;
			}

			string biome1 = e.Parameters[0].ToLowerInvariant();
			string biome2 = e.Parameters[1].ToLowerInvariant();
			if (!Biomes.ContainsKey(biome1) || !Biomes.ContainsKey(biome2))
				e.Player.SendErrorMessage("Invalid biome.");
			else
				_commandQueue.Add(new Commands.Biome(info.X, info.Y, info.X2, info.Y2, e.Player, biome1, biome2));
		}

		private void Copy(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
				e.Player.SendErrorMessage("Invalid selection!");
			else
				_commandQueue.Add(new Copy(info.X, info.Y, info.X2, info.Y2, e.Player, null!));
		}

		private void Cut(CommandArgs e)
		{
			if (e.Player.Account == null)
			{
				e.Player.SendErrorMessage("You have to be logged in to use this command.");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
				e.Player.SendErrorMessage("Invalid selection.");
			else
				_commandQueue.Add(new Cut(info.X, info.Y, info.X2, info.Y2, e.Player));
		}

		private void Drain(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
				e.Player.SendErrorMessage("Invalid selection.");
			else
				_commandQueue.Add(new Drain(info.X, info.Y, info.X2, info.Y2, e.Player));
		}

		private void Fill(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection.");
				return;
			}
			if (e.Parameters.Count == 0)
			{
				e.Player.SendErrorMessage("//fill <tile> [=> boolean expr...]");
				return;
			}

			var tiles = ID.GetTileID(e.Parameters[0].ToLowerInvariant());

			if (tiles.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid tile '{0}'!", e.Parameters[0]);
				return;
			}
			else if (tiles.Count > 1)
			{
				e.Player.SendErrorMessage("More than one tile matched!");
				return;
			}

			Expression? expression;
			if (e.Parameters.Count > 1)
			{
				if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
				{
					e.Player.SendErrorMessage("Invalid expression!");
					return;
				}
			}
			else { Parser.TryParseTree(new string[] { "=>", "!t" }, out expression); }

			_commandQueue.Add(new Set(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, tiles[0], expression!));
		}

		private void FillWall(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection.");
				return;
			}
			if (e.Parameters.Count == 0)
			{
				e.Player.SendErrorMessage("//fill <tile> [=> boolean expr...]");
				return;
			}

			var walls = ID.GetWallID(e.Parameters[0].ToLowerInvariant());
			if (walls.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid wall '{0}'!", e.Parameters[0]);
				return;
			}
			else if (walls.Count > 1)
			{
				e.Player.SendErrorMessage("More than one wall matched!");
				return;
			}

			Expression? expression;
			if (e.Parameters.Count > 1)
			{
				if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
				{
					e.Player.SendErrorMessage("Invalid expression!");
					return;
				}
			}
			else { Parser.TryParseTree(new string[] { "=>", "!w" }, out expression); }

			_commandQueue.Add(new SetWall(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, walls[0], expression!));
		}

		private void FixGhosts(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
				e.Player.SendErrorMessage("Invalid selection!");
			else
				_commandQueue.Add(new FixGhosts(info.X, info.Y, info.X2, info.Y2, e.Player));
		}

		private void FixGrass(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
				e.Player.SendErrorMessage("Invalid selection!");
			else
				_commandQueue.Add(new FixGrass(info.X, info.Y, info.X2, info.Y2, e.Player));
		}

		private void FixHalves(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
				e.Player.SendErrorMessage("Invalid selection!");
			else
				_commandQueue.Add(new FixHalves(info.X, info.Y, info.X2, info.Y2, e.Player));
		}

		private void FixSlopes(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
				e.Player.SendErrorMessage("Invalid selection!");
			else
				_commandQueue.Add(new FixSlopes(info.X, info.Y, info.X2, info.Y2, e.Player));
		}

		private void Flood(CommandArgs e)
		{
			if (e.Parameters.Count != 1)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //flood <liquid>");
				return;
			}

			int liquid = 0;
			if (string.Equals(e.Parameters[0], "lava", StringComparison.OrdinalIgnoreCase))
				liquid = 1;
			else if (string.Equals(e.Parameters[0], "honey", StringComparison.OrdinalIgnoreCase))
				liquid = 2;
			else if (string.Equals(e.Parameters[0], "shimmer", StringComparison.OrdinalIgnoreCase))
				liquid = 3;
			else if (!string.Equals(e.Parameters[0], "water", StringComparison.OrdinalIgnoreCase))
			{
				e.Player.SendErrorMessage("Invalid liquid type '{0}'!", e.Parameters[0]);
				return;
			}

			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
				e.Player.SendErrorMessage("Invalid selection!");
			_commandQueue.Add(new Flood(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, liquid));
		}

		private void Flip(CommandArgs e)
		{
			if (e.Player.Account == null)
			{
				e.Player.SendErrorMessage("You have to be logged in to use this command.");
				return;
			}
			if (e.Parameters.Count != 1)
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //flip <direction>");
			else if (!Tools.HasClipboard(e.Player.Account.ID))
				e.Player.SendErrorMessage("Invalid clipboard!");
			else
			{
				bool flipX = false;
				bool flipY = false;
				foreach (char c in e.Parameters[0].ToLowerInvariant())
				{
					if (c == 'x')
						flipX ^= true;
					else if (c == 'y')
						flipY ^= true;
					else
					{
						e.Player.SendErrorMessage("Invalid direction '{0}'!", c);
						return;
					}
				}
				_commandQueue.Add(new Flip(e.Player, flipX, flipY));
			}
		}

		private void MagicWandTool(CommandArgs e)
		{
			string error = "Invalid syntax! Proper syntax: //magicwand [<X> <Y>] => boolean expr...";
			if (e.Parameters.Count < 2)
			{
				e.Player.SendErrorMessage(error);
				return;
			}

			int skip = 0, x = -1, y = -1;
			if (e.Parameters[0] != "=>")
			{
				if (e.Parameters.Count < 4)
				{
					e.Player.SendErrorMessage(error);
					return;
				}

				if (!int.TryParse(e.Parameters[0], out x)
					|| !int.TryParse(e.Parameters[1], out y)
					|| x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY)
				{
					e.Player.SendErrorMessage(error);
					return;
				}
				skip = 2;
			}

			if (!Parser.TryParseTree(e.Parameters.Skip(skip), out Expression? expression))
			{
				e.Player.SendErrorMessage("Invalid expression!");
				return;
			}

			PlayerInfo info = e.Player.GetPlayerInfo();
			if (x != -1 && y != -1)
			{
				if (!MagicWand.GetMagicWandSelection(x, y,
					expression!, e.Player, out MagicWand selection))
				{
					e.Player.SendErrorMessage("Can't " +
						"start counting magic wand selection from this tile.");
				}
				else
				{
					info.MagicWand = selection;
					e.Player.SendSuccessMessage("Set magic wand selection.");
				}
				info.SavedExpression = null;
			}
			else
			{
				info.SavedExpression = expression;
				info.Point = 4;
				e.Player.SendInfoMessage("Modify a block to count hard selection.");
			}
		}

		private void KillEmpty(CommandArgs e)
		{
			byte action;
			switch (e.Parameters.ElementAtOrDefault(0)?.ToLower())
			{
				case "s":
				case "sign":
				case "signs":
					{
						action = 0;
						break;
					}
				case "c":
				case "chest":
				case "chests":
					{
						action = 1;
						break;
					}
				case "a":
				case "all":
					{
						action = 255;
						break;
					}
				default:
					{
						e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //killempty <signs/chests/all>");
						return;
					}
			}

			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
				e.Player.SendErrorMessage("Invalid selection!");
			_commandQueue.Add(new KillEmpty(info.X, info.Y, info.X2, info.Y2, e.Player, action));
		}

		private void Move(CommandArgs e)
		{
			if (e.Parameters.Count < 2)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //move <right> <down> [=> boolean expr...]");
				return;
			}

			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			if (!int.TryParse(e.Parameters[0], out int right)
				|| !int.TryParse(e.Parameters[1], out int down))
			{
				e.Player.SendErrorMessage("Invalid distance!");
				return;
			}

			Expression? expression = null;
			if (e.Parameters.Count > 2)
			{
				if (!Parser.TryParseTree(e.Parameters.Skip(2), out expression))
				{
					e.Player.SendErrorMessage("Invalid expression!");
					return;
				}
			}

			_commandQueue.Add(new Move(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, down, right, expression!));
		}

		private void Mow(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
				e.Player.SendErrorMessage("Invalid selection!");
			else
				_commandQueue.Add(new Mow(info.X, info.Y, info.X2, info.Y2, e.Player));
		}

		private void Near(CommandArgs e)
		{
			if (e.Parameters.Count != 1)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //near <radius>");
				return;
			}

			if (!int.TryParse(e.Parameters[0], out int radius) || radius <= 0)
			{
				e.Player.SendErrorMessage("Invalid radius '{0}'!", e.Parameters[0]);
				return;
			}

			PlayerInfo info = e.Player.GetPlayerInfo();
			info.X = e.Player.TileX - radius;
			info.X2 = e.Player.TileX + radius + 1;
			info.Y = e.Player.TileY - radius;
			info.Y2 = e.Player.TileY + radius + 2;
			e.Player.SendSuccessMessage("Selected tiles around you!");
		}

		private void Outline(CommandArgs e)
		{
			if (e.Parameters.Count < 3)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //outline <tile> <color> <state> [=> boolean expr...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			var colors = ID.GetColorID(e.Parameters[1].ToLowerInvariant());

			if (colors.Count == 0)
				e.Player.SendErrorMessage("Invalid color '{0}'!", e.Parameters[0]);
			else if (colors.Count > 1)
				e.Player.SendErrorMessage("More than one color matched!");
			else
			{
				bool state = false;
				if (string.Equals(e.Parameters[2], "active", StringComparison.OrdinalIgnoreCase))
					state = true;
				else if (string.Equals(e.Parameters[2], "a", StringComparison.OrdinalIgnoreCase))
					state = true;
				else if (string.Equals(e.Parameters[2], "na", StringComparison.OrdinalIgnoreCase))
					state = false;
				else if (!string.Equals(e.Parameters[2], "nactive", StringComparison.OrdinalIgnoreCase))
				{
					e.Player.SendErrorMessage("Invalid active state '{0}'!", e.Parameters[1]);
					return;
				}

				var tiles = ID.GetTileID(e.Parameters[0].ToLowerInvariant());

				if (tiles.Count == 0)
					e.Player.SendErrorMessage("Invalid tile '{0}'!", e.Parameters[0]);
				else if (tiles.Count > 1)
					e.Player.SendErrorMessage("More than one tile matched!");
				else
				{
					Expression? expression = null;
					if (e.Parameters.Count > 3)
					{
						if (!Parser.TryParseTree(e.Parameters.Skip(3), out expression))
						{
							e.Player.SendErrorMessage("Invalid expression!");
							return;
						}
					}
					_commandQueue.Add(new Outline(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, tiles[0], colors[0], state, expression!));
				}
			}
		}

		private void OutlineWall(CommandArgs e)
		{
			if (e.Parameters.Count < 2)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //outlinewall <wall> [color] [=> boolean expr...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			var colors = ID.GetColorID(e.Parameters[1].ToLowerInvariant());

			if (colors.Count == 0)
				e.Player.SendErrorMessage("Invalid color '{0}'!", e.Parameters[0]);
			else if (colors.Count > 1)
				e.Player.SendErrorMessage("More than one color matched!");
			else
			{
				var walls = ID.GetWallID(e.Parameters[0].ToLowerInvariant());

				if (walls.Count == 0)
					e.Player.SendErrorMessage("Invalid wall '{0}'!", e.Parameters[0]);
				else if (walls.Count > 1)
					e.Player.SendErrorMessage("More than one wall matched!");
				else
				{
					Expression? expression = null;
					if (e.Parameters.Count > 2)
					{
						if (!Parser.TryParseTree(e.Parameters.Skip(2), out expression))
						{
							e.Player.SendErrorMessage("Invalid expression!");
							return;
						}
					}
					_commandQueue.Add(new OutlineWall(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, walls[0], colors[0], expression!));
				}
			}
		}

		private void Paint(CommandArgs e)
		{
			if (e.Parameters.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //paint <color> [where] [conditions...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			var colors = ID.GetColorID(e.Parameters[0].ToLowerInvariant());

			if (colors.Count == 0)
				e.Player.SendErrorMessage("Invalid color '{0}'!", e.Parameters[0]);
			else if (colors.Count > 1)
				e.Player.SendErrorMessage("More than one color matched!");
			else
			{
				Expression? expression = null;
				if (e.Parameters.Count > 1)
				{
					if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
					{
						e.Player.SendErrorMessage("Invalid expression!");
						return;
					}
				}
				_commandQueue.Add(new Paint(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, colors[0], expression!));
			}
		}

		private void PaintWall(CommandArgs e)
		{
			if (e.Parameters.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //paintwall <color> [where] [conditions...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			var colors = ID.GetColorID(e.Parameters[0].ToLowerInvariant());

			if (colors.Count == 0)
				e.Player.SendErrorMessage("Invalid color '{0}'!", e.Parameters[0]);
			else if (colors.Count > 1)
				e.Player.SendErrorMessage("More than one color matched!");
			else
			{
				Expression? expression = null;
				if (e.Parameters.Count > 1)
				{
					if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
					{
						e.Player.SendErrorMessage("Invalid expression!");
						return;
					}
				}
				_commandQueue.Add(new PaintWall(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, colors[0], expression!));
			}
		}

		private void Coat(CommandArgs e)
		{
			if (e.Parameters.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //coat <coat> [where] [conditions...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			var coats = ID.GetCoatID(e.Parameters[0].ToLowerInvariant());

			if (coats.Count == 0)
				e.Player.SendErrorMessage("Invalid coat '{0}'!", e.Parameters[0]);
			else if (coats.Count > 1)
				e.Player.SendErrorMessage("More than one coat matched!");
			else
			{
				Expression? expression = null;
				if (e.Parameters.Count > 1)
				{
					if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
					{
						e.Player.SendErrorMessage("Invalid expression!");
						return;
					}
				}
				_commandQueue.Add(new Coat(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, coats[0], expression!));
			}
		}

		private void CoatWall(CommandArgs e)
		{
			if (e.Parameters.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //coatwall <coat> [where] [conditions...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			var coats = ID.GetCoatID(e.Parameters[0].ToLowerInvariant());

			if (coats.Count == 0)
				e.Player.SendErrorMessage("Invalid coat '{0}'!", e.Parameters[0]);
			else if (coats.Count > 1)
				e.Player.SendErrorMessage("More than one coat matched!");
			else
			{
				Expression? expression = null;
				if (e.Parameters.Count > 1)
				{
					if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
					{
						e.Player.SendErrorMessage("Invalid expression!");
						return;
					}
				}
				_commandQueue.Add(new CoatWall(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, coats[0], expression!));
			}
		}

		private void Paste(CommandArgs e)
		{
			if (e.Player.Account == null)
			{
				e.Player.SendErrorMessage("You have to be logged in to use this command.");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			e.Player.SendInfoMessage("X: {0}, Y: {1}", info.X, info.Y);
			if (info.X == -1 || info.Y == -1)
				e.Player.SendErrorMessage("Invalid first point!");
			else if (!Tools.HasClipboard(e.Player.Account.ID))
				e.Player.SendErrorMessage("Invalid clipboard!");
			else
			{
				int alignment = 0;
				bool mode_MainBlocks = true;
				Expression? expression = null;
				int Skip = 0;

				if (e.Parameters.Count > Skip)
				{
					if (!e.Parameters[Skip].ToLowerInvariant().StartsWith("-")
						&& !e.Parameters[Skip].ToLowerInvariant().StartsWith("="))
					{
						foreach (char c in e.Parameters[0].ToLowerInvariant())
						{
							if (c == 'l')
								alignment &= 2;
							else if (c == 'r')
								alignment |= 1;
							else if (c == 't')
								alignment &= 1;
							else if (c == 'b')
								alignment |= 2;
							else
							{
								e.Player.SendErrorMessage("Invalid paste alignment '{0}'!", c);
								return;
							}
						}
						Skip++;
					}

					if ((e.Parameters.Count > Skip) && ((e.Parameters[Skip].ToLowerInvariant() == "-f")
						|| (e.Parameters[Skip].ToLowerInvariant() == "-file")))
					{
						mode_MainBlocks = false;
						Skip++;
					}

					if (e.Parameters.Count > Skip)
					{
						if (!Parser.TryParseTree(e.Parameters.Skip(Skip), out expression))
						{
							e.Player.SendErrorMessage("Invalid expression!");
							return;
						}
					}
				}
				_commandQueue.Add(new Paste(info.X, info.Y, e.Player, Tools.GetClipboardPath(e.Player.Account.ID), alignment, expression!, mode_MainBlocks, true));
			}
		}

		private void SPaste(CommandArgs e)
		{
			if (e.Player.Account == null)
			{
				e.Player.SendErrorMessage("You have to be logged in to use this command.");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			e.Player.SendInfoMessage("X: {0}, Y: {1}", info.X, info.Y);
			if (info.X == -1 || info.Y == -1)
				e.Player.SendErrorMessage("Invalid first point!");
			else if (!Tools.HasClipboard(e.Player.Account.ID))
				e.Player.SendErrorMessage("Invalid clipboard!");
			else if (e.Parameters.Count < 1)
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //spaste [alignment] [-flag -flag ...] [=> boolean expr...]");
			else
			{
				int alignment = 0;
				Expression? expression = null;
				int Skip = 0;
				bool tiles = true;
				bool tilePaints = true;
				bool emptyTiles = true;
				bool walls = true;
				bool wallPaints = true;
				bool wires = true;
				bool liquids = true;

				if (e.Parameters.Count > Skip)
				{
					if (!e.Parameters[Skip].ToLowerInvariant().StartsWith("-"))
					{
						foreach (char c in e.Parameters[Skip].ToLowerInvariant())
						{
							if (c == 'l')
								alignment &= 2;
							else if (c == 'r')
								alignment |= 1;
							else if (c == 't')
								alignment &= 1;
							else if (c == 'b')
								alignment |= 2;
							else
							{
								e.Player.SendErrorMessage("Invalid paste alignment '{0}'!", c);
								return;
							}
						}
						Skip++;
					}

					List<string> InvalidFlags = new List<string>();
					while ((e.Parameters.Count > Skip) && (e.Parameters[Skip] != "=>"))
					{
						switch (e.Parameters[Skip].ToLower())
						{
							case "-t": { tiles = false; break; }
							case "-tp": { tilePaints = false; break; }
							case "-et": { emptyTiles = false; break; }
							case "-w": { walls = false; break; }
							case "-wp": { wallPaints = false; break; }
							case "-wi": { wires = false; break; }
							case "-l": { liquids = false; break; }
							default: { InvalidFlags.Add(e.Parameters[Skip]); break; }
						}
						Skip++;
					}

					if (e.Parameters.Count > Skip)
					{
						if (!Parser.TryParseTree(e.Parameters.Skip(Skip), out expression))
						{
							e.Player.SendErrorMessage("Invalid expression!");
							return;
						}
					}
				}
				_commandQueue.Add(new SPaste(info.X, info.Y, e.Player, alignment, expression!, tiles, tilePaints, emptyTiles, walls, wallPaints, wires, liquids));
			}
		}

		private void Point1(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (e.Parameters.Count == 0)
			{
				if (!e.Player.RealPlayer)
					e.Player.SendErrorMessage("You must use this command in-game.");
				else
				{
					info.Point = 1;
					e.Player.SendInfoMessage("Modify a block to set point 1.");
				}
				return;
			}
			if (e.Parameters.Count != 2)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //point1 <x> <y>");
				return;
			}

			if (!int.TryParse(e.Parameters[0], out int x) || x < 0 || x >= Main.maxTilesX
				|| !int.TryParse(e.Parameters[1], out int y) || y < 0 || y >= Main.maxTilesY)
			{
				e.Player.SendErrorMessage("Invalid coordinates.");
				return;
			}

			info.X = x;
			info.Y = y;
			e.Player.SendInfoMessage("Set point 1.");
		}

		private void Point2(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (e.Parameters.Count == 0)
			{
				if (!e.Player.RealPlayer)
					e.Player.SendErrorMessage("You must use this command in-game.");
				else
				{
					info.Point = 2;
					e.Player.SendInfoMessage("Modify a block to set point 2.");
				}
				return;
			}
			if (e.Parameters.Count != 2)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //point2 [x] [y]");
				return;
			}

			if (!int.TryParse(e.Parameters[0], out int x) || x < 0 || x >= Main.maxTilesX
				|| !int.TryParse(e.Parameters[1], out int y) || y < 0 || y >= Main.maxTilesY)
			{
				e.Player.SendErrorMessage("Invalid coordinates '({0}, {1})'!", e.Parameters[0], e.Parameters[1]);
				return;
			}

			info.X2 = x;
			info.Y2 = y;
			e.Player.SendInfoMessage("Set point 2.");
		}

		private void Redo(CommandArgs e)
		{
			if (e.Player.Account == null)
			{
				e.Player.SendErrorMessage("You have to be logged in to use this command.");
				return;
			}
			if (e.Parameters.Count > 2)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //redo [steps] [account]");
				return;
			}

			int steps = 1;
			int ID = e.Player.Account.ID;
			if (e.Parameters.Count > 0 && (!int.TryParse(e.Parameters[0], out steps) || steps <= 0))
				e.Player.SendErrorMessage("Invalid redo steps '{0}'!", e.Parameters[0]);
			else
			{
				if (e.Parameters.Count > 1)
				{
					if (!e.Player.HasPermission("worldedit.usage.otheraccounts"))
					{
						e.Player.SendErrorMessage("You do not have permission to redo other player's actions.");
						return;
					}
					UserAccount Account = TShock.UserAccounts.GetUserAccountByName(e.Parameters[1]);
					if (Account == null)
					{
						e.Player.SendErrorMessage("Invalid account name!");
						return;
					}
					ID = Account.ID;
				}
			}
			_commandQueue.Add(new Redo(e.Player, ID, steps));
		}

		private void RegionCmd(CommandArgs e)
		{
			if (e.Parameters.Count > 1)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //region [region name]");
				return;
			}

			PlayerInfo info = e.Player.GetPlayerInfo();
			if (e.Parameters.Count == 0)
			{
				info.Point = 3;
				e.Player.SendInfoMessage("Hit a block to select that region.");
			}
			else
			{
				Region region = TShock.Regions.GetRegionByName(e.Parameters[0]);
				if (region == null)
					e.Player.SendErrorMessage("Invalid region '{0}'!", e.Parameters[0]);
				else
				{
					info.X = region.Area.Left;
					info.Y = region.Area.Top;
					info.X2 = region.Area.Right;
					info.Y2 = region.Area.Bottom;
					e.Player.SendSuccessMessage("Set selection to region '{0}'.", region.Name);
				}
			}
		}

		private void Replace(CommandArgs e)
		{
			if (e.Parameters.Count < 2)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //replace <from tile> <to tile> [=> boolean expr...]");
				return;
			}

			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			var tilesFrom = ID.GetTileID(e.Parameters[0].ToLowerInvariant());

			if (tilesFrom.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid tile '{0}'!", e.Parameters[0]);
				return;
			}
			else if (tilesFrom.Count > 1)
			{
				e.Player.SendErrorMessage("More than one tile matched!");
				return;
			}

			var tilesTo = ID.GetTileID(e.Parameters[1].ToLowerInvariant());

			if (tilesTo.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid tile '{0}'!", e.Parameters[1]);
				return;
			}
			else if (tilesTo.Count > 1)
			{
				e.Player.SendErrorMessage("More than one tile matched!");
				return;
			}

			Expression? expression = null;
			if (e.Parameters.Count > 2)
			{
				if (!Parser.TryParseTree(e.Parameters.Skip(2), out expression))
				{
					e.Player.SendErrorMessage("Invalid expression!");
					return;
				}
			}

			_commandQueue.Add(new Replace(info.X, info.Y, info.X2, info.Y2, e.Player, tilesFrom[0], tilesTo[0], expression!));
		}

		private void ReplaceWall(CommandArgs e)
		{
			if (e.Parameters.Count < 2)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //replace <from tile> <to tile> [=> boolean expr...]");
				return;
			}

			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			var wallsFrom = ID.GetTileID(e.Parameters[0].ToLowerInvariant());

			if (wallsFrom.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid tile '{0}'!", e.Parameters[0]);
				return;
			}
			else if (wallsFrom.Count > 1)
			{
				e.Player.SendErrorMessage("More than one tile matched!");
				return;
			}

			var wallsTo = ID.GetWallID(e.Parameters[1].ToLowerInvariant());

			if (wallsTo.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid wall '{0}'!", e.Parameters[1]);
				return;
			}
			else if (wallsTo.Count > 1)
			{
				e.Player.SendErrorMessage("More than one wall matched!");
				return;
			}

			Expression? expression = null;
			if (e.Parameters.Count > 2)
			{
				if (!Parser.TryParseTree(e.Parameters.Skip(2), out expression))
				{
					e.Player.SendErrorMessage("Invalid expression!");
					return;
				}
			}

			_commandQueue.Add(new ReplaceWall(info.X, info.Y, info.X2, info.Y2, e.Player, wallsFrom[0], wallsTo[0], expression!));
		}

		private void Resize(CommandArgs e)
		{
			if (e.Parameters.Count != 2)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //resize <direction(s)> <amount>");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			if (!int.TryParse(e.Parameters[1], out int amount))
			{
				e.Player.SendErrorMessage("Invalid resize amount '{0}'!", e.Parameters[0]);
				return;
			}

			foreach (char c in e.Parameters[0].ToLowerInvariant())
			{
				if (c == 'd')
				{
					if (info.Y < info.Y2)
						info.Y2 += amount;
					else
						info.Y += amount;
				}
				else if (c == 'l')
				{
					if (info.X < info.X2)
						info.X -= amount;
					else
						info.X2 -= amount;
				}
				else if (c == 'r')
				{
					if (info.X < info.X2)
						info.X2 += amount;
					else
						info.X += amount;
				}
				else if (c == 'u')
				{
					if (info.Y < info.Y2)
						info.Y -= amount;
					else
						info.Y2 -= amount;
				}
				else
				{
					e.Player.SendErrorMessage("Invalid direction '{0}'!", c);
					return;
				}
			}
			e.Player.SendSuccessMessage("Resized selection.");
		}

		private void Rotate(CommandArgs e)
		{
			if (e.Player.Account == null)
			{
				e.Player.SendErrorMessage("You have to be logged in to use this command.");
				return;
			}
			if (e.Parameters.Count != 1)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //rotate <angle>");
				return;
			}
			if (!Tools.HasClipboard(e.Player.Account.ID))
			{
				e.Player.SendErrorMessage("Invalid clipboard!");
				return;
			}

			if (!int.TryParse(e.Parameters[0], out int degrees) || degrees % 90 != 0)
				e.Player.SendErrorMessage("Invalid angle '{0}'!", e.Parameters[0]);
			else
				_commandQueue.Add(new Rotate(e.Player, degrees));
		}

		private void Scale(CommandArgs e)
		{
			if (e.Player.Account == null)
			{
				e.Player.SendErrorMessage("You have to be logged in to use this command.");
				return;
			}
			if ((e.Parameters.Count != 2) || ((e.Parameters[0] != "+") && (e.Parameters[0] != "-")))
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //scale <+/-> <amount>");
				return;
			}
			if (!Tools.HasClipboard(e.Player.Account.ID))
			{
				e.Player.SendErrorMessage("Invalid clipboard!");
				return;
			}
			if (!int.TryParse(e.Parameters[1], out int scale))
			{
				e.Player.SendErrorMessage("Invalid amount!");
				return;
			}
			_commandQueue.Add(new Scale(e.Player, (e.Parameters[0] == "+"), scale));
		}

		private void Schematic(CommandArgs e)
		{
			const string fileFormat = "schematic-{0}.dat";

			string subCmd = e.Parameters.Count == 0 ? "help" : e.Parameters[0].ToLowerInvariant();
			switch (subCmd)
			{
				case "del":
				case "delete":
					{
						if (e.Parameters.Count > 2)
						{
							e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //schematic delete <name>");
							return;
						}

						var path = Path.Combine(Configuration<WorldEditSettings>.Settings.SchematicDirectory, string.Format(fileFormat, e.Parameters[1]));

						if (!File.Exists(path))
						{
							e.Player.SendErrorMessage("Invalid schematic '{0}'!", e.Parameters[1]);
							return;
						}
						else
						{
							if (!e.Player.HasPermission("worldedit.schematic.delete"))
							{
								e.Player.SendErrorMessage("You do not have permission to delete global schematics.");
								return;
							}

							File.Delete(path);
						}

						e.Player.SendErrorMessage("Deleted schematic '{0}'.", e.Parameters[1]);
					}
					return;
				case "list":
					{
						if (e.Parameters.Count > 2)
						{
							e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //schematic list [page]");
							return;
						}

						if (!e.Player.HasPermission("worldedit.schematic.list"))
						{
							e.Player.SendErrorMessage("You do not have permission to view global schematics.");
							return;
						}

						if (!PaginationTools.TryParsePageNumber(e.Parameters, 1, e.Player, out int pageNumber))
							return;

						var schematics = from s in Directory.EnumerateFiles(Configuration<WorldEditSettings>.Settings.SchematicDirectory, string.Format(fileFormat, "*"))
										 select Path.GetFileNameWithoutExtension(s)[10..];

						PaginationTools.SendPage(e.Player, pageNumber, PaginationTools.BuildLinesFromTerms(schematics),
							new PaginationTools.Settings
							{
								HeaderFormat = "Schematics ({0}/{1}):",
								FooterFormat = "Type //schematic list {0} for more."
							});
					}
					return;
				case "l":
				case "load":
					{
						if (e.Player.Account == null)
						{
							e.Player.SendErrorMessage("You have to be logged in to use this command.");
							return;
						}

						else if (e.Parameters.Count != 2)
						{
							e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //schematic load <name>");
							return;
						}

						var path = Path.Combine(Configuration<WorldEditSettings>.Settings.SchematicDirectory, string.Format(fileFormat, e.Parameters[1]));

						var clipboard = Tools.GetClipboardPath(e.Player.Account.ID);

						if (!File.Exists(path))
						{
							e.Player.SendErrorMessage("Invalid schematic '{0}'!", e.Parameters[1]);
							return;
						}
						else
						{

							if (!e.Player.HasPermission("worldedit.schematic.load"))
							{
								e.Player.SendErrorMessage("You do not have permission to load schematics.");
								return;
							}
							File.Copy(path, clipboard, true);
						}

						e.Player.SendSuccessMessage("Loaded schematic '{0}' to clipboard.", e.Parameters[1]);
					}
					return;
				case "s":
				case "save":
					{
						if (e.Player.Account == null)
						{
							e.Player.SendErrorMessage("You have to be logged in to use this command.");
							return;
						}

						if (!e.Player.HasPermission("worldedit.schematic.save"))
						{
							e.Player.SendErrorMessage("You do not have permission to save schematics.");
							return;
						}

						string _1 = e.Parameters.ElementAtOrDefault(1)?.ToLower()!;
						bool force = ((_1 == "-force") || (_1 == "-f"));
						string name = e.Parameters.ElementAtOrDefault(force ? 2 : 1)!;
						if (string.IsNullOrWhiteSpace(name))
						{
							e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //schematic save [-force/-f] <name>");
							return;
						}

						string clipboard = Tools.GetClipboardPath(e.Player.Account.ID);

						if (!File.Exists(clipboard))
						{
							e.Player.SendErrorMessage("Invalid clipboard!");
							return;
						}

						if (!Tools.IsCorrectName(name))
						{
							e.Player.SendErrorMessage("Name should not contain these symbols: \"{0}\".",
								string.Join("\", \"", Path.GetInvalidFileNameChars()));
							return;
						}

						var path = Path.Combine(Configuration<WorldEditSettings>.Settings.SchematicDirectory, string.Format(fileFormat, name));

						if (File.Exists(path))
						{
							if (!e.Player.HasPermission("worldedit.schematic.overwrite"))
							{
								e.Player.SendErrorMessage("You do not have permission to overwrite schematics.");
								return;
							}
							else if (!force)
							{
								e.Player.SendErrorMessage($"Schematic '{name}' already exists, " +
									$"write '//schematic save <-force/-f> {name}' to overwrite it.");
								return;
							}
						}

						File.Copy(clipboard, path, true);

						e.Player.SendSuccessMessage("Saved clipboard to schematic '{0}'.", name);
					}
					return;
			}
		}

		private void PrivateSchematic(CommandArgs e)
		{
			const string fileFormat = "schematic-{0}.dat";

			string subCmd = e.Parameters.Count == 0 ? "help" : e.Parameters[0].ToLowerInvariant();
			switch (subCmd)
			{
				case "del":
				case "delete":
					{
						if (e.Parameters.Count > 2)
						{
							e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //schematic delete <name>");
							return;
						}

						var path = Path.Combine(Tools.GetPlayerSchemPath(e.Player.Account.Name), string.Format(fileFormat, e.Parameters[1]));

						if (!File.Exists(path))
						{
							e.Player.SendErrorMessage("Invalid schematic '{0}'!", e.Parameters[1]);
							return;
						}
						else
						{
							File.Delete(path);
						}

						e.Player.SendErrorMessage("Deleted schematic '{0}'.", e.Parameters[1]);
					}
					return;
				case "list":
					{
						if (e.Parameters.Count > 2)
						{
							e.Player.SendErrorMessage("Invalid syntax! Proper syntax: /psc list [page]");
							return;
						}

						var selfPath = Path.Combine(Configuration<WorldEditSettings>.Settings.SchematicDirectory, e.Player.Account.Name);

						if (!Directory.Exists(selfPath))
							Directory.CreateDirectory(selfPath);

						var schematics = from s in Directory.EnumerateFiles(selfPath, string.Format(fileFormat, "*"))
										 select Path.GetFileNameWithoutExtension(s)[10..];

						if (!PaginationTools.TryParsePageNumber(e.Parameters, 1, e.Player, out int pageNumber))
							return;

						PaginationTools.SendPage(e.Player, pageNumber, PaginationTools.BuildLinesFromTerms(schematics),
							new PaginationTools.Settings
							{
								HeaderFormat = "Schematics ({0}/{1}):",
								FooterFormat = "Type //schematic list {0} for more."
							});
					}
					return;
				case "l":
				case "load":
					{
						if (e.Player.Account == null)
						{
							e.Player.SendErrorMessage("You have to be logged in to use this command.");
							return;
						}

						else if (e.Parameters.Count != 2)
						{
							e.Player.SendErrorMessage("Invalid syntax! Proper syntax: /psc load <name>");
							return;
						}

						var path = Path.Combine(Tools.GetPlayerSchemPath(e.Player.Account.Name), string.Format(fileFormat, e.Parameters[1]));

						var clipboard = Tools.GetClipboardPath(e.Player.Account.ID);

						if (!File.Exists(path))
						{
							e.Player.SendErrorMessage("Invalid schematic '{0}'!", e.Parameters[1]);
							return;
						}
						else
						{
							File.Copy(path, clipboard, true);
						}

						e.Player.SendSuccessMessage("Loaded schematic '{0}' to clipboard.", e.Parameters[1]);
					}
					return;
				case "s":
				case "save":
					{
						if (e.Player.Account == null)
						{
							e.Player.SendErrorMessage("You have to be logged in to use this command.");
							return;
						}

						string _1 = e.Parameters.ElementAtOrDefault(1)?.ToLower()!;
						bool force = ((_1 == "-force") || (_1 == "-f"));
						string name = e.Parameters.ElementAtOrDefault(force ? 2 : 1)!;

						if (string.IsNullOrWhiteSpace(name))
						{
							e.Player.SendErrorMessage("Invalid syntax! Proper syntax: /psc save [-force/-f] <name>");
							return;
						}

						string clipboard = Tools.GetClipboardPath(e.Player.Account.ID);

						if (!File.Exists(clipboard))
						{
							e.Player.SendErrorMessage("Invalid clipboard!");
							return;
						}

						if (!Tools.IsCorrectName(name))
						{
							e.Player.SendErrorMessage("Name should not contain these symbols: \"{0}\".",
								string.Join("\", \"", Path.GetInvalidFileNameChars()));
							return;
						}

						var path = Path.Combine(Tools.GetPlayerSchemPath(e.Player.Account.Name), string.Format(fileFormat, name));

						if (File.Exists(path))
						{
							if (!force)
							{
								e.Player.SendErrorMessage($"Schematic '{name}' already exists, " +
									$"write '/psc save <-force/-f> {name}' to overwrite it.");
								return;
							}
						}

						File.Copy(clipboard, path, true);

						e.Player.SendSuccessMessage("Saved clipboard to schematic '{0}'.", name);
					}
					return;
				default:
					e.Player.SendSuccessMessage("Schematics Subcommands:");
					e.Player.SendInfoMessage("/psc delete/del <name>\n"
										   + "/psc list [page]\n"
										   + "/psc load/l <name>\n"
										   + "/psc save/s <name>\n");
					return;
			}
		}

		private void Select(CommandArgs e)
		{
			if (e.Parameters.Count != 1)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //select <selection type>");
				e.Player.SendInfoMessage("Available selections: " + string.Join(", ", Selections.Keys) + ".");
				return;
			}

			if (e.Parameters[0].ToLowerInvariant() == "help")
			{
				e.Player.SendInfoMessage("Proper syntax: //select <selection type>");
				e.Player.SendInfoMessage("Available selections: " + string.Join(", ", Selections.Keys) + ".");
				return;
			}

			string selection = e.Parameters[0].ToLowerInvariant();
			if (!Selections.ContainsKey(selection))
			{
				string available = "Available selections: " + string.Join(", ", Selections.Keys) + ".";
				e.Player.SendErrorMessage("Invalid selection type '{0}'!\r\n{1}", selection, available);
				return;
			}
			e.Player.GetPlayerInfo().Select = Selections[selection];
			e.Player.SendSuccessMessage("Set selection type to '{0}'.", selection);
		}

		private void Set(CommandArgs e)
		{
			if (e.Parameters.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //set <tile> [=> boolean expr...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			var tiles = ID.GetTileID(e.Parameters[0].ToLowerInvariant());

			if (tiles.Count == 0)
				e.Player.SendErrorMessage("Invalid tile '{0}'!", e.Parameters[0]);
			else if (tiles.Count > 1)
				e.Player.SendErrorMessage("More than one tile matched!");
			else
			{
				Expression? expression = null;
				if (e.Parameters.Count > 1)
				{
					if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
					{
						e.Player.SendErrorMessage("Invalid expression!");
						return;
					}
				}
				_commandQueue.Add(new Set(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, tiles[0], expression!));
			}
		}

		private void SetWall(CommandArgs e)
		{
			if (e.Parameters.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //setwall <wall> [=> boolean expr...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			var walls = ID.GetWallID(e.Parameters[0].ToLowerInvariant());

			if (walls.Count == 0)
				e.Player.SendErrorMessage("Invalid wall '{0}'!", e.Parameters[0]);
			else if (walls.Count > 1)
				e.Player.SendErrorMessage("More than one wall matched!");
			else
			{
				Expression? expression = null;
				if (e.Parameters.Count > 1)
				{
					if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
					{
						e.Player.SendErrorMessage("Invalid expression!");
						return;
					}
				}
				_commandQueue.Add(new SetWall(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, walls[0], expression!));
			}
		}

		private void SetGrass(CommandArgs e)
		{
			if (e.Parameters.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //setgrass <grass> [=> boolean expr...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			if (!Biomes.Keys.Contains(e.Parameters[0].ToLowerInvariant()) || (e.Parameters[0].ToLowerInvariant() == "snow"))
			{
				e.Player.SendErrorMessage("Invalid grass '{0}'!", e.Parameters[0]);
				return;
			}

			Expression? expression = null;
			if (e.Parameters.Count > 1)
			{
				if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
				{
					e.Player.SendErrorMessage("Invalid expression!");
					return;
				}
			}

			_commandQueue.Add(new SetGrass(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, e.Parameters[0].ToLowerInvariant(), expression!));
		}

		private void SetWire(CommandArgs e)
		{
			if (e.Parameters.Count < 2)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //setwire <wire> <wire state> [=> boolean expr...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			if (!int.TryParse(e.Parameters[0], out int wire) || wire < 1 || wire > 4)
			{
				e.Player.SendErrorMessage("Invalid wire '{0}'!", e.Parameters[0]);
				return;
			}

			bool state = false;
			if (string.Equals(e.Parameters[1], "on", StringComparison.OrdinalIgnoreCase))
				state = true;
			else if (!string.Equals(e.Parameters[1], "off", StringComparison.OrdinalIgnoreCase))
			{
				e.Player.SendErrorMessage("Invalid wire state '{0}'!", e.Parameters[1]);
				return;
			}

			Expression? expression = null;
			if (e.Parameters.Count > 2)
			{
				if (!Parser.TryParseTree(e.Parameters.Skip(2), out expression))
				{
					e.Player.SendErrorMessage("Invalid expression!");
					return;
				}
			}
			_commandQueue.Add(new SetWire(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, wire, state, expression!));
		}

		private void Shape(CommandArgs e)
		{
			bool wall = false, filled = false;
			switch (e.Message.Split(' ')[0].Substring(6).ToLower())
			{
				case "f":
				case "fill":
					{
						filled = true;
						break;
					}
				case "w":
				case "wall":
					{
						wall = true;
						break;
					}
				case "wf":
				case "wallfill":
					{
						wall = true;
						filled = true;
						break;
					}
			}

			string error = $"Invalid syntax! Proper syntax: //shape" +
				(wall ? "wall" : "") + (filled ? "fill" : "") +
				" <shape> [rotate type] [flip type] <tile/wall> [=> boolean expr...]";
			if (e.Parameters.ElementAtOrDefault(0)?.ToLower() == "help")
			{
				e.Player.SendInfoMessage("Allowed shape types: line/l, " +
					"rectangle/r, ellipse/e, isoscelestriangle/it, righttriangle/rt.");
				return;
			}
			else if (e.Parameters.Count < 2)
			{
				e.Player.SendErrorMessage(error);
				return;
			}

			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			int type, rotateType = 0, flipType = 0, param = 1;
			switch (e.Parameters[0].ToLower())
			{
				case "l":
				case "line":
					{
						type = 0;
						break;
					}
				case "r":
				case "rect":
				case "rectangle":
					{
						type = 1;
						break;
					}
				case "e":
				case "ellipse":
					{
						type = 2;
						break;
					}
				case "it":
				case "itriangle":
				case "isoscelestriangle":
					{
						type = 3;
						if (e.Parameters.Count < param + 2)
						{
							e.Player.SendErrorMessage(error);
							e.Player.SendInfoMessage("Allowed rotate types: up/u, down/d, left/l, right/r.");
							return;
						}
						switch (e.Parameters[param])
						{
							case "u":
							case "up":
								{
									rotateType = 0;
									break;
								}
							case "d":
							case "down":
								{
									rotateType = 1;
									break;
								}
							case "l":
							case "left":
								{
									rotateType = 2;
									break;
								}
							case "r":
							case "right":
								{
									rotateType = 3;
									break;
								}
							default:
								{
									e.Player.SendErrorMessage("Invalid rotate type! " +
										"Allowed types: up/u, down/d, left/l, right/r.");
									break;
								}
						}
						param++;
						break;
					}
				case "rt":
				case "rtriangle":
				case "righttriangle":
					{
						type = 4;
						if (e.Parameters.Count < param + 3)
						{
							e.Player.SendErrorMessage(error);
							e.Player.SendInfoMessage("Allowed rotate types: up/u, down/d.");
							e.Player.SendInfoMessage("Allowed flip types: left/l, right/r.");
							return;
						}
						switch (e.Parameters[param])
						{
							case "u":
							case "up":
								{
									rotateType = 0;
									break;
								}
							case "d":
							case "down":
								{
									rotateType = 1;
									break;
								}
							default:
								{
									e.Player.SendErrorMessage("Invalid rotate type! " +
										"Allowed types: up/u, down/d.");
									break;
								}
						}
						switch (e.Parameters[param + 1])
						{
							case "l":
							case "left":
								{
									flipType = 0;
									break;
								}
							case "r":
							case "right":
								{
									flipType = 1;
									break;
								}
							default:
								{
									e.Player.SendErrorMessage("Invalid flip type! " +
										"Allowed types: left/l, right/r.");
									break;
								}
						}
						param += 2;
						break;
					}
				default:
					{
						e.Player.SendErrorMessage("Invalid shape type! Allowed types: line/l, " +
							"rectangle/r, ellipse/e, isoscelestriangle/it, righttriangle/rt.");
						return;
					}
			}

			if (e.Parameters.Count < param)
			{
				e.Player.SendErrorMessage(error);
				return;
			}

			int materialType;
			if (wall)
			{
				var walls = ID.GetWallID(e.Parameters[param].ToLowerInvariant());

				if (walls.Count == 0)
				{
					e.Player.SendErrorMessage("Invalid wall '{0}'!", e.Parameters[param]);
					return;
				}
				else if (walls.Count > 1)
				{
					e.Player.SendErrorMessage("More than one wall matched!");
					return;
				}
				materialType = walls[0];
			}
			else
			{
				var tiles = ID.GetTileID(e.Parameters[param].ToLowerInvariant());

				if (tiles.Count == 0)
				{
					e.Player.SendErrorMessage("Invalid tile '{0}'!", e.Parameters[param]);
					return;
				}
				else if (tiles.Count > 1)
				{
					e.Player.SendErrorMessage("More than one tile matched!");
					return;
				}
				materialType = tiles[0];
			}

			Expression? expression = null;
			if (e.Parameters.Count > ++param)
			{
				if (!Parser.TryParseTree(e.Parameters.Skip(param), out expression))
				{
					e.Player.SendErrorMessage("Invalid expression!");
					return;
				}
			}

			_commandQueue.Add(new Shape(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, type, rotateType, flipType, wall, filled, materialType, expression!));
		}

		private void Size(CommandArgs e)
		{
			switch (e.Parameters.ElementAtOrDefault(0)?.ToLower())
			{
				case "c":
				case "clipboard":
					{
						if (e.Player.Account == null)
						{
							e.Player.SendErrorMessage("You have to be logged in to use this command.");
							return;
						}

						UserAccount account = e.Player.Account;
						if (e.Parameters.Count > 1)
						{
							if (!e.Player.HasPermission("worldedit.usage.otheraccounts"))
							{
								e.Player.SendErrorMessage("You do not have permission to view other player's clipboards.");
								return;
							}
							account = TShock.UserAccounts.GetUserAccountByName(e.Parameters[1]);
							if (account == null)
							{
								e.Player.SendErrorMessage("Invalid account name!");
								return;
							}
						}

						if (!Tools.HasClipboard(account.ID))
						{
							e.Player.SendErrorMessage($"{account.Name} doesn't have a clipboard.");
							return;
						}

						WorldSectionData data = Tools.LoadWorldData(Tools.GetClipboardPath(account.ID));
						e.Player.SendSuccessMessage($"{account.Name}'s clipboard size: " +
							$"{data.Tiles.GetLength(0) - 1}x{data.Tiles.GetLength(1) - 1}.");
						break;
					}
				case "s":
				case "schematic":
					{
						if (!e.Player.HasPermission("worldedit.schematic"))
						{
							e.Player.SendErrorMessage("You do not have permission to use this command.");
							return;
						}

						var path = Path.Combine("worldedit", string.Format("schematic-{0}.dat", e.Parameters[1]));
						if (!File.Exists(path))
						{
							e.Player.SendErrorMessage("Invalid schematic '{0}'!", e.Parameters[1]);
							return;
						}

						WorldSectionData data = Tools.LoadWorldData(path);
						e.Player.SendSuccessMessage($"Schematic's size ('{e.Parameters[1]}'): " +
							$"{data.Tiles.GetLength(0) - 1}x{data.Tiles.GetLength(1) - 1}.");
						break;
					}
				default:
					{
						e.Player.SendErrorMessage("//size <clipboard/c> [user name]\n" +
												  "//size <schematic/s> <name>");
						break;
					}
			}
		}

		private void Slope(CommandArgs e)
		{
			if (e.Parameters.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //slope <type> [=> boolean expr...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			int slope = ID.GetSlopeID(e.Parameters[0].ToLowerInvariant());

			if (slope == -1)
				e.Player.SendErrorMessage("Invalid type '{0}'! Available slopes: " +
					"none (0), t (1), tr (2), tl (3), br (4), bl (5)", e.Parameters[0]);
			else
			{
				Expression? expression = null;
				if (e.Parameters.Count > 1)
				{
					if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
					{
						e.Player.SendErrorMessage("Invalid expression!");
						return;
					}
				}
				_commandQueue.Add(new Slope(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, slope, expression!));
			}
		}

		private void SlopeDelete(CommandArgs e)
		{
			int slope = 255;
			Expression? expression = null;
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}
			if (e.Parameters.Count >= 1)
			{
				slope = ID.GetSlopeID(e.Parameters[0].ToLowerInvariant());

				if (slope == -1)
				{
					e.Player.SendErrorMessage("Invalid type '{0}'! Available slopes: " +
						"none (0), t (1), tr (2), tl (3), br (4), bl (5)", e.Parameters[0]);
					return;
				}
				if (e.Parameters.Count > 1)
				{
					if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
					{
						e.Player.SendErrorMessage("Invalid expression!");
						return;
					}
				}
			}

			_commandQueue.Add(new SlopeDelete(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, slope, expression!));
		}

		private void Smooth(CommandArgs e)
		{
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			Expression? expression = null;
			if ((e.Parameters.Count > 0)
				&& !Parser.TryParseTree(e.Parameters, out expression))
			{
				e.Player.SendErrorMessage("Invalid expression!");
				return;
			}

			_commandQueue.Add(new Smooth(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, expression!));
		}

		private void Inactive(CommandArgs e)
		{
			if (e.Parameters.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //inactive <status(on/off/reverse)> [=> boolean expr...]");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			int mode = 2;
			var modeName = e.Parameters[0].ToLower();
			if (modeName == "on")
				mode = 0;
			else if (modeName == "off")
				mode = 1;
			else if (modeName != "reverse")
			{
				e.Player.SendErrorMessage("Invalid status! Proper: on, off, reverse");
				return;
			}
			Expression? expression = null;
			if (e.Parameters.Count > 1)
			{
				if (!Parser.TryParseTree(e.Parameters.Skip(1), out expression))
				{
					e.Player.SendErrorMessage("Invalid expression!");
					return;
				}
			}
			_commandQueue.Add(new Inactive(info.X, info.Y, info.X2, info.Y2, info.MagicWand!, e.Player, mode, expression!));
		}

		private void Shift(CommandArgs e)
		{
			if (e.Parameters.Count != 2)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //shift <direction> <amount>");
				return;
			}
			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			if (!int.TryParse(e.Parameters[1], out int amount) || amount < 0)
			{
				e.Player.SendErrorMessage("Invalid shift amount '{0}'!", e.Parameters[0]);
				return;
			}

			foreach (char c in e.Parameters[0].ToLowerInvariant())
			{
				if (c == 'd')
				{
					info.Y += amount;
					info.Y2 += amount;
				}
				else if (c == 'l')
				{
					info.X -= amount;
					info.X2 -= amount;
				}
				else if (c == 'r')
				{
					info.X += amount;
					info.X2 += amount;
				}
				else if (c == 'u')
				{
					info.Y -= amount;
					info.Y2 -= amount;
				}
				else
				{
					e.Player.SendErrorMessage("Invalid direction '{0}'!", c);
					return;
				}
			}
			e.Player.SendSuccessMessage("Shifted selection.");
		}

		private void Text(CommandArgs e)
		{
			if (e.Parameters.Count == 0)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //text <text> (\\n for new line)");
				e.Player.SendInfoMessage("In the beginning of new line:");
				e.Player.SendSuccessMessage("\\m for middle position\n" +
					"\\r for right position\n" +
					"\\s<num> (for example \\s3) for line spacing\n" +
					"\\c for cropped statue (2 blocks heigh, without stand)");
				return;
			}

			PlayerInfo info = e.Player.GetPlayerInfo();
			if (info.X == -1 || info.Y == -1 || info.X2 == -1 || info.Y2 == -1)
			{
				e.Player.SendErrorMessage("Invalid selection!");
				return;
			}

			_commandQueue.Add(new Text(info.X, info.Y, info.X2, info.Y2, e.Player, e.Message.Substring(5).TrimStart()));
		}

		private void Undo(CommandArgs e)
		{
			if (e.Player.Account == null)
			{
				e.Player.SendErrorMessage("You have to be logged in to use this command.");
				return;
			}
			if (e.Parameters.Count > 2)
			{
				e.Player.SendErrorMessage("Invalid syntax! Proper syntax: //undo [steps] [account]");
				return;
			}

			int steps = 1;
			int ID = e.Player.Account.ID;
			if (e.Parameters.Count > 0 && (!int.TryParse(e.Parameters[0], out steps) || steps <= 0))
				e.Player.SendErrorMessage("Invalid undo steps '{0}'!", e.Parameters[0]);
			else if (e.Parameters.Count > 1)
			{
				if (!e.Player.HasPermission("worldedit.usage.otheraccounts"))
				{
					e.Player.SendErrorMessage("You do not have permission to undo other player's actions.");
					return;
				}
				UserAccount User = TShock.UserAccounts.GetUserAccountByName(e.Parameters[1]);
				if (User == null)
				{
					e.Player.SendErrorMessage("Invalid account name!");
					return;
				}
				ID = User.ID;
			}
			_commandQueue.Add(new Undo(e.Player, ID, steps));
		}
	}
}