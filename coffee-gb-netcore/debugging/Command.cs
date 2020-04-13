namespace eu.rekawek.coffeegb.debug
{

    public interface Command
    {
        CommandPattern getPattern();

        void run(CommandPattern.ParsedCommandLine commandLine);
    }
}