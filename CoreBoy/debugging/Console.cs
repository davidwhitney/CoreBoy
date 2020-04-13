using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

namespace eu.rekawek.coffeegb.debug
{
	
    public class Console
    {

        // private static readonly Logger LOG = LoggerFactory.getLogger(Console.class);

        private readonly Deque<CommandExecution> commandBuffer = new ArrayDeque<>();

        private readonly Semaphore semaphore = new Semaphore(0);

        private volatile bool isStarted;

        private List<Command> commands;

        public Console()
        {
        }

        public void init(Gameboy gameboy)
        {
            commands = new List<Command>();
            commands.add(new ShowHelp(commands));
            commands.add(new ShowOpcode());
            commands.add(new ShowOpcodes());
            commands.add(new Quit());

            commands.add(new ShowBackground(gameboy, ShowBackground.Type.WINDOW));
            commands.add(new ShowBackground(gameboy, ShowBackground.Type.BACKGROUND));
            commands.add(new Channel(gameboy.getSound()));

            Collections.sort(commands, Comparator.comparing(c=>c.getPattern().getCommandNames().get(0)));
        }


        public void run()
        {
            isStarted = true;

            LineReader lineReader = LineReaderBuilder
                .builder()
                .build();

            while (true)
            {
                try
                {
                    String line = lineReader.readLine("coffee-gb> ");
                    foreach (Command cmd in commands) {
                        if (cmd.getPattern().matches(line))
                        {
                            ParsedCommandLine parsed = cmd.getPattern().parse(line);
                            commandBuffer.add(new CommandExecution(cmd, parsed));
                            semaphore.acquire();
                        }
                    }
                }
                catch (IllegalArgumentException e)
                {
                    System.err.println(e.getMessage());
                }
                catch (UserInterruptException e)
                {
                    System.exit(0);
                }
                catch (InterruptedException e)
                {
                    //LOG.error("Interrupted", e);
                    break;
                }
            }
        }

        public void tick()
        {
            if (!isStarted)
            {
                return;
            }

            while (!commandBuffer.isEmpty())
            {
                commandBuffer.poll().run();
                semaphore.release();
            }
        }

        private class CommandExecution
        {

            private readonly Command command;

            private readonly ParsedCommandLine arguments;

            public CommandExecution(Command command, ParsedCommandLine arguments)
            {
                this.command = command;
                this.arguments = arguments;
            }

            public void run()
            {
                command.run(arguments);
            }
        }
    }

}