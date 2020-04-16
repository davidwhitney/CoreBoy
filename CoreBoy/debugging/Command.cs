namespace CoreBoy.debugging
{
    public interface ICommand
    {
        CommandPattern GetPattern();
        void Run(CommandPattern.ParsedCommandLine commandLine);
    }
}