using System.Threading;

namespace CoreBoy.gui
{
    public interface IRunnable
    {
        void Run(CancellationToken token);
    }
}