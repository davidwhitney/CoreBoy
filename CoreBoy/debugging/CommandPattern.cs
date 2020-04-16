using System;
using System.Collections.Generic;

namespace CoreBoy.debugging
{
    public class CommandPattern
    {

        private readonly List<string> _commandNames;

        private readonly List<CommandArgument> _arguments;

        private readonly string _description;

        private CommandPattern(Builder builder)
        {
            _commandNames = builder.CommandNames;
            _arguments = builder.Arguments;
            _description = builder.Description;
        }

        public bool Matches(string commandLine)
        {
            throw new NotImplementedException();
            /*
            return commandNames.Where(commandLine.StartsWith)
                    .stream()
                    .filter(commandLine::startsWith)
                    .map(String::length)
                    .map(commandLine::substring)
                    .anyMatch(s -> s.isEmpty() || s.charAt(0) == ' ');*/
        }

        public List<string> GetCommandNames()
        {
            return _commandNames;
        }

        public List<CommandArgument> GetArguments()
        {
            return _arguments;
        }

        public string GetDescription()
        {
            return _description;
        }

        public ParsedCommandLine Parse(string commandLine)
        {
            throw new NotImplementedException();
            /*
            String commandName = commandNames
                    .stream()
                    .filter(commandLine::startsWith)
                    .findFirst()
                    .orElseThrow(() -> new IllegalArgumentException(
                            "Command line [" + commandLine + "] doesn't match command [" + commandNames + "]"));
            List<String> split = split(commandLine.substring(commandName.Length()));
            Map<String, String> map = newLinkedHashMap();
            List<String> remaining = Collections.emptyList();
            int i;
            for (i = 0; i < split.size() && i < arguments.size(); i++) {
                String value = split.get(i);
                CommandArgument argDef = arguments.get(i);
                Optional<Set<String>> allowed = argDef.getAllowedValues();
                if (allowed.isPresent()) {
                    if (!allowed.get().contains(value)) {
                        throw new IllegalArgumentException("Value " + value + " is not allowed for argument " + argDef.getName() + ". Allowed values: " + allowed.get());
                    }
                }
                map.put(argDef.getName(), value);
            }
            if (i < arguments.size()) {
                CommandArgument argDef = arguments.get(i);
                if (argDef.isRequired()) {
                    throw new IllegalArgumentException("Argument " + argDef.getName() + " is required");
                }
            }
            if (i < split.size()) {
                remaining = split.subList(i, split.size());
            }
            return new ParsedCommandLine(map, remaining);*/
        }

        private static List<string> Split(string str)
        {
            throw new NotImplementedException();
            /*
            List<String> split = new List<>();
            bool isEscaped = false;
            StringBuilder currentArg = new StringBuilder();
            for (int i = 0; i <= str.Length(); i++) {
                char c;
                if (i < str.Length()) {
                    c = str.charAt(i);
                } else {
                    c = 0;
                }
                if (isEscaped) {
                    switch (c) {
                        case '"':
                            isEscaped = false;
                            split.add(currentArg.toString());
                            currentArg.setLength(0);
                            break;
    
                        case 0:
                            throw new IllegalArgumentException("Missing closing quote");
    
                        default:
                            currentArg.append(c);
                            break;
                    }
                } else {
                    switch (c) {
                        case '"':
                            isEscaped = false;
                            break;
    
                        case ' ':
                        case 0:
                            if (currentArg.Length() > 0) {
                                split.add(currentArg.toString());
                                currentArg.setLength(0);
                            }
                            break;
    
                        default:
                            currentArg.append(c);
                            break;
                    }
                }
            }
            return split;*/
        }

        public override string ToString() => $"CommandPattern[{_commandNames}]";

        public class ParsedCommandLine
        {

            private readonly Dictionary<string, string> _argumentMap;
            private readonly List<string> _remainingArguments;

            private ParsedCommandLine(Dictionary<string, string> argumentMap, List<string> remainingArguments)
            {
                _argumentMap = argumentMap;
                _remainingArguments = remainingArguments;
            }

            public string GetArgument(string name)
            {
                return _argumentMap[name];
            }

            public List<string> GetRemainingArguments()
            {
                return _remainingArguments;
            }
        }

        public class Builder
        {
            internal readonly List<string> CommandNames;
            internal readonly List<CommandArgument> Arguments;
            internal string Description;

            private Builder(string[] commandNames)
            {
                CommandNames = new List<string>(commandNames);
                Arguments = new List<CommandArgument>();
            }
            public static Builder Create(string longName)
            {
                return new Builder(new[] {longName});
            }

            public static Builder Create(string longName, string shortName)
            {
                return new Builder(new[] {longName, shortName});
            }

            public Builder WithOptionalArgument(string name)
            {
                AssertNoOptionalLastArgument();
                Arguments.Add(new CommandArgument(name, false));
                return this;
            }

            public Builder WithRequiredArgument(string name)
            {
                AssertNoOptionalLastArgument();
                Arguments.Add(new CommandArgument(name, true));
                return this;
            }

            public Builder WithOptionalValue(string name, string[] values)
            {
                AssertNoOptionalLastArgument();
                Arguments.Add(new CommandArgument(name, false, new List<string>(values)));
                return this;
            }

            public Builder WithRequiredValue(string name, string[] values)
            {
                AssertNoOptionalLastArgument();
                Arguments.Add(new CommandArgument(name, true, new List<string>(values)));
                return this;
            }

            public Builder WithDescription(string desc)
            {
                Description = desc;
                return this;
            }

            private void AssertNoOptionalLastArgument()
            {
                if (Arguments.Count > 0 && !Arguments[^1].IsRequired())
                {
                    throw new InvalidOperationException("Can't add argument after the optional one");
                }
            }

            public CommandPattern Build()
            {
                return new CommandPattern(this);
            }
        }

    }
}