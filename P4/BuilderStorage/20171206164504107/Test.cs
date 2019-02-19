namespace Tests
{
	public class Test : ITest
	{
		public Test()
		{
			System.Console.WriteLine("Creating instance of Test");
		}
		public bool TestA_success()
		{
			bool i = TestA.testing();
			return i;
		}
	}
}