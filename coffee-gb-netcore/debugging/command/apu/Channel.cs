using System;
using System.Collections.Generic;
using eu.rekawek.coffeegb.sound;

namespace eu.rekawek.coffeegb.debug.command.apu
{
    public class Channel : Command
    {

        private static CommandPattern PATTERN = CommandPattern.Builder
            .create("apu chan")
            .withDescription("enable given channels (1-4)")
            .build();

        private Sound sound;

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
            var channels = new HashSet<string>(commandLine.getRemainingArguments());
            for (int i = 1; i <= 4; i++)
            {
                sound.enableChannel(i - 1, channels.Contains(i.ToString()));
            }
        }
    }
}