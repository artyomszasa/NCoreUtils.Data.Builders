namespace NCoreUtils.Data.Builders.Unit;

public class RefListTests
{
    [Fact]
    public void RemoveAll()
    {
        var list = new RefList<int>(24);
        for (var i = 0; i < 24; ++i)
        {
            list.Add(i);
        }
        Assert.Equal(12, list.RemoveAll((in int value) => value % 2 == 0));
        for (var i = 0; i < 12; ++i)
        {
            Assert.Equal(i * 2 + 1, list[i]);
        }
    }
}