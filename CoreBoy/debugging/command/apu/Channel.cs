using System.Collections.Generic;
using CoreBoy.sound;

namespace CoreBoy.debugging.command.apu
{
    public class Channel : Command
    {

        private static readonly CommandPattern PATTERN = CommandPattern.Builder
            .Create("apu chan")
            .WithDescription("enable given channels (1-4)")
            .Build();

        private readonly Sound sound;

        public Channel(Sound sound)
        {
            this.sound = sound;
        }

        public CommandPattern getPattern()
        {
            return PATTERN;
        }

        public void run(CommandPattern.ParsedCommandLine commandLine)
        {
            var channels = new HashSet<string>(commandLine.GetRemainingArguments());
            for (var i = 1; i <= 4; i++)
            {
                sound.enableChannel(i - 1, channels.Contains(i.ToString()));
            }
        }
    }
}