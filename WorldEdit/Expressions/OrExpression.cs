using Terraria;

namespace WorldEdit.Expressions
{
	public class OrExpression : Expression
	{
		public OrExpression(Expression left, Expression right)
		{
			Left = left;
			Right = right;
		}

		public override bool Evaluate(ITile tile, int x, int y)
		{
			return Left!.Evaluate(tile, x, y) || Right!.Evaluate(tile, x, y);
		}
	}
}