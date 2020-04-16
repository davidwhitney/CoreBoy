using System.Collections.Generic;
using CoreBoy.sound;

namespace CoreBoy.debugging.command.apu
{
    public class Channel : ICommand
    {
        private static readonly CommandPattern Pattern = CommandPattern.Builder
            .Create("apu chan")
            .WithDescription("enable given channels (1-4)")
            .Build();

        private readonly Sound _sound;

        public Channel(Sound sound)
        {
            _sound = sound;
        }

        public CommandPattern GetPattern()
        {
            return Pattern;
        }

        public void Run(CommandPattern.ParsedCommandLine commandLine)
        {
            var channels = new HashSet<string>(commandLine.GetRemainingArguments());
            for (var i = 1; i <= 4; i++)
            {
                _sound.EnableChannel(i - 1, channels.Contains(i.ToString()));
            }
        }
    }
}