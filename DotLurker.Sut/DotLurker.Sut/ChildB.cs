namespace DotLurker.Sut;

public class ChildB : Base, IInterfaceSut
{
    public delegate void TestDelegate();
    
    public override void TestBaseVirtualMethod()
    {
        Console.WriteLine(nameof(TestBaseVirtualMethod) + " in ChildB");
    }

    public void TestInterfaceMethod()
    {
        Console.WriteLine(nameof(TestInterfaceMethod) + " in ChildB");
        (this as IInterfaceSut).TestInterfaceMethod();
    }

    public new void TestBaseVirtualMethod2()
    {
        TestDelegate x = TestBaseVirtualMethod;
        x();
    } 

    void IInterfaceSut.TestInterfaceMethod()
    {
        Console.WriteLine(nameof(TestInterfaceMethod) + " in ChildB");
    }

    private int _property;

    public int Property
    {
        get
        {
            Console.WriteLine("Test");
            return _property;
        }
        set { _property = value; }
    }
}