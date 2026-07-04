using Xunit;

public class BoxHiddenTests
{
    [Fact]
    public void Box_exposes_its_value()
    {
        Assert.Equal(42, new Box<int>(42).Value);
    }

    [Fact]
    public void Reference_box_is_covariant_to_object()
    {
        // Requires `out T`. Without covariance this assignment does not compile and
        // the whole submission fails to build — which is the intended failure.
        IReadOnlyBox<string> text = new Box<string>("hi");
        IReadOnlyBox<object> boxed = text;
        Assert.Equal("hi", boxed.Value);
    }
}
