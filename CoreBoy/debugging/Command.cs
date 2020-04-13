namespace CoreBoy.debugging
{

    public interface Command
    {
        CommandPattern getPattern();

        void run(CommandPattern.ParsedCommandLine commandLine);
    }
}