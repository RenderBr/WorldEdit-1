using Terraria;

namespace WorldEdit.Expressions
{
	public class AndExpression : Expression
	{
		public AndExpression(Expression left, Expression right)
		{
			Left = left;
			Right = right;
		}

		public override bool Evaluate(ITile tile, int x, int y)
		{
			return Left!.Evaluate(tile, x, y) && Right!.Evaluate(tile, x, y);
		}
	}
}
