using Terraria;

namespace WorldEdit.Expressions
{
	public delegate bool Test(ITile tile, int x = -1, int y = -1);

	public sealed class TestExpression : Expression
	{
		public Test Test;

		public TestExpression(Test test)
		{
			Test = test;
		}

		public override bool Evaluate(ITile tile, int x, int y)
		{
			return Test(tile, x, y);
		}
	}
}
