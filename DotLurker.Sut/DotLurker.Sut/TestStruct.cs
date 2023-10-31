namespace DotLurker.Sut;

public struct TestStruct : IInterfaceSut
{
    public int i { get; set; }
    private ChildB ChildB { get; set; }

    public TestStruct(int i, ChildB childB)
    {
        this.i = i;
        ChildB = childB;
    }

    public void TestInterfaceMethod()
    {
        throw new NotImplementedException();
    }

    public int Property { get; set; }
}