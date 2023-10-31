namespace DotLurker.Sut;

public class Base : BaseAbstract
{
    public override void TestBaseAbstractMethod()
    {
        Console.WriteLine(nameof(TestBaseAbstractMethod));
    }
    
    public virtual void TestBaseVirtualMethod()
    {
        Console.WriteLine(nameof(TestBaseVirtualMethod));
    }
    
    public void TestBaseVirtualMethod2()
    {
        Console.WriteLine(nameof(TestBaseVirtualMethod));
    }
}