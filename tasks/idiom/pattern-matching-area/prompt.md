Define exactly these types and method:

```csharp
public abstract record Shape;
public record Circle(double Radius) : Shape;
public record Rectangle(double Width, double Height) : Shape;
public record Triangle(double Base, double Height) : Shape;

public static class Geometry
{
    public static double Area(Shape shape);
}
```

Requirements:

- `Area` returns the area of the shape: circle `π·r²`, rectangle `w·h`, triangle `½·base·height`.
- Every shape kind must be handled. For an unrecognized `Shape`, throw `ArgumentException`.
