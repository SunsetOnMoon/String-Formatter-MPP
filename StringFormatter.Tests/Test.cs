namespace StringFormatter.Tests
{
    public class TestClass
    {
        public int AgeField = 20;
        public int Age { get; set; } = 20;
        public int[] AgeArray { get; set; } = { 20 };
    }
    public class Test
    {
        private readonly TestClass _test = new TestClass();
        [Theory]
        [InlineData("{Age}")]
        [InlineData("{AgeField}")]
        public void ValidMemberName(string memberName)
        {
            var result = StringFormatter.Core.StringFormatter.Shared.Format($"{memberName}", _test);
            result.Should().Be("20");
        }

        [Fact]
        public void CollectionAccess()
        {
            var result = () => StringFormatter.Core.StringFormatter.Shared.Format("{AgeArray[0]}", _test);
            result.Should().Throw<Exception>();
        }

        [Fact]
        public void InvalidCurlyBracketsCount()
        {
            var result = () => StringFormatter.Core.StringFormatter.Shared.Format("{Age", _test);
            result.Should().Throw<ArgumentException>();
        }

    }
}