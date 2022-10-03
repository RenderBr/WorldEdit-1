using TShockAPI;

namespace WorldEdit.Commands
{
	public class Undo : WECommand
	{
		private readonly int _accountId;
		private readonly int _steps;

		public Undo(TSPlayer plr, int accountId, int steps)
			: base(0, 0, 0, 0, plr)
		{
			_accountId = accountId;
			_steps = steps;
		}

		public override void Execute()
		{
            if (!plr.RealPlayer || (_accountId == 0))
            {
                plr.SendErrorMessage("Undo system is disabled for unreal players.");
                return;
            }

			int i = -1;
			while (++i < _steps && Clipboard.Undo(_accountId)) ;
			if (i == 0)
				plr.SendErrorMessage("Failed to undo any actions.");
			else
				plr.SendSuccessMessage("Undid {0}'s last {1}action{2}.", ((_accountId == 0) ? "ServerConsole" : TShock.UserAccounts.GetUserAccountByID(_accountId).Name), i == 1 ? "" : i + " ", i == 1 ? "" : "s");
		}
	}
}