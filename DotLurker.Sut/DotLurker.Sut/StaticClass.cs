using System.Diagnostics;

namespace DotLurker.Sut;

public static class StaticClass
{
    private static BaseAbstract _baseAbstract;
    private static Base _base1;
    private static Base _base2;
    private static IInterfaceSut _interfaceSut;

    static StaticClass()
    {
        _baseAbstract = new ChildA();
        _base1 = new ChildB();
        _base2 = new Base();
        _interfaceSut = new ChildB();
    }

    public static void TestStaticMethod()
    {
        _base1.TestBaseAbstractMethod();
        _base1.TestBaseVirtualMethod();
        _baseAbstract.TestBaseAbstractMethod();
        _base2.TestBaseVirtualMethod();
        _base2.TestBaseAbstractMethod();
        _interfaceSut.TestInterfaceMethod();
        _interfaceSut.Property += 1;
        var sw = Stopwatch.StartNew();
        var i = 100;
        i += 100;
        Console.WriteLine(i);
        sw.Stop();
        Console.WriteLine(sw.ElapsedTicks);
    }

    public static void StaticMethodTwo()
    {
        Console.WriteLine("Test");
    }
}