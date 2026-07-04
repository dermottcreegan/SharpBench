using System;
using Xunit;

public class GeometryHiddenTests
{
    [Fact]
    public void Circle_area()
    {
        Assert.Equal(Math.PI * 4, Geometry.Area(new Circle(2)), precision: 10);
    }

    [Fact]
    public void Rectangle_area()
    {
        Assert.Equal(12.0, Geometry.Area(new Rectangle(3, 4)));
    }

    [Fact]
    public void Triangle_area()
    {
        // Catches a non-exhaustive dispatch that forgets a shape kind.
        Assert.Equal(6.0, Geometry.Area(new Triangle(3, 4)));
    }
}
