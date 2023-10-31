using Newtonsoft.Json;

namespace DotLurker.Sut;

public class ChildA : Base
{
    public override void TestBaseAbstractMethod()
    {
        void LocalFunction()
        {
            var serializer = new JsonException();
            StaticClass.TestStaticMethod();
        }

        Console.WriteLine(nameof(TestBaseAbstractMethod) + " in ChildA");
        Action callback = () =>
        {
            Console.WriteLine("lambda");
            StaticClass.TestStaticMethod();
        };
        callback();
        Delegate callback2 = () =>
        {
            Console.WriteLine("lambda 2");
            StaticClass.StaticMethodTwo();
        };
    }
}