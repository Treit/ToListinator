namespace Test
{
    internal class TestWhereCount
    {
        public void TestMethod()
        {
            var numbers = new List<int> { 1, 2, 3 };
            var count = numbers
                .Select(x => x * 2)
                .OrderBy(x => x)
                .Where(x => x > 1)
                .Where(x => x < 3)
                .Where(x => x % 2 == 0)
                .Count();
        }
    }
}
