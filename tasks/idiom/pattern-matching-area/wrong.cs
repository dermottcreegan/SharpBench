using System;

public abstract record Shape;
public record Circle(double Radius) : Shape;
public record Rectangle(double Width, double Height) : Shape;
public record Triangle(double Base, double Height) : Shape;

// Classic bug: a non-exhaustive if/else chain that forgets Triangle and silently
// falls through to 0.0 instead of throwing.
public static class Geometry
{
    public static double Area(Shape shape)
    {
        if (shape is Circle c) return Math.PI * c.Radius * c.Radius;
        if (shape is Rectangle r) return r.Width * r.Height;
        return 0.0;
    }
}
