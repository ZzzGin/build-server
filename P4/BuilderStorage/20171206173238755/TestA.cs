using System;

namespace Tests
{
    public class Test : ITest
    {
        public Test()
        {
            System.Console.WriteLine("Creating instance of Test");
        }
        public string id()
        {
            Type t = this.GetType();
            return t.FullName;
        }

        public bool test()
        {
            bool i = TestA_success.testing();
            return i;
        }
    }
}