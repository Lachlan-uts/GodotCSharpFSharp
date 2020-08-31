using Godot;
using GodotFs;

public class Pawn : PawnFs
{
    [Signal]
    public delegate void MySignal();

    [Export]
    private int blah = 200;

    public void testFun()
    {
        GD.Print("the C# version of the function");
        //GD.Print(this.GetMethodList());
        GodotFs.FsSignalFunctions.fsSignalFun("waa");
    }

    //public override void _Ready()
    //{
    //    //EmitSignal(nameof(MySignal));
    //}
}
