using System;

namespace CoreBoy.MonoGame
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using var game = new MonoGameEmulatorSurface();
            game.Run();
        }
    }
}
