namespace Test
{
    internal class TestWhereCount
    {
        public void TestMethod()
        {
            var numbers = new List<int> { 1, 2, 3 };
            var count = numbers
                .Where(x => x > 1)
                .Where(x => x < 3)
                .Count();
        }
    }
}
